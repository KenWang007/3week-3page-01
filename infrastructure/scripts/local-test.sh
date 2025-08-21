#!/bin/bash

# Local Docker Testing Script
# This script builds and runs the application locally for testing

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
CONTAINER_NAME="ordering-system-local"
IMAGE_NAME="ordering-system:local"
PORT=8080

echo -e "${GREEN}Building and testing application locally...${NC}"

# Stop and remove existing container if it exists
if [ "$(docker ps -aq -f name=${CONTAINER_NAME})" ]; then
    echo -e "${YELLOW}Stopping existing container...${NC}"
    docker stop ${CONTAINER_NAME} >/dev/null 2>&1 || true
    docker rm ${CONTAINER_NAME} >/dev/null 2>&1 || true
fi

# Build Docker image
echo -e "${GREEN}Building Docker image...${NC}"
docker build -t ${IMAGE_NAME} .

# Run container
echo -e "${GREEN}Starting container...${NC}"
docker run -d \
    --name ${CONTAINER_NAME} \
    -p ${PORT}:${PORT} \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -e ASPNETCORE_URLS=http://+:${PORT} \
    ${IMAGE_NAME}

# Wait for container to start
echo -e "${YELLOW}Waiting for application to start...${NC}"
sleep 5

# Check if container is running
if [ "$(docker ps -q -f name=${CONTAINER_NAME})" ]; then
    echo -e "${GREEN}Container is running!${NC}"
    echo -e "${GREEN}Application URL: http://localhost:${PORT}${NC}"
    
    # Show logs
    echo -e "${YELLOW}Container logs:${NC}"
    docker logs ${CONTAINER_NAME} --tail 20
    
    echo -e "${GREEN}To view logs: docker logs -f ${CONTAINER_NAME}${NC}"
    echo -e "${GREEN}To stop: docker stop ${CONTAINER_NAME}${NC}"
else
    echo -e "${RED}Container failed to start!${NC}"
    echo -e "${RED}Checking logs...${NC}"
    docker logs ${CONTAINER_NAME}
    exit 1
fi