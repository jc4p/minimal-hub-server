using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using HubSimulator.Domain.Protos;

namespace HubSimulator.Domain.Repositories
{
    /// <summary>
    /// In-memory implementation of the message repository
    /// </summary>
    public class InMemoryMessageRepository : IMessageRepository
    {
        // Main storage for messages indexed by hash
        private readonly ConcurrentDictionary<string, Message> _messagesByHash = new();
        
        // Index for messages by FID
        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, Message>> _messagesByFid = new();
        
        // Index for messages by parent cast
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Message>> _messagesByParent = new();
        
        // Index for user data messages by FID
        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, Message>> _userDataByFid = new();
        
        /// <summary>
        /// Store a message in the repository and update all indexes
        /// </summary>
        public Message StoreMessage(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            
            // Add to main storage
            string hashKey = Convert.ToBase64String(message.Hash.ToByteArray());
            _messagesByHash[hashKey] = message;
            
            // Update FID index
            if (message.Data?.Fid > 0)
            {
                var fidMessages = _messagesByFid.GetOrAdd(message.Data.Fid, _ => new ConcurrentDictionary<string, Message>());
                fidMessages[hashKey] = message;
                
                // If it's a user data message, update user data index
                if (message.Data.Type == (MessageType)ProtoConstants.MessageType.MessageTypeUserDataAdd)
                {
                    var userDataMessages = _userDataByFid.GetOrAdd(message.Data.Fid, _ => new ConcurrentDictionary<string, Message>());
                    userDataMessages[hashKey] = message;
                }
            }
            
            // Update parent cast index if it's a reply
            if (message.Data?.Type == (MessageType)ProtoConstants.MessageType.MessageTypeCastAdd && 
                message.Data.CastAddBody?.ParentCastId != null)
            {
                var parentCastId = message.Data.CastAddBody.ParentCastId;
                string parentKey = $"{parentCastId.Fid}:{Convert.ToBase64String(parentCastId.Hash.ToByteArray())}";
                
                var parentMessages = _messagesByParent.GetOrAdd(parentKey, _ => new ConcurrentDictionary<string, Message>());
                parentMessages[hashKey] = message;
            }
            
            return message;
        }
        
        /// <summary>
        /// Get a message by its hash
        /// </summary>
        public Message? GetMessageByHash(ByteString hash)
        {
            if (hash == null || hash.Length == 0)
            {
                return null;
            }
            
            string hashKey = Convert.ToBase64String(hash.ToByteArray());
            _messagesByHash.TryGetValue(hashKey, out var message);
            return message;
        }
        
        /// <summary>
        /// Get messages by FID with pagination support
        /// </summary>
        public (List<Message>, ByteString?) GetMessagesByFid(ulong fid, int pageSize = 10, ByteString? pageToken = null, bool reverse = false)
        {
            if (fid == 0 || pageSize <= 0)
            {
                return (new List<Message>(), null);
            }
            
            if (!_messagesByFid.TryGetValue(fid, out var fidMessages))
            {
                return (new List<Message>(), null);
            }
            
            // Sort messages by timestamp (contained within Data.Timestamp)
            var initialQuery = fidMessages.Values
                .Where(m => m.Data?.Type == (MessageType)ProtoConstants.MessageType.MessageTypeCastAdd);
                
            IEnumerable<Message> sortedMessages;
            if (reverse)
            {
                sortedMessages = initialQuery.OrderByDescending(m => m.Data?.Timestamp);
            }
            else
            {
                sortedMessages = initialQuery.OrderBy(m => m.Data?.Timestamp);
            }
            
            // Handle pagination
            string? lastHashKey = null;
            if (pageToken != null && pageToken.Length > 0)
            {
                lastHashKey = Convert.ToBase64String(pageToken.ToByteArray());
                // Skip messages up to the last hash key
                if (lastHashKey != null)
                {
                    var messagesToSkip = sortedMessages.TakeWhile(m => 
                        Convert.ToBase64String(m.Hash.ToByteArray()) != lastHashKey).Count();
                    
                    if (messagesToSkip < sortedMessages.Count())
                    {
                        sortedMessages = sortedMessages.Skip(messagesToSkip + 1);
                    }
                    else
                    {
                        return (new List<Message>(), null);
                    }
                }
            }
            
            // Take the next page of messages
            var messages = sortedMessages.Take(pageSize + 1).ToList();
            
            // Determine if there are more messages after this page
            ByteString? nextPageToken = null;
            if (messages.Count > pageSize)
            {
                var lastMessage = messages.Last();
                messages.RemoveAt(pageSize);
                nextPageToken = lastMessage.Hash;
            }
            
            return (messages, nextPageToken);
        }
        
        /// <summary>
        /// Get messages by parent cast ID with pagination support
        /// </summary>
        public (List<Message>, ByteString?) GetMessagesByParent(CastId parentCastId, int pageSize = 10, ByteString? pageToken = null, bool reverse = false)
        {
            if (parentCastId == null || pageSize <= 0)
            {
                return (new List<Message>(), null);
            }
            
            string parentKey = $"{parentCastId.Fid}:{Convert.ToBase64String(parentCastId.Hash.ToByteArray())}";
            
            if (!_messagesByParent.TryGetValue(parentKey, out var parentMessages))
            {
                return (new List<Message>(), null);
            }
            
            // Sort messages by timestamp
            IEnumerable<Message> sortedMessages;
            if (reverse)
            {
                sortedMessages = parentMessages.Values.OrderByDescending(m => m.Data?.Timestamp);
            }
            else
            {
                sortedMessages = parentMessages.Values.OrderBy(m => m.Data?.Timestamp);
            }
            
            // Handle pagination
            string? lastHashKey = null;
            if (pageToken != null && pageToken.Length > 0)
            {
                lastHashKey = Convert.ToBase64String(pageToken.ToByteArray());
                // Skip messages up to the last hash key
                if (lastHashKey != null)
                {
                    var messagesToSkip = sortedMessages.TakeWhile(m => 
                        Convert.ToBase64String(m.Hash.ToByteArray()) != lastHashKey).Count();
                    
                    if (messagesToSkip < sortedMessages.Count())
                    {
                        sortedMessages = sortedMessages.Skip(messagesToSkip + 1);
                    }
                    else
                    {
                        return (new List<Message>(), null);
                    }
                }
            }
            
            // Take the next page of messages
            var messages = sortedMessages.Take(pageSize + 1).ToList();
            
            // Determine if there are more messages after this page
            ByteString? nextPageToken = null;
            if (messages.Count > pageSize)
            {
                var lastMessage = messages.Last();
                messages.RemoveAt(pageSize);
                nextPageToken = lastMessage.Hash;
            }
            
            return (messages, nextPageToken);
        }
        
        /// <summary>
        /// Get user data messages for a specific FID
        /// </summary>
        public List<Message> GetUserDataByFid(ulong fid)
        {
            if (fid == 0 || !_userDataByFid.TryGetValue(fid, out var userDataMessages))
            {
                return new List<Message>();
            }
            
            return userDataMessages.Values.ToList();
        }
        
        /// <summary>
        /// Remove a message from the repository and all indexes
        /// </summary>
        public bool RemoveMessage(ByteString hash)
        {
            if (hash == null || hash.Length == 0)
            {
                return false;
            }
            
            string hashKey = Convert.ToBase64String(hash.ToByteArray());
            
            // Try to get the message first to update indexes
            if (!_messagesByHash.TryGetValue(hashKey, out var message))
            {
                return false;
            }
            
            // Remove from main storage
            bool removed = _messagesByHash.TryRemove(hashKey, out _);
            
            if (removed && message.Data?.Fid > 0)
            {
                // Remove from FID index
                if (_messagesByFid.TryGetValue(message.Data.Fid, out var fidMessages))
                {
                    fidMessages.TryRemove(hashKey, out _);
                }
                
                // Remove from user data index if it's a user data message
                if (message.Data.Type == (MessageType)ProtoConstants.MessageType.MessageTypeUserDataAdd &&
                    _userDataByFid.TryGetValue(message.Data.Fid, out var userDataMessages))
                {
                    userDataMessages.TryRemove(hashKey, out _);
                }
            }
            
            // Remove from parent cast index if it's a reply
            if (removed && message.Data?.Type == (MessageType)ProtoConstants.MessageType.MessageTypeCastAdd && 
                message.Data.CastAddBody?.ParentCastId != null)
            {
                var parentCastId = message.Data.CastAddBody.ParentCastId;
                string parentKey = $"{parentCastId.Fid}:{Convert.ToBase64String(parentCastId.Hash.ToByteArray())}";
                
                if (_messagesByParent.TryGetValue(parentKey, out var parentMessages))
                {
                    parentMessages.TryRemove(hashKey, out _);
                }
            }
            
            return removed;
        }
    }
} 