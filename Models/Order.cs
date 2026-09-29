using System;
using System.Collections.Generic;

namespace tieudaobadminton.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; } = ""; // IdentityUser Id
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";

        public decimal Total { get; set; }
        public string Status { get; set; } = "Pending"; // Pending/Shipping/Done/Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<OrderItem> Items { get; set; } = new();
    }
}
