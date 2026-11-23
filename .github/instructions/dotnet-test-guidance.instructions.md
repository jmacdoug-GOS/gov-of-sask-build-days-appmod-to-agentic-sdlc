---
applyTo: "capstone-src/api-dotnet/tests/**/*.cs"
---

# .NET capstone API: test guidance

- Use xUnit (`[Fact]`/`[Theory]`) as declared in
  `capstone-src/api-dotnet/tests/WorkItems.Api.Tests/WorkItems.Api.Tests.csproj`.
- Test repository behavior through `InMemoryWorkItemRepository` in
  `capstone-src/api-dotnet/src/WorkItems.Api/`, and keep API shape consistent
  with `capstone-src/contracts/openapi.yml`.
- Inject a stub `TimeProvider` (see `StubTimeProvider` in
  `InMemoryWorkItemRepositoryTests.cs`) instead of reading the real clock.
- From `capstone-src/api-dotnet`, run
  `dotnet build WorkItems.slnx --configuration Release --nologo` followed by
  `dotnet test WorkItems.slnx --configuration Release --no-build --nologo`.
- Do not depend on live cloud services, the network, or production data.
