namespace TelegramAspNetCoreAuth;

public interface ITelegramAuthenticator
{
    Task<string?> Authenticate(AuthInfo info, HttpContext ctx);
}