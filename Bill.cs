using System.ComponentModel.DataAnnotations;

namespace EBill.Models
{
    public class Bill
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; }

        public decimal Total { get; set; }

        public List<BillItem> Items { get; set; } = new List<BillItem>();
        //test
    }
}
