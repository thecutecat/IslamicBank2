namespace IslamicBank.Library
{
    public class ShariahComplianceException : Exception
    {
        public ShariahComplianceException(string message) : base(message)
        {
        }   
    }

    public class InsufficientFundsException : Exception
    {
        public InsufficientFundsException(string message) : base(message) { }
    }
}
