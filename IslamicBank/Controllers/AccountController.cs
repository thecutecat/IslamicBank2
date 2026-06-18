using IslamicBank.Library;
using IslamicBank.Models.ViewModels;
using IslamicBank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IslamicBank.Controllers
{
    public class AccountController:Controller
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAccountService accountService, ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        // GET: Account/Open
        [HttpGet]
        public IActionResult Open()
        { 
            var model = new AccountOpeningViewModel() { 
             CustomerId = Guid.NewGuid(),
                InitialDeposit = 1000,
                CustomerProfitRatio = 0.7m,
                BankProfitRatio = 0.3m 
            };

            // Create SelectList manually
            ViewBag.AccountTypes = new SelectList(
                Enum.GetValues(typeof(AccountTypeViewModel))
                    .Cast<AccountTypeViewModel>()
                    .Select(e => new { Id = (int)e, Name = e.ToString() }),
                "Id",
                "Name"
            );

            return View(model);
        }

        // POST: Account/Open
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(AccountOpeningViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var account = await _accountService.OpenAccountAsync(model);
                TempData["Success"] = $"Account opened successfully! Account ID: {account.Id}";
                return RedirectToAction(nameof(Details), new { id = account.Id });
            }
            catch (ShariahComplianceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        // GET: Account/Deposit
        [HttpGet]
        public IActionResult Deposit(Guid id)
        {
            return View(new DepositViewModel { AccountId = id });
        }

        // POST: Account/Deposit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(DepositViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.DepositAsync(model);

            if (result.Success)
            {
                TempData["Success"] = $"Deposited successfully. New balance: {result.NewBalance:C}";
                return RedirectToAction(nameof(Details), new { id = model.AccountId });
            }

            ModelState.AddModelError("", result.ErrorMessage);
            return View(model);
        }

        // GET: Account/Withdraw
        [HttpGet]
        public IActionResult Withdraw(Guid id)
        {
            return View(new WithdrawalViewModel { AccountId = id });
        }

        // POST: Account/Withdraw
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(WithdrawalViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.WithdrawAsync(model);

            if (result.Success)
            {
                TempData["Success"] = $"Withdrawn successfully. New balance: {result.NewBalance:C}";
                return RedirectToAction(nameof(Details), new { id = model.AccountId });
            }

            ModelState.AddModelError("", result.ErrorMessage);
            return View(model);
        }

        // GET: Account/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var balance = await _accountService.GetBalanceAsync(id);
            if (balance == null)
                return NotFound();

            return View(balance);
        }

        [HttpGet]
        public async Task<AccountBalanceViewModel> Show(Guid id)
        {
            var data = await _accountService.GetBalanceAsync(id);
            if (data == null)
                return new AccountBalanceViewModel();

            return data;
        }
    }
}
