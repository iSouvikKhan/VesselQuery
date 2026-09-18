# CLAUDE.md

## Code style

- Do not add comments to code. This covers `//` and `/* */` comments, XML doc comments (`///`), `<!-- -->` comments in project files, and comment text in `.http` files.
- Keep the comments that the .NET templates generate (for example in `Program.cs`). Do not add new ones.
- Put explanations in `README.md`, not in the source.

## Project

- `VesselQuery.Core`: the query engine (tokenizer, parser, evaluator, in-memory store). It has no ASP.NET dependency.
- `VesselQuery.Api`: the HTTP layer (controllers, contracts, DI setup, exception handling).
- `VesselQuery.Tests`: xUnit unit and integration tests.

## Commands

- Run: `dotnet run --project VesselQuery.Api --launch-profile http`
- Test: `dotnet test`
