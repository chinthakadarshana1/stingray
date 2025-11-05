# Stingray Outbox Processor Service

A standalone .NET 8 Worker Service that implements the Outbox Pattern for reliable event publishing to Apache Kafka.

## What is This?

This is a **background service** that continuously polls an outbox database table for unprocessed messages and reliably publishes them to
Kafka. It ensures at-least-once delivery of events even when Kafka is temporarily unavailable.

## Quick Start

### Run with Docker

```bash
docker-compose up -d stingray.services.outboxprocessor
```

### Run Locally

```bash
dotnet run
```

### Configuration

Set environment variables or edit `appsettings.json`:

- `Kafka__BootstrapServers` - Kafka broker address (default: kafka:9092)
- `DatabaseName` - Database to monitor (default: OutboxProcessorDb)

## How It Works

1. **Polls** the outbox table every 5 seconds
2. **Fetches** unprocessed messages
3. **Publishes** each message to its Kafka topic
4. **Marks** successfully published messages as processed
5. **Retries** automatically on failure

## Key Features

✅ **Reliable Event Delivery** - Guarantees at-least-once delivery
✅ **Automatic Retry** - Retries failed publishes every 5 seconds
✅ **Idempotent Publishing** - Kafka producer configured for idempotence
✅ **Structured Logging** - Comprehensive logging for debugging
✅ **Docker Ready** - Fully containerized
✅ **Production Ready** - Restart policies and error handling

## Monitoring

### View Logs

```bash
docker logs -f outbox_processor
```

### Check Status

```bash
docker ps | grep outbox_processor
```

### Restart Service

```bash
docker-compose restart stingray.services.outboxprocessor
```

## Architecture

```
OutboxProcessor Service
    │
    ├─► Polls Database (every 5s)
    │
    ├─► Publishes to Kafka
    │   ├─ UserCreated topic
    │   └─ OrderCreated topic
    │
    └─► Marks as Processed
```

## Dependencies

- **Confluent.Kafka** - Kafka client
- **Microsoft.Extensions.Hosting** - Worker service framework
- **Entity Framework Core** - Database access
- **Stingray.Domain** - Domain interfaces
- **Stingray.Storage.InMemory** - Repository implementations

## Configuration Options

### appsettings.json

```json
{
  "Kafka": {
    "BootstrapServers": "kafka:9092"
  },
  "DatabaseName": "OutboxProcessorDb",
  "OutboxProcessor": {
    "PollingIntervalSeconds": 5,
    "BatchSize": 100
  }
}
```

## Deployment

### Docker Compose (Included)

Already configured in the main `docker-compose.yml`

### Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: outbox-processor
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: outbox-processor
        image: outbox-processor:latest
```

### Windows Service

```bash
sc create "StingrayOutboxProcessor" binPath="C:\path\to\exe"
```

## Troubleshooting

### Service Not Processing Messages

1. Check Kafka connectivity: `telnet kafka 9092`
2. Verify database has unprocessed messages
3. Check logs for errors: `docker logs outbox_processor`

### High CPU Usage

- Increase polling interval in configuration
- Reduce batch size

### Connection Errors

- Verify Kafka bootstrap servers setting
- Check network connectivity
- Ensure Kafka is running

## Development

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Publish

```bash
dotnet publish -c Release
```

## License

Part of the Stingray E-Commerce Backend project.

## Related Documentation

- [Complete Standalone Guide](../OUTBOX_STANDALONE_GUIDE.md)
- [Architecture Overview](../ARCHITECTURE_UPDATED.md)
- [Main README](../README.md)

