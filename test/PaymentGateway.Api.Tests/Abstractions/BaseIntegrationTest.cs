using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Api.Tests.Abstractions;

/// <summary>
/// Base class for integration tests, providing common setup and teardown functionality.
/// Implements <see cref="IClassFixture{TFixture}"/> to share a single instance of <see cref="IntegrationWebApplicationFactory"/> across tests.
/// </summary>
public abstract class BaseIntegrationTest : IClassFixture<IntegrationWebApplicationFactory>, IDisposable
{
    private readonly IServiceScope _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseIntegrationTest"/> class.
    /// </summary>
    protected BaseIntegrationTest()
    {
        IntegrationWebApplicationFactory factory = new();

        _scope = factory.Services.CreateScope();
    }
    
    /// <summary>
    /// Disposes the service scope.
    /// </summary>
    public void Dispose()
    {
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }
}