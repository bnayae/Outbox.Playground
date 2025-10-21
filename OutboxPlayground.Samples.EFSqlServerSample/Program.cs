using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions;
using OutboxPlayground.Samples.Abstractions;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

var connStr = builder.Configuration.GetConnectionString("PaymentConnection") ?? throw new Exception("PaymentConnection not found");
services.AddPaymentRepository(connStr);
builder.AddOtel();
services.AddJsonDataSchemaProvider();

var app = builder.Build();

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
// Auto-apply migrations on startup
await scopeFactory.BootstrapDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/", () =>
{
    return DateTimeOffset.Now;
})
.WithOpenApi();

app.MapPost("/", async (Payment payment, [FromServices] IPaymentRepository repository) =>
{
    await repository.AddPaymentAsync(payment);
    return Results.Created();
})
.WithOpenApi();

await app.RunAsync();
