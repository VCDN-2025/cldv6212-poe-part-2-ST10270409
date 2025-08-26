
using Azure.Data.Tables;
using FitHub.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Web.Controllers
{ 
public class MembersController : Controller
{
    private readonly StorageFactory _sf;
    private const string TableName = "Members";

    public MembersController(StorageFactory sf) => _sf = sf;

    private TableClient Table() => _sf.Table(TableName);

    // GET: /Members
    public IActionResult Index()
    {
        var table = Table();
        var items = table.Query<MemberEntity>(m => m.PartitionKey == "Member").ToList();
        return View(items);
    }

    // GET: /Members/Create
    [HttpGet]
    public IActionResult Create() => View(new MemberEntity());

    // POST: /Members/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(MemberEntity m)
    {
        if (string.IsNullOrWhiteSpace(m.Email))
            ModelState.AddModelError(nameof(m.Email), "Email is required");
        if (!ModelState.IsValid) return View(m);

        m.PartitionKey = "Member";
        if (string.IsNullOrWhiteSpace(m.RowKey)) m.RowKey = Guid.NewGuid().ToString();

        Table().AddEntity(m);
        TempData["ok"] = "Member added.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Members/Delete?rowKey=...  (PartitionKey is fixed)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string rowKey)
    {
        if (!string.IsNullOrWhiteSpace(rowKey))
            Table().DeleteEntity("Member", rowKey);
        TempData["ok"] = "Member deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Utility to quickly get 5 records for the rubric
    // GET: /Members/Seed
    public IActionResult Seed()
    {
        var table = Table();
        var demo = new[]
        {
            new MemberEntity{ FirstName="Ava",   LastName="Mokoena", Email="ava@demo.com",   Phone="0710000001"},
            new MemberEntity{ FirstName="Liam",  LastName="Dlamini", Email="liam@demo.com",  Phone="0710000002"},
            new MemberEntity{ FirstName="Noah",  LastName="Naidoo",  Email="noah@demo.com",  Phone="0710000003"},
            new MemberEntity{ FirstName="Mia",   LastName="Pillay",  Email="mia@demo.com",   Phone="0710000004"},
            new MemberEntity{ FirstName="Ethan", LastName="Botha",   Email="ethan@demo.com", Phone="0710000005"},
        };
        foreach (var m in demo) table.UpsertEntity(m);
        TempData["ok"] = "Seeded 5 demo members.";
        return RedirectToAction(nameof(Index));
        }
    }
}