# AWS Deployment Guide for Ordering System

This guide provides comprehensive instructions for deploying the C# Ordering System to AWS using containerized infrastructure as code.

## 📋 Table of Contents

- [Architecture Overview](#architecture-overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Detailed Deployment](#detailed-deployment)
- [Environment Configuration](#environment-configuration)
- [CI/CD Pipeline](#cicd-pipeline)
- [Monitoring and Logging](#monitoring-and-logging)
- [Troubleshooting](#troubleshooting)
- [Cost Optimization](#cost-optimization)
- [Security Considerations](#security-considerations)

## 🏗️ Architecture Overview

The deployment creates a production-ready, scalable infrastructure on AWS:

```
┌─────────────────────────────────────────────────────────────┐
│                        Internet                              │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                Application Load Balancer                     │
│                    (Public Subnets)                         │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              ECS Fargate Tasks (Private Subnets)            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   App       │  │   App       │  │   App       │         │
│  │ Container   │  │ Container   │  │ Container   │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                    NAT Gateways                             │
│                  (Public Subnets)                          │
└─────────────────────────────────────────────────────────────┘
```

### Components

- **VPC**: Isolated network with public/private subnets across multiple AZs
- **Application Load Balancer**: Distributes traffic with health checks
- **ECS Fargate**: Serverless container platform with auto-scaling
- **ECR**: Container registry for Docker images
- **CloudWatch**: Logging and monitoring
- **Auto Scaling**: Automatic scaling based on CPU/memory utilization

## 🔧 Prerequisites

### Required Tools

1. **AWS CLI** (v2.x)
   ```bash
   # Install AWS CLI
   curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
   unzip awscliv2.zip
   sudo ./aws/install
   ```

2. **Terraform** (v1.0+)
   ```bash
   # Install Terraform
   wget https://releases.hashicorp.com/terraform/1.6.0/terraform_1.6.0_linux_amd64.zip
   unzip terraform_1.6.0_linux_amd64.zip
   sudo mv terraform /usr/local/bin/
   ```

3. **Docker** (v20.x+)
   ```bash
   # Install Docker
   curl -fsSL https://get.docker.com -o get-docker.sh
   sudo sh get-docker.sh
   sudo usermod -aG docker $USER
   ```

4. **.NET 8.0 SDK**
   ```bash
   # Install .NET 8.0 SDK
   wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-8.0
   ```

### AWS Configuration

1. **Configure AWS Credentials**
   ```bash
   aws configure
   # Enter your AWS Access Key ID, Secret Access Key, and default region
   ```

2. **Verify AWS Access**
   ```bash
   aws sts get-caller-identity
   ```

3. **Required AWS Permissions**
   Your AWS user/role needs the following services:
   - EC2 (VPC, Subnets, Security Groups, Load Balancers)
   - ECS (Clusters, Services, Task Definitions)
   - ECR (Repositories, Images)
   - IAM (Roles, Policies)
   - CloudWatch (Log Groups, Alarms)
   - Application Auto Scaling

## 🚀 Quick Start

### 1. Clone and Prepare

```bash
# Navigate to your project directory
cd /workspace

# Make scripts executable
chmod +x scripts/*.sh
```

### 2. Deploy to Development

```bash
# Deploy everything (infrastructure + application)
./scripts/deploy.sh -e dev

# Or deploy with specific settings
./scripts/deploy.sh -e dev -r us-east-1 -t v1.0.0
```

### 3. Access Your Application

After deployment, you'll see output like:
```
Application is available at: http://your-alb-dns-name.us-east-1.elb.amazonaws.com
API Documentation: http://your-alb-dns-name.us-east-1.elb.amazonaws.com/swagger
Health Check: http://your-alb-dns-name.us-east-1.elb.amazonaws.com/health
```

## 📋 Detailed Deployment

### Step 1: Infrastructure Deployment

```bash
cd infrastructure

# Initialize Terraform
terraform init

# Plan deployment for development
terraform plan -var-file="environments/dev.tfvars" -out=tfplan

# Apply the plan
terraform apply tfplan
```

### Step 2: Build and Push Container

```bash
# Get ECR login token
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com

# Build and tag image
docker build -t ordering-system:latest .
docker tag ordering-system:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/ordering-system-app:latest

# Push to ECR
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/ordering-system-app:latest
```

### Step 3: Deploy Application

```bash
# Update ECS service with new image
aws ecs update-service \
  --cluster ordering-system-cluster \
  --service ordering-system-service \
  --force-new-deployment
```

## ⚙️ Environment Configuration

### Development Environment

```bash
# Deploy to development
./scripts/deploy.sh -e dev

# Configuration in infrastructure/environments/dev.tfvars:
# - 1 ECS task (minimal resources)
# - Basic auto-scaling (1-3 tasks)
# - 3-day log retention
# - Development-optimized settings
```

### Production Environment

```bash
# Deploy to production
./scripts/deploy.sh -e prod

# Configuration in infrastructure/environments/prod.tfvars:
# - 3 ECS tasks (high availability)
# - Aggressive auto-scaling (2-10 tasks)
# - 30-day log retention
# - Production-optimized settings
```

### Custom Configuration

Create your own `.tfvars` file:

```bash
# Copy example configuration
cp infrastructure/terraform.tfvars.example infrastructure/terraform.tfvars

# Edit the configuration
vim infrastructure/terraform.tfvars

# Deploy with custom configuration
terraform plan -var-file="terraform.tfvars"
terraform apply
```

## 🔄 CI/CD Pipeline

### GitHub Actions Setup

1. **Add Repository Secrets**
   ```
   AWS_ACCESS_KEY_ID: Your AWS access key
   AWS_SECRET_ACCESS_KEY: Your AWS secret key
   ```

2. **Workflow Triggers**
   - **Push to `main`**: Deploys to production
   - **Push to `develop`**: Deploys to development
   - **Pull Requests**: Runs tests and security scans
   - **Manual Dispatch**: Deploy to any environment

3. **Pipeline Stages**
   - **Test**: Run .NET tests
   - **Security Scan**: Trivy vulnerability scanning
   - **Build**: Create and push Docker image
   - **Deploy**: Update infrastructure and application
   - **Health Check**: Verify deployment success

### Manual Deployment

```bash
# Deploy specific version
./scripts/deploy.sh -e prod -t v2.1.0

# Skip infrastructure updates (app only)
./scripts/deploy.sh -e prod --skip-infrastructure

# Deploy to custom region
./scripts/deploy.sh -e prod -r us-west-2
```

## 📊 Monitoring and Logging

### CloudWatch Logs

```bash
# View application logs
aws logs tail /ecs/ordering-system --follow

# View logs for specific time period
aws logs tail /ecs/ordering-system --since 1h
```

### Application Metrics

The deployment automatically creates CloudWatch alarms for:
- High CPU utilization (>85%)
- High memory utilization (>85%)
- ECS service health

### Health Checks

- **Load Balancer**: `/health` endpoint every 30 seconds
- **ECS Tasks**: Container health checks
- **Auto Scaling**: Based on CPU/memory metrics

### Monitoring Dashboard

Access CloudWatch dashboards:
1. Go to AWS Console → CloudWatch
2. Navigate to Dashboards
3. Look for `ordering-system-*` dashboards

## 🔧 Troubleshooting

### Common Issues

#### 1. Deployment Fails with "Repository does not exist"

```bash
# Create ECR repository manually
aws ecr create-repository --repository-name ordering-system-app --region us-east-1
```

#### 2. ECS Tasks Keep Restarting

```bash
# Check task logs
aws logs tail /ecs/ordering-system --follow

# Check task definition
aws ecs describe-tasks --cluster ordering-system-cluster --tasks <task-arn>
```

#### 3. Application Not Accessible

```bash
# Check load balancer status
aws elbv2 describe-load-balancers --names ordering-system-alb

# Check target group health
aws elbv2 describe-target-health --target-group-arn <target-group-arn>
```

#### 4. High Costs

```bash
# Check running resources
aws ecs list-services --cluster ordering-system-cluster
aws ec2 describe-nat-gateways
aws logs describe-log-groups --log-group-name-prefix "/ecs/ordering-system"
```

### Debug Commands

```bash
# Check ECS service status
aws ecs describe-services --cluster ordering-system-cluster --services ordering-system-service

# View recent deployments
aws ecs list-tasks --cluster ordering-system-cluster --service-name ordering-system-service

# Check auto scaling
aws application-autoscaling describe-scalable-targets --service-namespace ecs
```

## 💰 Cost Optimization

### Development Environment

- **Fargate**: 0.25 vCPU, 512 MB RAM
- **NAT Gateway**: Single gateway (consider NAT instances for lower cost)
- **Log Retention**: 3 days
- **Auto Scaling**: Conservative settings

**Estimated Monthly Cost**: $50-80

### Production Environment

- **Fargate**: 0.5 vCPU, 1024 MB RAM (3 tasks)
- **NAT Gateway**: Multi-AZ for high availability
- **Log Retention**: 30 days
- **Auto Scaling**: Responsive settings

**Estimated Monthly Cost**: $150-250

### Cost Reduction Tips

1. **Use Spot Instances** (for non-critical workloads):
   ```hcl
   capacity_providers = ["FARGATE_SPOT"]
   ```

2. **Optimize Log Retention**:
   ```hcl
   log_retention_days = 7  # Instead of 30
   ```

3. **Scale Down During Off-Hours**:
   ```bash
   # Scale to 0 tasks (development only)
   aws ecs update-service --cluster ordering-system-cluster --service ordering-system-service --desired-count 0
   ```

4. **Use Reserved Capacity** (for predictable workloads)

## 🔒 Security Considerations

### Network Security

- **Private Subnets**: Application runs in private subnets
- **Security Groups**: Restrictive ingress/egress rules
- **NAT Gateways**: Secure outbound internet access

### Application Security

- **Non-root User**: Container runs as non-privileged user
- **Image Scanning**: ECR automatically scans for vulnerabilities
- **Secrets Management**: Use AWS Secrets Manager for sensitive data

### Access Control

- **IAM Roles**: Least privilege access for ECS tasks
- **VPC Endpoints**: Secure access to AWS services
- **SSL/TLS**: Enable HTTPS with ACM certificates (optional)

### Security Best Practices

1. **Enable GuardDuty** for threat detection
2. **Use AWS Config** for compliance monitoring
3. **Implement WAF** for application protection
4. **Regular Security Scans** with tools like Trivy

## 🗑️ Cleanup

### Destroy Environment

```bash
# Destroy development environment
./scripts/destroy.sh -e dev

# Destroy production environment (with confirmation)
./scripts/destroy.sh -e prod

# Force destroy without confirmation (dangerous!)
./scripts/destroy.sh -e dev --force
```

### Manual Cleanup

```bash
# Stop ECS service
aws ecs update-service --cluster ordering-system-cluster --service ordering-system-service --desired-count 0

# Delete ECR images
aws ecr list-images --repository-name ordering-system-app --query 'imageIds[*]' --output text | \
while read digest tag; do
  aws ecr batch-delete-image --repository-name ordering-system-app --image-ids imageDigest=$digest
done

# Destroy infrastructure
cd infrastructure
terraform destroy -var-file="environments/dev.tfvars"
```

## 📞 Support

### Getting Help

1. **Check Logs**: Always start with CloudWatch logs
2. **AWS Documentation**: Refer to AWS ECS and Fargate documentation
3. **Terraform Documentation**: Check Terraform AWS provider docs
4. **GitHub Issues**: Report bugs and feature requests

### Useful Commands

```bash
# Local development
./scripts/local-dev.sh run          # Run locally with .NET
./scripts/local-dev.sh docker       # Run with Docker
./scripts/local-dev.sh compose      # Run with Docker Compose

# Deployment
./scripts/deploy.sh -h              # Show deployment help
./scripts/destroy.sh -h             # Show destroy help

# Monitoring
aws logs tail /ecs/ordering-system --follow
```

---

## 📝 Additional Resources

- [AWS ECS Documentation](https://docs.aws.amazon.com/ecs/)
- [AWS Fargate Documentation](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html)
- [Terraform AWS Provider](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [.NET Docker Documentation](https://docs.microsoft.com/en-us/dotnet/core/docker/)

---

**Happy Deploying! 🚀**