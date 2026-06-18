namespace IslamicBank.Services
{
    public interface ICurrentUserService
    {
        int GetCurrentUserId();
        string GetCurrentUserName();
        string GetCurrentUserEmail();
        bool IsAuthenticated();
        string GetUserRole();
    }
}
