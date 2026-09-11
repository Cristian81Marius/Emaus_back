using Emaus.Api.Dtos.Logs;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Logs;

/// <summary>Partea de citire a jurnalului de cereri scris de
/// <see cref="Emaus.Api.Middleware.RequestLoggingMiddleware"/> — "ce a apelat clientul,
/// când și cu ce rezultat". Rezervat rolului Nucleus (vezi LogsController); un voluntar
/// nu are nevoie să vadă traficul API.</summary>
public class RequestLogService(IRepository<RequestLog> logs)
{
    public async Task<List<RequestLogDto>> GetRecentAsync(
        string? method, string? pathContains, int? statusCode, int take)
    {
        var query = logs.Query().AsQueryable();

        if (!string.IsNullOrWhiteSpace(method)) query = query.Where(l => l.Method == method.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(pathContains)) query = query.Where(l => l.Path.Contains(pathContains));
        if (statusCode is not null) query = query.Where(l => l.StatusCode == statusCode);

        var clampedTake = Math.Clamp(take, 1, 500);

        var list = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(clampedTake)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    private static RequestLogDto ToDto(RequestLog l) => new(
        l.Id, l.Method, l.Path, l.StatusCode, l.DurationMs, l.UserId, l.UserName, l.IpAddress, l.ErrorMessage, l.CreatedAt);
}
