using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

// Configure xUnit to capture test output and disable test parallelization for integration tests
// This ensures database operations don't conflict and logs are captured properly
[assembly: CaptureConsole, CollectionBehavior(DisableTestParallelization = true)]

namespace ForaFinancial.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// Supports logging output to xUnit test results
/// </summary>
public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        // `this.Services.get` ensures that the Application is fully started `app.Run()`
        // within an async method because is a long running task, so we avoid blocking the main thread
        await Task.Run(() => this.Services);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            
            // Add console logging that will be captured by xUnit test output
            logging.AddConsole();
            logging.AddDebug();
            
            // Set minimum log level for integration tests
            logging.SetMinimumLevel(LogLevel.Information);
        });
    }
}

/// <summary>
/// Collection definition for integration tests
/// Allows sharing the WebApplicationFactory across multiple test classes
/// </summary>
[CollectionDefinition(nameof(IntegrationTest))]
public class IntegrationTest 
    : ICollectionFixture<WebApplicationFactory>
{
}
