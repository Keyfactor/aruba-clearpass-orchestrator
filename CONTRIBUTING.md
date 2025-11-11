## Requirements
- .NET 8 SDK and above

## Unit + Integration Tests

This project features unit and integration tests that can be run from any IDE or command line.

### Setting Up Integration Tests

Inside the `ArubaClearPassOrchestrator.IntegrationTests` directory, there is a [.env.test.example](./ArubaClearPassOrchestrator.IntegrationTests/.env.test.example) file with the environment variables you can fill out. Each integration test has a flag that you can toggle to skip running that test. Copy the `.env.test.example` to `.env.test` within the same directory and fill out the environment variable values. Make sure to configure `.env.test` to **always copy** to the output directory. 

Some integration tests may suited towards running against a service hosted in a Docker container. The [local](./local) directory will contain Docker Compose files relevant to an associated integration test (for example, `S3CompatibleFileServerClientTests`).

### Running the Tests

Here are some command line scripts to run the test suites.

Restore project dependencies (optional):
```bash
dotnet restore
```

Run integration and unit tests:
```bash
dotnet test
```

Run just the unit tests:
```bash
dotnet test --filter "Category!=Integration"
```

Run just the integration tests:
```bash
dotnet test --filter "Category=Integration"
```
