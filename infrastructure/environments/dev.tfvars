# Development environment configuration

project_name = "ordering-system"
environment  = "dev"
aws_region   = "us-east-1"

# VPC Configuration - smaller for dev
vpc_cidr = "10.0.0.0/16"
public_subnet_cidrs = [
  "10.0.1.0/24",
  "10.0.2.0/24"
]
private_subnet_cidrs = [
  "10.0.10.0/24",
  "10.0.20.0/24"
]

# ECS Configuration - minimal resources for dev
fargate_cpu    = 256
fargate_memory = 512
app_count      = 1
container_port = 8080

# Auto Scaling - conservative for dev
auto_scaling_enabled        = true
min_capacity               = 1
max_capacity               = 3
target_cpu_utilization     = 80
target_memory_utilization  = 85

# Health Check - faster checks for dev
health_check_path       = "/health"
health_check_interval   = 30
health_check_timeout    = 5
healthy_threshold       = 2
unhealthy_threshold     = 3

# Logging - shorter retention for dev
log_retention_days = 3

# Tags
common_tags = {
  Project     = "ordering-system"
  Environment = "dev"
  ManagedBy   = "terraform"
  Owner       = "devops"
  Purpose     = "development"
}