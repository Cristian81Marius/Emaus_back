namespace Emaus.Api.Common;

public enum ServiceErrorType { NotFound, Conflict, Validation, Unauthorized }

public class ServiceError
{
    public required ServiceErrorType Type { get; init; }
    public required string Message { get; init; }
}

/// <summary>Rezultatul unei operații de serviciu, cu sau fără valoare. Controllerul nu mai
/// aruncă/prinde excepții pentru cazuri de business așteptate (nu găsit, conflict, invalid) —
/// serviciul întoarce un rezultat explicit, iar <see cref="ServiceResultExtensions"/> îl
/// transformă în răspunsul HTTP potrivit. O excepție rămâne pentru ce e cu adevărat neașteptat
/// (vezi Middleware/ExceptionHandlingMiddleware.cs).</summary>
public class ServiceResult
{
    public ServiceError? Error { get; protected init; }
    public bool IsSuccess => Error is null;

    public static ServiceResult Ok() => new();
    public static ServiceResult NotFound(string message) => new() { Error = new ServiceError { Type = ServiceErrorType.NotFound, Message = message } };
    public static ServiceResult Conflict(string message) => new() { Error = new ServiceError { Type = ServiceErrorType.Conflict, Message = message } };
    public static ServiceResult Invalid(string message) => new() { Error = new ServiceError { Type = ServiceErrorType.Validation, Message = message } };
    public static ServiceResult Unauthorized(string message) => new() { Error = new ServiceError { Type = ServiceErrorType.Unauthorized, Message = message } };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    public static ServiceResult<T> Ok(T value) => new() { Value = value };
    public new static ServiceResult<T> NotFound(string message) => new() { Error = new ServiceError { Type = ServiceErrorType.NotFound, Message = message } };
    public new static ServiceResult<T> Conflict(string message) => new() { Error = new ServiceError { Type = ServiceErrorType.Conflict, Message = message } };
    public new static ServiceResult<T> Invalid(string message) => new() { Error = new ServiceError { Type = ServiceErrorType.Validation, Message = message } };
    public new static ServiceResult<T> Unauthorized(string message) => new() { Error = new ServiceError { Type = ServiceErrorType.Unauthorized, Message = message } };
}
