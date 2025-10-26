#pragma warning disable CA2201 // Do not raise reserved exception types
#pragma warning disable S112 // General or reserved exceptions should never be thrown

using Microsoft.Extensions;
using OutboxPlayground.Samples.Abstractions;
using OutboxPlayground.Samples.EFSqlServerSample;

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
services.AddSingleton<IRiskAssessmentService, RiskAssessmentProxy>(); // just a sample

var app = builder.Build();

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

app.MapPost("/", async (PaymentRequest payment, [FromKeyedServices("default")] IPaymentRepository repository) =>
{
    await repository.AddPaymentAsync(payment);
    return Results.Created();
})
.WithOpenApi();

app.MapPost("/multi", async (PaymentRequest payment, [FromKeyedServices("multi-outbox")] IPaymentRepository repository) =>
{
    await repository.AddPaymentAsync(payment);
    return Results.Created();
})
.WithOpenApi();

await app.RunAsync();
