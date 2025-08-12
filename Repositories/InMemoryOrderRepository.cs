using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderingSystem.Models;

namespace OrderingSystem.Repositories
{
    public class InMemoryOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = new List<Order>();
        private int _nextId = 1;

        public Task<Order> CreateAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            order.OrderId = _nextId++;
            
            // Assign IDs to order items
            int orderItemId = 1;
            foreach (var item in order.OrderItems)
            {
                item.OrderItemId = orderItemId++;
                item.OrderId = order.OrderId;
            }

            _orders.Add(order);
            return Task.FromResult(order);
        }

        public Task<Order> GetByIdAsync(int orderId)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            return Task.FromResult(order);
        }

        public Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
        {
            var orders = _orders.Where(o => o.CustomerId == customerId).ToList();
            return Task.FromResult<IEnumerable<Order>>(orders);
        }

        public Task<IEnumerable<Order>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Order>>(_orders.ToList());
        }

        public Task<Order> UpdateAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var existingOrderIndex = _orders.FindIndex(o => o.OrderId == order.OrderId);
            if (existingOrderIndex == -1)
                throw new ArgumentException("Order not found", nameof(order));

            _orders[existingOrderIndex] = order;
            return Task.FromResult(order);
        }

        public Task<bool> DeleteAsync(int orderId)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
                return Task.FromResult(false);

            _orders.Remove(order);
            return Task.FromResult(true);
        }

        public Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
        {
            var orders = _orders.Where(o => o.Status == status).ToList();
            return Task.FromResult<IEnumerable<Order>>(orders);
        }

        public Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var orders = _orders.Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate).ToList();
            return Task.FromResult<IEnumerable<Order>>(orders);
        }
    }
}