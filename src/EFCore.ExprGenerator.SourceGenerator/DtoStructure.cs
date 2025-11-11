using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EFCore.ExprGenerator;

/// <summary>
/// DTO structure information
/// </summary>
internal record DtoStructure(
    string SourceTypeName,
    string SourceTypeFullName,
    List<DtoProperty> Properties
)
{
    public string GetUniqueId()
    {
        // Generate hash from property structure
        var signature = string.Join(
            "|",
            Properties.Select(p => $"{p.Name}:{p.TypeName}:{p.IsNullable}")
        );
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signature));
        return BitConverter.ToString(hash).Replace("-", "")[..8]; // Use first 8 characters
    }

    public static DtoStructure? AnalyzeNamedType(
        ObjectCreationExpressionSyntax namedObj,
        SemanticModel semanticModel,
        ITypeSymbol sourceType
    )
    {
        // TODO
        return null;
    }

    public static DtoStructure? AnalyzeAnonymousType(
        AnonymousObjectCreationExpressionSyntax anonymousObj,
        SemanticModel semanticModel,
        ITypeSymbol sourceType
    )
    {
        var properties = new List<DtoProperty>();
        foreach (var initializer in anonymousObj.Initializers)
        {
            string propertyName;
            var expression = initializer.Expression;
            // For explicit property names (e.g., Id = s.Id)
            if (initializer.NameEquals is not null)
            {
                propertyName = initializer.NameEquals.Name.Identifier.Text;
            }
            // For implicit property names (e.g., s.Id)
            else
            {
                // Get property name inferred from expression
                var name = GetImplicitPropertyName(expression);
                if (name is null)
                {
                    continue;
                }
                propertyName = name;
            }
            var property = DtoProperty.AnalyzeExpression(propertyName, expression, semanticModel);
            if (property is not null)
            {
                properties.Add(property);
            }
        }
        return new DtoStructure(
            SourceTypeName: sourceType.Name,
            SourceTypeFullName: sourceType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            ),
            Properties: properties
        );
    }

    private static string? GetImplicitPropertyName(ExpressionSyntax expression)
    {
        // Get property name from member access (e.g., s.Id)
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.Text;
        }

        // Get property name from identifier (e.g., id)
        if (expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.Text;
        }

        // Do not process other complex expressions
        return null;
    }
}
