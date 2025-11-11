using System;
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

    public override string GetClassName(DtoStructure structure) => structure.SourceTypeName;
}
