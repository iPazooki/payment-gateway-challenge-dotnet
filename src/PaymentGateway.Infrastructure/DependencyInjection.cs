using FluentValidation;

using PaymentGateway.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Infrastructure;

/// <summary>
/// Provides extension methods for setting up infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the specified IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The IServiceCollection with the added services.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IBankService, BankService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}