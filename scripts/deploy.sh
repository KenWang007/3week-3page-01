#!/bin/bash

# Deploy script for the Ordering System to AWS ECS
# This script builds, pushes, and deploys the application

set -e  # Exit on any error

# Configuration
PROJECT_NAME="ordering-system"
AWS_REGION="${AWS_REGION:-us-east-1}"
ENVIRONMENT="${ENVIRONMENT:-dev}"
IMAGE_TAG="${IMAGE_TAG:-$(date +%Y%m%d-%H%M%S)}"

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

# Function to check if required tools are installed
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    local missing_tools=()
    
    if ! command -v aws &> /dev/null; then
        missing_tools+=("aws-cli")
    fi
    
    if ! command -v docker &> /dev/null; then
        missing_tools+=("docker")
    fi
    
    if ! command -v terraform &> /dev/null; then
        missing_tools+=("terraform")
    fi
    
    if [ ${#missing_tools[@]} -ne 0 ]; then
        log_error "Missing required tools: ${missing_tools[*]}"
        log_info "Please install the missing tools and try again."
        exit 1
    fi
    
    log_success "All prerequisites are installed"
}

# Function to get AWS account ID
get_aws_account_id() {
    aws sts get-caller-identity --query Account --output text
}

# Function to build and push Docker image
build_and_push_image() {
    log_info "Building and pushing Docker image..."
    
    local account_id=$(get_aws_account_id)
    local ecr_uri="${account_id}.dkr.ecr.${AWS_REGION}.amazonaws.com"
    local repository_name="${PROJECT_NAME}-app"
    local full_image_name="${ecr_uri}/${repository_name}:${IMAGE_TAG}"
    local latest_image_name="${ecr_uri}/${repository_name}:latest"
    
    # Login to ECR
    log_info "Logging into ECR..."
    aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${ecr_uri}
    
    # Build the Docker image
    log_info "Building Docker image: ${full_image_name}"
    docker build -t ${full_image_name} -t ${latest_image_name} .
    
    # Push the Docker image
    log_info "Pushing Docker image to ECR..."
    docker push ${full_image_name}
    docker push ${latest_image_name}
    
    log_success "Docker image pushed successfully: ${full_image_name}"
    
    # Return the image URI for use in deployment
    echo ${full_image_name}
}

# Function to deploy infrastructure
deploy_infrastructure() {
    log_info "Deploying infrastructure with Terraform..."
    
    cd infrastructure
    
    # Initialize Terraform
    log_info "Initializing Terraform..."
    terraform init
    
    # Plan the deployment
    log_info "Planning Terraform deployment..."
    terraform plan -var="environment=${ENVIRONMENT}" -var="aws_region=${AWS_REGION}" -out=tfplan
    
    # Apply the deployment
    log_info "Applying Terraform deployment..."
    terraform apply tfplan
    
    cd ..
    
    log_success "Infrastructure deployed successfully"
}

# Function to update ECS service
update_ecs_service() {
    local image_uri=$1
    
    log_info "Updating ECS service with new image: ${image_uri}"
    
    cd infrastructure
    
    # Get the cluster and service names from Terraform output
    local cluster_name=$(terraform output -raw ecs_cluster_name)
    local service_name=$(terraform output -raw ecs_service_name)
    
    cd ..
    
    # Force new deployment
    log_info "Forcing new deployment for ECS service..."
    aws ecs update-service \
        --cluster ${cluster_name} \
        --service ${service_name} \
        --force-new-deployment \
        --region ${AWS_REGION}
    
    # Wait for deployment to complete
    log_info "Waiting for deployment to complete..."
    aws ecs wait services-stable \
        --cluster ${cluster_name} \
        --services ${service_name} \
        --region ${AWS_REGION}
    
    log_success "ECS service updated successfully"
}

# Function to get application URL
get_application_url() {
    cd infrastructure
    local app_url=$(terraform output -raw application_url)
    cd ..
    echo ${app_url}
}

# Function to perform health check
health_check() {
    local app_url=$1
    local health_url="${app_url}/health"
    local max_attempts=30
    local attempt=1
    
    log_info "Performing health check at: ${health_url}"
    
    while [ ${attempt} -le ${max_attempts} ]; do
        if curl -f -s ${health_url} > /dev/null; then
            log_success "Health check passed!"
            return 0
        fi
        
        log_info "Health check attempt ${attempt}/${max_attempts} failed, retrying in 10 seconds..."
        sleep 10
        attempt=$((attempt + 1))
    done
    
    log_error "Health check failed after ${max_attempts} attempts"
    return 1
}

# Main deployment function
main() {
    log_info "Starting deployment of ${PROJECT_NAME} to AWS ECS"
    log_info "Environment: ${ENVIRONMENT}"
    log_info "Region: ${AWS_REGION}"
    log_info "Image Tag: ${IMAGE_TAG}"
    
    # Check prerequisites
    check_prerequisites
    
    # Deploy infrastructure first (if needed)
    if [ "${SKIP_INFRASTRUCTURE:-false}" != "true" ]; then
        deploy_infrastructure
    fi
    
    # Build and push Docker image
    local image_uri=$(build_and_push_image)
    
    # Update ECS service
    update_ecs_service ${image_uri}
    
    # Get application URL
    local app_url=$(get_application_url)
    
    # Perform health check
    if health_check ${app_url}; then
        log_success "Deployment completed successfully!"
        log_info "Application is available at: ${app_url}"
        log_info "API Documentation: ${app_url}/swagger"
        log_info "Health Check: ${app_url}/health"
    else
        log_error "Deployment completed but health check failed"
        log_info "Please check the application logs for issues"
        exit 1
    fi
}

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help              Show this help message"
    echo "  -e, --environment ENV   Set environment (default: dev)"
    echo "  -r, --region REGION     Set AWS region (default: us-east-1)"
    echo "  -t, --tag TAG           Set image tag (default: timestamp)"
    echo "  --skip-infrastructure   Skip infrastructure deployment"
    echo ""
    echo "Environment Variables:"
    echo "  AWS_REGION              AWS region (default: us-east-1)"
    echo "  ENVIRONMENT             Environment name (default: dev)"
    echo "  IMAGE_TAG               Docker image tag (default: timestamp)"
    echo "  SKIP_INFRASTRUCTURE     Skip infrastructure deployment (default: false)"
    echo ""
    echo "Examples:"
    echo "  $0                                    # Deploy with defaults"
    echo "  $0 -e prod -r us-west-2             # Deploy to prod in us-west-2"
    echo "  $0 --skip-infrastructure             # Skip infrastructure, only update app"
}

# Parse command line arguments
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
        -t|--tag)
            IMAGE_TAG="$2"
            shift 2
            ;;
        --skip-infrastructure)
            SKIP_INFRASTRUCTURE="true"
            shift
            ;;
        *)
            log_error "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Run main function
main