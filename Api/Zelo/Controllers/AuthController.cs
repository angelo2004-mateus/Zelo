using System.IdentityModel.Tokens.Jwt;
using Zelo.Application.Contract;
using Zelo.Application.Model.Auths;
using Zelo.Application.Shared.Auths;
using Zelo.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Zelo.Web.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserAppService _userAppService;
    private readonly IAuthAppService _authService;
    private readonly IAuthFlowAppService _flow;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserAppService userAppService, IAuthAppService authService, IAuthFlowAppService flow, ILogger<AuthController> logger)
    {
        _userAppService = userAppService;
        _authService = authService;
        _flow = flow;
        _logger = logger;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userAppService.RegisterAsync(request, cancellationToken);
        if (result.Succeeded)
        {
            if (!result.ConfirmationEmailSent)
            {
                _logger.LogWarning(
                    "Confirmation email delivery failed for user {UserId}.",
                    result.User?.Id);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        return Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Email already in use"
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register-user")]
    public async Task<IActionResult> RegisterUser([FromBody] AuthRegisterUserRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userAppService.RegisterUserAsync(request, cancellationToken);
        if (result.Succeeded)
        {
            if (!result.ConfirmationEmailSent)
            {
                _logger.LogWarning(
                    "Confirmation email delivery failed for user {UserId}.",
                    result.User?.Id);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        return Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Email already in use"
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (result.Succeeded)
        {
            return Ok(result);
        }

        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Invalid email or password"
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(AuthTokenRequestDto request, CancellationToken cancellationToken)
    {
        if (await _flow.ConfirmEmailAsync(request.Token, cancellationToken))
        {
            return NoContent();
        }

        return BadRequest(new ProblemDetails { Title = "Invalid or expired token" });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(AuthEmailRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _flow.ResendConfirmationAsync(request.Email, cancellationToken);
        }
        catch (AuthEmailDeliveryException exception)
        {
            _logger.LogWarning(exception, "Confirmation email delivery failed.");
        }

        return Accepted();
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(AuthEmailRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _flow.RequestPasswordResetAsync(request.Email, cancellationToken);
        }
        catch (AuthEmailDeliveryException exception)
        {
            _logger.LogWarning(exception, "Password reset email delivery failed.");
        }

        return Accepted();
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(AuthResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (await _flow.ResetPasswordAsync(
            request.Token, request.Password, cancellationToken))
        {
            return NoContent();
        }

        return BadRequest(new ProblemDetails { Title = "Invalid or expired token" });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(AuthRefreshRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (result.Succeeded)
        {
            return Ok(result);
        }

        return Unauthorized();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!TryCurrentSession(out var userId, out var sessionId))
        {
            return Unauthorized();
        }

        await _authService.LogoutAsync(userId, sessionId, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("totp/verify")]
    public async Task<IActionResult> VerifyTotp(AuthTotpVerifyRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyTwoFactorAsync(
            request.ChallengeToken, request.Code, cancellationToken);
        if (result.Succeeded)
        {
            return Ok(result);
        }

        return Unauthorized();
    }

    [Authorize]
    [EnableRateLimiting("auth")]
    [HttpPost("totp/setup")]
    public async Task<IActionResult> SetupTotp(AuthTotpPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryCurrentSession(out var userId, out _))
        {
            return Unauthorized();
        }

        var result = await _flow.SetupTotpAsync(
            userId, request.Password, cancellationToken);
        if (result is null)
        {
            return BadRequest();
        }

        return Ok(result);
    }

    [Authorize]
    [EnableRateLimiting("auth")]
    [HttpPost("totp/enable")]
    public async Task<IActionResult> EnableTotp(AuthTotpCodeRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryCurrentSession(out var userId, out _))
        {
            return Unauthorized();
        }

        var result = await _flow.EnableTotpAsync(
            userId, request.Code, cancellationToken);
        if (result is null)
        {
            return BadRequest();
        }

        return Ok(result);
    }

    [Authorize]
    [EnableRateLimiting("auth")]
    [HttpPost("totp/disable")]
    public async Task<IActionResult> DisableTotp(AuthTotpDisableRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryCurrentSession(out var userId, out _))
        {
            return Unauthorized();
        }

        if (await _flow.DisableTotpAsync(
            userId, request.Password, request.Code, cancellationToken))
        {
            return NoContent();
        }

        return BadRequest();
    }

    private bool TryCurrentSession(out Guid userId, out Guid sessionId)
    {
        var validUser = Guid.TryParse(
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);
        var validSession = Guid.TryParse(
            User.FindFirst("sid")?.Value, out sessionId);
        return validUser && validSession;
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        return Ok(new { UserId = userId });
    }
}
