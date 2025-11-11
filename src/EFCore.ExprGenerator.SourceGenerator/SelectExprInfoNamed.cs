using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EFCore.ExprGenerator;

/// <summary>
/// Record for named Select expression information
/// </summary>
internal record SelectExprInfoNamed : SelectExprInfo
{
    public required ObjectCreationExpressionSyntax ObjectCreation { get; init; }

    public override DtoStructure GenerateDtoStructure()
    {
        return DtoStructure.AnalyzeNamedType(ObjectCreation, SemanticModel, SourceType)!;
    }

    public override string GenerateDtoClasses(
        DtoStructure structure,
        List<string> dtoClasses,
        string namespaceName
    )
    {
        // nothing to do, as named types are already defined
        return "";
    }

    public override string GetClassName(DtoStructure structure) => structure.SourceTypeName;
}
