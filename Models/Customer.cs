using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderingSystem.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; }
        
        public string FullName => $"{FirstName} {LastName}";
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Phone]
        public string PhoneNumber { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public string Address { get; set; }
        
        public string City { get; set; }
        
        public string State { get; set; }
        
        public string PostalCode { get; set; }
        
        public string Country { get; set; }
        
        public List<Order> Orders { get; set; } = new List<Order>();
        
        public bool IsActive { get; set; } = true;
    }
}