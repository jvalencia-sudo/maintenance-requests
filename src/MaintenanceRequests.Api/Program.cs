using System.Text.Json.Serialization;
using MaintenanceRequests.Api.Http;
using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Application.Requests;
using MaintenanceRequests.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var connectionString = configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services
    .AddControllers(options =>
    {
        // Validation errors use the JSON (camelCase) names, same as the domain's field names.
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());

        // Required fields are declared explicitly on the DTOs; the implicit rule would add a
        // redundant "dto field is required" error whenever the body fails to deserialize.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

        var messages = options.ModelBindingMessageProvider;
        messages.SetAttemptedValueIsInvalidAccessor((value, _) => $"El valor '{value}' no es válido.");
        messages.SetValueIsInvalidAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetValueMustNotBeNullAccessor(_ => "El campo es obligatorio.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "El cuerpo de la solicitud es obligatorio.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetUnknownValueIsInvalidAccessor(_ => "El valor no es válido.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor no es válido.");
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Malformed JSON gets a generic message instead of serializer internals (type names, paths).
        options.AllowInputFormatterExceptionMessages = false;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<MaintenanceRequestService>();
builder.Services.AddInfrastructure(connectionString);

var allowedOrigins = (configuration["CORS_ALLOWED_ORIGINS"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (allowedOrigins.Contains("*"))
{
    throw new InvalidOperationException("CORS_ALLOWED_ORIGINS must list explicit origins; '*' is not allowed.");
}

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .WithMethods("GET", "POST", "PATCH")
    .WithHeaders("Content-Type", CurrentUser.HeaderName)
    .WithExposedHeaders("Location")));

var enableSwagger = configuration.GetValue<bool>("ENABLE_SWAGGER");
if (enableSwagger)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        var userHeader = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = CurrentUser.HeaderName,
            Description = "Id del usuario que realiza la operación (1 a 5). Obligatorio en POST y PATCH.",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = CurrentUser.HeaderName }
        };
        options.AddSecurityDefinition(CurrentUser.HeaderName, userHeader);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { [userHeader] = [] });
    });
}

var app = builder.Build();

if (configuration.GetValue<bool>("APPLY_MIGRATIONS"))
{
    await app.Services.ApplyMigrationsAsync();
}

if (configuration.GetValue<bool>("SEED_DEMO_DATA"))
{
    await app.Services.SeedDemoDataAsync();
}

app.UseExceptionHandler();

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// No UseHttpsRedirection: the API runs behind plain HTTP in the container and TLS,
// when needed, belongs to the reverse proxy in front of it.
app.UseCors();
app.MapControllers();

await app.RunAsync();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program { }
