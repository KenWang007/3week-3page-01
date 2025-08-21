#!/bin/bash

# Destroy script for the Ordering System AWS infrastructure
# This script safely destroys all AWS resources created by Terraform

set -e  # Exit on any error

# Configuration
PROJECT_NAME="ordering-system"
AWS_REGION="${AWS_REGION:-us-east-1}"
ENVIRONMENT="${ENVIRONMENT:-dev}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to confirm destruction
confirm_destruction() {
    log_warning "This will destroy ALL AWS resources for the ${PROJECT_NAME} project in ${ENVIRONMENT} environment."
    log_warning "This action cannot be undone!"
    
    read -p "Are you sure you want to continue? (yes/no): " -r
    if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        log_info "Destruction cancelled."
        exit 0
    fi
    
    log_warning "Last chance! Type 'DESTROY' to confirm:"
    read -p "> " -r
    if [[ $REPLY != "DESTROY" ]]; then
        log_info "Destruction cancelled."
        exit 0
    fi
}

# Function to scale down ECS service
scale_down_ecs_service() {
    log_info "Scaling down ECS service to 0 tasks..."
    
    cd infrastructure
    
    # Check if Terraform state exists
    if [ ! -f "terraform.tfstate" ]; then
        log_warning "No Terraform state found. Skipping ECS service scale down."
        cd ..
        return
    fi
    
    # Get the cluster and service names from Terraform output
    local cluster_name=$(terraform output -raw ecs_cluster_name 2>/dev/null || echo "")
    local service_name=$(terraform output -raw ecs_service_name 2>/dev/null || echo "")
    
    cd ..
    
    if [ -n "$cluster_name" ] && [ -n "$service_name" ]; then
        log_info "Scaling down service ${service_name} in cluster ${cluster_name}..."
        
        aws ecs update-service \
            --cluster ${cluster_name} \
            --service ${service_name} \
            --desired-count 0 \
            --region ${AWS_REGION} || log_warning "Failed to scale down ECS service"
        
        log_info "Waiting for tasks to stop..."
        aws ecs wait services-stable \
            --cluster ${cluster_name} \
            --services ${service_name} \
            --region ${AWS_REGION} || log_warning "Failed to wait for service to stabilize"
        
        log_success "ECS service scaled down successfully"
    else
        log_warning "Could not determine ECS service details. Skipping scale down."
    fi
}

# Function to empty ECR repository
empty_ecr_repository() {
    log_info "Emptying ECR repository..."
    
    local repository_name="${PROJECT_NAME}-app"
    
    # Check if repository exists
    if aws ecr describe-repositories --repository-names ${repository_name} --region ${AWS_REGION} &>/dev/null; then
        log_info "Found ECR repository: ${repository_name}"
        
        # Get all image digests
        local image_digests=$(aws ecr list-images \
            --repository-name ${repository_name} \
            --region ${AWS_REGION} \
            --query 'imageIds[*].imageDigest' \
            --output text)
        
        if [ -n "$image_digests" ]; then
            log_info "Deleting images from ECR repository..."
            
            # Convert space-separated digests to JSON format for batch delete
            local image_ids=""
            for digest in $image_digests; do
                if [ -n "$image_ids" ]; then
                    image_ids="${image_ids},"
                fi
                image_ids="${image_ids}{\"imageDigest\":\"${digest}\"}"
            done
            
            aws ecr batch-delete-image \
                --repository-name ${repository_name} \
                --image-ids "[${image_ids}]" \
                --region ${AWS_REGION} || log_warning "Failed to delete some images"
            
            log_success "ECR repository emptied"
        else
            log_info "ECR repository is already empty"
        fi
    else
        log_info "ECR repository does not exist or already deleted"
    fi
}

# Function to destroy infrastructure
destroy_infrastructure() {
    log_info "Destroying infrastructure with Terraform..."
    
    cd infrastructure
    
    # Check if Terraform is initialized
    if [ ! -d ".terraform" ]; then
        log_info "Initializing Terraform..."
        terraform init
    fi
    
    # Plan the destruction
    log_info "Planning Terraform destruction..."
    terraform plan -destroy -var="environment=${ENVIRONMENT}" -var="aws_region=${AWS_REGION}" -out=destroy.tfplan
    
    # Apply the destruction
    log_info "Applying Terraform destruction..."
    terraform apply destroy.tfplan
    
    # Clean up Terraform files
    log_info "Cleaning up Terraform state files..."
    rm -f terraform.tfstate terraform.tfstate.backup destroy.tfplan tfplan
    
    cd ..
    
    log_success "Infrastructure destroyed successfully"
}

# Function to verify destruction
verify_destruction() {
    log_info "Verifying that resources have been destroyed..."
    
    local errors=0
    
    # Check ECS cluster
    if aws ecs describe-clusters --clusters "${PROJECT_NAME}-cluster" --region ${AWS_REGION} &>/dev/null; then
        log_error "ECS cluster still exists"
        errors=$((errors + 1))
    fi
    
    # Check ECR repository
    if aws ecr describe-repositories --repository-names "${PROJECT_NAME}-app" --region ${AWS_REGION} &>/dev/null; then
        log_error "ECR repository still exists"
        errors=$((errors + 1))
    fi
    
    # Check Load Balancer
    local lb_arn=$(aws elbv2 describe-load-balancers \
        --names "${PROJECT_NAME}-alb" \
        --region ${AWS_REGION} \
        --query 'LoadBalancers[0].LoadBalancerArn' \
        --output text 2>/dev/null || echo "None")
    
    if [ "$lb_arn" != "None" ] && [ "$lb_arn" != "" ]; then
        log_error "Load Balancer still exists"
        errors=$((errors + 1))
    fi
    
    if [ $errors -eq 0 ]; then
        log_success "All resources have been successfully destroyed"
    else
        log_warning "Some resources may still exist. Please check the AWS console."
    fi
}

# Main destruction function
main() {
    log_info "Starting destruction of ${PROJECT_NAME} AWS infrastructure"
    log_info "Environment: ${ENVIRONMENT}"
    log_info "Region: ${AWS_REGION}"
    
    # Confirm destruction
    confirm_destruction
    
    # Scale down ECS service first
    scale_down_ecs_service
    
    # Empty ECR repository
    empty_ecr_repository
    
    # Wait a bit for resources to settle
    log_info "Waiting 30 seconds for resources to settle..."
    sleep 30
    
    # Destroy infrastructure
    destroy_infrastructure
    
    # Verify destruction
    verify_destruction
    
    log_success "Destruction process completed!"
}

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help              Show this help message"
    echo "  -e, --environment ENV   Set environment (default: dev)"
    echo "  -r, --region REGION     Set AWS region (default: us-east-1)"
    echo "  -f, --force             Skip confirmation prompts (dangerous!)"
    echo ""
    echo "Environment Variables:"
    echo "  AWS_REGION              AWS region (default: us-east-1)"
    echo "  ENVIRONMENT             Environment name (default: dev)"
    echo ""
    echo "Examples:"
    echo "  $0                      # Destroy dev environment with confirmation"
    echo "  $0 -e prod -r us-west-2 # Destroy prod in us-west-2"
    echo "  $0 -f                   # Force destroy without confirmation (dangerous!)"
}

# Parse command line arguments
FORCE=false
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            usage
            exit 0
            ;;
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -r|--region)
            AWS_REGION="$2"
            shift 2
            ;;
        -f|--force)
            FORCE=true
            shift
            ;;
        *)
            log_error "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Override confirmation if force is set
if [ "$FORCE" = true ]; then
    confirm_destruction() {
        log_warning "Force mode enabled - skipping confirmation"
    }
fi

# Run main function
main