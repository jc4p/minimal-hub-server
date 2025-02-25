using System;
using System.Collections.Generic;
using Google.Protobuf;
using HubSimulator.Domain.Protos;

namespace HubSimulator.Domain.Repositories
{
    /// <summary>
    /// Interface for managing messages in the hub service
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>
        /// Store a message in the repository
        /// </summary>
        /// <param name="message">The message to store</param>
        /// <returns>The stored message</returns>
        Message StoreMessage(Message message);
        
        /// <summary>
        /// Get a message by its hash
        /// </summary>
        /// <param name="hash">The hash of the message to retrieve</param>
        /// <returns>The message if found, null otherwise</returns>
        Message? GetMessageByHash(ByteString hash);
        
        /// <summary>
        /// Get messages by FID (user identifier)
        /// </summary>
        /// <param name="fid">The FID to get messages for</param>
        /// <param name="pageSize">Optional page size for pagination</param>
        /// <param name="pageToken">Optional page token for pagination</param>
        /// <param name="reverse">Whether to sort in reverse order</param>
        /// <returns>A tuple containing the list of messages and the next page token</returns>
        (List<Message>, ByteString?) GetMessagesByFid(ulong fid, int pageSize = 10, ByteString? pageToken = null, bool reverse = false);
        
        /// <summary>
        /// Get messages by parent cast (for thread replies)
        /// </summary>
        /// <param name="parentCastId">The parent cast ID to get replies for</param>
        /// <param name="pageSize">Optional page size for pagination</param>
        /// <param name="pageToken">Optional page token for pagination</param>
        /// <param name="reverse">Whether to sort in reverse order</param>
        /// <returns>A tuple containing the list of messages and the next page token</returns>
        (List<Message>, ByteString?) GetMessagesByParent(CastId parentCastId, int pageSize = 10, ByteString? pageToken = null, bool reverse = false);
        
        /// <summary>
        /// Get user data messages for a specific FID
        /// </summary>
        /// <param name="fid">The FID to get user data for</param>
        /// <returns>The list of user data messages</returns>
        List<Message> GetUserDataByFid(ulong fid);
        
        /// <summary>
        /// Remove a message by its hash
        /// </summary>
        /// <param name="hash">The hash of the message to remove</param>
        /// <returns>True if the message was removed, false otherwise</returns>
        bool RemoveMessage(ByteString hash);
    }
} 