using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EFCore.ExprGenerator;

/// <summary>
/// Record for anonymous Select expression information
/// </summary>
internal record SelectExprInfoAnonymous : SelectExprInfo
{
	public required AnonymousObjectCreationExpressionSyntax AnonymousObject { get; init; }

	public override DtoStructure GenerateDtoStructure()
	{
		return DtoStructure.AnalyzeAnonymousType(AnonymousObject, SemanticModel, SourceType)!;
	}

	public override string GetClassName(DtoStructure structure) =>
		$"{structure.SourceTypeName}Dto_{GetUniqueId()}";
}
