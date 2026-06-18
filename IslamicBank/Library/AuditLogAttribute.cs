namespace IslamicBank.Library
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AuditLogAttribute : Attribute
    {
        public string ActionType { get; set; }
        public bool SensitiveData { get; set; } = false; // Default to masking sensitive data

        public AuditLogAttribute(string actionType)
        {
            ActionType = actionType;
        }
    }
}
