using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderingSystem.Models;

namespace OrderingSystem.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId);
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order> UpdateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(int orderId);
        Task<Order> UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task<Order> AddOrderItemAsync(int orderId, OrderItem orderItem);
        Task<bool> RemoveOrderItemAsync(int orderId, int orderItemId);
        Task<decimal> CalculateOrderTotalAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status);
        Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}