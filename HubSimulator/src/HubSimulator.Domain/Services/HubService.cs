using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using HubSimulator.Domain.Repositories;
using HubSimulator.Domain.Protos;
using Microsoft.Extensions.Logging;

namespace HubSimulator.Domain.Services
{
    /// <summary>
    /// Implementation of the hub service
    /// </summary>
    public class HubService : IHubService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IHubEventRepository _eventRepository;
        private readonly IMessageValidator _messageValidator;
        private readonly ILogger<HubService> _logger;
        
        // Hub service information
        private readonly string _version = "1.0.0";
        private readonly string _nickname = "HubSimulator";
        private readonly bool _isSyncing = false;
        
        public HubService(
            IMessageRepository messageRepository,
            IHubEventRepository eventRepository,
            IMessageValidator messageValidator,
            ILogger<HubService> logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _messageValidator = messageValidator ?? throw new ArgumentNullException(nameof(messageValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Submit a message to the hub
        /// </summary>
        public Message SubmitMessage(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            
            // Validate the message
            var (isValid, validationMessage) = _messageValidator.ValidateMessage(message);
            if (!isValid)
            {
                _logger.LogWarning("Message validation failed: {ValidationMessage}", validationMessage);
                throw new InvalidOperationException($"Message validation failed: {validationMessage}");
            }
            
            // Check if the message already exists
            var existingMessage = _messageRepository.GetMessageByHash(message.Hash);
            if (existingMessage != null)
            {
                _logger.LogInformation("Message with hash {Hash} already exists", 
                    Convert.ToBase64String(message.Hash.ToByteArray()));
                return existingMessage;
            }
            
            try
            {
                // Store the message
                var storedMessage = _messageRepository.StoreMessage(message);
                _logger.LogInformation("Message stored successfully with hash {Hash}",
                    Convert.ToBase64String(message.Hash.ToByteArray()));
                
                // Create a merge event for the message
                var mergeEvent = new HubEvent
                {
                    Type = (HubEventType)ProtoConstants.HubEventType.HubEventTypeMergeMessage,
                    MergeMessageBody = new MergeMessageBody
                    {
                        Message = storedMessage
                    }
                };
                
                // Store the event
                _eventRepository.StoreEvent(mergeEvent);
                _logger.LogInformation("Merge event created for message with hash {Hash}",
                    Convert.ToBase64String(message.Hash.ToByteArray()));
                
                return storedMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting message with hash {Hash}",
                    Convert.ToBase64String(message.Hash.ToByteArray()));
                throw;
            }
        }
        
        /// <summary>
        /// Validate a message without storing it
        /// </summary>
        public ValidationResponse ValidateMessage(Message message)
        {
            if (message == null)
            {
                return new ValidationResponse { Valid = false, Message = "Message cannot be null" };
            }
            
            var (isValid, validationMessage) = _messageValidator.ValidateMessage(message);
            return new ValidationResponse
            {
                Valid = isValid,
                Message = validationMessage ?? string.Empty
            };
        }
        
        /// <summary>
        /// Get a hub event by ID
        /// </summary>
        public HubEvent? GetEvent(ulong id)
        {
            return _eventRepository.GetEventById(id);
        }
        
        /// <summary>
        /// Get a cast by ID
        /// </summary>
        public Message? GetCast(CastId castId)
        {
            if (castId == null || castId.Hash == null || castId.Hash.Length == 0)
            {
                return null;
            }
            
            var message = _messageRepository.GetMessageByHash(castId.Hash);
            
            // Ensure it's the right message type and from the correct FID
            if (message?.Data?.Type == (MessageType)ProtoConstants.MessageType.MessageTypeCastAdd && message.Data.Fid == castId.Fid)
            {
                return message;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get casts by FID
        /// </summary>
        public MessagesResponse GetCastsByFid(ulong fid, int pageSize = 10, ByteString? pageToken = null, bool reverse = false)
        {
            var (messages, nextPageToken) = _messageRepository.GetMessagesByFid(fid, pageSize, pageToken, reverse);
            
            return new MessagesResponse
            {
                Messages = { messages },
                NextPageToken = nextPageToken
            };
        }
        
        /// <summary>
        /// Get casts by parent
        /// </summary>
        public MessagesResponse GetCastsByParent(CastId parentCastId, int pageSize = 10, ByteString? pageToken = null, bool reverse = false)
        {
            var (messages, nextPageToken) = _messageRepository.GetMessagesByParent(parentCastId, pageSize, pageToken, reverse);
            
            return new MessagesResponse
            {
                Messages = { messages },
                NextPageToken = nextPageToken
            };
        }
        
        /// <summary>
        /// Get user data by FID
        /// </summary>
        public MessagesResponse GetUserDataByFid(ulong fid)
        {
            var messages = _messageRepository.GetUserDataByFid(fid);
            
            return new MessagesResponse
            {
                Messages = { messages }
            };
        }
        
        /// <summary>
        /// Get hub information
        /// </summary>
        public HubInfoResponse GetHubInfo(bool includeDbStats)
        {
            var response = new HubInfoResponse
            {
                Version = _version,
                IsSyncing = _isSyncing,
                Nickname = _nickname
            };
            
            if (includeDbStats)
            {
                response.DbStats = new DbStats
                {
                    NumMessages = (ulong)EstimateMessageCount(),
                    ApproxSize = (ulong)EstimateDbSize()
                };
            }
            
            return response;
        }
        
        /// <summary>
        /// Estimate the total number of messages in the repository
        /// </summary>
        private long EstimateMessageCount()
        {
            // In a real implementation, we would have a proper counter
            // For this simulator, we'll use a dummy implementation
            
            // Count is entirely fictional for simulation purposes
            return 10000;
        }
        
        /// <summary>
        /// Estimate the size of the database in bytes
        /// </summary>
        private long EstimateDbSize()
        {
            // In a real implementation, we would calculate this from the actual data
            // For this simulator, we'll use a dummy implementation
            
            // Size is entirely fictional for simulation purposes
            return 1024 * 1024 * 100; // 100 MB
        }
    }
} 