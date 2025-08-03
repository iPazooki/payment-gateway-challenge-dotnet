using System.Reflection;

using Asp.Versioning;

using PaymentGateway.Api.Exceptions;
using PaymentGateway.Infrastructure;
using PaymentGateway.Infrastructure.Persistence;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Payment Gateway API",
        Version = "v1",
        Description = "A payment gateway API for processing card payments"
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddHttpClient("BankApi", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var bankApiUrl = configuration["BankApiUrl"];
    
    ArgumentNullException.ThrowIfNull(bankApiUrl);
    
    client.BaseAddress = new Uri(bankApiUrl);
});

builder.Services
    .AddInfrastructureServices()
    .AddInfrastructurePersistenceServices();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Logging.ClearProviders();
builder.Host.UseSerilog(((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration)));

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.DescribeApiVersions();
        
        foreach (var description in descriptions)
        {
            var url = $"/swagger/{description.GroupName}/swagger.json";
            var name = description.GroupName.ToUpperInvariant();
            options.SwaggerEndpoint(url, name);
        }
    });

}


app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseHealthChecks("/health");
app.MapControllers();
app.UseExceptionHandler();

app.Run();
