using Microsoft.AspNetCore.Mvc;
using FitHub.Web.Services;

namespace FitHub.Web.Controllers
{
    public class Part2Controller : Controller
    {
        private readonly FunctionsClient _fx;

        public Part2Controller(FunctionsClient fx) => _fx = fx;

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> SeedProduct()
        {
            var res = await _fx.CreateProductAsync(new
            {
                name = "Turbo Tyres",
                sku = "TYR-001",
                price = 1999.00,
                category = "Wheels",
                imageBlobName = "racecar-1.jpg",
                notes = "Seeded from MVC"
            });
            TempData["SeedProduct"] = await FormatAsync(res);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UploadBlob()
        {
            var res = await _fx.UploadBlobFromUrlAsync(new
            {
                fileName = "racecar-1.jpg",
                fileUrl = "https://picsum.photos/seed/racecar/1200/800"
            });
            TempData["UploadBlob"] = await FormatAsync(res);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Enqueue()
        {
            var res = await _fx.EnqueueAsync(new
            {
                type = "ORDER_PLACED",
                sku = "TYR-001",
                quantity = 2,
                timestampUtc = DateTime.UtcNow.ToString("o"),
                notes = "Enqueued from MVC"
            });
            TempData["Enqueue"] = await FormatAsync(res);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> WriteFile()
        {
            var res = await _fx.WriteFileShareAsync(new
            {
                directory = $"receipts/{DateTime.UtcNow:yyyy-MM-dd}",
                fileName = "order-TYR-001.txt",
                contentText = "Order TYR-001 x2 placed from MVC after environment rebuild."
            });
            TempData["WriteFile"] = await FormatAsync(res);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> RunAll()
        {
            await SeedProduct();
            await UploadBlob();
            await Enqueue();
            await WriteFile();
            return RedirectToAction(nameof(Index));
        }

        private static async Task<string> FormatAsync(HttpResponseMessage res)
        {
            var body = await res.Content.ReadAsStringAsync();
            return $"{(int)res.StatusCode} {res.ReasonPhrase} | {Truncate(body, 300)}";
        }

        private static string Truncate(string s, int len)
            => string.IsNullOrEmpty(s) ? "" : (s.Length <= len ? s : s.Substring(0, len) + "...");
    }
}
