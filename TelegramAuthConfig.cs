namespace TelegramAspNetCoreAuth;

public sealed record TelegramAuthConfig(string BotUserName, string BotToken, string AuthEndpoint = "/api/auth");