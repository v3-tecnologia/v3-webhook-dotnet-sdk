using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Security;
using WebhookExample;
using WebhookExample.Data;
using WebhookExample.Persistence;
using WebhookExample.Factories;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<WebhookDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=webhookdb;Username=postgres;Password=postgres"
    )
);

builder.Services.AddScoped<PostgresEventWriter>();
builder.Services.AddScoped<PostgresEventReader>();

builder.Services.AddSingleton<WebhookEventProcessor>(sp =>
{
    var secret =
        builder.Configuration["Webhook:Secret"]
        ?? throw new InvalidOperationException("Webhook:Secret not configured");

    var dbContext = sp.GetRequiredService<WebhookDbContext>();
    var writer = new PostgresEventWriter(dbContext);
    var reader = new PostgresEventReader(dbContext);

    return new WebhookEventProcessorBuilder()
        .WithSignatureValidator(new HmacSha256SignatureValidator(secret))
        .WithPersistence(reader, writer)
        // Adicione handlers conforme necessário
        .Build();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    db.Database.Migrate();
}

app.MapGet("/health", () => Results.Ok("ok"));

app.MapPost("/hooks/callback/dms", async (string body, IServiceProvider sp) =>
{
    var secret = builder.Configuration["Webhook:Secret"] ?? throw new InvalidOperationException("Webhook:Secret not configured");
    var processor = DmsWebhookProcessorFactory.Create(sp, secret);
    var result = await processor.ProcessWebhookAsync(body);
    return result switch
    {
        { IsSuccess: true } => Results.Accepted(),
        { Exception: WebhookSignatureException } => Results.Problem(title: "Unauthorized", detail: result.ErrorMessage, statusCode: StatusCodes.Status401Unauthorized),
        { Exception: InvalidOperationException } => Results.BadRequest(new { error = result.ErrorMessage }),
        _ => Results.Problem(title: "Internal Server Error", detail: result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.MapPost("/hooks/callback/orders", async (string body, IServiceProvider sp) =>
{
    var secret = builder.Configuration["Webhook:Secret"] ?? throw new InvalidOperationException("Webhook:Secret not configured");
    var processor = OrderWebhookProcessorFactory.Create(sp, secret);
    var result = await processor.ProcessWebhookAsync(body);
    return result switch
    {
        { IsSuccess: true } => Results.Accepted(),
        { Exception: WebhookSignatureException } => Results.Problem(title: "Unauthorized", detail: result.ErrorMessage, statusCode: StatusCodes.Status401Unauthorized),
        { Exception: InvalidOperationException } => Results.BadRequest(new { error = result.ErrorMessage }),
        _ => Results.Problem(title: "Internal Server Error", detail: result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError)
    };
});

// Endpoint ALERT
app.MapPost("/hooks/callback/alerts", async (string body, IServiceProvider sp) =>
{
    var secret = builder.Configuration["Webhook:Secret"] ?? throw new InvalidOperationException("Webhook:Secret not configured");
    var processor = AlertWebhookProcessorFactory.Create(sp, secret);
    var result = await processor.ProcessWebhookAsync(body);
    return result switch
    {
        { IsSuccess: true } => Results.Accepted(),
        { Exception: WebhookSignatureException } => Results.Problem(title: "Unauthorized", detail: result.ErrorMessage, statusCode: StatusCodes.Status401Unauthorized),
        { Exception: InvalidOperationException } => Results.BadRequest(new { error = result.ErrorMessage }),
        _ => Results.Problem(title: "Internal Server Error", detail: result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.Run();
