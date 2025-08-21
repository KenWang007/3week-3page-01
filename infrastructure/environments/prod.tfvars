# Production environment configuration

project_name = "ordering-system"
environment  = "prod"
aws_region   = "us-east-1"

# VPC Configuration - production ready
vpc_cidr = "10.0.0.0/16"
public_subnet_cidrs = [
  "10.0.1.0/24",
  "10.0.2.0/24"
]
private_subnet_cidrs = [
  "10.0.10.0/24",
  "10.0.20.0/24"
]

# ECS Configuration - production resources
fargate_cpu    = 512
fargate_memory = 1024
app_count      = 3
container_port = 8080

# Auto Scaling - aggressive for production
auto_scaling_enabled        = true
min_capacity               = 2
max_capacity               = 10
target_cpu_utilization     = 60
target_memory_utilization  = 70

# Health Check - production settings
health_check_path       = "/health"
health_check_interval   = 30
health_check_timeout    = 5
healthy_threshold       = 2
unhealthy_threshold     = 2

# Logging - longer retention for production
log_retention_days = 30

# Tags
common_tags = {
  Project     = "ordering-system"
  Environment = "prod"
  ManagedBy   = "terraform"
  Owner       = "devops"
  Purpose     = "production"
  Backup      = "required"
  Monitoring  = "critical"
}