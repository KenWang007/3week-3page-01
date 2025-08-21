using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderingSystem.Models;
using OrderingSystem.Repositories;
using OrderingSystem.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Add health checks
builder.Services.AddHealthChecks();

// Configure JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint
app.MapGet("/", () => Results.Json(new
{
    message = "Ordering System API",
    version = "1.0.0",
    endpoints = new[]
    {
        "/health - Health check endpoint",
        "/api/products - Get all products",
        "/api/orders - Get all orders",
        "/api/orders/{id} - Get order by ID",
        "/api/orders - Create new order (POST)",
        "/api/demo - Run demo scenario"
    }
}));

// Products endpoints
app.MapGet("/api/products", async (IProductRepository productRepo) =>
{
    var products = await productRepo.GetAllProductsAsync();
    return Results.Json(products);
});

app.MapGet("/api/products/{id}", async (string id, IProductRepository productRepo) =>
{
    var product = await productRepo.GetProductByIdAsync(id);
    return product != null ? Results.Json(product) : Results.NotFound();
});

// Orders endpoints
app.MapGet("/api/orders", async (IOrderService orderService) =>
{
    var orders = await orderService.GetAllOrdersAsync();
    return Results.Json(orders);
});

app.MapGet("/api/orders/{id}", async (string id, IOrderService orderService) =>
{
    var order = await orderService.GetOrderDetailsAsync(id);
    return order != null ? Results.Json(order) : Results.NotFound();
});

app.MapPost("/api/orders", async (CreateOrderRequest request, IOrderService orderService) =>
{
    try
    {
        var customer = new Customer(
            Guid.NewGuid().ToString(),
            request.CustomerFirstName,
            request.CustomerLastName,
            request.CustomerEmail
        );

        var orderItems = request.Items.Select(item => new OrderItem(
            item.ProductId,
            item.Quantity,
            item.UnitPrice
        )).ToList();

        var order = await orderService.CreateOrderAsync(customer, orderItems);
        return Results.Created($"/api/orders/{order.OrderId}", order);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/orders/{id}/status", async (string id, UpdateStatusRequest request, IOrderService orderService) =>
{
    try
    {
        await orderService.UpdateOrderStatusAsync(id, request.Status);
        return Results.Ok(new { message = "Order status updated successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Demo endpoint - runs the original console demo
app.MapGet("/api/demo", async (IOrderService orderService, IProductRepository productRepo) =>
{
    var results = new List<object>();

    // Get products
    var products = await productRepo.GetAllProductsAsync();
    results.Add(new { step = "Available Products", data = products });

    // Create sample customer
    var customer = new Customer(
        Guid.NewGuid().ToString(),
        "John",
        "Doe",
        "john.doe@example.com"
    );
    results.Add(new { step = "Created Customer", data = customer });

    // Create order
    var orderItems = new List<OrderItem>
    {
        new OrderItem("LAPTOP001", 1, 999.99m),
        new OrderItem("MOUSE001", 2, 24.99m)
    };
    var order = await orderService.CreateOrderAsync(customer, orderItems);
    results.Add(new { step = "Created Order", data = order });

    // Get order details
    var orderDetails = await orderService.GetOrderDetailsAsync(order.OrderId);
    results.Add(new { step = "Order Details", data = orderDetails });

    return Results.Json(new { demo = "completed", results });
});

// Start the application
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://+:{port}");

app.Run();

// Request models
public record CreateOrderRequest(
    string CustomerFirstName,
    string CustomerLastName,
    string CustomerEmail,
    List<OrderItemRequest> Items
);

public record OrderItemRequest(
    string ProductId,
    int Quantity,
    decimal UnitPrice
);

public record UpdateStatusRequest(OrderStatus Status);