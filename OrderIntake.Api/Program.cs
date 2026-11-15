using OrderIntake.Api.Data;
using OrderIntake.Api.Endpoints;
using OrderIntake.Api.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OrderIntake")
    ?? throw new InvalidOperationException("Connection string 'OrderIntake' was not found.");

builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));
builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();

app.Run();