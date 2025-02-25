using System;
using System.Collections.Generic;
using Google.Protobuf;
using HubSimulator.Domain.Protos;

namespace HubSimulator.Domain.Services
{
    /// <summary>
    /// Interface for hub service operations
    /// </summary>
    public interface IHubService
    {
        /// <summary>
        /// Submit a message to the hub
        /// </summary>
        /// <param name="message">The message to submit</param>
        /// <returns>The stored message</returns>
        Message SubmitMessage(Message message);
        
        /// <summary>
        /// Validate a message without storing it
        /// </summary>
        /// <param name="message">The message to validate</param>
        /// <returns>Validation response with result and error message if applicable</returns>
        ValidationResponse ValidateMessage(Message message);
        
        /// <summary>
        /// Get a hub event by ID
        /// </summary>
        /// <param name="id">The ID of the event to retrieve</param>
        /// <returns>The hub event if found, null otherwise</returns>
        HubEvent? GetEvent(ulong id);
        
        /// <summary>
        /// Get a cast by ID
        /// </summary>
        /// <param name="castId">The cast ID to retrieve</param>
        /// <returns>The cast message if found, null otherwise</returns>
        Message? GetCast(CastId castId);
        
        /// <summary>
        /// Get casts by FID
        /// </summary>
        /// <param name="fid">The FID to get casts for</param>
        /// <param name="pageSize">Optional page size for pagination</param>
        /// <param name="pageToken">Optional page token for pagination</param>
        /// <param name="reverse">Whether to sort in reverse order</param>
        /// <returns>A response containing the messages and next page token</returns>
        MessagesResponse GetCastsByFid(ulong fid, int pageSize = 10, ByteString? pageToken = null, bool reverse = false);
        
        /// <summary>
        /// Get casts by parent
        /// </summary>
        /// <param name="parentCastId">The parent cast ID to get replies for</param>
        /// <param name="pageSize">Optional page size for pagination</param>
        /// <param name="pageToken">Optional page token for pagination</param>
        /// <param name="reverse">Whether to sort in reverse order</param>
        /// <returns>A response containing the messages and next page token</returns>
        MessagesResponse GetCastsByParent(CastId parentCastId, int pageSize = 10, ByteString? pageToken = null, bool reverse = false);
        
        /// <summary>
        /// Get user data by FID
        /// </summary>
        /// <param name="fid">The FID to get user data for</param>
        /// <returns>A response containing the user data messages</returns>
        MessagesResponse GetUserDataByFid(ulong fid);
        
        /// <summary>
        /// Get hub information
        /// </summary>
        /// <param name="includeDbStats">Whether to include database statistics</param>
        /// <returns>Hub information response</returns>
        HubInfoResponse GetHubInfo(bool includeDbStats);
    }
} 