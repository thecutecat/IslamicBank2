using System.Security.Claims;
using Microsoft.AspNetCore.Http; 

namespace IslamicBank.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
                return userId;

            // For development/testing when not authenticated
            return 10;
        }

        public string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.Name)?.Value ?? "System";
        }

        public string GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.Email)?.Value ?? "system@internal.com";//string.Empty;
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?
                .Identity?.IsAuthenticated ?? false;
        }

        public string GetUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.Role)?.Value ?? "Guest";
        }
    }
}