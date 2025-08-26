using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs.Models;

namespace FitHub.Web.Controllers
{
    public class FilesController : Controller
    {
        private readonly StorageFactory _sf;
        private const string ContainerName = "class-images1"; // <-- your container

        public FilesController(StorageFactory sf) => _sf = sf;

        // GET: /Files
        public async Task<IActionResult> Index()
        {
            var container = _sf.Blob(ContainerName);
            var items = new List<(string Name, string Url)>();

            await foreach (BlobItem blob in container.GetBlobsAsync())
            {
                var client = container.GetBlobClient(blob.Name);
                items.Add((blob.Name, client.Uri.ToString()));
            }

            return View(items);
        }

        // GET: /Files/Upload
        [HttpGet]
        public IActionResult Upload() => View();

        // POST: /Files/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25_000_000)] // 25MB
        public async Task<IActionResult> Upload(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                ModelState.AddModelError("", "Please choose an image file.");
                return View();
            }

            var container = _sf.Blob(ContainerName);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var blob = container.GetBlobClient(fileName);

            using var s = image.OpenReadStream();
            await blob.UploadAsync(s, overwrite: false);

            TempData["ok"] = "Image uploaded.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Files/Delete?name=<blobName>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));
            var container = _sf.Blob(ContainerName);
            await container.DeleteBlobIfExistsAsync(name);
            TempData["ok"] = $"Deleted {name}";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Download(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));

            var container = _sf.Blob("class-images1");
            var blob = container.GetBlobClient(name);
            if (!await blob.ExistsAsync()) return RedirectToAction(nameof(Index));

            var resp = await blob.DownloadContentAsync();
            var bytes = resp.Value.Content.ToArray();
            var contentType = resp.Value.Details.ContentType ?? "application/octet-stream";
            return File(bytes, contentType, fileDownloadName: name);
        }
    }
}