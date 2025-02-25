using System;
using System.Linq;
using HubSimulator.Domain.Repositories;
using HubSimulator.Domain.Protos;

namespace HubSimulator.Domain.Services
{
    /// <summary>
    /// Basic implementation of the message validator
    /// </summary>
    public class BasicMessageValidator : IMessageValidator
    {
        private readonly IMessageRepository _messageRepository;
        
        public BasicMessageValidator(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        }
        
        /// <summary>
        /// Validates a message for basic correctness and consistency
        /// </summary>
        public (bool IsValid, string? ValidationMessage) ValidateMessage(Message message)
        {
            // Check if message is null
            if (message == null)
            {
                return (false, "Message cannot be null");
            }
            
            // Check if message data is present
            if (message.Data == null)
            {
                return (false, "Message data cannot be null");
            }
            
            // Check if hash is present
            if (message.Hash == null || message.Hash.Length == 0)
            {
                return (false, "Message hash cannot be empty");
            }
            
            // Check if signature is present
            if (message.Signature == null || message.Signature.Length == 0)
            {
                return (false, "Message signature cannot be empty");
            }
            
            // Check if signer is present
            if (message.Signer == null || message.Signer.Length == 0)
            {
                return (false, "Message signer cannot be empty");
            }
            
            // Validate FID
            if (message.Data.Fid == 0)
            {
                return (false, "FID must be non-zero");
            }
            
            // Validate timestamp
            if (message.Data.Timestamp == 0)
            {
                return (false, "Timestamp must be non-zero");
            }
            
            // Validate network (we only support testnet in this simulator)
            if (message.Data.Network != (Network)ProtoConstants.Network.NetworkTestnet)
            {
                return (false, "Only testnet network is supported");
            }
            
            // Validate message body based on type
            switch (message.Data.Type)
            {
                case (MessageType)ProtoConstants.MessageType.MessageTypeCastAdd:
                    return ValidateCastAdd(message);
                
                case (MessageType)ProtoConstants.MessageType.MessageTypeCastRemove:
                    return ValidateCastRemove(message);
                
                case (MessageType)ProtoConstants.MessageType.MessageTypeUserDataAdd:
                    return ValidateUserDataAdd(message);
                
                default:
                    return (false, $"Unsupported message type: {message.Data.Type}");
            }
        }
        
        /// <summary>
        /// Validates a cast add message
        /// </summary>
        private (bool IsValid, string? ValidationMessage) ValidateCastAdd(Message message)
        {
            // Ensure cast add body is present
            if (message.Data.CastAddBody == null)
            {
                return (false, "Cast add body cannot be null");
            }
            
            // Validate text
            if (string.IsNullOrWhiteSpace(message.Data.CastAddBody.Text))
            {
                return (false, "Cast text cannot be empty");
            }
            
            // Validate text length (arbitrary limit for simulation)
            if (message.Data.CastAddBody.Text.Length > 300)
            {
                return (false, "Cast text exceeds maximum length of 300 characters");
            }
            
            // Validate parent cast if present
            if (message.Data.CastAddBody.ParentCastId != null)
            {
                var parentCastId = message.Data.CastAddBody.ParentCastId;
                
                // Validate parent FID
                if (parentCastId.Fid == 0)
                {
                    return (false, "Parent cast FID must be non-zero");
                }
                
                // Validate parent hash
                if (parentCastId.Hash == null || parentCastId.Hash.Length == 0)
                {
                    return (false, "Parent cast hash cannot be empty");
                }
                
                // Check if parent cast exists
                var parentCast = _messageRepository.GetMessageByHash(parentCastId.Hash);
                if (parentCast == null)
                {
                    return (false, "Parent cast does not exist");
                }
                
                // Ensure parent is a cast
                if (parentCast.Data?.Type != (MessageType)ProtoConstants.MessageType.MessageTypeCastAdd)
                {
                    return (false, "Parent message is not a cast");
                }
            }
            
            // Validate mentions if present
            if (message.Data.CastAddBody.Mentions != null && message.Data.CastAddBody.Mentions.Count > 0)
            {
                // Ensure mentions are unique
                if (message.Data.CastAddBody.Mentions.Count != message.Data.CastAddBody.Mentions.Distinct().Count())
                {
                    return (false, "Duplicate mentions are not allowed");
                }
                
                // Arbitrary limit on mentions
                if (message.Data.CastAddBody.Mentions.Count > 10)
                {
                    return (false, "Too many mentions (maximum is 10)");
                }
            }
            
            return (true, null);
        }
        
        /// <summary>
        /// Validates a cast remove message
        /// </summary>
        private (bool IsValid, string? ValidationMessage) ValidateCastRemove(Message message)
        {
            // Ensure cast remove body is present
            if (message.Data.CastRemoveBody == null)
            {
                return (false, "Cast remove body cannot be null");
            }
            
            // Validate target hash
            if (message.Data.CastRemoveBody.TargetHash == null || message.Data.CastRemoveBody.TargetHash.Length == 0)
            {
                return (false, "Target hash cannot be empty");
            }
            
            // Check if target cast exists
            var targetCast = _messageRepository.GetMessageByHash(message.Data.CastRemoveBody.TargetHash);
            if (targetCast == null)
            {
                return (false, "Target cast does not exist");
            }
            
            // Ensure target is a cast
            if (targetCast.Data?.Type != (MessageType)ProtoConstants.MessageType.MessageTypeCastAdd)
            {
                return (false, "Target message is not a cast");
            }
            
            // Ensure the user is removing their own cast
            if (targetCast.Data?.Fid != message.Data.Fid)
            {
                return (false, "Users can only remove their own casts");
            }
            
            return (true, null);
        }
        
        /// <summary>
        /// Validates a user data add message
        /// </summary>
        private (bool IsValid, string? ValidationMessage) ValidateUserDataAdd(Message message)
        {
            // Ensure user data body is present
            if (message.Data.UserDataBody == null)
            {
                return (false, "User data body cannot be null");
            }
            
            // Validate user data type
            if (message.Data.UserDataBody.Type == (UserDataType)ProtoConstants.UserDataType.UserDataTypeNone)
            {
                return (false, "User data type must be specified");
            }
            
            // Validate value
            if (string.IsNullOrEmpty(message.Data.UserDataBody.Value))
            {
                return (false, "User data value cannot be empty");
            }
            
            // Validate based on data type
            switch (message.Data.UserDataBody.Type)
            {
                case (UserDataType)ProtoConstants.UserDataType.UserDataTypePfp:
                    // Basic URL validation
                    if (!message.Data.UserDataBody.Value.StartsWith("http"))
                    {
                        return (false, "PFP must be a valid URL");
                    }
                    break;
                
                case (UserDataType)ProtoConstants.UserDataType.UserDataTypeDisplay:
                    // Validate display name length
                    if (message.Data.UserDataBody.Value.Length > 50)
                    {
                        return (false, "Display name cannot exceed 50 characters");
                    }
                    break;
                
                case (UserDataType)ProtoConstants.UserDataType.UserDataTypeBio:
                    // Validate bio length
                    if (message.Data.UserDataBody.Value.Length > 300)
                    {
                        return (false, "Bio cannot exceed 300 characters");
                    }
                    break;
                
                case (UserDataType)ProtoConstants.UserDataType.UserDataTypeUrl:
                    // Basic URL validation
                    if (!message.Data.UserDataBody.Value.StartsWith("http"))
                    {
                        return (false, "URL must be valid");
                    }
                    break;
            }
            
            return (true, null);
        }
    }
} 