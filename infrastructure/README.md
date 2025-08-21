# Infrastructure as Code for Ordering System

This directory contains all the Infrastructure as Code (IaC) scripts and configurations needed to deploy the Ordering System application to AWS using ECS Fargate.

## Quick Start

### Prerequisites

1. AWS CLI configured with appropriate credentials
2. Terraform >= 1.0 installed
3. Docker installed
4. An AWS account with necessary permissions

### Deployment Steps

1. **Set up the infrastructure:**
   ```bash
   cd scripts
   ./setup-infrastructure.sh dev
   ```

2. **Test locally (optional):**
   ```bash
   ./local-test.sh
   ```

3. **Deploy to AWS:**
   ```bash
   ./deploy.sh dev
   ```

## Directory Structure

```
infrastructure/
├── terraform/              # Terraform configuration files
│   ├── main.tf            # Main infrastructure definition
│   ├── variables.tf       # Input variables
│   ├── outputs.tf         # Output values
│   └── terraform.tfvars.example  # Example variables file
├── scripts/               # Helper scripts
│   ├── deploy.sh         # Deploy application to ECS
│   ├── setup-infrastructure.sh  # Set up AWS infrastructure
│   └── local-test.sh     # Test application locally
├── DEPLOYMENT.md         # Detailed deployment guide
└── README.md            # This file
```

## Architecture

The infrastructure creates:

- **VPC** with public and private subnets across 2 AZs
- **ECS Cluster** running on Fargate
- **Application Load Balancer** for traffic distribution
- **ECR Repository** for Docker images
- **Auto Scaling** based on CPU/Memory metrics
- **CloudWatch Logs** for application logging

## Configuration

Copy the example variables file and customize:

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values
```

Key configuration options:

- `environment`: Environment name (dev/staging/production)
- `aws_region`: AWS region to deploy to
- `desired_count`: Number of containers to run
- `task_cpu`: CPU units (256 = 0.25 vCPU)
- `task_memory`: Memory in MB
- `enable_nat_gateway`: Enable NAT for private subnets

## CI/CD

The repository includes GitHub Actions workflow for automated deployments:

- Automatic deployments on push to main/develop
- Manual deployments with environment selection
- Docker image building and pushing to ECR
- ECS service updates with zero-downtime deployments

## Monitoring

After deployment, monitor your application:

1. **CloudWatch Logs**: `/ecs/ordering-system-{environment}`
2. **ECS Console**: View service health and metrics
3. **ALB Target Group**: Check health check status

## Costs

Estimated monthly costs (us-east-1):

- ECS Fargate (2 tasks, 0.25 vCPU, 512MB): ~$18
- Application Load Balancer: ~$16
- NAT Gateway (if enabled): ~$45
- Data transfer and other services: Variable

Total: ~$80-100/month for development environment

## Security

- Applications run in private subnets
- ALB handles public traffic
- Security groups restrict access
- IAM roles follow least privilege
- Container images scanned for vulnerabilities

## Cleanup

To destroy all resources:

```bash
cd terraform
terraform destroy
```

## Support

For detailed instructions, see [DEPLOYMENT.md](DEPLOYMENT.md)