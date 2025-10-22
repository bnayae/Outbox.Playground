using Microsoft.Extensions;
using OutboxPlayground.Samples.Jobs;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
builder.AddOtel();

// Register the background service
builder.Services.AddHostedService<Job>();

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
    Results.Ok();
})
.WithOpenApi();

await app.RunAsync();
