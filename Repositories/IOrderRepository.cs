using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderingSystem.Models;

namespace OrderingSystem.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order);
        Task<Order> GetByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order> UpdateAsync(Order order);
        Task<bool> DeleteAsync(int orderId);
        Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
        Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}