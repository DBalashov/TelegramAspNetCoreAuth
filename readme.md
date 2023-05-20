## Telegram authenticate in ASP.NET core applications (.net 6+)

### Preparations

1. Register own ITelegramAuthenticator implementation in DI container as scoped:

```csharp
s.AddScoped<ITelegramAuthenticator, TelegramAuthenticator>();
```
Implementation of ITelegramAuthenticator interface must be set cookie, generate JWT token or something else for  authenticate future requests and MUST return:
* url for redirect to application page for authenticated user
* null, if authentication invalid


2. Add configuration and use middleware

```csharp
public void ConfigureServices(IServiceCollection s)
{
    ...
    // optionally can be set url for auth callback - for example, /authenticationUrl
    s.AddTelegramAuth(_ => new TelegramAuthConfig("botIdHere", "botTokenHere", "/authenticationUrl"));
    ...
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...
    // after app.UseEndpoints(...)
    app.UseTelegramAuth();
    ...
}
```

3. Send message (example) to user with link for authentication

```csharp
var msgId = await bot.SendTextMessageAsync(update.Message.Chat,
                                           "Press button to authenticate and open site",
                                           cancellationToken: cancellationToken,
                                           replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton("Login")
                                                                                 {
                                                                                     LoginUrl = new LoginUrl()
                                                                                                {
                                                                                                    Url                = "https://domain/authenticationUrl",
                                                                                                    BotUsername        = "botIdHere",
                                                                                                    RequestWriteAccess = true
                                                                                                }
                                                                                 }));

// optionally pin message with 'Login' button in chat
await bot.PinChatMessageAsync(update.Message.Chat, msgId.MessageId, cancellationToken: cancellationToken);
```

4. That's all. After user click on button, he will be redirected to your application page with authenticated user.