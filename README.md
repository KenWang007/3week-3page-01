# C# Ordering System

A comprehensive ordering system built in C# that demonstrates clean architecture patterns, SOLID principles, and modern software development practices.

## Features

- **Complete Order Management**: Create, read, update, and delete orders
- **Order Status Tracking**: Track orders through different lifecycle stages (Pending, Processing, Shipped, Delivered, etc.)
- **Product Management**: Manage products with support for both digital and physical items
- **Customer Management**: Handle customer information and order history
- **Validation**: Comprehensive input validation and business rule enforcement
- **RESTful API**: Complete REST API with proper HTTP status codes and error handling
- **Repository Pattern**: Clean separation of data access logic
- **Service Layer**: Business logic encapsulation with proper error handling
- **In-Memory Storage**: Ready-to-run with sample data (easily replaceable with database)

## Architecture

The system follows a layered architecture pattern:

```
┌─────────────────┐
│   Controllers   │  ← REST API endpoints
├─────────────────┤
│    Services     │  ← Business logic
├─────────────────┤
│  Repositories   │  ← Data access
├─────────────────┤
│     Models      │  ← Domain entities
└─────────────────┘
```

## Project Structure

```
OrderingSystem/
├── Models/
│   ├── Order.cs           # Order entity with order items
│   ├── OrderItem.cs       # Individual order line items
│   ├── Customer.cs        # Customer information
│   └── Product.cs         # Product catalog
├── Services/
│   ├── IOrderService.cs   # Order service interface
│   └── OrderService.cs    # Order business logic
├── Repositories/
│   ├── IOrderRepository.cs        # Order repository interface
│   ├── IProductRepository.cs      # Product repository interface
│   ├── InMemoryOrderRepository.cs # In-memory order storage
│   └── InMemoryProductRepository.cs # In-memory product storage
├── Controllers/
│   └── OrderController.cs # REST API endpoints
├── Program.cs            # Console demo application
├── OrderingSystem.csproj # Project file
└── README.md            # This file
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Any C# IDE (Visual Studio, VS Code, Rider, etc.)

### Running the Console Demo

1. Navigate to the project directory:
   ```bash
   cd /workspace
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

The console application will demonstrate:
- Displaying available products
- Creating customers and orders
- Adding items to orders
- Updating order status
- Searching and filtering orders

### Using the API

The `OrderController` provides a complete REST API for order management:

#### Order Endpoints

- `GET /api/order` - Get all orders
- `GET /api/order/{id}` - Get order by ID
- `POST /api/order` - Create new order
- `PUT /api/order/{id}` - Update existing order
- `DELETE /api/order/{id}` - Delete order
- `PATCH /api/order/{id}/status` - Update order status
- `GET /api/order/{id}/total` - Get order total
- `GET /api/order/customer/{customerId}` - Get orders by customer
- `GET /api/order/status/{status}` - Get orders by status
- `GET /api/order/daterange?startDate=...&endDate=...` - Get orders by date range

#### Order Item Management

- `POST /api/order/{id}/items` - Add item to order
- `DELETE /api/order/{id}/items/{itemId}` - Remove item from order

## Sample Usage

### Creating an Order

```csharp
var order = new Order
{
    CustomerId = 1,
    OrderDate = DateTime.UtcNow,
    ShippingAddress = "123 Main St, City, State 12345",
    BillingAddress = "123 Main St, City, State 12345",
    OrderItems = new List<OrderItem>
    {
        new OrderItem
        {
            ProductId = 1,
            Quantity = 2,
            UnitPrice = 50.00m
        }
    }
};

var createdOrder = await orderService.CreateOrderAsync(order);
```

### Updating Order Status

```csharp
// Update to processing
await orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Processing);

// Update to shipped (automatically sets shipped date)
await orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Shipped);

// Update to delivered (automatically sets delivered date)
await orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Delivered);
```

### Searching Orders

```csharp
// Get orders by customer
var customerOrders = await orderService.GetOrdersByCustomerIdAsync(customerId);

// Get orders by status
var pendingOrders = await orderService.GetOrdersByStatusAsync(OrderStatus.Pending);

// Get orders by date range
var recentOrders = await orderService.GetOrdersByDateRangeAsync(
    DateTime.UtcNow.AddDays(-30), 
    DateTime.UtcNow
);
```

## Order Status Workflow

The system enforces a proper order status workflow:

1. **Pending** → Processing or Cancelled
2. **Processing** → Shipped or Cancelled
3. **Shipped** → Delivered
4. **Delivered** → Refunded (optional)
5. **Cancelled** → Final state
6. **Refunded** → Final state

## Validation Features

- **Order Validation**: Orders must have at least one item
- **Product Validation**: Ensures products exist and are active
- **Stock Validation**: Checks stock availability for physical products
- **Status Transition Validation**: Enforces proper order status workflow
- **Input Validation**: Comprehensive validation using data annotations

## Sample Products

The system comes pre-loaded with sample products suitable for an educational platform:

1. **Mathematics Tutoring Session** - $50.00 (Digital)
2. **Science Tutoring Session** - $55.00 (Digital)
3. **English Tutoring Session** - $45.00 (Digital)
4. **Study Materials Bundle** - $29.99 (Physical)
5. **Online Course Access** - $99.99 (Digital)

## Error Handling

The system includes comprehensive error handling:

- **Argument Validation**: Proper validation of input parameters
- **Business Rule Validation**: Enforcement of business constraints
- **Exception Handling**: Graceful handling of errors with meaningful messages
- **HTTP Status Codes**: Proper REST API status codes (200, 201, 400, 404, 500)

## Extensibility

The system is designed for easy extension:

- **Database Integration**: Replace in-memory repositories with Entity Framework
- **Authentication**: Add authentication and authorization
- **Payment Processing**: Integrate payment gateways
- **Inventory Management**: Add advanced inventory tracking
- **Notifications**: Add email/SMS notifications for status changes
- **Reporting**: Add order analytics and reporting features

## Technologies Used

- **.NET 8.0**: Latest .NET framework
- **C# 12**: Modern C# language features
- **ASP.NET Core**: For REST API controller
- **System.ComponentModel.Annotations**: For data validation
- **Async/Await**: Asynchronous programming patterns
- **LINQ**: For data querying and manipulation

## Contributing

Feel free to extend this system with additional features:

1. Add unit tests using xUnit or NUnit
2. Implement Entity Framework for database persistence
3. Add more sophisticated validation rules
4. Implement caching strategies
5. Add logging with Serilog or NLog
6. Create a web UI using Blazor or MVC

## License

This project is provided as a learning example and can be freely used and modified.