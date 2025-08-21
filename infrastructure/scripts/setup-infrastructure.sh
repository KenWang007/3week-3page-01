#!/bin/bash

# Infrastructure Setup Script
# This script sets up the AWS infrastructure using Terraform

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=${1:-dev}
TERRAFORM_DIR="../terraform"

echo -e "${GREEN}Setting up infrastructure for environment: ${ENVIRONMENT}${NC}"

# Check prerequisites
command -v terraform >/dev/null 2>&1 || { echo -e "${RED}Terraform is required but not installed.${NC}" >&2; exit 1; }
command -v aws >/dev/null 2>&1 || { echo -e "${RED}AWS CLI is required but not installed.${NC}" >&2; exit 1; }

# Change to terraform directory
cd "$(dirname "$0")/${TERRAFORM_DIR}"

# Check if terraform.tfvars exists
if [ ! -f "terraform.tfvars" ]; then
    echo -e "${YELLOW}terraform.tfvars not found. Creating from example...${NC}"
    cp terraform.tfvars.example terraform.tfvars
    echo -e "${YELLOW}Please edit terraform.tfvars with your configuration before proceeding.${NC}"
    exit 1
fi

# Initialize Terraform
echo -e "${GREEN}Initializing Terraform...${NC}"
terraform init

# Validate configuration
echo -e "${GREEN}Validating Terraform configuration...${NC}"
terraform validate

# Plan deployment
echo -e "${GREEN}Planning infrastructure deployment...${NC}"
terraform plan -var="environment=${ENVIRONMENT}" -out=tfplan

# Ask for confirmation
echo -e "${YELLOW}Do you want to apply this plan? (yes/no)${NC}"
read -r response
if [[ "$response" != "yes" ]]; then
    echo -e "${RED}Deployment cancelled.${NC}"
    exit 0
fi

# Apply infrastructure
echo -e "${GREEN}Applying infrastructure changes...${NC}"
terraform apply tfplan

# Output important values
echo -e "${GREEN}Infrastructure deployment completed!${NC}"
echo -e "${GREEN}Important outputs:${NC}"
terraform output

# Save outputs to file
terraform output -json > outputs.json
echo -e "${GREEN}Outputs saved to outputs.json${NC}"