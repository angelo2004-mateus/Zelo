namespace Zelo.Application.Shared.Authorization;

public class AppServiceResult<T>
{
    public T? Value { get; set; }
    public AppServiceError Error { get; set; }

    public bool Succeeded
    {
        get
        {
            return Error == AppServiceError.None;
        }
    }

    public static AppServiceResult<T> Success(T value)
    {
        return new AppServiceResult<T> { Value = value };
    }

    public static AppServiceResult<T> Failure(AppServiceError error)
    {
        return new AppServiceResult<T> { Error = error };
    }
}
