using System;
using System.Threading.Tasks;
using Grpc.Core;
using HubSimulator.Domain.Services;
using Microsoft.Extensions.Logging;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using HubSimulator.Domain.Repositories;
using HubSimulator.Service.Mappers;
using HubSimulator.Service.Middleware;
using HubSimulator.Service.Protos;

namespace HubSimulator.Service.Services
{
    /// <summary>
    /// gRPC implementation of the Minimal Hub Service
    /// </summary>
    public class MinimalHubServiceImpl : MinimalHubService.MinimalHubServiceBase
    {
        private readonly IHubService _hubService;
        private readonly ILogger<MinimalHubServiceImpl> _logger;
        private readonly IMessageRepository _messageRepository;
        private readonly ErrorSimulationMiddleware _errorSimulation;
        
        public MinimalHubServiceImpl(
            IHubService hubService, 
            ILogger<MinimalHubServiceImpl> logger, 
            IMessageRepository messageRepository, 
            ErrorSimulationMiddleware errorSimulation)
        {
            _hubService = hubService ?? throw new ArgumentNullException(nameof(hubService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _errorSimulation = errorSimulation ?? throw new ArgumentNullException(nameof(errorSimulation));
        }
        
        /// <summary>
        /// Submit a message to the hub
        /// </summary>
        public override async Task<Message> SubmitMessage(Message request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Received SubmitMessage request");
                
                // Maybe inject a simulated error
                _errorSimulation.MaybeThrowError("SubmitMessage");
                
                // Convert service Message to domain Message
                var domainMessage = request.ToDomain();
                
                // Call domain service
                var result = await Task.FromResult(_hubService.SubmitMessage(domainMessage));
                
                // Convert domain Message result back to service Message
                return result.ToService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting message");
                throw new RpcException(new Status(StatusCode.Internal, $"Error submitting message: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Validate a message without storing it
        /// </summary>
        public override async Task<ValidationResponse> ValidateMessage(Message request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Received ValidateMessage request");
                
                // Maybe inject a simulated error
                _errorSimulation.MaybeThrowError("ValidateMessage");
                
                // Convert service Message to domain Message
                var domainMessage = request.ToDomain();
                
                // Call domain service
                var result = await Task.FromResult(_hubService.ValidateMessage(domainMessage));
                
                // Convert domain ValidationResponse to service ValidationResponse
                return result.ToService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating message");
                throw new RpcException(new Status(StatusCode.Internal, $"Error validating message: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Get an event by ID
        /// </summary>
        public override async Task<HubEvent> GetEvent(EventRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Received GetEvent request");
                
                // Maybe inject a simulated error
                _errorSimulation.MaybeThrowError("GetEvent");
                
                // Convert service EventRequest to domain EventRequest
                var domainRequest = request.ToDomain();
                
                // Call domain service
                var result = await Task.FromResult(_hubService.GetEvent(domainRequest.Id));
                
                // Convert domain HubEvent to service HubEvent or return empty if null
                return result?.ToService() ?? new HubEvent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event");
                throw new RpcException(new Status(StatusCode.Internal, $"Error getting event: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Get a cast by ID
        /// </summary>
        public override async Task<Message> GetCast(CastId request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Received GetCast request");
                
                // Maybe inject a simulated error
                _errorSimulation.MaybeThrowError("GetCast");
                
                // Convert service CastId to domain CastId
                var domainCastId = request.ToDomain();
                
                // Call domain service
                var result = await Task.FromResult(_hubService.GetCast(domainCastId));
                
                if (result == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Cast not found"));
                }
                
                // Convert domain Message to service Message
                return result.ToService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cast");
                throw new RpcException(new Status(StatusCode.Internal, $"Error getting cast: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Get casts by FID
        /// </summary>
        public override async Task<MessagesResponse> GetCastsByFid(FidRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Received GetCastsByFid request");
                
                // Maybe inject a simulated error
                _errorSimulation.MaybeThrowError("GetCastsByFid");
                
                // Convert service FidRequest to domain FidRequest
                var domainRequest = request.ToDomain();
                
                // Call domain service
                var result = await Task.FromResult(_hubService.GetCastsByFid(
                    domainRequest.Fid, 
                    domainRequest.PageSize > 0 ? (int)domainRequest.PageSize : 10, 
                    domainRequest.PageToken ?? ByteString.Empty, 
                    domainRequest.Reverse));
                
                // Convert domain MessagesResponse to service MessagesResponse
                return result.ToService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting casts by FID");
                throw new RpcException(new Status(StatusCode.Internal, $"Error getting casts by FID: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Get casts by parent
        /// </summary>
        public override async Task<MessagesResponse> GetCastsByParent(CastsByParentRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Received GetCastsByParent request");
                
                // Maybe inject a simulated error
                _errorSimulation.MaybeThrowError("GetCastsByParent");
                
                // Convert service CastsByParentRequest to domain CastsByParentRequest
                var domainRequest = request.ToDomain();
                
                // Call domain service
                var result = await Task.FromResult(_hubService.GetCastsByParent(
                    domainRequest.ParentCastId, 
                    domainRequest.PageSize > 0 ? (int)domainRequest.PageSize : 10, 
                    domainRequest.PageToken ?? ByteString.Empty, 
                    domainRequest.Reverse));
                
                // Convert domain MessagesResponse to service MessagesResponse
                return result.ToService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting casts by parent");
                throw new RpcException(new Status(StatusCode.Internal, $"Error getting casts by parent: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Get user data by FID
        /// </summary>
        public override async Task<MessagesResponse> GetUserDataByFid(FidRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Received GetUserDataByFid request");
                
                // Maybe inject a simulated error
                _errorSimulation.MaybeThrowError("GetUserDataByFid");
                
                // Convert service FidRequest to domain FidRequest
                var domainRequest = request.ToDomain();
                
                // Call domain service
                var result = await Task.FromResult(_hubService.GetUserDataByFid(domainRequest.Fid));
                
                // Convert domain MessagesResponse to service MessagesResponse
                return result.ToService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user data by FID");
                throw new RpcException(new Status(StatusCode.Internal, $"Error getting user data by FID: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Get hub information
        /// </summary>
        public override async Task<HubInfoResponse> GetInfo(HubInfoRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Received GetInfo request");
                
                // Maybe inject a simulated error
                _errorSimulation.MaybeThrowError("GetInfo");
                
                // Convert service HubInfoRequest to domain HubInfoRequest
                var domainRequest = request.ToDomain();
                
                // Call domain service
                var result = await Task.FromResult(_hubService.GetHubInfo(domainRequest.DbStats));
                
                // Convert domain HubInfoResponse to service HubInfoResponse
                return result.ToService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hub info");
                throw new RpcException(new Status(StatusCode.Internal, $"Error getting hub info: {ex.Message}"));
            }
        }
    }
} 