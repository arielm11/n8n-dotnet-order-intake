using OrderIntake.Api.Data;
using OrderIntake.Api.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OrderIntake")
    ?? throw new InvalidOperationException("Connection string 'OrderIntake' was not found.");

builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthEndpoints();

app.Run();