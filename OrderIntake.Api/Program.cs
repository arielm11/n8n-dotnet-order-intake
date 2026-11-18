using OrderIntake.Api.Data;
using OrderIntake.Api.Endpoints;
using OrderIntake.Api.Repositories;
using OrderIntake.Api.Transformers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OrderIntake")
    ?? throw new InvalidOperationException("Connection string 'OrderIntake' was not found.");

var apiKey = builder.Configuration["ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException("Configuration value 'ApiKey' was not found.");
}

builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));
builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes(ApiKeySecuritySchemeTransformer.SchemeName)
        .AddApiKeyAuthentication(ApiKeySecuritySchemeTransformer.SchemeName, scheme => scheme.WithValue(apiKey)));
}

app.MapHealthEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();

app.Run();