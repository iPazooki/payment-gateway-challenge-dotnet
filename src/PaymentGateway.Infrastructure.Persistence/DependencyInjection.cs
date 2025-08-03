using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Infrastructure.Persistence.Services;

namespace PaymentGateway.Infrastructure.Persistence;

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
    public static IServiceCollection AddInfrastructurePersistenceServices(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentsRepository, PaymentsRepository>();

        return services;
    }
}