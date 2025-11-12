# EFCore.ExprGenerator - Developer Guide

This document is a developer guide for understanding and editing the code in this project.

## Overview

Automatically generates corresponding `Select` queries and DTO classes from the content written in the `.SelectExpr` method.
For example:

```csharp
public class SampleClass
{
    public void GetSample(List<BaseClass> data)
    {
        var converted = data.AsQueryable()
            .SelectExpr(x => new
            {
                x.Id,
                x.Name,
                ChildDescription = x.Child?.Description,
                GrandChildTitles = x.Child?.GrandChild.Select(gc => gc.Title).ToList() ?? []
            })
            .ToList();
    }
}

// class definitions
internal class BaseClass
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public Child? Child { get; set; }
}
internal class ChildClass
{
    public string? Description { get; set; }
    public IEnumerable<GrandChildClass> GrandChild { get; set; }
}
internal class GrandChildClass
{
    public string Title { get; set; } = null!;
}
```

When you write code like the above, the following method and DTO are automatically generated:

```csharp
internal static class GeneratedExpression_ABCD1234
{
    public static IQueryable<BaseClassDto_ABCD1234> SelectExpr<TResult>(
        this IQueryable<BaseClass> query,
        Func<BaseClass, TResult> selector)
    {
        return query.Select(s => new BaseClassDto_ABCD1234
        {
            Id = s.Id,
            Name = s.Name,
            ChildDescription = s.Child != null ? s.Child.Description : default,
            GrandChildTitles = s.Child != null && s.Child.GrandChild != null ? s.Child.GrandChild.Select(gc => gc.Title).ToList() : default,
        });
    }

    public class BaseClassDto_ABCD1234
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string? ChildDescription { get; set; }
        public required IEnumerable<string>? GrandChildTitles { get; set; }
    }
}
```

The DummyExpression in EFCore.ExprGenerator is a "hook" point for triggering the above auto-generation and does nothing by itself.
Therefore, it will not cause any bugs and does not need to be edited.

## Project Structure

This project consists of the following four projects:

### `src/EFCore.ExprGenerator/`
The main library distributed as a NuGet package. It contains:
- `DummyExpression.cs`: An empty extension method that serves as a marker for the Source Generator to detect
- It performs no operations at runtime and is only used at compile time

### `src/EFCore.ExprGenerator.SourceGenerator/`
The Source Generator implementation that performs the actual code generation. It contains:
- `SelectExprGenerator.cs`: Main Source Generator implementation
- `SelectExprInfo.cs`: Extracts information from anonymous types and generates DTO classes
- `DtoProperty.cs`: Manages DTO property information

### `tests/EFCore.ExprGenerator.Tests/`
The test project. Contains test cases for various scenarios:
- Simple/Nested/CaseTest, etc.: Tests for various patterns
- Verifies that the generated code is as expected

### `examples/EFCore.ExprGenerator.Sample/`
A sample project demonstrating usage examples.

## Technical Background
This project is implemented as a **C# Source Generator**.

## Build and Test

Always perform a clean build as past caches may remain.
```bash
dotnet clean
dotnet build
dotnet test --no-build
```

If you want to access the generated code, you can output it to actual files by following these steps:

1. Delete the `(test-project)/.generated` directory if it already exists.
2. Set `EmitCompilerGeneratedFiles` to true in EFCore.ExprGenerator.Tests.csproj.
3. The generated code will be output to `(test-project)/.generated/**/*.g.cs`.
4. After confirmation, make sure to set `EmitCompilerGeneratedFiles` back to false.

## Development Guidelines

### Test-Driven Development Recommended
- When adding new features, create test cases first
- Verify the generated code in the test project to ensure it's as expected
- Ensure all existing tests pass before committing changes

### Source Generator-Specific Considerations
- **Cache Issues**: If changes are not reflected, run `dotnet clean`
- **IDE Restart**: If changes are not reflected in Visual Studio or Rider, an IDE restart may be necessary
- **Debugging**: Debugging Source Generators is more complex than regular code. To check generated code, use `EmitCompilerGeneratedFiles` as described above

### Code Editing Guidelines
- Do not edit `DummyExpression.cs` (it functions as a marker)
- When changing the Source Generator itself, always add test cases
- Pay attention to the quality (readability, performance) of the generated code

## Troubleshooting

### Generated Code Not Updating
1. Run `dotnet clean` to clear the cache
2. Restart the IDE
3. Delete the `.generated` directory and rebuild

### Tests Failing
1. Run `dotnet clean`
2. Build with `dotnet build`
3. Run tests with `dotnet test --no-build`
4. Check the error messages of failed tests
5. Check the generated code with `EmitCompilerGeneratedFiles`

### Local Testing of NuGet Package
1. Create a package with `dotnet pack`
2. Place it in a local NuGet feed
3. Reference it in a sample project and verify functionality

## Limitations

- Overly complex expression trees may not be parsed correctly
- Some LINQ methods may not be supported
- There are limitations on custom type mapping

For detailed limitations and known issues, please check the GitHub Issues.
