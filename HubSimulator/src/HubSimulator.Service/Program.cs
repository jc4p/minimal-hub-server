using HubSimulator.DataGeneration.Generators;
using HubSimulator.Domain.Repositories;
using HubSimulator.Domain.Services;
using HubSimulator.Service.Middleware;
using HubSimulator.Service.Services;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddGrpc(options =>
{
    // Configure gRPC to handle concurrent requests
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
    options.MaxSendMessageSize = 16 * 1024 * 1024; // 16 MB
    options.EnableDetailedErrors = true;
});

// Add repositories as singletons
builder.Services.AddSingleton<IMessageRepository, InMemoryMessageRepository>();
builder.Services.AddSingleton<IHubEventRepository, InMemoryHubEventRepository>();

// Add services
builder.Services.AddSingleton<IMessageValidator, BasicMessageValidator>();
builder.Services.AddSingleton<IHubService, HubService>();

// Add error simulation middleware
builder.Services.AddSingleton<ErrorSimulationMiddleware>();

// Add data generators
builder.Services.AddTransient<TestDataGenerator>();
builder.Services.AddTransient<TimeBasedDataGenerator>();

var app = builder.Build();

// Initialize time-based test data asynchronously with progress reporting
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Initializing time-based social network data");
        
        var dataGenerator = services.GetRequiredService<TimeBasedDataGenerator>();
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Set up a timer to show that the server is still responsive during data generation
        var timer = new System.Timers.Timer(30000); // 30 second updates
        timer.Elapsed += (sender, e) => logger.LogInformation("Server is still responsive while generating data...");
        timer.Start();
        
        // Start a stopwatch to measure generation time
        var stopwatch = Stopwatch.StartNew();
        
        // Generate time-based data with realistic growth pattern
        await dataGenerator.GenerateTimeBasedDataAsync(
            progressCallback: (message, percent) => 
            {
                logger.LogInformation("Data generation: {Message} - {Percent}% complete", message, percent);
            },
            totalUsers: 50000,            // 50k unique users
            months: 18,                   // 18 months of data
            initialActiveUsers: 200,      // ~200 daily active users in month 1
            finalActiveUsers: 10000,      // ~10k daily active users by month 12+
            cancellationToken: cancellationTokenSource.Token
        );
        
        stopwatch.Stop();
        timer.Stop();
        
        logger.LogInformation("Time-based data generation complete in {ElapsedTime}", stopwatch.Elapsed);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing test data");
    }
}

// Configure the HTTP request pipeline
app.UseRouting();

// Configure error simulation
var errorSimulator = app.Services.GetRequiredService<ErrorSimulationMiddleware>();
// Default: 5% chance of errors, can be adjusted at runtime through an API endpoint if needed

// Map gRPC service
app.MapGrpcService<MinimalHubServiceImpl>();

// Map a simple homepage for basic information
app.MapGet("/", () => "Hub Simulator gRPC Service is running. Use a gRPC client to interact with the service.");

// Add API endpoint to control error simulation (enable/disable, set probability)
app.MapGet("/api/errors/toggle", (bool? enabled) => 
{
    var errorSimulator = app.Services.GetRequiredService<ErrorSimulationMiddleware>();
    if (enabled.HasValue)
    {
        errorSimulator.SetEnabled(enabled.Value);
        return $"Error simulation {(enabled.Value ? "enabled" : "disabled")}";
    }
    return "Provide '?enabled=true' or '?enabled=false' query parameter";
});

app.MapGet("/api/errors/probability", (double? probability) => 
{
    var errorSimulator = app.Services.GetRequiredService<ErrorSimulationMiddleware>();
    if (probability.HasValue)
    {
        try 
        {
            errorSimulator.SetErrorProbability(probability.Value);
            return $"Error probability set to {probability.Value:P2}";
        }
        catch (ArgumentOutOfRangeException)
        {
            return "Probability must be between 0.0 and 1.0";
        }
    }
    return "Provide '?probability=0.1' query parameter (value between 0.0 and 1.0)";
});

// Log startup information
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
appLogger.LogInformation("Hub Simulator is starting up and listening for gRPC connections");
appLogger.LogInformation("Error simulation is enabled with a {Probability:P2} error rate", 0.05);
appLogger.LogInformation("Control error simulation with /api/errors/toggle?enabled=true|false and /api/errors/probability?probability=0.1");

app.Run();
