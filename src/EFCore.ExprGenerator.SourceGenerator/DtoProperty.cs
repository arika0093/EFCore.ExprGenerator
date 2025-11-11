using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EFCore.ExprGenerator;

internal record DtoProperty(
    string Name,
    bool IsNullable,
    string OriginalExpression,
    ITypeSymbol TypeSymbol,
    DtoStructure? NestedStructure
)
{
    public string TypeName => TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static DtoProperty? AnalyzeExpression(
        string propertyName,
        ExpressionSyntax expression,
        SemanticModel semanticModel
    )
    {
        var typeInfo = semanticModel.GetTypeInfo(expression);
        if (typeInfo.Type is null)
            return null;

        var propertyType = typeInfo.Type;
        var isNullable = propertyType.NullableAnnotation == NullableAnnotation.Annotated;

        // Check if nullable operator ?. is used
        var hasNullableAccess = HasNullableAccess(expression);

        // Detect nested Select (e.g., s.Childs.Select(...))
        DtoStructure? nestedStructure = null;
        if (expression is InvocationExpressionSyntax nestedInvocation)
        {
            // Check if it's a Select method invocation
            if (
                nestedInvocation.Expression is MemberAccessExpressionSyntax nestedMemberAccess
                && nestedMemberAccess.Name.Identifier.Text == "Select"
            )
            {
                // Analyze anonymous type in lambda expression
                if (nestedInvocation.ArgumentList.Arguments.Count > 0)
                {
                    var lambdaArg = nestedInvocation.ArgumentList.Arguments[0].Expression;
                    if (
                        lambdaArg is LambdaExpressionSyntax nestedLambda
                        && nestedLambda.Body
                            is AnonymousObjectCreationExpressionSyntax nestedAnonymous
                    )
                    {
                        // Get collection element type
                        var collectionType = semanticModel
                            .GetTypeInfo(nestedMemberAccess.Expression)
                            .Type;
                        if (
                            collectionType is INamedTypeSymbol namedCollectionType
                            && namedCollectionType.TypeArguments.Length > 0
                        )
                        {
                            var elementType = namedCollectionType.TypeArguments[0];
                            nestedStructure = DtoStructure.AnalyzeAnonymousType(
                                nestedAnonymous,
                                semanticModel,
                                elementType
                            );
                        }
                    }
                }
            }
        }

        return new DtoProperty(
            Name: propertyName,
            IsNullable: isNullable || hasNullableAccess,
            OriginalExpression: expression.ToString(),
            TypeSymbol: propertyType,
            NestedStructure: nestedStructure
        );
    }

    private static bool HasNullableAccess(ExpressionSyntax expression)
    {
        // Check if ?. operator is used
        return expression.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Any();
    }
}
