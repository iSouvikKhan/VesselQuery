using VesselQuery.Api.Configuration;
using VesselQuery.Api.Infrastructure;
using VesselQuery.Core.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<QueryExceptionHandler>();
builder.Services.AddVesselQuery();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "VesselQuery API v1"));
}

app.UseHttpsRedirection();

app.MapControllers();

app.Services.GetRequiredService<IVesselStore>();

app.Run();
