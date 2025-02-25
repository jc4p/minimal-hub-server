using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using HubSimulator.Domain.Protos;

namespace HubSimulator.Domain.Repositories
{
    /// <summary>
    /// In-memory implementation of the hub event repository
    /// </summary>
    public class InMemoryHubEventRepository : IHubEventRepository
    {
        // Main storage for events indexed by ID
        private readonly ConcurrentDictionary<ulong, HubEvent> _eventsById = new();
        
        // Auto-incrementing ID counter
        private long _lastEventId = 0;
        
        /// <summary>
        /// Store a hub event in the repository
        /// </summary>
        public HubEvent StoreEvent(HubEvent hubEvent)
        {
            if (hubEvent == null)
            {
                throw new ArgumentNullException(nameof(hubEvent));
            }
            
            // Use the provided ID if it's already set, otherwise generate a new one
            if (hubEvent.Id == 0)
            {
                hubEvent.Id = GetNextEventId();
            }
            
            _eventsById[hubEvent.Id] = hubEvent;
            return hubEvent;
        }
        
        /// <summary>
        /// Get a hub event by its ID
        /// </summary>
        public HubEvent? GetEventById(ulong id)
        {
            if (id == 0)
            {
                return null;
            }
            
            _eventsById.TryGetValue(id, out var hubEvent);
            return hubEvent;
        }
        
        /// <summary>
        /// Get the next available event ID
        /// </summary>
        public ulong GetNextEventId()
        {
            return (ulong)Interlocked.Increment(ref _lastEventId);
        }
    }
} 