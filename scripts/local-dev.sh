#!/bin/bash

# Local development script for the Ordering System
# This script helps with local development tasks

set -e  # Exit on any error

# Configuration
PROJECT_NAME="ordering-system"

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

# Function to build the application
build_app() {
    log_info "Building the .NET application..."
    dotnet restore
    dotnet build --configuration Release
    log_success "Application built successfully"
}

# Function to run tests
run_tests() {
    log_info "Running tests..."
    dotnet test --configuration Release --verbosity normal
    log_success "Tests completed"
}

# Function to run the application locally
run_app() {
    log_info "Starting the application locally..."
    log_info "The application will be available at:"
    log_info "  - Web API: http://localhost:5000"
    log_info "  - Swagger UI: http://localhost:5000/swagger"
    log_info "  - Health Check: http://localhost:5000/health"
    log_info ""
    log_info "Press Ctrl+C to stop the application"
    
    ASPNETCORE_ENVIRONMENT=Development dotnet run --configuration Release
}

# Function to run with Docker
run_docker() {
    log_info "Building and running with Docker..."
    
    # Build the Docker image
    docker build -t ${PROJECT_NAME}:local .
    
    # Stop any existing container
    docker stop ${PROJECT_NAME}-local 2>/dev/null || true
    docker rm ${PROJECT_NAME}-local 2>/dev/null || true
    
    # Run the container
    log_info "Starting Docker container..."
    docker run -d \
        --name ${PROJECT_NAME}-local \
        -p 8080:8080 \
        -e ASPNETCORE_ENVIRONMENT=Development \
        ${PROJECT_NAME}:local
    
    log_success "Docker container started successfully"
    log_info "The application is available at:"
    log_info "  - Web API: http://localhost:8080"
    log_info "  - Swagger UI: http://localhost:8080/swagger"
    log_info "  - Health Check: http://localhost:8080/health"
    log_info ""
    log_info "Use 'docker logs ${PROJECT_NAME}-local' to view logs"
    log_info "Use 'docker stop ${PROJECT_NAME}-local' to stop the container"
}

# Function to run with Docker Compose
run_compose() {
    log_info "Running with Docker Compose..."
    
    # Stop any existing services
    docker-compose down 2>/dev/null || true
    
    # Build and start services
    docker-compose up --build -d
    
    log_success "Docker Compose services started successfully"
    log_info "The application is available at:"
    log_info "  - Web API: http://localhost:8080"
    log_info "  - Swagger UI: http://localhost:8080/swagger"
    log_info "  - Health Check: http://localhost:8080/health"
    log_info ""
    log_info "Use 'docker-compose logs' to view logs"
    log_info "Use 'docker-compose down' to stop services"
}

# Function to clean up Docker resources
cleanup_docker() {
    log_info "Cleaning up Docker resources..."
    
    # Stop and remove containers
    docker stop ${PROJECT_NAME}-local 2>/dev/null || true
    docker rm ${PROJECT_NAME}-local 2>/dev/null || true
    docker-compose down 2>/dev/null || true
    
    # Remove images
    docker rmi ${PROJECT_NAME}:local 2>/dev/null || true
    
    # Clean up unused resources
    docker system prune -f
    
    log_success "Docker cleanup completed"
}

# Function to watch logs
watch_logs() {
    local service="$1"
    
    case $service in
        "docker")
            log_info "Watching Docker container logs..."
            docker logs -f ${PROJECT_NAME}-local
            ;;
        "compose")
            log_info "Watching Docker Compose logs..."
            docker-compose logs -f
            ;;
        *)
            log_error "Unknown service: $service"
            log_info "Available options: docker, compose"
            exit 1
            ;;
    esac
}

# Function to check application health
health_check() {
    local url="$1"
    local max_attempts=10
    local attempt=1
    
    log_info "Checking application health at: $url"
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f -s "$url" > /dev/null; then
            log_success "Application is healthy!"
            return 0
        fi
        
        log_info "Health check attempt $attempt/$max_attempts failed, retrying in 5 seconds..."
        sleep 5
        attempt=$((attempt + 1))
    done
    
    log_error "Health check failed after $max_attempts attempts"
    return 1
}

# Function to run console demo
run_demo() {
    log_info "Running console demo..."
    dotnet run --configuration Release -- --demo
}

# Function to display usage
usage() {
    echo "Usage: $0 <command> [options]"
    echo ""
    echo "Commands:"
    echo "  build                   Build the application"
    echo "  test                    Run tests"
    echo "  run                     Run the application locally"
    echo "  demo                    Run the console demo"
    echo "  docker                  Build and run with Docker"
    echo "  compose                 Run with Docker Compose"
    echo "  logs <service>          Watch logs (docker|compose)"
    echo "  health <url>            Check application health"
    echo "  cleanup                 Clean up Docker resources"
    echo "  help                    Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 build                # Build the application"
    echo "  $0 run                  # Run locally with .NET"
    echo "  $0 docker               # Run with Docker"
    echo "  $0 compose              # Run with Docker Compose"
    echo "  $0 logs docker          # Watch Docker container logs"
    echo "  $0 health http://localhost:8080/health  # Check health"
    echo "  $0 cleanup              # Clean up Docker resources"
}

# Main function
main() {
    local command="$1"
    shift || true
    
    case $command in
        "build")
            build_app
            ;;
        "test")
            run_tests
            ;;
        "run")
            build_app
            run_app
            ;;
        "demo")
            build_app
            run_demo
            ;;
        "docker")
            run_docker
            ;;
        "compose")
            run_compose
            ;;
        "logs")
            watch_logs "$1"
            ;;
        "health")
            health_check "$1"
            ;;
        "cleanup")
            cleanup_docker
            ;;
        "help"|"--help"|"-h"|"")
            usage
            ;;
        *)
            log_error "Unknown command: $command"
            usage
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"