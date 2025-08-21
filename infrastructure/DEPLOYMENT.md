# AWS ECS Deployment Guide for Ordering System

This guide provides comprehensive instructions for deploying the Ordering System application to AWS using ECS Fargate, Terraform, and GitHub Actions.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Prerequisites](#prerequisites)
3. [Initial Setup](#initial-setup)
4. [Infrastructure Deployment](#infrastructure-deployment)
5. [Application Deployment](#application-deployment)
6. [CI/CD Pipeline](#cicd-pipeline)
7. [Monitoring and Maintenance](#monitoring-and-maintenance)
8. [Troubleshooting](#troubleshooting)
9. [Cost Optimization](#cost-optimization)
10. [Security Considerations](#security-considerations)

## Architecture Overview

The deployment uses the following AWS services:

- **Amazon ECS Fargate**: Serverless container hosting
- **Application Load Balancer (ALB)**: Traffic distribution
- **Amazon ECR**: Container image registry
- **Amazon VPC**: Network isolation with public/private subnets
- **CloudWatch**: Logging and monitoring
- **Auto Scaling**: Automatic scaling based on CPU/memory metrics

### Infrastructure Components

```
┌─────────────────┐
│   Route 53      │ (Optional: Custom Domain)
└────────┬────────┘
         │
┌────────▼────────┐
│      ALB        │ (Application Load Balancer)
└────────┬────────┘
         │
┌────────▼────────┐
│   Target Group  │
└────────┬────────┘
         │
┌────────▼────────┐
│   ECS Service   │ (Fargate)
├─────────────────┤
│  ┌───────────┐  │
│  │Container 1│  │
│  └───────────┘  │
│  ┌───────────┐  │
│  │Container 2│  │
│  └───────────┘  │
└─────────────────┘
```

## Prerequisites

1. **AWS Account** with appropriate permissions
2. **AWS CLI** installed and configured
3. **Terraform** >= 1.0
4. **Docker** installed locally
5. **GitHub Account** (for CI/CD)
6. **.NET 8.0 SDK** (for local development)

### Required AWS Permissions

Create an IAM user with the following policies:
- AmazonECS_FullAccess
- AmazonEC2ContainerRegistryFullAccess
- AmazonVPCFullAccess
- ElasticLoadBalancingFullAccess
- IAMFullAccess (or specific role creation permissions)
- CloudWatchLogsFullAccess
- AmazonRoute53FullAccess (if using custom domain)

## Initial Setup

### 1. Clone the Repository

```bash
git clone <your-repository-url>
cd ordering-system
```

### 2. Configure AWS Credentials

```bash
aws configure
# Enter your AWS Access Key ID
# Enter your AWS Secret Access Key
# Enter your default region (e.g., us-east-1)
# Enter output format (json)
```

### 3. Create S3 Backend for Terraform State (Recommended)

```bash
# Create S3 bucket for Terraform state
aws s3api create-bucket \
  --bucket your-terraform-state-bucket \
  --region us-east-1

# Enable versioning
aws s3api put-bucket-versioning \
  --bucket your-terraform-state-bucket \
  --versioning-configuration Status=Enabled

# Enable encryption
aws s3api put-bucket-encryption \
  --bucket your-terraform-state-bucket \
  --server-side-encryption-configuration '{
    "Rules": [{
      "ApplyServerSideEncryptionByDefault": {
        "SSEAlgorithm": "AES256"
      }
    }]
  }'
```

### 4. Update Terraform Backend Configuration

Edit `infrastructure/terraform/main.tf` and uncomment the backend configuration:

```hcl
backend "s3" {
  bucket = "your-terraform-state-bucket"
  key    = "ordering-system/terraform.tfstate"
  region = "us-east-1"
}
```

## Infrastructure Deployment

### 1. Navigate to Terraform Directory

```bash
cd infrastructure/terraform
```

### 2. Create terraform.tfvars

```bash
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your specific values
```

### 3. Initialize Terraform

```bash
terraform init
```

### 4. Plan the Infrastructure

```bash
terraform plan
```

### 5. Apply the Infrastructure

```bash
terraform apply
# Review the plan and type 'yes' to confirm
```

### 6. Note the Outputs

After successful deployment, note these important outputs:
- `ecr_repository_url`: ECR repository for Docker images
- `alb_dns_name`: Application Load Balancer URL
- `ecs_cluster_name`: ECS cluster name
- `ecs_service_name`: ECS service name

## Application Deployment

### 1. Build and Push Docker Image

```bash
# Navigate to project root
cd ../..

# Get ECR login token
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ecr_repository_url>

# Build Docker image
docker build -t ordering-system .

# Tag the image
docker tag ordering-system:latest <ecr_repository_url>:latest

# Push to ECR
docker push <ecr_repository_url>:latest
```

### 2. Update ECS Service

The ECS service will automatically pull the latest image. To force a new deployment:

```bash
aws ecs update-service \
  --cluster ordering-system-dev-cluster \
  --service ordering-system-dev-service \
  --force-new-deployment
```

### 3. Verify Deployment

```bash
# Check service status
aws ecs describe-services \
  --cluster ordering-system-dev-cluster \
  --services ordering-system-dev-service

# Get ALB URL
echo "Application URL: http://$(terraform output -raw alb_dns_name)"
```

## CI/CD Pipeline

### 1. Set up GitHub Secrets

In your GitHub repository, go to Settings > Secrets and add:

- `AWS_ACCESS_KEY_ID`: Your AWS access key
- `AWS_SECRET_ACCESS_KEY`: Your AWS secret key

### 2. GitHub Actions Workflow

The workflow is already configured in `.github/workflows/deploy.yml`. It will:

- Trigger on pushes to `main` and `develop` branches
- Build and push Docker images to ECR
- Deploy to ECS automatically
- Support manual deployments with environment selection

### 3. Manual Deployment

To manually deploy:
1. Go to Actions tab in GitHub
2. Select "Deploy to AWS ECS" workflow
3. Click "Run workflow"
4. Select the environment (dev/staging/production)
5. Click "Run workflow"

## Monitoring and Maintenance

### 1. CloudWatch Logs

View application logs:

```bash
# List log streams
aws logs describe-log-streams \
  --log-group-name /ecs/ordering-system-dev

# View recent logs
aws logs filter-log-events \
  --log-group-name /ecs/ordering-system-dev \
  --start-time $(date -u -d '1 hour ago' +%s)000
```

### 2. CloudWatch Metrics

Monitor key metrics:
- CPU Utilization
- Memory Utilization
- Request Count
- Target Response Time

### 3. Auto Scaling

The service automatically scales based on:
- CPU utilization > 70%
- Memory utilization > 80%

Adjust thresholds in `terraform.tfvars`:

```hcl
cpu_threshold    = 70
memory_threshold = 80
min_capacity     = 1
max_capacity     = 10
```

### 4. Health Checks

The ALB performs health checks on the `/` endpoint. Customize in `terraform.tfvars`:

```hcl
health_check_path = "/health"
```

## Troubleshooting

### Common Issues

1. **Task fails to start**
   ```bash
   # Check task logs
   aws ecs describe-tasks \
     --cluster ordering-system-dev-cluster \
     --tasks <task-arn>
   ```

2. **ALB health checks failing**
   - Ensure the application responds on the configured port (8080)
   - Check security group rules
   - Verify the health check path returns 200 OK

3. **Out of memory errors**
   - Increase task memory in `terraform.tfvars`
   - Check for memory leaks in the application

4. **Cannot pull Docker image**
   - Verify ECR permissions in the task execution role
   - Check if the image exists in ECR

### Debugging Commands

```bash
# List all tasks
aws ecs list-tasks --cluster ordering-system-dev-cluster

# Describe a specific task
aws ecs describe-tasks \
  --cluster ordering-system-dev-cluster \
  --tasks <task-arn>

# View container logs
aws logs get-log-events \
  --log-group-name /ecs/ordering-system-dev \
  --log-stream-name <log-stream-name>

# Check ALB target health
aws elbv2 describe-target-health \
  --target-group-arn <target-group-arn>
```

## Cost Optimization

### 1. Right-size Resources

Start with minimal resources and scale up based on monitoring:

```hcl
# Development environment
task_cpu    = "256"   # 0.25 vCPU
task_memory = "512"   # 512 MB

# Production environment
task_cpu    = "1024"  # 1 vCPU
task_memory = "2048"  # 2 GB
```

### 2. Use Spot Capacity (Optional)

For non-critical workloads, consider Fargate Spot:

```hcl
capacity_providers = ["FARGATE_SPOT", "FARGATE"]
```

### 3. Implement Proper Auto Scaling

Ensure auto scaling is properly configured to scale down during low traffic.

### 4. Clean Up Unused Resources

```bash
# Delete old ECR images
aws ecr list-images --repository-name ordering-system-dev \
  --filter tagStatus=UNTAGGED \
  --query 'imageIds[*]' \
  --output json | \
  jq '.[] | @base64' | \
  while read -r image; do
    aws ecr batch-delete-image \
      --repository-name ordering-system-dev \
      --image-ids "$(echo $image | base64 -d)"
  done
```

## Security Considerations

### 1. Network Security

- Application runs in private subnets
- ALB in public subnets handles external traffic
- Security groups restrict traffic appropriately

### 2. Container Security

- Use specific image tags (not `latest` in production)
- Regularly update base images
- Scan images for vulnerabilities:

```bash
aws ecr start-image-scan \
  --repository-name ordering-system-dev \
  --image-id imageTag=latest
```

### 3. Secrets Management

For production, use AWS Secrets Manager:

```hcl
# In task definition
secrets = [
  {
    name      = "DB_CONNECTION_STRING"
    valueFrom = "arn:aws:secretsmanager:region:account:secret:name"
  }
]
```

### 4. IAM Best Practices

- Use least privilege principle
- Separate roles for different environments
- Enable MFA for AWS console access

## Cleanup

To destroy all resources:

```bash
cd infrastructure/terraform
terraform destroy
# Type 'yes' to confirm
```

## Next Steps

1. **Add HTTPS**: Configure ACM certificate and update ALB listener
2. **Add Custom Domain**: Use Route 53 for DNS management
3. **Implement CI/CD**: Enhance GitHub Actions with testing stages
4. **Add Monitoring**: Set up CloudWatch dashboards and alarms
5. **Implement Blue/Green Deployments**: Use ECS deployment strategies

## Support

For issues or questions:
1. Check CloudWatch logs
2. Review Terraform state
3. Verify AWS service limits
4. Check GitHub Actions logs