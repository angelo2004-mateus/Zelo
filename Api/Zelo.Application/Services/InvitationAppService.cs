using System.ComponentModel.DataAnnotations;
using Zelo.Application.Contract;
using Zelo.Application.Model.Invitations;
using Zelo.Application.Shared.Auths;
using Zelo.Application.Shared.Authorization;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Data;
using Zelo.Application.Shared.Roles;
using Zelo.Domain.Invitations;
using Zelo.Domain.Roles;
using Zelo.Domain.UserTenants;
using Zelo.Domain.Users;

namespace Zelo.Application.Services;

public class InvitationAppService : IInvitationAppService
{
    private readonly IInvitationRepository _invitations;
    private readonly IUserTenantRepository _memberships;
    private readonly IRoleRepository _roles;
    private readonly IAuthLifecycleStore _users;
    private readonly ITenantAuthorizationService _authorization;
    private readonly IAuthEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    public InvitationAppService(IInvitationRepository invitations, IUserTenantRepository memberships, IRoleRepository roles, IAuthLifecycleStore users, ITenantAuthorizationService authorization, IAuthEmailSender emailSender, IUnitOfWork unitOfWork, ICurrentUser currentUser, ICurrentTenant currentTenant)
    {
        _invitations = invitations;
        _memberships = memberships;
        _roles = roles;
        _users = users;
        _authorization = authorization;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    public async Task<AppServiceResult<InvitationDto>> CreateOrResendAsync(InvitationCreateRequestDto request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid actorId || _currentTenant.Id is not Guid tenantId)
        {
            return AppServiceResult<InvitationDto>.Failure(AppServiceError.Forbidden);
        }

        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) ||
            !new EmailAddressAttribute().IsValid(email) ||
            request.RoleId == Guid.Empty)
        {
            return AppServiceResult<InvitationDto>.Failure(AppServiceError.Invalid);
        }

        var rawToken = AuthSecret.Create();
        AppServiceResult<InvitationDto>? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _memberships.LockTenantAsync(tenantId, ct);
            var actor = await _authorization.GetAccessAsync(actorId, tenantId, ct);
            var role = await _roles.FindAsync(tenantId, request.RoleId, ct);
            if (actor is null || role is null ||
                !await _authorization.CanGrantRoleAsync(actor, role, ct))
            {
                result = AppServiceResult<InvitationDto>.Failure(
                    AppServiceError.Forbidden);
                return;
            }

            var existingUser = await _users.FindUserByEmailAsync(email, ct);
            if (existingUser is not null)
            {
                var membership = await _memberships.FindAsync(
                    existingUser.Id, tenantId, ct);
                if (membership?.Status == UserTenantStatus.Active ||
                    membership?.Status == UserTenantStatus.Suspended)
                {
                    result = AppServiceResult<InvitationDto>.Failure(
                        AppServiceError.Conflict);
                    return;
                }
            }

            var now = DateTime.UtcNow;
            var invitation = await _invitations.FindPendingAsync(tenantId, email, ct);
            if (invitation is not null && invitation.ExpirationTime <= now)
            {
                await _invitations.TryExpireAsync(invitation.Id, now, ct);
                invitation = null;
            }

            if (invitation is null)
            {
                invitation = new Invitation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Email = email,
                };
                invitation.RoleId = role.Id;
                invitation.InvitedByUserId = actorId;
                invitation.Status = InvitationStatus.Pending;
                invitation.TokenHash = AuthSecret.Hash(rawToken);
                invitation.ExpirationTime = now.AddDays(7);
                await _invitations.CreateAsync(invitation, ct);
            }
            else
            {
                invitation.RoleId = role.Id;
                invitation.InvitedByUserId = actorId;
                invitation.TokenHash = AuthSecret.Hash(rawToken);
                invitation.ExpirationTime = now.AddDays(7);
                await _invitations.UpdateAsync(invitation, ct);
            }

            result = AppServiceResult<InvitationDto>.Success(ToDto(invitation));
        }, cancellationToken);

        if (result!.Succeeded)
        {
            try
            {
                await _emailSender.SendAsync(
                    email, "You are invited to a Zelo account",
                    $"Invitation token: {rawToken}", cancellationToken);
                result.Value!.EmailSent = true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                result.Value!.EmailSent = false;
            }
        }

        return result;
    }

    public async Task<AppServiceResult<InvitationAcceptDto>> AcceptAsync(string token, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid userId)
        {
            return AppServiceResult<InvitationAcceptDto>.Failure(AppServiceError.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return AppServiceResult<InvitationAcceptDto>.Failure(
                AppServiceError.Invalid);
        }

        var hash = AuthSecret.Hash(token);
        var candidate = await _invitations.FindByTokenHashAsync(hash, cancellationToken);
        if (candidate is null)
        {
            return AppServiceResult<InvitationAcceptDto>.Failure(
                AppServiceError.NotFound);
        }

        AppServiceResult<InvitationAcceptDto>? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _memberships.LockTenantAsync(candidate.TenantId, ct);
            var invitation = await _invitations.FindByTokenHashAsync(hash, ct);
            var user = await _users.FindUserByIdAsync(userId, ct);
            if (invitation is null || invitation.Status != InvitationStatus.Pending ||
                invitation.ExpirationTime <= DateTime.UtcNow)
            {
                result = AppServiceResult<InvitationAcceptDto>.Failure(
                    AppServiceError.Conflict);
                return;
            }

            if (user is null || user.Status != UserStatus.Active ||
                user.EmailConfirmationTime is null ||
                !string.Equals(user.Email, invitation.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                result = AppServiceResult<InvitationAcceptDto>.Failure(
                    AppServiceError.Forbidden);
                return;
            }

            var inviter = await _authorization.GetAccessAsync(
                invitation.InvitedByUserId, invitation.TenantId, ct);
            var role = await _roles.FindAsync(
                invitation.TenantId, invitation.RoleId, ct);
            if (inviter is null || role is null ||
                !await _authorization.CanGrantRoleAsync(inviter, role, ct))
            {
                result = AppServiceResult<InvitationAcceptDto>.Failure(
                    AppServiceError.Forbidden);
                return;
            }

            var membership = await _memberships.FindAsync(
                userId, invitation.TenantId, ct);
            if (membership is not null && membership.Status != UserTenantStatus.Revoked)
            {
                result = AppServiceResult<InvitationAcceptDto>.Failure(
                    AppServiceError.Conflict);
                return;
            }

            var now = DateTime.UtcNow;
            if (!await _invitations.TryAcceptAsync(invitation.Id, now, ct))
            {
                result = AppServiceResult<InvitationAcceptDto>.Failure(
                    AppServiceError.Conflict);
                return;
            }

            if (membership is null)
            {
                await _memberships.CreateAsync(new UserTenant
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TenantId = invitation.TenantId,
                    RoleId = role.Id,
                    Status = UserTenantStatus.Active,
                    JoinTime = now,
                }, ct);
            }
            else
            {
                membership.RoleId = role.Id;
                membership.Status = UserTenantStatus.Active;
                membership.JoinTime = now;
                await _memberships.UpdateAsync(membership, ct);
            }

            result = AppServiceResult<InvitationAcceptDto>.Success(
                new InvitationAcceptDto
                {
                    TenantId = invitation.TenantId,
                    RoleId = role.Id
                });
        }, cancellationToken);
        return result!;
    }

    public async Task<AppServiceResult<bool>> RevokeAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid actorId || _currentTenant.Id is not Guid tenantId)
        {
            return AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
        }

        AppServiceResult<bool>? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _memberships.LockTenantAsync(tenantId, ct);
            var actor = await _authorization.GetAccessAsync(actorId, tenantId, ct);
            if (actor is null ||
                !_authorization.HasPermission(actor, PermissionCodes.MemberManage))
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
                return;
            }

            var revoked = await _invitations.TryRevokeAsync(
                tenantId, invitationId, DateTime.UtcNow, ct);
            if (revoked)
            {
                result = AppServiceResult<bool>.Success(true);
            }
            else
            {
                result = AppServiceResult<bool>.Failure(
                    AppServiceError.NotFound);
            }
        }, cancellationToken);
        return result!;
    }

    private static InvitationDto ToDto(Invitation invitation)
    {
        return new InvitationDto
        {
            Id = invitation.Id,
            TenantId = invitation.TenantId,
            Email = invitation.Email,
            RoleId = invitation.RoleId,
            Status = invitation.Status,
            ExpirationTime = invitation.ExpirationTime
        };
    }
}
