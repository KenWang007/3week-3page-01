using System.ComponentModel.DataAnnotations;

namespace OrderingSystem.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
        
        public string SKU { get; set; }
        
        public string Category { get; set; }
        
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string ImageUrl { get; set; }
        
        public decimal Weight { get; set; }
        
        public string Dimensions { get; set; }
        
        public bool IsDigital { get; set; } = false;
    }
}