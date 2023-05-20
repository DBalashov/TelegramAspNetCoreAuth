namespace TelegramAspNetCoreAuth;

public sealed record AuthInfo(Int64 id, string? first_name, string? last_name, string? username, string? photo_url, Int64 auth_date, string hash);