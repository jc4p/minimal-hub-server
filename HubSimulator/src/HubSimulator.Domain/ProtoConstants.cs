namespace HubSimulator.Domain
{
    /// <summary>
    /// Constants from the proto files to use in our code
    /// </summary>
    public static class ProtoConstants
    {
        public static class MessageType
        {
            public const int MessageTypeNone = 0;
            public const int MessageTypeCastAdd = 1;
            public const int MessageTypeCastRemove = 2;
            public const int MessageTypeUserDataAdd = 3;
        }
        
        public static class Network
        {
            public const int NetworkNone = 0;
            public const int NetworkMainnet = 1;
            public const int NetworkTestnet = 2;
            public const int NetworkDevnet = 3;
        }
        
        public static class UserDataType
        {
            public const int UserDataTypeNone = 0;
            public const int UserDataTypePfp = 1;
            public const int UserDataTypeDisplay = 2;
            public const int UserDataTypeBio = 3;
            public const int UserDataTypeUrl = 4;
        }
        
        public static class HubEventType
        {
            public const int HubEventTypeNone = 0;
            public const int HubEventTypeMergeMessage = 1;
            public const int HubEventTypePruneMessage = 2;
            public const int HubEventTypeRevokeMessage = 3;
        }
        
        public static class HashScheme
        {
            public const int HashSchemeNone = 0;
            public const int HashSchemeBlake3 = 1;
        }
        
        public static class SignatureScheme
        {
            public const int SignatureSchemeNone = 0;
            public const int SignatureSchemeEd25519 = 1;
        }
    }
} 