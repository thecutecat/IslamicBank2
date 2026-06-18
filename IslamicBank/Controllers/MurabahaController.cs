using IslamicBank.Library;
using IslamicBank.Models.ViewModels;
using IslamicBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamicBank.Controllers
{
    public class MurabahaController:Controller
    {
        private readonly IMurabahaService _murabahaService;

        public MurabahaController(IMurabahaService murabahaService)
        {
            _murabahaService = murabahaService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MurabahaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MurabahaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var contract = await _murabahaService.CreateContractAsync(model);
                TempData["Success"] = $"Murabaha contract created. Contract ID: {contract.Id}";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }
            catch (ShariahComplianceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Execute(Guid id)
        {
            var contract = await _murabahaService.ExecuteContractAsync(id);
            TempData["Success"] = $"Contract executed. Selling price: {contract.SellingPrice:C}";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            // Fetch contract details from repository
            var contract = await _murabahaService.GetContractAsync(id); // You'd implement this
            return View(contract);
        }
    }
}
