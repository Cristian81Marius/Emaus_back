namespace Emaus.Api.Dtos.Logs;

public record RequestLogDto(
    Guid Id, string Method, string Path, int StatusCode, long DurationMs,
    Guid? UserId, string? UserName, string? IpAddress, string? ErrorMessage, DateTime CreatedAt);
