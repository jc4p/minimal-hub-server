using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace HubSimulator.Service.Middleware
{
    /// <summary>
    /// Middleware to randomly simulate errors in gRPC service calls
    /// </summary>
    public class ErrorSimulationMiddleware
    {
        private readonly Random _random = new Random();
        private readonly ILogger<ErrorSimulationMiddleware> _logger;
        
        // Default probability of error injection (5%)
        private double _errorProbability = 0.05;
        
        // Flag to enable/disable error simulation
        private bool _isEnabled = true;
        
        public ErrorSimulationMiddleware(ILogger<ErrorSimulationMiddleware> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Enable or disable error simulation
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Error simulation {status}", enabled ? "enabled" : "disabled");
        }
        
        /// <summary>
        /// Set the probability of injecting an error (0.0 to 1.0)
        /// </summary>
        public void SetErrorProbability(double probability)
        {
            if (probability < 0.0 || probability > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0.0 and 1.0");
            }
            
            _errorProbability = probability;
            _logger.LogInformation("Error probability set to {probability:P2}", probability);
        }
        
        /// <summary>
        /// Maybe throw an error based on configured probability
        /// </summary>
        public void MaybeThrowError(string operationName)
        {
            // Skip if disabled
            if (!_isEnabled)
            {
                return;
            }
            
            // Roll the dice
            if (_random.NextDouble() < _errorProbability)
            {
                // Select a random error type
                var errorType = _random.Next(4);
                
                switch (errorType)
                {
                    case 0:
                        _logger.LogWarning("Simulating Internal error during {operation}", operationName);
                        throw new RpcException(new Status(StatusCode.Internal, $"Simulated internal error in {operationName}"));
                    
                    case 1:
                        _logger.LogWarning("Simulating Unavailable error during {operation}", operationName);
                        throw new RpcException(new Status(StatusCode.Unavailable, $"Simulated service unavailable in {operationName}"));
                    
                    case 2:
                        _logger.LogWarning("Simulating DeadlineExceeded error during {operation}", operationName);
                        throw new RpcException(new Status(StatusCode.DeadlineExceeded, $"Simulated deadline exceeded in {operationName}"));
                    
                    case 3:
                        _logger.LogWarning("Simulating ResourceExhausted error during {operation}", operationName);
                        throw new RpcException(new Status(StatusCode.ResourceExhausted, $"Simulated resource exhaustion in {operationName}"));
                }
            }
        }
    }
} 