using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderingSystem.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        
        [Required]
        public int CustomerId { get; set; }
        
        public Customer Customer { get; set; }
        
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        
        [Required]
        public DateTime OrderDate { get; set; }
        
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        public decimal TotalAmount => CalculateTotalAmount();
        
        public string ShippingAddress { get; set; }
        
        public string BillingAddress { get; set; }
        
        public DateTime? ShippedDate { get; set; }
        
        public DateTime? DeliveredDate { get; set; }
        
        public string TrackingNumber { get; set; }
        
        public string Notes { get; set; }

        private decimal CalculateTotalAmount()
        {
            decimal total = 0;
            foreach (var item in OrderItems)
            {
                total += item.Quantity * item.UnitPrice;
            }
            return total;
        }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Refunded
    }
}