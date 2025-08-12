using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderingSystem.Models;

namespace OrderingSystem.Repositories
{
    public class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new List<Product>();
        private int _nextId = 1;

        public InMemoryProductRepository()
        {
            // Seed with sample products
            SeedProducts();
        }

        public Task<Product> CreateAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.ProductId = _nextId++;
            _products.Add(product);
            return Task.FromResult(product);
        }

        public Task<Product> GetByIdAsync(int productId)
        {
            var product = _products.FirstOrDefault(p => p.ProductId == productId);
            return Task.FromResult(product);
        }

        public Task<IEnumerable<Product>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Product>>(_products.Where(p => p.IsActive).ToList());
        }

        public Task<Product> UpdateAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var existingProductIndex = _products.FindIndex(p => p.ProductId == product.ProductId);
            if (existingProductIndex == -1)
                throw new ArgumentException("Product not found", nameof(product));

            _products[existingProductIndex] = product;
            return Task.FromResult(product);
        }

        public Task<bool> DeleteAsync(int productId)
        {
            var product = _products.FirstOrDefault(p => p.ProductId == productId);
            if (product == null)
                return Task.FromResult(false);

            // Soft delete - mark as inactive
            product.IsActive = false;
            return Task.FromResult(true);
        }

        public Task<IEnumerable<Product>> GetByCategoryAsync(string category)
        {
            var products = _products.Where(p => p.IsActive && 
                string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult<IEnumerable<Product>>(products);
        }

        public Task<IEnumerable<Product>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAllAsync();

            var products = _products.Where(p => p.IsActive && 
                (p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                 p.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                 p.Category?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)).ToList();
            
            return Task.FromResult<IEnumerable<Product>>(products);
        }

        private void SeedProducts()
        {
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = _nextId++,
                    Name = "Mathematics Tutoring Session",
                    Description = "One-on-one mathematics tutoring session (1 hour)",
                    Price = 50.00m,
                    SKU = "MATH-TUT-001",
                    Category = "Tutoring",
                    StockQuantity = 100,
                    IsDigital = true,
                    IsActive = true
                },
                new Product
                {
                    ProductId = _nextId++,
                    Name = "Science Tutoring Session",
                    Description = "One-on-one science tutoring session (1 hour)",
                    Price = 55.00m,
                    SKU = "SCI-TUT-001",
                    Category = "Tutoring",
                    StockQuantity = 100,
                    IsDigital = true,
                    IsActive = true
                },
                new Product
                {
                    ProductId = _nextId++,
                    Name = "English Tutoring Session",
                    Description = "One-on-one English tutoring session (1 hour)",
                    Price = 45.00m,
                    SKU = "ENG-TUT-001",
                    Category = "Tutoring",
                    StockQuantity = 100,
                    IsDigital = true,
                    IsActive = true
                },
                new Product
                {
                    ProductId = _nextId++,
                    Name = "Study Materials Bundle",
                    Description = "Complete study materials package with workbooks and guides",
                    Price = 29.99m,
                    SKU = "STUDY-BUNDLE-001",
                    Category = "Materials",
                    StockQuantity = 50,
                    IsDigital = false,
                    Weight = 2.5m,
                    Dimensions = "8.5x11x2 inches",
                    IsActive = true
                },
                new Product
                {
                    ProductId = _nextId++,
                    Name = "Online Course Access",
                    Description = "3-month access to comprehensive online learning platform",
                    Price = 99.99m,
                    SKU = "ONLINE-COURSE-001",
                    Category = "Digital",
                    StockQuantity = 1000,
                    IsDigital = true,
                    IsActive = true
                }
            };

            _products.AddRange(products);
        }
    }
}