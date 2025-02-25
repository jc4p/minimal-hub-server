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
    /// Generates time-based test data with realistic growth patterns
    /// </summary>
    public class TimeBasedDataGenerator
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IHubEventRepository _eventRepository;
        private readonly ILogger<TimeBasedDataGenerator> _logger;
        private readonly Random _random = new Random();
        
        // Default parameters
        private const int DefaultTotalUsers = 50000;
        private const int DefaultMonths = 18;
        private const int DefaultInitialActiveUsers = 200;
        private const int DefaultFinalActiveUsers = 10000;
        private const int DefaultMaxPostsPerActiveUserPerDay = 5;
        private const double DefaultReplyProbability = 0.4;
        private const int DefaultMaxRepliesPerPost = 10;
        private const int DefaultMaxRepliesDepth = 3;
        
        // User activity growth model parameters
        private readonly Dictionary<int, double> _monthlyActivityProbability = new Dictionary<int, double>();
        
        public TimeBasedDataGenerator(
            IMessageRepository messageRepository,
            IHubEventRepository eventRepository,
            ILogger<TimeBasedDataGenerator> logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Generate time-based data simulating 18 months of user activity with growth
        /// </summary>
        public async Task GenerateTimeBasedDataAsync(
            Action<string, int>? progressCallback = null,
            int totalUsers = DefaultTotalUsers,
            int months = DefaultMonths,
            int initialActiveUsers = DefaultInitialActiveUsers,
            int finalActiveUsers = DefaultFinalActiveUsers,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating time-based data: {TotalUsers} users over {Months} months, growing from {InitialActive} to {FinalActive} active users",
                totalUsers, months, initialActiveUsers, finalActiveUsers);
            
            // Initialize growth model
            InitializeGrowthModel(months, initialActiveUsers, finalActiveUsers, totalUsers);
            
            // Calculate total work for progress tracking
            DateTime startDate = DateTime.UtcNow.AddMonths(-months);
            DateTime endDate = DateTime.UtcNow;
            int totalDays = (int)(endDate - startDate).TotalDays;
            int approximateWorkItems = totalUsers + (totalDays * (initialActiveUsers + finalActiveUsers) / 2);
            int currentWorkItem = 0;
            
            progressCallback?.Invoke("Starting time-based data generation", 0);
            
            // Generate all users first
            _logger.LogInformation("Generating {Count} user profiles", totalUsers);
            var users = new List<UserProfile>();
            
            for (int i = 1; i <= totalUsers; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                // Generate a user with a join date within the time period
                // Earlier users are more likely to be active
                int dayOffset = _random.Next(totalDays);
                DateTime joinDate = startDate.AddDays(dayOffset);
                
                var user = new UserProfile
                {
                    Fid = (ulong)i,
                    DisplayName = $"User{i}",
                    Bio = $"This is the bio for user {i}",
                    ProfilePicUrl = $"https://example.com/profiles/{i}.jpg",
                    Url = $"https://example.com/users/{i}",
                    JoinDate = joinDate,
                    // Earlier users tend to be more active
                    ActivityLevel = Math.Max(0.1, 1.0 - (dayOffset / (double)totalDays))
                };
                
                users.Add(user);
                
                // Generate user data messages with the proper timestamp
                GenerateUserDataMessages(user, joinDate);
                
                // Update progress
                currentWorkItem++;
                int percentComplete = (int)((double)currentWorkItem / approximateWorkItems * 100);
                progressCallback?.Invoke($"Generated user {i}/{totalUsers}", percentComplete);
                
                // Yield to other tasks occasionally
                if (i % 100 == 0)
                {
                    await Task.Delay(5, cancellationToken);
                }
            }
            
            // Now generate activity day by day
            _logger.LogInformation("Generating daily activity across {Days} days", totalDays);
            
            // Track post interactions for generating replies
            var postsForReplies = new List<(Message Message, DateTime PostTime)>();
            var messagesToReplyTo = new Dictionary<string, (Message Message, DateTime PostTime)>();
            
            // For each day in the time period
            for (int day = 0; day < totalDays; day++)
            {
                // Current date
                DateTime currentDate = startDate.AddDays(day);
                int currentMonth = ((currentDate.Year - startDate.Year) * 12) + currentDate.Month - startDate.Month;
                
                // Get active users for this day based on growth model
                double activityProb = GetActivityProbabilityForMonth(currentMonth);
                int targetActiveUsersForDay = GetActiveUsersForDay(currentMonth, initialActiveUsers, finalActiveUsers, months);
                
                // Track posts made on this day
                var postsOnThisDay = new List<(Message Message, DateTime PostTime)>();
                
                // For each user, decide if they post today
                int activeUserCount = 0;
                foreach (var user in users)
                {
                    // Skip users who haven't joined yet
                    if (user.JoinDate > currentDate)
                    {
                        continue;
                    }
                    
                    // Check if this user is active today
                    double userActivityProb = activityProb * user.ActivityLevel;
                    if (_random.NextDouble() < userActivityProb)
                    {
                        // User is active today, generate 1-5 posts
                        int postsToday = _random.Next(1, DefaultMaxPostsPerActiveUserPerDay + 1);
                        
                        for (int p = 0; p < postsToday; p++)
                        {
                            // Create timestamp within the current day
                            DateTime postTime = currentDate.AddHours(_random.Next(24)).AddMinutes(_random.Next(60));
                            uint timestamp = (uint)new DateTimeOffset(postTime).ToUnixTimeSeconds();
                            
                            // Create the post
                            string text = GenerateRandomText($"Post on {postTime:yyyy-MM-dd} by user {user.Fid}");
                            var message = CreateCastMessage(user.Fid, text, null, timestamp);
                            StoreMessage(message);
                            
                            // Add to today's posts and to posts available for replies
                            postsOnThisDay.Add((message, postTime));
                            
                            // Keep recent posts (last 30 days) available for replies
                            if (postsForReplies.Count > 10000)
                            {
                                // Remove oldest posts if we have too many
                                postsForReplies.RemoveAt(0);
                            }
                        }
                        
                        activeUserCount++;
                        if (activeUserCount >= targetActiveUsersForDay)
                        {
                            // We've reached our target active users for today
                            break;
                        }
                    }
                }
                
                // Add today's posts to the pool for future replies
                postsForReplies.AddRange(postsOnThisDay);
                
                // Generate replies to posts
                if (postsForReplies.Count > 0)
                {
                    await GenerateRepliesForDayAsync(users, postsForReplies, currentDate, cancellationToken);
                }
                
                // Update progress
                currentWorkItem += activeUserCount;
                int percentComplete = (int)((double)currentWorkItem / approximateWorkItems * 100);
                progressCallback?.Invoke($"Generated activity for day {day+1}/{totalDays}: {activeUserCount} active users", percentComplete);
                
                // Yield to other tasks occasionally
                await Task.Delay(5, cancellationToken);
            }
            
            _logger.LogInformation("Time-based data generation complete");
            progressCallback?.Invoke("Time-based data generation complete", 100);
        }
        
        /// <summary>
        /// Generate replies to posts for a specific day
        /// </summary>
        private async Task GenerateRepliesForDayAsync(
            List<UserProfile> users,
            List<(Message Message, DateTime PostTime)> availablePosts,
            DateTime currentDate,
            CancellationToken cancellationToken)
        {
            // Select random posts to reply to (posts made in the last 30 days are candidates)
            var recentPosts = availablePosts.FindAll(p => (currentDate - p.PostTime).TotalDays <= 30);
            if (recentPosts.Count == 0)
            {
                return;
            }
            
            // Determine how many posts get replies today
            int postsToReplyTo = Math.Min(recentPosts.Count, Math.Max(5, recentPosts.Count / 10));
            
            // Track posts that get replies for later potential nested replies
            var postsWithNewReplies = new List<(Message Reply, DateTime ReplyTime)>();
            
            for (int i = 0; i < postsToReplyTo; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                // Select a random post to reply to
                int postIndex = _random.Next(recentPosts.Count);
                var (parentMessage, parentTime) = recentPosts[postIndex];
                
                // More recent posts are more likely to get more replies
                double daysAgo = (currentDate - parentTime).TotalDays;
                int repliesCount = _random.Next(1, Math.Max(2, (int)(DefaultMaxRepliesPerPost * Math.Exp(-daysAgo / 7))));
                
                for (int r = 0; r < repliesCount; r++)
                {
                    // Find a random user to make the reply
                    var randomUserIndex = _random.Next(users.Count);
                    var replyingUser = users[randomUserIndex];
                    
                    // Skip if user hasn't joined yet
                    if (replyingUser.JoinDate > currentDate)
                    {
                        continue;
                    }
                    
                    // Create timestamp for reply (always after parent post)
                    DateTime replyTime;
                    int minutesSinceParent = Math.Max(1, (int)(currentDate - parentTime).TotalMinutes);
                    
                    if (minutesSinceParent > 1)
                    {
                        replyTime = parentTime.AddMinutes(_random.Next(1, minutesSinceParent));
                    }
                    else
                    {
                        // If the difference is too small, just add 1 minute
                        replyTime = parentTime.AddMinutes(1);
                    }
                    
                    if (replyTime > currentDate)
                    {
                        replyTime = currentDate.AddHours(-1).AddMinutes(_random.Next(60));
                    }
                    
                    uint timestamp = (uint)new DateTimeOffset(replyTime).ToUnixTimeSeconds();
                    
                    // Create the reply
                    var parentCastId = new CastId
                    {
                        Fid = parentMessage.Data.Fid,
                        Hash = parentMessage.Hash
                    };
                    
                    string replyText = GenerateRandomText($"Reply to post by user {parentMessage.Data.Fid}");
                    var reply = CreateCastMessage(replyingUser.Fid, replyText, parentCastId, timestamp);
                    StoreMessage(reply);
                    
                    // Add this reply to possible future reply parents
                    postsWithNewReplies.Add((reply, replyTime));
                }
            }
            
            // 30% chance to generate nested replies (replies to replies)
            if (postsWithNewReplies.Count > 0 && _random.NextDouble() < 0.3)
            {
                // Determine how many nested replies
                int nestedReplyCount = Math.Min(postsWithNewReplies.Count, _random.Next(1, 10));
                
                for (int n = 0; n < nestedReplyCount; n++)
                {
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Select a random reply to respond to
                    int replyIndex = _random.Next(postsWithNewReplies.Count);
                    var (parentReply, parentReplyTime) = postsWithNewReplies[replyIndex];
                    
                    // Find a random user to make the nested reply
                    var randomUserIndex = _random.Next(users.Count);
                    var replyingUser = users[randomUserIndex];
                    
                    // Skip if user hasn't joined yet
                    if (replyingUser.JoinDate > currentDate)
                    {
                        continue;
                    }
                    
                    // Create timestamp for nested reply (always after parent reply)
                    DateTime nestedReplyTime;
                    int minutesSinceParentReply = Math.Max(1, (int)(currentDate - parentReplyTime).TotalMinutes);
                    
                    if (minutesSinceParentReply > 1)
                    {
                        nestedReplyTime = parentReplyTime.AddMinutes(_random.Next(1, minutesSinceParentReply));
                    }
                    else
                    {
                        // If the difference is too small, just add 1 minute
                        nestedReplyTime = parentReplyTime.AddMinutes(1);
                    }
                    
                    if (nestedReplyTime > currentDate)
                    {
                        nestedReplyTime = currentDate.AddHours(-1).AddMinutes(_random.Next(60));
                    }
                    
                    uint timestamp = (uint)new DateTimeOffset(nestedReplyTime).ToUnixTimeSeconds();
                    
                    // Create the nested reply
                    var parentCastId = new CastId
                    {
                        Fid = parentReply.Data.Fid,
                        Hash = parentReply.Hash
                    };
                    
                    string replyText = GenerateRandomText($"Nested reply to user {parentReply.Data.Fid}");
                    var nestedReply = CreateCastMessage(replyingUser.Fid, replyText, parentCastId, timestamp);
                    StoreMessage(nestedReply);
                }
            }
            
            // Yield to other tasks occasionally
            await Task.Delay(1, cancellationToken);
        }
        
        /// <summary>
        /// Generate a realistic random post text
        /// </summary>
        private string GenerateRandomText(string baseText)
        {
            string[] randomPhrases = new string[]
            {
                "Just had a great idea about this!",
                "Has anyone else experienced this?",
                "Thinking about the implications of this.",
                "Not sure if I agree, but interesting nonetheless.",
                "This is game-changing!",
                "I've been saying this for years.",
                "First time posting about this topic.",
                "What are your thoughts on this?",
                "Let me know if you've tried this before.",
                "Controversial opinion perhaps, but worth discussing.",
                "Really excited about this new development!",
                "Need some advice on this situation.",
                "Sharing this because it helped me a lot.",
                "Can't believe this is happening!",
                "Thanks for the earlier discussion on this topic."
            };
            
            // Sometimes include hashtags
            string[] randomHashtags = new string[]
            {
                "#technology", "#innovation", "#discussion", "#thoughts", "#newbie", 
                "#learning", "#advice", "#help", "#trending", "#viral", 
                "#interesting", "#today", "#question", "#community", "#sharing"
            };
            
            // Build the message
            string text = randomPhrases[_random.Next(randomPhrases.Length)];
            
            // 40% chance to add hashtags
            if (_random.NextDouble() < 0.4)
            {
                int hashtagCount = _random.Next(1, 3);
                for (int i = 0; i < hashtagCount; i++)
                {
                    text += " " + randomHashtags[_random.Next(randomHashtags.Length)];
                }
            }
            
            // 20% chance to add a reference to the base text
            if (_random.NextDouble() < 0.2)
            {
                text += " | " + baseText;
            }
            
            return text;
        }
        
        /// <summary>
        /// Generate user data messages with proper creation timestamp
        /// </summary>
        private void GenerateUserDataMessages(UserProfile user, DateTime joinDate)
        {
            uint timestamp = (uint)new DateTimeOffset(joinDate).ToUnixTimeSeconds();
            
            // Display name
            var displayNameMessage = CreateUserDataMessage(user.Fid, (UserDataType)ProtoConstants.UserDataType.UserDataTypeDisplay, user.DisplayName, timestamp);
            StoreMessage(displayNameMessage);
            
            // Bio
            var bioMessage = CreateUserDataMessage(user.Fid, (UserDataType)ProtoConstants.UserDataType.UserDataTypeBio, user.Bio, timestamp);
            StoreMessage(bioMessage);
            
            // Profile picture
            var pfpMessage = CreateUserDataMessage(user.Fid, (UserDataType)ProtoConstants.UserDataType.UserDataTypePfp, user.ProfilePicUrl, timestamp);
            StoreMessage(pfpMessage);
            
            // URL
            var urlMessage = CreateUserDataMessage(user.Fid, (UserDataType)ProtoConstants.UserDataType.UserDataTypeUrl, user.Url, timestamp);
            StoreMessage(urlMessage);
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
        /// Create a new cast message with specified timestamp
        /// </summary>
        private Message CreateCastMessage(ulong fid, string text, CastId? parentCastId, uint timestamp)
        {
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
                    ulong mentionFid = (ulong)_random.Next(1, (int)fid + 100);
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
        /// Create a user data message with specified timestamp
        /// </summary>
        private Message CreateUserDataMessage(ulong fid, UserDataType type, string value, uint timestamp)
        {
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
        /// Initialize the growth model for user activity over time
        /// </summary>
        private void InitializeGrowthModel(int months, int initialActiveUsers, int finalActiveUsers, int totalUsers)
        {
            // We'll use an S-curve growth model
            for (int month = 0; month < months; month++)
            {
                // S-curve factor (0 to 1)
                double growthFactor;
                if (month < months / 3)
                {
                    // Slow initial growth
                    growthFactor = Math.Pow(month / (double)(months / 3), 2) * 0.3;
                }
                else if (month < 2 * months / 3)
                {
                    // Rapid middle growth
                    double normalizedMonth = (month - months / 3) / (double)(months / 3);
                    growthFactor = 0.3 + normalizedMonth * 0.5;
                }
                else
                {
                    // Slowing final growth
                    double normalizedMonth = (month - 2 * months / 3) / (double)(months / 3);
                    growthFactor = 0.8 + (Math.Sin(normalizedMonth * Math.PI / 2 - Math.PI / 2) + 1) * 0.1;
                }
                
                // Mapping the growth factor to active users by month
                int activeUsersForMonth = (int)(initialActiveUsers + (finalActiveUsers - initialActiveUsers) * growthFactor);
                
                // Probability of a user being active on any given day
                _monthlyActivityProbability[month] = Math.Min(1.0, activeUsersForMonth * 1.5 / (double)totalUsers);
            }
        }
        
        /// <summary>
        /// Get the activity probability for a specific month
        /// </summary>
        private double GetActivityProbabilityForMonth(int month)
        {
            if (_monthlyActivityProbability.TryGetValue(month, out double probability))
            {
                return probability;
            }
            
            // Default for months outside our model
            return _monthlyActivityProbability.TryGetValue(_monthlyActivityProbability.Count - 1, out double lastProb) 
                ? lastProb 
                : 0.01;
        }
        
        /// <summary>
        /// Get the number of active users for a specific day based on the month
        /// </summary>
        private int GetActiveUsersForDay(int month, int initialActiveUsers, int finalActiveUsers, int totalMonths)
        {
            // Cap to last month if we're beyond
            if (month >= totalMonths)
            {
                month = totalMonths - 1;
            }
            
            // Simple S-curve growth model
            double progress = month / (double)(totalMonths - 1);
            double growthFactor = 1 / (1 + Math.Exp(-10 * (progress - 0.5)));
            
            int targetActiveUsers = (int)(initialActiveUsers + (finalActiveUsers - initialActiveUsers) * growthFactor);
            
            // Add some daily variation (±15%)
            double variation = 0.85 + (_random.NextDouble() * 0.3);
            return (int)(targetActiveUsers * variation);
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
            public DateTime JoinDate { get; set; }
            public double ActivityLevel { get; set; } = 0.5; // 0.0-1.0 representing how active a user is
        }
    }
} 