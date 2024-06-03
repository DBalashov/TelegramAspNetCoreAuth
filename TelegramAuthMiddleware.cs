using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
        if (ctx.Request.Method != "GET")
        {
            ctx.Response.StatusCode = 400;
            return;
        }
        
        if (tryParseNormal(ctx.Request.Query, logger, out var normalAuthInfo))
        {
            if (!checkHash(config, ctx.Request.Query))
            {
                logger.LogWarning("[{0}] {1}: HASH MISMATCH", normalAuthInfo.id, normalAuthInfo.username);
                ctx.Response.ContentType = "text/html";
                ctx.Response.StatusCode  = 200;
                await ctx.Response.WriteAsync("<html><body><script>alert('Error! Invalid hash!'); window.close();</script></body></html>");
                return;
            }
            
            var redirectUrl = await authenticator.Authenticate(normalAuthInfo, ctx);
            if (redirectUrl != null)
            {
                ctx.Response.Redirect(redirectUrl);
                return;
            }
        }
        
        if (tryParseBot(ctx.Request.Query, logger, out var botAuthInfo))
        {
            if (!checkHash(config, ctx.Request.Query, "WebAppData"))
            {
                logger.LogWarning("[{0}] {1}: HASH MISMATCH", botAuthInfo.id, botAuthInfo.username);
                ctx.Response.ContentType = "text/json";
                ctx.Response.StatusCode  = 400;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResult(ctx.TraceIdentifier, "Invalid hash")));
                return;
            }
            
            var redirectUrl = await authenticator.Authenticate(botAuthInfo, ctx);
            if (redirectUrl != null)
            {
                ctx.Response.StatusCode  = 200;
                await ctx.Response.WriteAsJsonAsync(new {redirectUrl});
                return;
            }
            
            ctx.Response.StatusCode  = 401;
            ctx.Response.ContentType = "text/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResult(ctx.TraceIdentifier, "Invalid hash")));
            return;
        }
        
        ctx.Response.StatusCode = 400;
        await next(ctx);
    }
    
    #region parseQueryString
    
    bool tryParseNormal(IQueryCollection q, ILogger<TelegramAuthMiddleware> logger, out AuthInfo authInfo)
    {
        authInfo = null!;
        if (!q.TryGetValue("id", out var _id) || !Int64.TryParse(_id.ToString(), out var id))
        {
            logger.LogDebug(MissingOrInvalid, "id");
            return false;
        }
        
        if (!q.TryGetValue("auth_date", out var _authDate) || !Int64.TryParse(_authDate.ToString(), out var authDate))
        {
            logger.LogDebug(MissingOrInvalid, "auth_date");
            return false;
        }
        
        if (!q.TryGetValue("hash", out var _hash))
        {
            logger.LogDebug(MissingOrInvalid, "hash");
            return false;
        }
        
        authInfo = new AuthInfo(id,
                                !q.TryGetValue("first_name", out var _firstName) ? null : _firstName.ToString(),
                                !q.TryGetValue("last_name",  out var _lastName) ? null : _lastName.ToString(),
                                !q.TryGetValue("username",   out var _userName) ? null : _userName.ToString(),
                                !q.TryGetValue("photo_url",  out var _photoUrl) ? null : _photoUrl.ToString(),
                                authDate,
                                _hash.ToString());
        return true;
    }
    
    bool tryParseBot(IQueryCollection q, ILogger<TelegramAuthMiddleware> logger, out AuthInfo authInfo)
    {
        authInfo = null!;
        if (!q.TryGetValue("query_id", out var _query_id))
        {
            logger.LogDebug(MissingOrInvalid, "id");
            return false;
        }
        
        if (!q.TryGetValue("auth_date", out var _authDate) || !Int64.TryParse(_authDate.ToString(), out var authDate))
        {
            logger.LogDebug(MissingOrInvalid, "auth_date");
            return false;
        }
        
        if (!q.TryGetValue("hash", out var _hash))
        {
            logger.LogDebug(MissingOrInvalid, "hash");
            return false;
        }
        
        if (!q.TryGetValue("user", out var _user))
        {
            logger.LogDebug(MissingOrInvalid, "user");
            return false;
        }
        
        authInfo = JsonSerializer.Deserialize<AuthInfo>(_user.ToString())! with {auth_date = authDate, hash = _hash.ToString()};
        return true;
    }
    
    #endregion
    
    #region checkHash
    
    bool checkHash(TelegramAuthConfig config, IQueryCollection query, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        
        var passedHash      = query["hash"];
        var dataCheckString = query.Where(p => p.Key != "hash").Select(p => p.Key + "=" + p.Value).OrderBy(p => p).Aggregate((a, b) => a + "\n" + b);
        var secretKey = key == null
                            ? SHA256.HashData(Encoding.UTF8.GetBytes(config.BotToken))
                            : HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(config.BotToken));
        
        var calculatedHash = string.Join("", HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)).Select(p => p.ToString("x2")));
        return string.Compare(passedHash, calculatedHash, StringComparison.OrdinalIgnoreCase) == 0;
    }
    
    #endregion
}