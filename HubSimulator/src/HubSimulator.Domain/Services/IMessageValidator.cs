using System;
using HubSimulator.Domain.Protos;

namespace HubSimulator.Domain.Services
{
    /// <summary>
    /// Interface for validating messages
    /// </summary>
    public interface IMessageValidator
    {
        /// <summary>
        /// Validates a message
        /// </summary>
        /// <param name="message">The message to validate</param>
        /// <returns>A tuple containing a boolean indicating if the message is valid, and a message explaining why if it's not</returns>
        (bool IsValid, string? ValidationMessage) ValidateMessage(Message message);
    }
} 