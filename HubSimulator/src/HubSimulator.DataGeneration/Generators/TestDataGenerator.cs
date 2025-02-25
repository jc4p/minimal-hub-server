using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using HubSimulator.Domain;
using HubSimulator.Domain.Protos;
using HubSimulator.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace HubSimulator.DataGeneration.Generators
{
    /// <summary>
    /// Generates test data for the hub simulator
    /// </summary>
    public class TestDataGenerator
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IHubEventRepository _eventRepository;
        private readonly ILogger<TestDataGenerator> _logger;
        private readonly Random _random = new Random();
        
        // Constants for generation
        private const int DefaultUserCount = 10;
        private const int DefaultMessagesPerUser = 20;
        private const int DefaultReplyDepth = 3;
        private const int DefaultReplyCount = 5;
        
        public TestDataGenerator(
            IMessageRepository messageRepository,
            IHubEventRepository eventRepository,
            ILogger<TestDataGenerator> logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Generate test data with progress reporting
        /// </summary>
        public async Task GenerateTestDataAsync(
            Action<string, int>? progressCallback = null,
            int userCount = DefaultUserCount,
            int messagesPerUser = DefaultMessagesPerUser,
            int replyDepth = DefaultReplyDepth,
            int replyCount = DefaultReplyCount,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating test data with {UserCount} users, {MessagesPerUser} messages per user, {ReplyDepth} reply depth, {ReplyCount} replies per message",
                userCount, messagesPerUser, replyDepth, replyCount);
                
            // Calculate total work items for progress reporting
            int totalSteps = userCount + // User generation step
                            userCount * messagesPerUser + // Base messages 
                            CalculateTotalReplies(userCount * messagesPerUser, replyDepth, replyCount); // Replies
            
            int currentStep = 0;
            
            progressCallback?.Invoke("Starting data generation", 0);
            
            // Generate user profiles
            _logger.LogInformation("Generating {Count} user profiles", userCount);
            var users = new List<UserProfile>();
            
            for (int i = 1; i <= userCount; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                var user = new UserProfile
                {
                    Fid = (ulong)i,
                    DisplayName = $"User{i}",
                    Bio = $"This is the bio for user {i}",
                    ProfilePicUrl = $"https://example.com/profiles/{i}.jpg",
                    Url = $"https://example.com/users/{i}"
                };
                
                users.Add(user);
                
                // Generate user data messages
                GenerateUserDataMessages(user);
                
                // Update progress
                currentStep++;
                int percentComplete = (int)((double)currentStep / totalSteps * 100);
                progressCallback?.Invoke($"Generated user {i}/{userCount}", percentComplete);
                
                // Simulate work by yielding to other tasks (more realistic progress reporting)
                await Task.Delay(10, cancellationToken);
            }
            
            // Generate base messages for each user
            _logger.LogInformation("Generating {Count} messages per user", messagesPerUser);
            var allMessages = new List<Message>();
            
            foreach (var user in users)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                for (int i = 0; i < messagesPerUser; i++)
                {
                    var message = CreateCastMessage(user.Fid, $"Message {i} from user {user.Fid}", null);
                    StoreMessage(message);
                    allMessages.Add(message);
                    
                    // Update progress
                    currentStep++;
                    int percentComplete = (int)((double)currentStep / totalSteps * 100);
                    progressCallback?.Invoke($"Generated message {i+1}/{messagesPerUser} for user {user.Fid}", percentComplete);
                    
                    // Yield to other tasks occasionally
                    if (i % 5 == 0)
                    {
                        await Task.Delay(5, cancellationToken);
                    }
                }
            }
            
            // Generate replies for random messages
            _logger.LogInformation("Generating replies with depth {Depth} and {Count} replies per message", replyDepth, replyCount);
            await GenerateRepliesAsync(users, allMessages, replyDepth, replyCount, 
                (message, step, total) => {
                    currentStep++;
                    int percentComplete = (int)((double)currentStep / totalSteps * 100);
                    progressCallback?.Invoke(message, percentComplete);
                }, 
                cancellationToken);
            
            _logger.LogInformation("Test data generation complete. Generated {MessageCount} total messages",
                allMessages.Count);
                
            progressCallback?.Invoke("Data generation complete", 100);
        }
        
        /// <summary>
        /// Simplified version for backwards compatibility
        /// </summary>
        public void GenerateTestData(
            int userCount = DefaultUserCount,
            int messagesPerUser = DefaultMessagesPerUser,
            int replyDepth = DefaultReplyDepth,
            int replyCount = DefaultReplyCount)
        {
            // Use the async version but block until complete
            GenerateTestDataAsync(null, userCount, messagesPerUser, replyDepth, replyCount).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Calculate total number of replies that will be generated
        /// </summary>
        private int CalculateTotalReplies(int baseMessageCount, int depth, int repliesPerMessage)
        {
            int total = 0;
            int messagesAtCurrentLevel = Math.Min(baseMessageCount, 10); // We select up to 10 messages to reply to
            
            for (int i = 0; i < depth; i++)
            {
                int repliesAtThisLevel = messagesAtCurrentLevel * repliesPerMessage;
                total += repliesAtThisLevel;
                messagesAtCurrentLevel = repliesAtThisLevel;
            }
            
            return total;
        }
        
        /// <summary>
        /// Generate user data messages (profile info)
        /// </summary>
        private void GenerateUserDataMessages(UserProfile user)
        {
            // Display name
            var displayNameMessage = CreateUserDataMessage(user.Fid, (UserDataType)ProtoConstants.UserDataType.UserDataTypeDisplay, user.DisplayName);
            StoreMessage(displayNameMessage);
            
            // Bio
            var bioMessage = CreateUserDataMessage(user.Fid, (UserDataType)ProtoConstants.UserDataType.UserDataTypeBio, user.Bio);
            StoreMessage(bioMessage);
            
            // Profile picture
            var pfpMessage = CreateUserDataMessage(user.Fid, (UserDataType)ProtoConstants.UserDataType.UserDataTypePfp, user.ProfilePicUrl);
            StoreMessage(pfpMessage);
            
            // URL
            var urlMessage = CreateUserDataMessage(user.Fid, (UserDataType)ProtoConstants.UserDataType.UserDataTypeUrl, user.Url);
            StoreMessage(urlMessage);
        }
        
        /// <summary>
        /// Generate replies to messages with progress reporting
        /// </summary>
        private async Task GenerateRepliesAsync(
            List<UserProfile> users, 
            List<Message> baseMessages, 
            int depth, 
            int repliesPerMessage,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (depth <= 0 || baseMessages.Count == 0 || users.Count == 0)
            {
                return;
            }
            
            var replies = new List<Message>();
            
            // Select random messages to reply to
            var messagesToReplyTo = SelectRandomItems(baseMessages, Math.Min(baseMessages.Count, 10));
            int totalReplies = messagesToReplyTo.Count * repliesPerMessage;
            int currentReply = 0;
            
            foreach (var parentMessage in messagesToReplyTo)
            {
                // Generate replies from random users
                for (int i = 0; i < repliesPerMessage; i++)
                {
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var randomUser = SelectRandomItems(users, 1)[0];
                    
                    var parentCastId = new CastId
                    {
                        Fid = parentMessage.Data.Fid,
                        Hash = parentMessage.Hash
                    };
                    
                    var replyText = $"Reply to message from user {parentMessage.Data.Fid}";
                    var reply = CreateCastMessage(randomUser.Fid, replyText, parentCastId);
                    
                    StoreMessage(reply);
                    replies.Add(reply);
                    
                    // Update progress
                    currentReply++;
                    progressCallback?.Invoke($"Generated reply {currentReply}/{totalReplies} at depth {depth}", currentReply, totalReplies);
                    
                    // Yield to other tasks occasionally
                    if (i % 5 == 0)
                    {
                        await Task.Delay(5, cancellationToken);
                    }
                }
            }
            
            // Recursively generate deeper replies
            if (depth > 1 && replies.Count > 0)
            {
                await GenerateRepliesAsync(users, replies, depth - 1, repliesPerMessage, progressCallback, cancellationToken);
            }
        }
        
        /// <summary>
        /// Generate replies to messages (synchronous version for backward compatibility)
        /// </summary>
        private void GenerateReplies(List<UserProfile> users, List<Message> baseMessages, int depth, int repliesPerMessage)
        {
            // Use the async version but block until complete
            GenerateRepliesAsync(users, baseMessages, depth, repliesPerMessage).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Store a message in the repository and create a corresponding event
        /// </summary>
        private void StoreMessage(Message message)
        {
            // Store the message
            _messageRepository.StoreMessage(message);
            
            // Create a merge event for the message
            var mergeEvent = new HubEvent
            {
                Type = (HubEventType)ProtoConstants.HubEventType.HubEventTypeMergeMessage,
                MergeMessageBody = new MergeMessageBody
                {
                    Message = message
                }
            };
            
            // Store the event
            _eventRepository.StoreEvent(mergeEvent);
        }
        
        /// <summary>
        /// Create a new cast message
        /// </summary>
        private Message CreateCastMessage(ulong fid, string text, CastId? parentCastId)
        {
            var timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // Create message data
            var messageData = new MessageData
            {
                Type = (MessageType)ProtoConstants.MessageType.MessageTypeCastAdd,
                Fid = fid,
                Timestamp = timestamp,
                Network = (Network)ProtoConstants.Network.NetworkTestnet,
                CastAddBody = new CastAddBody
                {
                    Text = text
                }
            };
            
            // Add parent cast ID if present
            if (parentCastId != null)
            {
                messageData.CastAddBody.ParentCastId = parentCastId;
            }
            
            // Add random mentions (20% chance)
            if (_random.NextDouble() < 0.2)
            {
                int mentionCount = _random.Next(1, 4);
                for (int i = 0; i < mentionCount; i++)
                {
                    ulong mentionFid = (ulong)_random.Next(1, (int)fid + 10);
                    if (mentionFid != fid) // Don't mention self
                    {
                        messageData.CastAddBody.Mentions.Add(mentionFid);
                    }
                }
            }
            
            // Create hash from message data
            byte[] dataBytes = messageData.ToByteArray();
            byte[] hash = SHA256.HashData(dataBytes);
            
            // Create simulated signature (just a copy of the hash for testing)
            byte[] signature = new byte[hash.Length];
            Array.Copy(hash, signature, hash.Length);
            
            // Create signer (simulated public key)
            byte[] signer = Encoding.UTF8.GetBytes($"pubkey-{fid}");
            
            // Create the full message
            var message = new Message
            {
                Data = messageData,
                Hash = ByteString.CopyFrom(hash),
                HashScheme = (HashScheme)ProtoConstants.HashScheme.HashSchemeBlake3,
                Signature = ByteString.CopyFrom(signature),
                SignatureScheme = (SignatureScheme)ProtoConstants.SignatureScheme.SignatureSchemeEd25519,
                Signer = ByteString.CopyFrom(signer)
            };
            
            return message;
        }
        
        /// <summary>
        /// Create a user data message
        /// </summary>
        private Message CreateUserDataMessage(ulong fid, UserDataType type, string value)
        {
            var timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // Create message data
            var messageData = new MessageData
            {
                Type = (MessageType)ProtoConstants.MessageType.MessageTypeUserDataAdd,
                Fid = fid,
                Timestamp = timestamp,
                Network = (Network)ProtoConstants.Network.NetworkTestnet,
                UserDataBody = new UserDataBody
                {
                    Type = type,
                    Value = value
                }
            };
            
            // Create hash from message data
            byte[] dataBytes = messageData.ToByteArray();
            byte[] hash = SHA256.HashData(dataBytes);
            
            // Create simulated signature (just a copy of the hash for testing)
            byte[] signature = new byte[hash.Length];
            Array.Copy(hash, signature, hash.Length);
            
            // Create signer (simulated public key)
            byte[] signer = Encoding.UTF8.GetBytes($"pubkey-{fid}");
            
            // Create the full message
            var message = new Message
            {
                Data = messageData,
                Hash = ByteString.CopyFrom(hash),
                HashScheme = (HashScheme)ProtoConstants.HashScheme.HashSchemeBlake3,
                Signature = ByteString.CopyFrom(signature),
                SignatureScheme = (SignatureScheme)ProtoConstants.SignatureScheme.SignatureSchemeEd25519,
                Signer = ByteString.CopyFrom(signer)
            };
            
            return message;
        }
        
        /// <summary>
        /// Select random items from a list
        /// </summary>
        private List<T> SelectRandomItems<T>(List<T> source, int count)
        {
            count = Math.Min(count, source.Count);
            var result = new List<T>();
            var indices = new HashSet<int>();
            
            while (indices.Count < count)
            {
                int index = _random.Next(source.Count);
                if (indices.Add(index))
                {
                    result.Add(source[index]);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Simple user profile class for generating test data
        /// </summary>
        private class UserProfile
        {
            public ulong Fid { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string Bio { get; set; } = string.Empty;
            public string ProfilePicUrl { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }
    }
} 