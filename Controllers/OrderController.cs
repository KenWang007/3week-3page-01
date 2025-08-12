using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Models;
using OrderingSystem.Services;

namespace OrderingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="order">The order to create</param>
        /// <returns>The created order</returns>
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdOrder = await _orderService.CreateOrderAsync(order);
                return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.OrderId }, createdOrder);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while creating the order: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets an order by ID
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <returns>The order</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }
                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the order: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all orders
        /// </summary>
        /// <returns>List of all orders</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving orders: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets orders by customer ID
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <returns>List of orders for the customer</returns>
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByCustomer(int customerId)
        {
            try
            {
                var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
                return Ok(orders);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving customer orders: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets orders by status
        /// </summary>
        /// <param name="status">The order status</param>
        /// <returns>List of orders with the specified status</returns>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByStatus(OrderStatus status)
        {
            try
            {
                var orders = await _orderService.GetOrdersByStatusAsync(status);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving orders by status: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets orders within a date range
        /// </summary>
        /// <param name="startDate">Start date (ISO format)</param>
        /// <param name="endDate">End date (ISO format)</param>
        /// <returns>List of orders within the date range</returns>
        [HttpGet("daterange")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByDateRange(
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            try
            {
                var orders = await _orderService.GetOrdersByDateRangeAsync(startDate, endDate);
                return Ok(orders);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving orders by date range: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing order
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <param name="order">The updated order data</param>
        /// <returns>The updated order</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<Order>> UpdateOrder(int id, [FromBody] Order order)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != order.OrderId)
                {
                    return BadRequest("Order ID in URL does not match order ID in body");
                }

                var updatedOrder = await _orderService.UpdateOrderAsync(order);
                return Ok(updatedOrder);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the order: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the status of an order
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <param name="status">The new status</param>
        /// <returns>The updated order</returns>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<Order>> UpdateOrderStatus(int id, [FromBody] OrderStatus status)
        {
            try
            {
                var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, status);
                return Ok(updatedOrder);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating order status: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds an item to an existing order
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <param name="orderItem">The order item to add</param>
        /// <returns>The updated order</returns>
        [HttpPost("{id}/items")]
        public async Task<ActionResult<Order>> AddOrderItem(int id, [FromBody] OrderItem orderItem)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedOrder = await _orderService.AddOrderItemAsync(id, orderItem);
                return Ok(updatedOrder);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while adding order item: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes an item from an existing order
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <param name="itemId">The order item ID to remove</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}/items/{itemId}")]
        public async Task<ActionResult> RemoveOrderItem(int id, int itemId)
        {
            try
            {
                var success = await _orderService.RemoveOrderItemAsync(id, itemId);
                if (!success)
                {
                    return NotFound("Order item not found");
                }
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while removing order item: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the total amount for an order
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <returns>The total amount</returns>
        [HttpGet("{id}/total")]
        public async Task<ActionResult<decimal>> GetOrderTotal(int id)
        {
            try
            {
                var total = await _orderService.CalculateOrderTotalAsync(id);
                return Ok(total);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while calculating order total: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an order
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteOrder(int id)
        {
            try
            {
                var success = await _orderService.DeleteOrderAsync(id);
                if (!success)
                {
                    return NotFound($"Order with ID {id} not found");
                }
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the order: {ex.Message}");
            }
        }
    }
}