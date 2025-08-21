#!/bin/bash

# AWS ECS Deployment Script
# This script builds and deploys the Ordering System to AWS ECS

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=${1:-dev}
AWS_REGION=${AWS_REGION:-us-east-1}
PROJECT_NAME="ordering-system"

echo -e "${GREEN}Starting deployment for environment: ${ENVIRONMENT}${NC}"

# Check prerequisites
command -v aws >/dev/null 2>&1 || { echo -e "${RED}AWS CLI is required but not installed.${NC}" >&2; exit 1; }
command -v docker >/dev/null 2>&1 || { echo -e "${RED}Docker is required but not installed.${NC}" >&2; exit 1; }

# Get AWS account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
if [ -z "$AWS_ACCOUNT_ID" ]; then
    echo -e "${RED}Failed to get AWS account ID. Please configure AWS credentials.${NC}"
    exit 1
fi

# ECR repository URL
ECR_REPOSITORY="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/${PROJECT_NAME}-${ENVIRONMENT}"

echo -e "${YELLOW}ECR Repository: ${ECR_REPOSITORY}${NC}"

# Login to ECR
echo -e "${GREEN}Logging in to ECR...${NC}"
aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com

# Build Docker image
echo -e "${GREEN}Building Docker image...${NC}"
docker build -t ${PROJECT_NAME} .

# Tag image
IMAGE_TAG=${GITHUB_SHA:-$(git rev-parse --short HEAD 2>/dev/null || echo "latest")}
docker tag ${PROJECT_NAME}:latest ${ECR_REPOSITORY}:${IMAGE_TAG}
docker tag ${PROJECT_NAME}:latest ${ECR_REPOSITORY}:latest

# Push to ECR
echo -e "${GREEN}Pushing image to ECR...${NC}"
docker push ${ECR_REPOSITORY}:${IMAGE_TAG}
docker push ${ECR_REPOSITORY}:latest

# Update ECS service
echo -e "${GREEN}Updating ECS service...${NC}"
CLUSTER_NAME="${PROJECT_NAME}-${ENVIRONMENT}-cluster"
SERVICE_NAME="${PROJECT_NAME}-${ENVIRONMENT}-service"

aws ecs update-service \
    --cluster ${CLUSTER_NAME} \
    --service ${SERVICE_NAME} \
    --force-new-deployment \
    --region ${AWS_REGION}

echo -e "${GREEN}Waiting for service to stabilize...${NC}"
aws ecs wait services-stable \
    --cluster ${CLUSTER_NAME} \
    --services ${SERVICE_NAME} \
    --region ${AWS_REGION}

# Get ALB URL
ALB_DNS=$(aws elbv2 describe-load-balancers \
    --names ${PROJECT_NAME}-${ENVIRONMENT}-alb \
    --query 'LoadBalancers[0].DNSName' \
    --output text \
    --region ${AWS_REGION})

echo -e "${GREEN}Deployment completed successfully!${NC}"
echo -e "${GREEN}Application URL: http://${ALB_DNS}${NC}"