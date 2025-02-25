using System;
using System.Linq;
using Google.Protobuf;
using ServiceMessage = HubSimulator.Service.Protos.Message;
using DomainMessage = HubSimulator.Domain.Protos.Message;
using ServiceCastId = HubSimulator.Service.Protos.CastId;
using DomainCastId = HubSimulator.Domain.Protos.CastId;
using ServiceMessagesResponse = HubSimulator.Service.Protos.MessagesResponse;
using DomainMessagesResponse = HubSimulator.Domain.Protos.MessagesResponse;
using ServiceHubEvent = HubSimulator.Service.Protos.HubEvent;
using DomainHubEvent = HubSimulator.Domain.Protos.HubEvent;
using ServiceValidationResponse = HubSimulator.Service.Protos.ValidationResponse;
using DomainValidationResponse = HubSimulator.Domain.Protos.ValidationResponse;
using ServiceHubInfoResponse = HubSimulator.Service.Protos.HubInfoResponse;
using DomainHubInfoResponse = HubSimulator.Domain.Protos.HubInfoResponse;
using ServiceHubInfoRequest = HubSimulator.Service.Protos.HubInfoRequest;
using DomainHubInfoRequest = HubSimulator.Domain.Protos.HubInfoRequest;
using ServiceFidRequest = HubSimulator.Service.Protos.FidRequest;
using DomainFidRequest = HubSimulator.Domain.Protos.FidRequest;
using ServiceEventRequest = HubSimulator.Service.Protos.EventRequest;
using DomainEventRequest = HubSimulator.Domain.Protos.EventRequest;
using ServiceCastsByParentRequest = HubSimulator.Service.Protos.CastsByParentRequest;
using DomainCastsByParentRequest = HubSimulator.Domain.Protos.CastsByParentRequest;

namespace HubSimulator.Service.Mappers
{
    /// <summary>
    /// Utility class for mapping between service protobuf types and domain protobuf types
    /// </summary>
    public static class ProtobufMapper
    {
        /// <summary>
        /// Convert service Message to domain Message
        /// </summary>
        public static DomainMessage? ToDomain(this ServiceMessage message)
        {
            if (message == null) return null;
            
            var domainMessage = new DomainMessage();
            domainMessage.MergeFrom(message.ToByteArray());
            return domainMessage;
        }
        
        /// <summary>
        /// Convert domain Message to service Message
        /// </summary>
        public static ServiceMessage? ToService(this DomainMessage message)
        {
            if (message == null) return null;
            
            var serviceMessage = new ServiceMessage();
            serviceMessage.MergeFrom(message.ToByteArray());
            return serviceMessage;
        }
        
        /// <summary>
        /// Convert service CastId to domain CastId
        /// </summary>
        public static DomainCastId? ToDomain(this ServiceCastId castId)
        {
            if (castId == null) return null;
            
            var domainCastId = new DomainCastId();
            domainCastId.MergeFrom(castId.ToByteArray());
            return domainCastId;
        }
        
        /// <summary>
        /// Convert domain CastId to service CastId
        /// </summary>
        public static ServiceCastId? ToService(this DomainCastId castId)
        {
            if (castId == null) return null;
            
            var serviceCastId = new ServiceCastId();
            serviceCastId.MergeFrom(castId.ToByteArray());
            return serviceCastId;
        }
        
        /// <summary>
        /// Convert service MessagesResponse to domain MessagesResponse
        /// </summary>
        public static DomainMessagesResponse? ToDomain(this ServiceMessagesResponse response)
        {
            if (response == null) return null;
            
            var domainResponse = new DomainMessagesResponse();
            domainResponse.MergeFrom(response.ToByteArray());
            return domainResponse;
        }
        
        /// <summary>
        /// Convert domain MessagesResponse to service MessagesResponse
        /// </summary>
        public static ServiceMessagesResponse? ToService(this DomainMessagesResponse response)
        {
            if (response == null) return null;
            
            var serviceResponse = new ServiceMessagesResponse();
            serviceResponse.MergeFrom(response.ToByteArray());
            return serviceResponse;
        }
        
        /// <summary>
        /// Convert service HubEvent to domain HubEvent
        /// </summary>
        public static DomainHubEvent? ToDomain(this ServiceHubEvent hubEvent)
        {
            if (hubEvent == null) return null;
            
            var domainEvent = new DomainHubEvent();
            domainEvent.MergeFrom(hubEvent.ToByteArray());
            return domainEvent;
        }
        
        /// <summary>
        /// Convert domain HubEvent to service HubEvent
        /// </summary>
        public static ServiceHubEvent? ToService(this DomainHubEvent hubEvent)
        {
            if (hubEvent == null) return null;
            
            var serviceEvent = new ServiceHubEvent();
            serviceEvent.MergeFrom(hubEvent.ToByteArray());
            return serviceEvent;
        }
        
        /// <summary>
        /// Convert service ValidationResponse to domain ValidationResponse
        /// </summary>
        public static DomainValidationResponse? ToDomain(this ServiceValidationResponse response)
        {
            if (response == null) return null;
            
            var domainResponse = new DomainValidationResponse();
            domainResponse.MergeFrom(response.ToByteArray());
            return domainResponse;
        }
        
        /// <summary>
        /// Convert domain ValidationResponse to service ValidationResponse
        /// </summary>
        public static ServiceValidationResponse? ToService(this DomainValidationResponse response)
        {
            if (response == null) return null;
            
            var serviceResponse = new ServiceValidationResponse();
            serviceResponse.MergeFrom(response.ToByteArray());
            return serviceResponse;
        }
        
        /// <summary>
        /// Convert service HubInfoRequest to domain HubInfoRequest
        /// </summary>
        public static DomainHubInfoRequest? ToDomain(this ServiceHubInfoRequest request)
        {
            if (request == null) return null;
            
            var domainRequest = new DomainHubInfoRequest();
            domainRequest.MergeFrom(request.ToByteArray());
            return domainRequest;
        }
        
        /// <summary>
        /// Convert domain HubInfoRequest to service HubInfoRequest
        /// </summary>
        public static ServiceHubInfoRequest? ToService(this DomainHubInfoRequest request)
        {
            if (request == null) return null;
            
            var serviceRequest = new ServiceHubInfoRequest();
            serviceRequest.MergeFrom(request.ToByteArray());
            return serviceRequest;
        }
        
        /// <summary>
        /// Convert service HubInfoResponse to domain HubInfoResponse
        /// </summary>
        public static DomainHubInfoResponse? ToDomain(this ServiceHubInfoResponse response)
        {
            if (response == null) return null;
            
            var domainResponse = new DomainHubInfoResponse();
            domainResponse.MergeFrom(response.ToByteArray());
            return domainResponse;
        }
        
        /// <summary>
        /// Convert domain HubInfoResponse to service HubInfoResponse
        /// </summary>
        public static ServiceHubInfoResponse? ToService(this DomainHubInfoResponse response)
        {
            if (response == null) return null;
            
            var serviceResponse = new ServiceHubInfoResponse();
            serviceResponse.MergeFrom(response.ToByteArray());
            return serviceResponse;
        }
        
        /// <summary>
        /// Convert service FidRequest to domain FidRequest
        /// </summary>
        public static DomainFidRequest? ToDomain(this ServiceFidRequest request)
        {
            if (request == null) return null;
            
            var domainRequest = new DomainFidRequest();
            domainRequest.MergeFrom(request.ToByteArray());
            return domainRequest;
        }
        
        /// <summary>
        /// Convert domain FidRequest to service FidRequest
        /// </summary>
        public static ServiceFidRequest? ToService(this DomainFidRequest request)
        {
            if (request == null) return null;
            
            var serviceRequest = new ServiceFidRequest();
            serviceRequest.MergeFrom(request.ToByteArray());
            return serviceRequest;
        }
        
        /// <summary>
        /// Convert service EventRequest to domain EventRequest
        /// </summary>
        public static DomainEventRequest? ToDomain(this ServiceEventRequest request)
        {
            if (request == null) return null;
            
            var domainRequest = new DomainEventRequest();
            domainRequest.MergeFrom(request.ToByteArray());
            return domainRequest;
        }
        
        /// <summary>
        /// Convert domain EventRequest to service EventRequest
        /// </summary>
        public static ServiceEventRequest? ToService(this DomainEventRequest request)
        {
            if (request == null) return null;
            
            var serviceRequest = new ServiceEventRequest();
            serviceRequest.MergeFrom(request.ToByteArray());
            return serviceRequest;
        }
        
        /// <summary>
        /// Convert service CastsByParentRequest to domain CastsByParentRequest
        /// </summary>
        public static DomainCastsByParentRequest? ToDomain(this ServiceCastsByParentRequest request)
        {
            if (request == null) return null;
            
            var domainRequest = new DomainCastsByParentRequest();
            domainRequest.MergeFrom(request.ToByteArray());
            return domainRequest;
        }
        
        /// <summary>
        /// Convert domain CastsByParentRequest to service CastsByParentRequest
        /// </summary>
        public static ServiceCastsByParentRequest? ToService(this DomainCastsByParentRequest request)
        {
            if (request == null) return null;
            
            var serviceRequest = new ServiceCastsByParentRequest();
            serviceRequest.MergeFrom(request.ToByteArray());
            return serviceRequest;
        }
    }
} 