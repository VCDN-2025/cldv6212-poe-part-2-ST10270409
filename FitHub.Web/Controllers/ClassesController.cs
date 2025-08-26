using Azure.Data.Tables;
using FitHub.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Web.Controllers { 
public class ClassesController : Controller
{
    private readonly StorageFactory _sf;
    private const string TableName = "Classes";
    private const string ContainerName = "class-images1";

    public ClassesController(StorageFactory sf) => _sf = sf;
    private TableClient Table() => _sf.Table(TableName);

    // GET: /Classes
    public IActionResult Index()
    {
        var list = Table().Query<ClassEntity>(c => c.PartitionKey == "Class").ToList();
        return View(list);
    }

    // GET: /Classes/Create
    [HttpGet]
    public IActionResult Create() => View(new ClassEntity());

    // POST: /Classes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassEntity model, IFormFile? image)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), "Title is required");
        if (model.Price <= 0)
            ModelState.AddModelError(nameof(model.Price), "Price must be > 0");
        if (!ModelState.IsValid) return View(model);

        // Optional image upload
        if (image != null && image.Length > 0)
        {
            var container = _sf.Blob(ContainerName);
            var blob = container.GetBlobClient($"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}");
            using var s = image.OpenReadStream();
            await blob.UploadAsync(s);
            model.ImageUrl = blob.Uri.ToString();
        }

        Table().AddEntity(model);
        TempData["ok"] = "Class created.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Classes/Delete?rowKey=...
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string rowKey)
    {
        if (!string.IsNullOrWhiteSpace(rowKey))
            Table().DeleteEntity("Class", rowKey);
        TempData["ok"] = "Class deleted.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Classes/Seed  (quickly add 5)
    public IActionResult Seed()
    {
        var tbl = Table();
        var demo = new[]
        {
            new ClassEntity{ Title="HIIT Blast", Description="High intensity 30min", TrainerName="Zee", Price=149.99M },
            new ClassEntity{ Title="Power Yoga", Description="Strength & stretch", TrainerName="Asha", Price=129.00M },
            new ClassEntity{ Title="Spin Xpress", Description="Cardio spin", TrainerName="Mo", Price=119.00M },
            new ClassEntity{ Title="Box Fit", Description="Conditioning", TrainerName="Neo", Price=139.00M },
            new ClassEntity{ Title="Pilates Core", Description="Core strength", TrainerName="Lara", Price=149.00M },
        };
        foreach (var c in demo) tbl.UpsertEntity(c);
        TempData["ok"] = "Seeded 5 demo classes.";
        return RedirectToAction(nameof(Index));
    }
}
}