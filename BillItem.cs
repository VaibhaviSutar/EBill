using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EBill.Models
{
    public class BillItem
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        [ValidateNever] // 🚀 Ignore during validation

        public Bill Bill { get; set; }
    }
}
