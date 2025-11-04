# Contributing to Google Connector Service

Thank you for your interest in contributing to the Google Connector Service!

## Development Setup

1. Install prerequisites (see README.md)
2. Clone the repository
3. Run `dotnet restore`
4. Copy `local.settings.json.example` to `src/FunctionApp/local.settings.json` and update with your credentials
5. Run `dotnet build` to ensure everything compiles

## Architecture Guidelines

This project follows Clean Architecture (Onion Architecture) principles:

### Layer Dependencies

- **Domain**: No dependencies on other layers
- **Application**: Depends only on Domain
- **Infrastructure**: Depends on Application and Domain
- **FunctionApp**: Depends on Application and Infrastructure

### CQRS Pattern

- **Commands** go in `Application/Commands/` - use for writes/mutations
- **Queries** go in `Application/Queries/` - use for reads
- Use MediatR's `IRequest<T>` and `IRequestHandler<TRequest, TResponse>`

### Repository Pattern

- Define interfaces in Application layer
- Implement in Infrastructure layer
- Use dependency injection to wire them up

## Coding Standards

- Follow the .editorconfig settings
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Write unit tests for all business logic
- Write integration tests for Azure Functions

## Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UnitTests/UnitTests.csproj

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Pull Request Process

1. Create a feature branch from `main`
2. Make your changes
3. Add/update tests
4. Ensure all tests pass
5. Update documentation if needed
6. Submit a pull request

## Code Review Criteria

- Does it follow Clean Architecture principles?
- Are there adequate tests?
- Is the code well-documented?
- Does it handle errors gracefully?
- Is logging appropriate?



