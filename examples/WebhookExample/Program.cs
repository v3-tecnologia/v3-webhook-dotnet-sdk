using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Security;
using WebhookExample;
using WebhookExample.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WebhookDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=webhookdb;Username=postgres;Password=postgres"
    )
);

builder.Services.AddSingleton<WebhookEventProcessor>(sp =>
{
    var secret =
        builder.Configuration["Webhook:Secret"]
        ?? throw new InvalidOperationException("Webhook:Secret not configured");

    return WebhookProcessorFactory.Create(sp, secret);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    db.Database.Migrate();
}

app.MapPost("/webhook", async (
    string body,
    WebhookEventProcessor processor
) =>
{
    try
    {
        await processor.ProcessWebhookAsync(body);
        return Results.Accepted();
    }
    catch (WebhookSignatureException ex)
    {
        return Results.Problem(
            title: "Unauthorized",
            detail: ex.Message,
            statusCode: StatusCodes.Status401Unauthorized
        );
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new
        {
            error = ex.Message
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Internal Server Error",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
