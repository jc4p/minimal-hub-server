# Minimal Hub Service Simulator: Design & Implementation Guide

This document outlines the approach to building a simulator for stress testing implementations of the `MinimalHubService` defined in our proto files. The goal is to create a flexible framework that can generate realistic workloads and measure performance metrics in a language-agnostic way.

## 1. Overall Architecture

### 1.1 Core Components

- **Protocol Buffer Handler**: Component for serializing/deserializing protocol buffer messages
- **Service Client**: Interface for making RPC calls to the service implementation
- **Service Implementation**: The target system being tested
- **Workload Generator**: Component for creating realistic test scenarios
- **Metrics Collector**: Component for recording performance metrics
- **Test Controller**: Orchestration component that coordinates the testing

### 1.2 Architectural Patterns

- **Producer-Consumer Pattern**: Separating message generation from submission
- **Observer Pattern**: For metrics collection without impacting test execution
- **Strategy Pattern**: To easily swap different workload generation strategies
- **Repository Pattern**: For message storage in the simulator and service implementation

### 1.3 Component Interactions

```
┌─────────────────┐      ┌────────────────┐      ┌───────────────────┐
│                 │      │                │      │                   │
│    Workload     │──────▶   Test         │──────▶  Service Client   │
│    Generator    │      │   Controller   │      │                   │
│                 │      │                │      │                   │
└─────────────────┘      └────────────────┘      └─────────┬─────────┘
                               ▲                           │
                               │                           │
                               │                           │
                         ┌─────┴───────┐           ┌───────▼─────────┐
                         │             │           │                 │
                         │   Metrics   │◀──────────┤     Service     │
                         │  Collector  │           │  Implementation │
                         │             │           │                 │
                         └─────────────┘           └─────────────────┘
```

## 2. Data Generation Strategies

### 2.1 User Profile Generation

- Generate a pool of virtual users with unique FIDs
- Create realistic user data (display names, bios, profile pictures)
- Establish a social graph (who follows whom) to drive message generation
- Example:
  ```
  UserProfile {
    fid: <unique integer>
    displayName: <random name>
    bio: <random text>
    profilePicture: <random URL>
    followingFids: [<list of fids>]
  }
  ```

### 2.2 Message Content Generation

- Generate a variety of message types (basic posts, replies, threads)
- Create realistic message text (varying lengths, mentions, threading)
- Generate timestamps with realistic distribution patterns
- Example:
  ```
  MessageTemplate {
    type: CAST_ADD
    text: <random text>
    mentions: [<list of fids>]
    timestamp: <timestamp in expected range>
    parentCastId: <optional, for replies>
  }
  ```

### 2.3 Event Generation

- Create a mix of event types (MERGE, PRUNE, REVOKE)
- Generate events that reference existing messages
- Create chains of related events
- Example:
  ```
  EventTemplate {
    type: MERGE_MESSAGE
    message: <reference to existing message>
    timestamp: <timestamp>
  }
  ```

### 2.4 Realistic Message Distribution

- Follow Zipf's law for user activity distribution (some users post a lot, most post little)
- Model conversation thread depth with a power law distribution
- Generate temporal patterns for message creation (time-of-day effects)
- Generate realistic mention patterns based on social graph

## 3. Test Scenarios

### 3.1 Basic Load Testing

- **Read-Heavy Load**: Predominately read operations (GetCasts, GetCastsByFid)
- **Write-Heavy Load**: Predominately write operations (SubmitMessage)
- **Mixed Load**: Realistic mix of read and write operations
- **Burst Load**: Short periods of extremely high load followed by normal traffic

### 3.2 Conversation Simulation

- Generate realistic conversation threads
- Model different thread depths and branching factors
- Simulate hot topics (many replies to same parent)
- Example pattern:
  ```
  A posts message -> B,C,D reply to A -> E,F reply to B -> etc.
  ```

### 3.3 User Behavior Simulation

- Active users posting frequently
- Users browsing many conversations
- Users interacting with specific conversation threads
- New user onboarding (creating profile, first messages)
- Returning users checking feed

### 3.4 Edge Cases

- Very long conversation threads (1000+ replies)
- Messages with maximum allowed content size
- Extremely active users (posting hundreds of messages)
- Rapid succession of message submissions
- Concurrent access to same conversation thread

## 4. Service Implementation 

### 4.1 In-Memory Implementation

- Store messages in appropriate in-memory data structures
- Use indexes for efficient lookups (by FID, by parent message)
- Implement validation logic
- Example data structures:
  ```
  messages_by_hash: {hash -> Message}
  messages_by_fid: {fid -> [Message]}
  messages_by_parent: {parent_hash -> [Message]}
  user_data: {fid -> {type -> UserData}}
  ```

## 5. Performance Measurement

### 5.1 Key Metrics

- **Throughput**: Messages processed per second
- **Latency**: Time to process each request (min, max, percentiles)
- **Error Rate**: Failed requests as percentage of total
- **Resource Usage**: CPU, memory, network, disk I/O
- **Concurrent Users**: Number of simultaneous users supported

## 6. Optimization Techniques

### 6.1 Connection Management

- Implement connection pooling
- Reuse connections across requests
- Balance connection pool size with server capacity
- Monitor connection establishment time

### 6.2 Request Batching

- Group related requests into batches where appropriate
- Tune batch sizes based on performance characteristics
- Implement smart batching based on request types

### 6.3 Caching Strategies

- Cache frequently accessed messages and threads
- Implement smart invalidation strategies
- Use tiered caching (memory, local storage, distributed)
- Cache query results for common access patterns

### 6.4 Concurrency Control

- Implement appropriate locking strategies
- Use optimistic concurrency for read-heavy scenarios
- Use pessimistic concurrency for write-heavy scenarios
- Partition data to minimize lock contention

## 7. Message Generation Examples

### 7.1 Cast Message Generation

```
// Pseudocode for generating a new Cast message
function generateCastMessage(fid, timestamp):
    text = generateRandomText(minLength=10, maxLength=280)
    
    // Decide if this is a reply (20% chance)
    isReply = randomChance(0.2)
    parentCastId = null
    if isReply:
        parentCastId = selectRandomExistingCast()
    
    // Maybe include mentions (30% chance)
    mentions = []
    if randomChance(0.3):
        numMentions = randomInt(1, 3)
        mentions = selectRandomUsers(numMentions, excludingSelf=fid)
    
    // Create message data
    messageData = {
        type: MESSAGE_TYPE_CAST_ADD,
        fid: fid,
        timestamp: timestamp,
        network: NETWORK_TESTNET,
        body: {
            cast_add_body: {
                text: text,
                parent_cast_id: parentCastId,
                mentions: mentions
            }
        }
    }
    
    // Create full message with hash and signature
    message = {
        data: messageData,
        hash: computeHash(messageData),
        hash_scheme: HASH_SCHEME_BLAKE3,
        signature: computeSignature(hash),
        signature_scheme: SIGNATURE_SCHEME_ED25519,
        signer: getUserPublicKey(fid)
    }
    
    return message
```

### 7.2 User Data Generation

```
// Pseudocode for generating user data messages
function generateUserDataMessages(fid):
    messages = []
    
    // Generate display name
    displayName = generateRandomName()
    messages.push(createUserDataMessage(fid, USER_DATA_TYPE_DISPLAY, displayName))
    
    // Generate bio
    bio = generateRandomBio()
    messages.push(createUserDataMessage(fid, USER_DATA_TYPE_BIO, bio))
    
    // Generate profile picture URL
    pfpUrl = generateRandomImageUrl()
    messages.push(createUserDataMessage(fid, USER_DATA_TYPE_PFP, pfpUrl))
    
    // Maybe generate URL
    if randomChance(0.7):
        url = generateRandomUrl()
        messages.push(createUserDataMessage(fid, USER_DATA_TYPE_URL, url))
    
    return messages

function createUserDataMessage(fid, dataType, value):
    // Create message data
    messageData = {
        type: MESSAGE_TYPE_USER_DATA_ADD,
        fid: fid,
        timestamp: getCurrentTimestamp(),
        network: NETWORK_TESTNET,
        body: {
            user_data_body: {
                type: dataType,
                value: value
            }
        }
    }
    
    // Create full message
    message = {
        data: messageData,
        hash: computeHash(messageData),
        hash_scheme: HASH_SCHEME_BLAKE3,
        signature: computeSignature(hash),
        signature_scheme: SIGNATURE_SCHEME_ED25519,
        signer: getUserPublicKey(fid)
    }
    
    return message
```

## 8. Test Execution

### 8.1 Basic Test Loop

```
// Pseudocode for test execution
function runTest(duration, rps):
    startTime = getCurrentTime()
    endTime = startTime + duration
    
    // Setup metrics
    latencyHistogram = createHistogram()
    throughputCounter = createCounter()
    errorCounter = createCounter()
    
    // Create user pool
    users = generateUsers(1000)
    
    // Start test loop
    while getCurrentTime() < endTime:
        // Calculate how many requests to generate
        requestsToGenerate = calculateRequestsForTimeslice(rps)
        
        // Generate and execute requests
        for i = 0 to requestsToGenerate:
            // Select random operation based on distribution
            operation = selectRandomOperation()
            
            // Select user
            user = selectUserBasedOnActivity(users)
            
            // Generate request
            request = generateRequest(operation, user)
            
            // Execute request and measure
            startRequestTime = getCurrentTimeNanos()
            try:
                response = executeRequest(operation, request)
                throughputCounter.increment()
            catch (error):
                errorCounter.increment()
            finally:
                latency = getCurrentTimeNanos() - startRequestTime
                latencyHistogram.record(latency)
        
        // Sleep if needed to maintain target RPS
        sleepToMaintainRate(rps)
    
    // Return test results
    return {
        totalRequests: throughputCounter.getValue(),
        errors: errorCounter.getValue(),
        latencyP50: latencyHistogram.getPercentile(50),
        latencyP95: latencyHistogram.getPercentile(95),
        latencyP99: latencyHistogram.getPercentile(99),
        testDuration: duration
    }
```

### 8.2 Realistic Load Patterns

```
// Pseudocode for generating realistic load patterns
function generateDailyLoadProfile():
    // 24-hour load profile where 1.0 is average load
    hourlyMultipliers = [
        0.2, 0.1, 0.1, 0.1, 0.2, 0.5,  // 0-5 hours (night, low activity)
        0.7, 1.0, 1.3, 1.5, 1.4, 1.3,  // 6-11 hours (morning, rising activity)
        1.2, 1.3, 1.4, 1.5, 1.7, 1.8,  // 12-17 hours (afternoon, high activity)
        2.0, 1.8, 1.5, 1.0, 0.7, 0.4   // 18-23 hours (evening, decreasing activity)
    ]
    
    return hourlyMultipliers

function runRealisticTest(baseRps, duration):
    loadProfile = generateDailyLoadProfile()
    startHour = getCurrentHour()
    
    results = []
    remainingDuration = duration
    
    while remainingDuration > 0:
        currentHour = (startHour + Math.floor((duration - remainingDuration) / 3600)) % 24
        hourlyMultiplier = loadProfile[currentHour]
        
        // Calculate test duration for this hour (cap at remaining or 1 hour)
        hourDuration = min(remainingDuration, 3600)
        
        // Run test with adjusted RPS
        adjustedRps = baseRps * hourlyMultiplier
        hourResults = runTest(hourDuration, adjustedRps)
        
        results.push(hourResults)
        remainingDuration -= hourDuration
    
    return aggregateResults(results)
```

## 9. Implementation Considerations

### 9.1 Thread Safety

- Ensure thread-safe access to shared data structures
- Use appropriate synchronization mechanisms
- Consider using immutable data structures where possible
- Be careful with reference sharing between threads

### 9.2 Resource Management

- Close connections properly
- Use resource pooling for expensive objects
- Implement proper cleanup during shutdown
- Monitor resource usage to detect leaks

### 9.3 Error Handling

- Implement comprehensive error handling
- Differentiate between client and server errors
- Record detailed error information for analysis
- Handle partial failures gracefully

### 9.4 Configuration Management

- Make all test parameters configurable
- Use configuration files or environment variables
- Allow runtime reconfiguration where appropriate
- Document all configuration options
