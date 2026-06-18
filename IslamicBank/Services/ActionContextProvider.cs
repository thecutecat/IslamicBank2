using Microsoft.AspNetCore.Mvc.Filters;
using static IslamicBank.Services.ActionContextProvider;

namespace IslamicBank.Services
{
    public class ActionContextProvider : IActionContextProvider
    {
        private readonly AsyncLocal<ActionExecutingContext> _currentContext = new();

        public void SetActionExecutingContext(ActionExecutingContext context)
        {
            _currentContext.Value = context;
        }

        public ActionExecutingContext? GetActionExecutingContext()
        {
            return _currentContext.Value;
        }

        public void Clear()
        {
            _currentContext.Value = null;
        }

    }
        public interface IActionContextProvider
    {
        void SetActionExecutingContext(ActionExecutingContext context);
        ActionExecutingContext? GetActionExecutingContext();
        void Clear();
    }
}
