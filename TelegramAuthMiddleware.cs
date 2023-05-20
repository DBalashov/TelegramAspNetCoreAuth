using System.Security.Cryptography;
using System.Text;

namespace TelegramAspNetCoreAuth;

public class TelegramAuthMiddleware
{
    readonly RequestDelegate next;

    const string MissingOrInvalid = "'{0}' is missing or invalid";

    public TelegramAuthMiddleware(RequestDelegate next) => this.next = next;

    public async Task Invoke(HttpContext                     ctx,
                             ILogger<TelegramAuthMiddleware> logger,
                             TelegramAuthConfig              config,
                             ITelegramAuthenticator          authenticator)
    {
        var info = parseQueryString(ctx.Request.Query, logger);
        if (info == null || ctx.Request.Method != "GET")
        {
            ctx.Response.StatusCode = 400;
            return;
        }

        if (!checkHash(config, ctx.Request.Query))
        {
            logger.LogWarning("[{0}] {1}: HASH MISMATCH", info.id, info.username);
            ctx.Response.ContentType = "text/html";
            ctx.Response.StatusCode  = 200;
            await ctx.Response.WriteAsync("<html><body><script>alert('Error! Invalid hash!'); window.close();</script></body></html>");
        }

        var redirectUrl = await authenticator.Authenticate(info, ctx);
        if (redirectUrl != null)
        {
            ctx.Response.Redirect(redirectUrl);
            return;
        }

        ctx.Response.StatusCode = 400;
        await next(ctx);
    }

    #region parseQueryString

    AuthInfo? parseQueryString(IQueryCollection q, ILogger<TelegramAuthMiddleware> logger)
    {
        if (!q.TryGetValue("id", out var _id) || !Int64.TryParse(_id.ToString(), out var id))
        {
            logger.LogWarning(MissingOrInvalid, "id");
            return null;
        }

        if (!q.TryGetValue("auth_date", out var _authDate) || !Int64.TryParse(_authDate.ToString(), out var authDate))
        {
            logger.LogWarning(MissingOrInvalid, "auth_date");
            return null;
        }

        if (!q.TryGetValue("hash", out var hash))
        {
            logger.LogWarning(MissingOrInvalid, "hash");
            return null;
        }

        return new AuthInfo(id,
                            !q.TryGetValue("first_name", out var _firstName) ? null : _firstName.ToString(),
                            !q.TryGetValue("last_name",  out var _lastName) ? null : _lastName.ToString(),
                            !q.TryGetValue("username",   out var _userName) ? null : _userName.ToString(),
                            !q.TryGetValue("photo_url",  out var _photoUrl) ? null : _photoUrl.ToString(),
                            authDate,
                            hash.ToString());
    }

    #endregion

    #region checkHash

    bool checkHash(TelegramAuthConfig config, IQueryCollection query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var passedHash      = query["hash"];
        var dataCheckString = query.Where(p => p.Key != "hash").Select(p => p.Key + "=" + p.Value).OrderBy(p => p).Aggregate((a, b) => a + "\n" + b);
        var secretKey       = SHA256.HashData(Encoding.UTF8.GetBytes(config.BotToken));

        var calculatedHash = string.Join("", HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)).Select(p => p.ToString("x2")));
        return string.Compare(passedHash, calculatedHash, StringComparison.OrdinalIgnoreCase) == 0;
    }

    #endregion
}