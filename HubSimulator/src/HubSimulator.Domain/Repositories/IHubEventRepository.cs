using System;
using System.Collections.Generic;
using Google.Protobuf;
using HubSimulator.Domain.Protos;

namespace HubSimulator.Domain.Repositories
{
    /// <summary>
    /// Interface for managing hub events in the service
    /// </summary>
    public interface IHubEventRepository
    {
        /// <summary>
        /// Store a hub event in the repository
        /// </summary>
        /// <param name="hubEvent">The hub event to store</param>
        /// <returns>The stored hub event</returns>
        HubEvent StoreEvent(HubEvent hubEvent);
        
        /// <summary>
        /// Get a hub event by its ID
        /// </summary>
        /// <param name="id">The ID of the event to retrieve</param>
        /// <returns>The hub event if found, null otherwise</returns>
        HubEvent? GetEventById(ulong id);
        
        /// <summary>
        /// Get the next available event ID (for auto-incrementing IDs)
        /// </summary>
        /// <returns>The next available event ID</returns>
        ulong GetNextEventId();
    }
} 