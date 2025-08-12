using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderingSystem.Models;
using OrderingSystem.Repositories;

namespace OrderingSystem.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            // Validate order items
            await ValidateOrderItemsAsync(order.OrderItems);

            // Set order date if not provided
            if (order.OrderDate == default)
                order.OrderDate = DateTime.UtcNow;

            // Set initial status
            order.Status = OrderStatus.Pending;

            return await _orderRepository.CreateAsync(order);
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than 0", nameof(orderId));

            return await _orderRepository.GetByIdAsync(orderId);
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("Customer ID must be greater than 0", nameof(customerId));

            return await _orderRepository.GetByCustomerIdAsync(customerId);
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _orderRepository.GetAllAsync();
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (order.OrderId <= 0)
                throw new ArgumentException("Order ID must be greater than 0");

            // Validate order items if they exist
            if (order.OrderItems?.Any() == true)
            {
                await ValidateOrderItemsAsync(order.OrderItems);
            }

            return await _orderRepository.UpdateAsync(order);
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than 0", nameof(orderId));

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                return false;

            // Only allow deletion of pending or cancelled orders
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Cancelled)
                throw new InvalidOperationException("Cannot delete orders that are being processed or have been shipped");

            return await _orderRepository.DeleteAsync(orderId);
        }

        public async Task<Order> UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than 0", nameof(orderId));

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found", nameof(orderId));

            // Validate status transition
            ValidateStatusTransition(order.Status, status);

            order.Status = status;

            // Set shipped date when status changes to shipped
            if (status == OrderStatus.Shipped && !order.ShippedDate.HasValue)
            {
                order.ShippedDate = DateTime.UtcNow;
            }

            // Set delivered date when status changes to delivered
            if (status == OrderStatus.Delivered && !order.DeliveredDate.HasValue)
            {
                order.DeliveredDate = DateTime.UtcNow;
            }

            return await _orderRepository.UpdateAsync(order);
        }

        public async Task<Order> AddOrderItemAsync(int orderId, OrderItem orderItem)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than 0", nameof(orderId));

            if (orderItem == null)
                throw new ArgumentNullException(nameof(orderItem));

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found", nameof(orderId));

            // Can only add items to pending orders
            if (order.Status != OrderStatus.Pending)
                throw new InvalidOperationException("Cannot add items to orders that are not pending");

            // Validate the order item
            await ValidateOrderItemAsync(orderItem);

            orderItem.OrderId = orderId;
            order.OrderItems.Add(orderItem);

            return await _orderRepository.UpdateAsync(order);
        }

        public async Task<bool> RemoveOrderItemAsync(int orderId, int orderItemId)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than 0", nameof(orderId));

            if (orderItemId <= 0)
                throw new ArgumentException("Order Item ID must be greater than 0", nameof(orderItemId));

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                return false;

            // Can only remove items from pending orders
            if (order.Status != OrderStatus.Pending)
                throw new InvalidOperationException("Cannot remove items from orders that are not pending");

            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.OrderItemId == orderItemId);
            if (orderItem == null)
                return false;

            order.OrderItems.Remove(orderItem);
            await _orderRepository.UpdateAsync(order);

            return true;
        }

        public async Task<decimal> CalculateOrderTotalAsync(int orderId)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than 0", nameof(orderId));

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found", nameof(orderId));

            return order.TotalAmount;
        }

        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await _orderRepository.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("Start date cannot be after end date");

            return await _orderRepository.GetByDateRangeAsync(startDate, endDate);
        }

        private async Task ValidateOrderItemsAsync(IEnumerable<OrderItem> orderItems)
        {
            if (orderItems == null || !orderItems.Any())
                throw new ArgumentException("Order must contain at least one item");

            foreach (var item in orderItems)
            {
                await ValidateOrderItemAsync(item);
            }
        }

        private async Task ValidateOrderItemAsync(OrderItem orderItem)
        {
            if (orderItem.ProductId <= 0)
                throw new ArgumentException("Product ID must be greater than 0");

            if (orderItem.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0");

            if (orderItem.UnitPrice <= 0)
                throw new ArgumentException("Unit price must be greater than 0");

            // Validate product exists and is active
            var product = await _productRepository.GetByIdAsync(orderItem.ProductId);
            if (product == null)
                throw new ArgumentException($"Product with ID {orderItem.ProductId} not found");

            if (!product.IsActive)
                throw new ArgumentException($"Product {product.Name} is not available");

            // Check stock availability for physical products
            if (!product.IsDigital && product.StockQuantity < orderItem.Quantity)
                throw new ArgumentException($"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {orderItem.Quantity}");

            // Set unit price from product if not provided
            if (orderItem.UnitPrice == 0)
                orderItem.UnitPrice = product.Price;
        }

        private void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
            {
                { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Cancelled } },
                { OrderStatus.Processing, new List<OrderStatus> { OrderStatus.Shipped, OrderStatus.Cancelled } },
                { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Delivered } },
                { OrderStatus.Delivered, new List<OrderStatus> { OrderStatus.Refunded } },
                { OrderStatus.Cancelled, new List<OrderStatus>() },
                { OrderStatus.Refunded, new List<OrderStatus>() }
            };

            if (!validTransitions[currentStatus].Contains(newStatus))
            {
                throw new InvalidOperationException($"Cannot transition from {currentStatus} to {newStatus}");
            }
        }
    }
}