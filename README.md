# Hub Service Simulator

A .NET 9.0 implementation of a simulated Hub Service for testing gRPC clients. This simulator provides an in-memory implementation of the Minimal Hub Service API for testing purposes. The primary purpose of this simulator is to assist in profiling and stress testing client integrations.

## Overview

The Hub Service Simulator implements the core functionality described in the Minimal Hub Service protocol buffer definitions, including:

- Message submission and validation
- Event handling
- Cast/message retrieval by various criteria
- User data management

All data is stored in-memory, making it ideal for performance testing and client development without needing a full production deployment.

## Project Structure

The simulator is organized into several projects:

- **HubSimulator.Domain**: Core domain models and interfaces
- **HubSimulator.Service**: gRPC service implementation
- **HubSimulator.DataGeneration**: Test data generation components

## Features

- Complete implementation of the Minimal Hub Service gRPC API
- In-memory storage for fast response times
- Realistic test data generation
- Support for pagination in queries
- Thread-safe implementation using concurrent collections
- Time-based data generation with realistic social network growth patterns

## Getting Started

### Prerequisites

- .NET 9.0 SDK (RC2 or higher)

### Building the Simulator

1. Clone the repository
2. Navigate to the root directory of the simulator
3. Build the solution:

```bash
cd HubSimulator
dotnet build
```

### Running the Simulator

To run the simulator, you must specify the correct project to run (the Service project):

```bash
cd HubSimulator
dotnet run --project src/HubSimulator.Service
```

Important: You must specify the `--project` parameter pointing to the HubSimulator.Service project, not the solution file.

The server will start on `https://localhost:5001` and `http://localhost:5000` by default.

### Initial Data Generation

On startup, the simulator will generate a realistic dataset of 50,000 users over 18 months of activity. This process may take several minutes to complete. The console will display progress updates during this generation:

```
info: Program[0]
      Data generation: Generated user 31528/50000 - 1% complete
```

You can modify the data generation parameters in `Program.cs` if you need a smaller dataset for faster startup.

### Connecting to the Simulator

You can connect to the simulator using any gRPC client that supports the protocol buffer definitions. The simulator provides endpoints for all methods defined in the `MinimalHubService` interface.

## Troubleshooting

### Common Issues

1. **"Couldn't find a project to run"**
   - Make sure to use `dotnet run --project src/HubSimulator.Service` instead of just `dotnet run`
   - Running from the solution file (`dotnet run --project HubSimulator.sln`) won't work

2. **Build Errors**
   - Ensure you're using .NET 9.0 SDK
   - Make sure all dependencies are properly restored with `dotnet restore`

3. **Slow Startup**
   - The default configuration generates 50,000 users and their activity
   - For faster testing, modify the parameters in `Program.cs` to reduce the data size

4. **Memory Issues**
   - The simulator stores all data in memory
   - For large datasets, ensure your machine has sufficient RAM

## Performance Considerations

The simulator is designed to be fast and efficient, with the following optimizations:

- Concurrent collections for thread-safe access
- Indexed lookups for common query patterns
- Efficient pagination implementation
- Minimal object copying

## Test Data Generation

The simulator automatically generates test data on startup, including:

- User profiles with display names, bios, and profile pictures
- Messages (casts) from each user
- Replies to messages, forming conversation threads
- User mentions in messages
- Realistic time-based growth patterns simulating 18 months of social network activity

You can customize the test data generation by modifying the parameters in the `Program.cs` file.

## API Reference

The simulator implements the following gRPC methods:

- `SubmitMessage`: Submit a new message to the hub
- `ValidateMessage`: Validate a message without storing it
- `GetEvent`: Get a hub event by ID
- `GetCast`: Get a cast by ID
- `GetCastsByFid`: Get casts by user FID
- `GetCastsByParent`: Get cast replies
- `GetUserDataByFid`: Get user profile data
- `GetInfo`: Get hub information

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details. 