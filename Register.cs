namespace TelegramAspNetCoreAuth;

public static class Register
{
    public static IServiceCollection AddTelegramAuth(this IServiceCollection s, Func<IServiceProvider, TelegramAuthConfig> config)
    {
        s.AddSingleton<TelegramAuthConfig>(config);
        return s;
    }

    public static IApplicationBuilder UseTelegramAuth(this IApplicationBuilder app)
    {
        var config = app.ApplicationServices.GetRequiredService<TelegramAuthConfig>();
        app.Map(config.AuthEndpoint, a => a.UseMiddleware<TelegramAuthMiddleware>());
        return app;
    }
}