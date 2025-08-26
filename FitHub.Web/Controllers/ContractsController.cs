using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using FitHub.Web;
using FitHub.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Web.Controllers
{
    public class ContractsController : Controller
    {
        private readonly StorageFactory _sf;
        private const string ShareName = "contracts";

        public ContractsController(StorageFactory sf) => _sf = sf;

        // GET: /Contracts
        public async Task<IActionResult> Index()
        {
            var share = _sf.Share(ShareName);
            var root = share.GetRootDirectoryClient();

            var list = new List<ContractFile>();
            await foreach (ShareFileItem item in root.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    var file = root.GetFileClient(item.Name);
                    ShareFileProperties props = (await file.GetPropertiesAsync()).Value;

                    list.Add(new ContractFile
                    {
                        Name = item.Name,
                        // In your SDK, ContentLength is non-nullable long
                        Length = props.ContentLength,
                        LastModified = props.LastModified
                    });
                }
            }

            return View(list.OrderBy(f => f.Name).ToList());
        }

        // POST: /Contracts/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ok"] = "Please choose a file.";
                return RedirectToAction(nameof(Index));
            }

            var share = _sf.Share(ShareName);
            var root = share.GetRootDirectoryClient();
            var fileClient = root.GetFileClient(file.FileName);

            // Create then set headers (signature expects options in your SDK)
            await fileClient.CreateAsync(file.Length);
            await fileClient.SetHttpHeadersAsync(new ShareFileSetHttpHeadersOptions
            {
                HttpHeaders = new ShareFileHttpHeaders
                {
                    ContentType = file.ContentType ?? "application/octet-stream"
                }
            });

            using var s = file.OpenReadStream();
            await fileClient.UploadRangeAsync(new HttpRange(0, s.Length), s);

            TempData["ok"] = $"Uploaded {file.FileName}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Contracts/Download?name=Contract1.pdf
        [HttpGet]
        public async Task<IActionResult> Download(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return NotFound();

            var fileClient = _sf.Share(ShareName).GetRootDirectoryClient().GetFileClient(name);
            if (!await fileClient.ExistsAsync()) return NotFound();

            // Properties for content type
            var props = await fileClient.GetPropertiesAsync();
            var dl = await fileClient.DownloadAsync();

            var contentType = props.Value.ContentType ?? "application/octet-stream";
            return File(dl.Value.Content, contentType, name);
        }

        // POST: /Contracts/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var fileClient = _sf.Share(ShareName).GetRootDirectoryClient().GetFileClient(name);
                await fileClient.DeleteIfExistsAsync();
                TempData["ok"] = $"Deleted {name}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Contracts/Seed  (creates 5 dummy text files)
        public async Task<IActionResult> Seed()
        {
            var share = _sf.Share(ShareName);
            var root = share.GetRootDirectoryClient();

            for (int i = 1; i <= 5; i++)
            {
                var name = $"Contract{i}.txt";
                var fc = root.GetFileClient(name);

                var bytes = System.Text.Encoding.UTF8.GetBytes($"Dummy contract {i} - {DateTime.UtcNow:O}");
                using var ms = new MemoryStream(bytes);

                await fc.CreateAsync(ms.Length);
                await fc.SetHttpHeadersAsync(new ShareFileSetHttpHeadersOptions
                {
                    HttpHeaders = new ShareFileHttpHeaders { ContentType = "text/plain" }
                });

                ms.Position = 0;
                await fc.UploadRangeAsync(new HttpRange(0, ms.Length), ms);
            }

            TempData["ok"] = "Seeded 5 dummy contracts.";
            return RedirectToAction(nameof(Index));
        }
    }
}
