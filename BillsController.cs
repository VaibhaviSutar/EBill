using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

namespace EBill.Controllers
{
    public class BillsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bills
        public IActionResult Index()
        {
            var bills = _context.Bills
                .Include(b => b.Items) // include products
                .ToList();
            return View(bills);
        }

        // GET: Bills/Create
        public IActionResult Create()
        {
            return View(new Bill { Items = new List<BillItem> { new BillItem() } });
        }

        // POST: Bills/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Bill bill)
        {
            if (bill.Items != null && bill.Items.Any())
            {
                bill.Total = bill.Items.Sum(i => i.Price * i.Quantity);
            }

            if (ModelState.IsValid)
            {
                _context.Bills.Add(bill);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index)); // redirect after save
            }
            else
            {
                var errors = ModelState
                .Where(x => x.Value.Errors.Any())
                .Select(x => new {
                    Field = x.Key,
                    Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                });

                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(errors));

                return View(bill);
            }

                return View(bill);
        }

        // GET: Bills/Edit/5
        public IActionResult Edit(int id)
        {
            var bill = _context.Bills
                .Include(b => b.Items)
                .FirstOrDefault(b => b.Id == id);

            if (bill == null)
            {
                return NotFound();
            }
            return View(bill);
        }

        // POST: Bills/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Bill bill)
        {
            if (bill.Items != null && bill.Items.Any())
            {
                bill.Total = bill.Items.Sum(i => i.Price * i.Quantity);
            }

            if (ModelState.IsValid)
            {
                var existingBill = _context.Bills
                    .Include(b => b.Items)
                    .FirstOrDefault(b => b.Id == bill.Id);

                if (existingBill == null)
                    return NotFound();

                // Update bill details
                existingBill.CustomerName = bill.CustomerName;
                existingBill.Total = bill.Total;

                // Remove deleted items
                var removedItems = existingBill.Items
                    .Where(ei => bill.Items.All(i => i.Id != ei.Id))
                    .ToList();
                _context.BillItems.RemoveRange(removedItems);

                // Update or add items
                foreach (var item in bill.Items)
                {
                    var existingItem = existingBill.Items.FirstOrDefault(i => i.Id == item.Id);
                    if (existingItem != null)
                    {
                        existingItem.ProductName = item.ProductName;
                        existingItem.Quantity = item.Quantity;
                        existingItem.Price = item.Price;
                    }
                    else
                    {
                        existingBill.Items.Add(item);
                    }
                }

                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            return View(bill);
        }


        public IActionResult DownloadPdf(int id)
        {
            var bill = _context.Bills
                .Include(b => b.Items)
                .FirstOrDefault(b => b.Id == id);

            if (bill == null)
                return NotFound();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // --- Header ---
                    page.Header()
                        .Column(col =>
                        {
                            col.Item().Text("Bill Receipt")
                                .FontSize(20)
                                .SemiBold()
                                .AlignCenter();

                            col.Item().Text($"Bill ID: {bill.Id}");
                            col.Item().Text($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}");
                        });

                    // --- Content ---
                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // Customer Details
                        col.Item().Text($"Customer Name: {bill.CustomerName}")
                            .FontSize(14)
                            .SemiBold();

                        col.Item().Text($"Number of Items: {bill.Items.Count}");

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Product
                                columns.ConstantColumn(80); // Quantity
                                columns.ConstantColumn(80); // Price
                                columns.ConstantColumn(80); // Subtotal
                            });

                            // Table Header
                            table.Header(header =>
                            {
                                header.Cell().Text("Product").SemiBold();
                                header.Cell().Text("Qty").SemiBold();
                                header.Cell().Text("Price").SemiBold();
                                header.Cell().Text("Subtotal").SemiBold();
                            });

                            // Table Data
                            foreach (var item in bill.Items)
                            {
                                table.Cell().Text(item.ProductName);
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text(item.Price.ToString("C"));
                                table.Cell().Text((item.Price * item.Quantity).ToString("C"));
                            }
                        });

                        // Total
                        col.Item().AlignRight().Text($"Grand Total: {bill.Total:C}")
                            .FontSize(14)
                            .SemiBold();
                    });

                    // --- Footer ---
                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Bill_{bill.Id}.pdf");
        }

        // GET: Bills/Delete/5
        public IActionResult Delete(int id)
        {
            var bill = _context.Bills
                .Include(b => b.Items)
                .FirstOrDefault(b => b.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            return View(bill);
        }

        // POST: Bills/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var bill = _context.Bills
                .Include(b => b.Items)
                .FirstOrDefault(b => b.Id == id);

            if (bill != null)
            {
                _context.BillItems.RemoveRange(bill.Items); // remove products first
                _context.Bills.Remove(bill); // then remove bill
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
