using Amazon.Runtime;
using AWSTelegramBot;
using AWSTelegramBot.Services;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson();

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

builder.Services.Configure<BotConfiguration>(builder.Configuration);
builder.Services.AddTransient<IUpdateService, UpdateService>();

builder.Services
    .AddHttpClient("purchase-bot")
    .AddTypedClient<ITelegramBotClient>((client, sp) =>
    {
        var configuration = sp.GetRequiredService<IOptionsMonitor<BotConfiguration>>();
        return new TelegramBotClient(configuration.CurrentValue.BotToken, client);
    })
    .AddTypedClient<BasicAWSCredentials>((client, sp) =>
    {
        var configuration = sp.GetRequiredService<IOptionsMonitor<BotConfiguration>>();
        return new BasicAWSCredentials(configuration.CurrentValue.AccessKey, configuration.CurrentValue.SecretKey);
    });

var app = builder.Build();

app.MapControllers();

app.Run();
