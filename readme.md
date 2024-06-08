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
    // if not set, default value is /api/auth
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

### Usage (Telegram Bot)

Send message (example) to user with link for authentication

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

After user click on button, will called TelegramAuthenticator.Authenticate and he will be redirected to your application page with authenticated user.

### Usage (Telegram Mini App)

```javascript
import {useWebApp} from "vue-tg";

const data = useWebApp(); // use injected Telegram data

onMounted(async () => {
    const authenticationUrl = '/authenticationUrl?' + data.initData;
    const authResult = await fetch(authenticationUrl); // send GET request to /authenticationUrl with initData
    if (authResult.ok) {
        const jsonResponse = await authResult.json() as { redirectUrl?: string };
        alert(jsonResponse.redirectUrl); // show redirectUrl (example)
    }
})
```
