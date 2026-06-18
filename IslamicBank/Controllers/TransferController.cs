using IslamicBank.Library;
using IslamicBank.Models.ViewModels;
using IslamicBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamicBank.Controllers
{
    public class TransferController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IAuditService _auditService;

        public TransferController(IAccountService accountService, IAuditService auditService)
        {
            _accountService = accountService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var accounts = await _accountService.GetListAccountSummaryAsync();
            ViewBag.UserAccounts = accounts;
            return View(new TransferViewModel());
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog("TRANSFER")]
        public async Task<IActionResult> Create(TransferViewModel model)
        {
            await _auditService.LogTransferStart(model);

            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.TransferAsync(model);

            if (result.Success)
            {
                await _auditService.LogTransferSuccess(model, result.TransferId.ToString());

                TempData["Success"] = $"Transfer completed. Transfer ID: {result.TransferId}";
                return RedirectToAction("Details", "Account", new { id = model.FromAccountId });
            }

            ModelState.AddModelError("", result.ErrorMessage);
            return View(model);
        }

        //API to get Transfer Daily Total for an account
        [HttpGet]
        public async Task<TransferDailyTotalViewModel> dailytotal(Guid id, DateTime _dateTime)
        {
            if (_dateTime == default) _dateTime = DateTime.UtcNow;

            var transferDetails = await _accountService.GetTransferDailyTotalAsync(id, _dateTime);
            if (transferDetails == null)
                return new TransferDailyTotalViewModel() { Date = _dateTime.Date, FromAccountId = id, TotalAmount = 0 };
            return (transferDetails);
        }

        //API to get latest Transfer 
        [HttpGet]
        public async Task<TransferViewModel> RecentTransferTransaction(Guid id)
        {
            var transferDetails = await _accountService.GetRecentTransferTransactionAsync(id);
            if (transferDetails == null) {
                return new TransferViewModel(); 
            }
            return new TransferViewModel() { Amount = transferDetails.Amount, FromAccountId = transferDetails.FromAccountId, ToAccountId = transferDetails.FromAccountId }; //  transferDetails; 
        }
    }
}
