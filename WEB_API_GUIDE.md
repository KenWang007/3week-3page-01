# Web API Configuration for Ordering System

The Ordering System can run either as a console application or as a web API service suitable for containerized deployment.

## Running as Web API

To run the application as a web API (required for AWS ECS deployment), replace the `Program.cs` with `Program.Web.cs`:

```bash
# Backup original console app
mv Program.cs Program.Console.cs

# Use web API version
mv Program.Web.cs Program.cs
```

## API Endpoints

Once running as a web service, the following endpoints are available:

### Health Check
- `GET /health` - Returns 200 OK if service is healthy

### Root
- `GET /` - Returns API information and available endpoints

### Products
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID

### Orders
- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get order by ID
- `POST /api/orders` - Create new order
- `PUT /api/orders/{id}/status` - Update order status

### Demo
- `GET /api/demo` - Run the demo scenario and return results

## Example API Calls

### Create Order
```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerFirstName": "John",
    "customerLastName": "Doe",
    "customerEmail": "john.doe@example.com",
    "items": [
      {
        "productId": "LAPTOP001",
        "quantity": 1,
        "unitPrice": 999.99
      }
    ]
  }'
```

### Get Order
```bash
curl http://localhost:8080/api/orders/{orderId}
```

### Update Order Status
```bash
curl -X PUT http://localhost:8080/api/orders/{orderId}/status \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Shipped"
  }'
```

## Local Testing

Test the web API locally using Docker:

```bash
cd infrastructure/scripts
./local-test.sh
```

Then access the API at `http://localhost:8080`

## Deployment

The Dockerfile is configured to run the application as a web service on port 8080, suitable for deployment to AWS ECS or any container platform.