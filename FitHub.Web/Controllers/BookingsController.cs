// Controllers/BookingsController.cs
using Azure.Data.Tables;
using FitHub.Web;
using FitHub.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FitHub.Web.Controllers
{
    public record BookingEvent(string type, string bookingId, string memberId, string classId, DateTime timestamp);

    public class BookingsController : Controller
    {
        private readonly StorageFactory _sf;
        private const string BookingsTable = "Bookings";
        private const string MembersTable = "Members";
        private const string ClassesTable = "Classes";
        private const string QueueName = "booking-events";

        public BookingsController(StorageFactory sf) => _sf = sf;

        private TableClient Bookings() => _sf.Table(BookingsTable);
        private TableClient Members() => _sf.Table(MembersTable);
        private TableClient Classes() => _sf.Table(ClassesTable);

        // GET: /Bookings/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Members = Members().Query<MemberEntity>(m => m.PartitionKey == "Member").ToList();
            ViewBag.Classes = Classes().Query<ClassEntity>(c => c.PartitionKey == "Class").ToList();
            return View();
        }

        // POST: /Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string memberId, string classId)
        {
            if (string.IsNullOrWhiteSpace(memberId) || string.IsNullOrWhiteSpace(classId))
            {
                ModelState.AddModelError("", "Select a member and a class.");
                return Create();
            }

            // 1) Write booking to Table storage
            var booking = new BookingEntity
            {
                PartitionKey = memberId, // groups bookings by member
                ClassId = classId
            };
            Bookings().AddEntity(booking);

            // 2) Send event to Queue
            var q = _sf.Queue(QueueName);
            var evt = new BookingEvent(
                type: "BookingCreated",
                bookingId: booking.RowKey,
                memberId: memberId,
                classId: classId,
                timestamp: DateTime.UtcNow
            );
            var payload = JsonSerializer.Serialize(evt);

            // The SDK will Base64-encode the string payload automatically.
            await q.SendMessageAsync(payload);

            TempData["ok"] = "Booking created and message queued.";
            return RedirectToAction(nameof(Thanks), new { id = booking.RowKey });
        }

        // GET: /Bookings/Thanks
        public IActionResult Thanks(string id) { ViewBag.Id = id; return View(); }
    }
}
