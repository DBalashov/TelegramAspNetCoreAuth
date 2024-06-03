using System.Text.Json.Serialization;

namespace TelegramAspNetCoreAuth;

public sealed record AuthInfo(Int64 id, string? first_name, string? last_name, string? username, string? photo_url, Int64 auth_date, string hash);

sealed record ErrorResult([property: JsonPropertyName("traceId")]
                          string TraceId,
                          [property: JsonPropertyName("title")]  string Title,
                          [property: JsonPropertyName("status")] int    Status = 400,
                          [property: JsonPropertyName("type")]   string Type   = "https://tools.ietf.org/html/rfc7231#section-6.5.1");