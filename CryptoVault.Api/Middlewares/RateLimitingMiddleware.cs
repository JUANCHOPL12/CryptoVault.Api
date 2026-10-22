using System.Collections.Concurrent;
using System.Net;

namespace CryptoVault.Api.Middlewares;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    // Diccionario en memoria para almacenar marcas de tiempo de peticiones por IP
    private static readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();

    private const int MAX_REQUESTS = 5; // Máximo 5 peticiones
    private static readonly TimeSpan TIME_WINDOW = TimeSpan.FromMinutes(1); // En 1 minuto

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Aplicar la limitación principalmente en los endpoints de Login
        if (context.Request.Path.Value?.ToLower().Contains("/api/auth/login") == true)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var now = DateTime.UtcNow;

            var timestamps = _requestLog.GetOrAdd(ipAddress, _ => new List<DateTime>());

            lock (timestamps)
            {
                // Limpiar peticiones fuera de la ventana de 1 minuto
                timestamps.RemoveAll(time => now - time > TIME_WINDOW);

                if (timestamps.Count >= MAX_REQUESTS)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests; // HTTP 429
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        message = "Demasiados intentos de inicio de sesión. Por favor espere 1 minuto antes de reintentar."
                    };

                    context.Response.WriteAsJsonAsync(response);
                    return;
                }

                timestamps.Add(now);
            }
        }

        await _next(context);
    }
}