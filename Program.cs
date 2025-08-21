using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderingSystem.Models;
using OrderingSystem.Repositories;
using OrderingSystem.Services;

namespace OrderingSystem
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Check if running in web mode (default for containers) or console demo mode
            bool runAsWebApi = args.Length == 0 || Array.Exists(args, arg => arg.Equals("--web", StringComparison.OrdinalIgnoreCase));
            
            if (runAsWebApi)
            {
                await RunWebApi(args);
            }
            else
            {
                await RunConsoleDemo();
            }
        }

        static async Task RunWebApi(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            // Register repositories and services
            builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
            builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
            builder.Services.AddScoped<IOrderService, OrderService>();

            // Add CORS for development
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();
            app.UseAuthorization();
            app.MapControllers();

            // Add health check endpoint
            app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

            Console.WriteLine("=== C# Ordering System Web API ===");
            Console.WriteLine($"Starting web server on port {app.Environment.IsDevelopment() ? "5000" : "8080"}");
            
            await app.RunAsync();
        }

        static async Task RunConsoleDemo()
        {
            Console.WriteLine("=== C# Ordering System Demo ===\n");

            // Initialize repositories and services
            var orderRepository = new InMemoryOrderRepository();
            var productRepository = new InMemoryProductRepository();
            var orderService = new OrderService(orderRepository, productRepository);

            try
            {
                // Demo 1: Display available products
                await DisplayAvailableProducts(productRepository);

                // Demo 2: Create a sample customer
                var customer = CreateSampleCustomer();
                Console.WriteLine($"Created customer: {customer.FullName} ({customer.Email})\n");

                // Demo 3: Create a new order
                var order = await CreateSampleOrder(orderService, customer);
                Console.WriteLine($"Created order #{order.OrderId} for {customer.FullName}");
                Console.WriteLine($"Order total: ${order.TotalAmount:F2}\n");

                // Demo 4: Display order details
                await DisplayOrderDetails(orderService, order.OrderId);

                // Demo 5: Update order status
                await UpdateOrderStatus(orderService, order.OrderId);

                // Demo 6: Add another item to the order
                await AddItemToOrder(orderService, order.OrderId);

                // Demo 7: Display all orders
                await DisplayAllOrders(orderService);

                // Demo 8: Search orders by status
                await SearchOrdersByStatus(orderService);

                Console.WriteLine("\n=== Demo completed successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task DisplayAvailableProducts(IProductRepository productRepository)
        {
            Console.WriteLine("Available Products:");
            Console.WriteLine("==================");

            var products = await productRepository.GetAllAsync();
            foreach (var product in products)
            {
                Console.WriteLine($"#{product.ProductId}: {product.Name}");
                Console.WriteLine($"  Price: ${product.Price:F2}");
                Console.WriteLine($"  Category: {product.Category}");
                Console.WriteLine($"  Stock: {(product.IsDigital ? "Digital" : product.StockQuantity.ToString())}");
                Console.WriteLine($"  Description: {product.Description}");
                Console.WriteLine();
            }
        }

        private static Customer CreateSampleCustomer()
        {
            return new Customer
            {
                CustomerId = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@email.com",
                PhoneNumber = "+1-555-0123",
                Address = "123 Main Street",
                City = "Anytown",
                State = "CA",
                PostalCode = "12345",
                Country = "USA",
                CreatedDate = DateTime.UtcNow,
                DateOfBirth = new DateTime(1990, 5, 15)
            };
        }

        private static async Task<Order> CreateSampleOrder(IOrderService orderService, Customer customer)
        {
            var order = new Order
            {
                CustomerId = customer.CustomerId,
                Customer = customer,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = $"{customer.Address}, {customer.City}, {customer.State} {customer.PostalCode}",
                BillingAddress = $"{customer.Address}, {customer.City}, {customer.State} {customer.PostalCode}",
                Notes = "Sample order for demonstration"
            };

            // Add some order items
            order.OrderItems.AddRange(new[]
            {
                new OrderItem
                {
                    ProductId = 1, // Mathematics Tutoring Session
                    Quantity = 2,
                    UnitPrice = 50.00m,
                    Notes = "Algebra and Calculus sessions"
                },
                new OrderItem
                {
                    ProductId = 4, // Study Materials Bundle
                    Quantity = 1,
                    UnitPrice = 29.99m
                }
            });

            return await orderService.CreateOrderAsync(order);
        }

        private static async Task DisplayOrderDetails(IOrderService orderService, int orderId)
        {
            Console.WriteLine("Order Details:");
            Console.WriteLine("==============");

            var order = await orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                Console.WriteLine($"Order ID: {order.OrderId}");
                Console.WriteLine($"Customer: {order.Customer?.FullName ?? "N/A"}");
                Console.WriteLine($"Order Date: {order.OrderDate:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Status: {order.Status}");
                Console.WriteLine($"Shipping Address: {order.ShippingAddress}");
                Console.WriteLine($"Notes: {order.Notes}");
                Console.WriteLine($"Total Amount: ${order.TotalAmount:F2}");
                Console.WriteLine("\nOrder Items:");

                foreach (var item in order.OrderItems)
                {
                    Console.WriteLine($"  - {item.Product?.Name ?? $"Product #{item.ProductId}"}");
                    Console.WriteLine($"    Quantity: {item.Quantity}, Unit Price: ${item.UnitPrice:F2}, Total: ${item.TotalPrice:F2}");
                    if (!string.IsNullOrEmpty(item.Notes))
                    {
                        Console.WriteLine($"    Notes: {item.Notes}");
                    }
                }
                Console.WriteLine();
            }
        }

        private static async Task UpdateOrderStatus(IOrderService orderService, int orderId)
        {
            Console.WriteLine("Updating Order Status:");
            Console.WriteLine("======================");

            // Update to Processing
            var updatedOrder = await orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Processing);
            Console.WriteLine($"Order #{orderId} status updated to: {updatedOrder.Status}");

            // Update to Shipped
            updatedOrder = await orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Shipped);
            Console.WriteLine($"Order #{orderId} status updated to: {updatedOrder.Status}");
            Console.WriteLine($"Shipped Date: {updatedOrder.ShippedDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
        }

        private static async Task AddItemToOrder(IOrderService orderService, int orderId)
        {
            Console.WriteLine("Adding Item to Order:");
            Console.WriteLine("=====================");

            try
            {
                var newItem = new OrderItem
                {
                    ProductId = 2, // Science Tutoring Session
                    Quantity = 1,
                    UnitPrice = 55.00m,
                    Notes = "Physics session"
                };

                await orderService.AddOrderItemAsync(orderId, newItem);
                Console.WriteLine("Successfully added Science Tutoring Session to the order.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not add item to order: {ex.Message}");
                Console.WriteLine("This is expected since the order status is no longer 'Pending'.");
            }
            Console.WriteLine();
        }

        private static async Task DisplayAllOrders(IOrderService orderService)
        {
            Console.WriteLine("All Orders:");
            Console.WriteLine("===========");

            var orders = await orderService.GetAllOrdersAsync();
            foreach (var order in orders)
            {
                Console.WriteLine($"Order #{order.OrderId} - Status: {order.Status} - Total: ${order.TotalAmount:F2} - Date: {order.OrderDate:yyyy-MM-dd}");
            }
            Console.WriteLine();
        }

        private static async Task SearchOrdersByStatus(IOrderService orderService)
        {
            Console.WriteLine("Orders by Status (Shipped):");
            Console.WriteLine("============================");

            var shippedOrders = await orderService.GetOrdersByStatusAsync(OrderStatus.Shipped);
            foreach (var order in shippedOrders)
            {
                Console.WriteLine($"Order #{order.OrderId} - Shipped on: {order.ShippedDate:yyyy-MM-dd HH:mm:ss} - Total: ${order.TotalAmount:F2}");
            }
            Console.WriteLine();
        }
    }
}