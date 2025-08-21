# Project Configuration
variable "project_name" {
  description = "Name of the project"
  type        = string
  default     = "ordering-system"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

# AWS Configuration
variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

# VPC Configuration
variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for public subnets"
  type        = list(string)
  default     = ["10.0.1.0/24", "10.0.2.0/24"]
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for private subnets"
  type        = list(string)
  default     = ["10.0.10.0/24", "10.0.20.0/24"]
}

# ECS Configuration
variable "fargate_cpu" {
  description = "Fargate instance CPU units to provision (1 vCPU = 1024 CPU units)"
  type        = number
  default     = 256
  
  validation {
    condition = contains([256, 512, 1024, 2048, 4096], var.fargate_cpu)
    error_message = "Fargate CPU must be one of 256, 512, 1024, 2048, 4096."
  }
}

variable "fargate_memory" {
  description = "Fargate instance memory to provision (in MiB)"
  type        = number
  default     = 512
  
  validation {
    condition = var.fargate_memory >= 512 && var.fargate_memory <= 30720
    error_message = "Fargate memory must be between 512 and 30720 MiB."
  }
}

variable "app_count" {
  description = "Number of app instances to run"
  type        = number
  default     = 2
  
  validation {
    condition = var.app_count >= 1 && var.app_count <= 10
    error_message = "App count must be between 1 and 10."
  }
}

variable "container_port" {
  description = "Port exposed by the docker image to redirect traffic to"
  type        = number
  default     = 8080
}

# Application Configuration
variable "app_image_tag" {
  description = "Docker image tag for the application"
  type        = string
  default     = "latest"
}

# Auto Scaling Configuration
variable "auto_scaling_enabled" {
  description = "Enable auto scaling for ECS service"
  type        = bool
  default     = true
}

variable "min_capacity" {
  description = "Minimum number of tasks for auto scaling"
  type        = number
  default     = 1
}

variable "max_capacity" {
  description = "Maximum number of tasks for auto scaling"
  type        = number
  default     = 6
}

variable "target_cpu_utilization" {
  description = "Target CPU utilization percentage for auto scaling"
  type        = number
  default     = 70
}

variable "target_memory_utilization" {
  description = "Target memory utilization percentage for auto scaling"
  type        = number
  default     = 80
}

# Health Check Configuration
variable "health_check_path" {
  description = "Health check path"
  type        = string
  default     = "/health"
}

variable "health_check_interval" {
  description = "Health check interval in seconds"
  type        = number
  default     = 30
}

variable "health_check_timeout" {
  description = "Health check timeout in seconds"
  type        = number
  default     = 5
}

variable "healthy_threshold" {
  description = "Number of consecutive successful health checks"
  type        = number
  default     = 2
}

variable "unhealthy_threshold" {
  description = "Number of consecutive failed health checks"
  type        = number
  default     = 3
}

# Logging Configuration
variable "log_retention_days" {
  description = "CloudWatch log retention in days"
  type        = number
  default     = 7
  
  validation {
    condition = contains([1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1827, 3653], var.log_retention_days)
    error_message = "Log retention days must be a valid CloudWatch retention period."
  }
}

# Tags
variable "common_tags" {
  description = "Common tags to apply to all resources"
  type        = map(string)
  default = {
    Project     = "ordering-system"
    ManagedBy   = "terraform"
    Owner       = "devops"
  }
}