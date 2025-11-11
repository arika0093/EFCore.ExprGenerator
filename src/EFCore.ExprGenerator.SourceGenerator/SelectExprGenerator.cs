using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EFCore.ExprGenerator;

/// <summary>
/// Generator for SelectExpr method
/// </summary>
[Generator]
public partial class SelectExprGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initialize the generator
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Provider to detect SelectExpr method invocations
        var invocations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => IsSelectExprInvocation(node),
                transform: static (ctx, _) => GetSelectExprInfo(ctx)
            )
            .Where(static info => info is not null)
            .Collect();

        // Code generation
        context.RegisterSourceOutput(
            invocations,
            (spc, infos) =>
            {
                // deduplicate infos based on unique id
                var dict = new Dictionary<string, SelectExprInfo>();
                foreach (var info in infos)
                {
                    if (info is null)
                        continue;
                    var id = info.GetUniqueId();
                    if (!dict.ContainsKey(id))
                    {
                        dict.Add(id, info);
                    }
                }

                // Generate code for each unique info
                foreach (var info in dict.Values)
                {
                    info?.GenerateCode(spc);
                }
            }
        );
    }

    private static bool IsSelectExprInvocation(SyntaxNode node)
    {
        // Detect InvocationExpression with method name "SelectExpr"
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        var expression = invocation.Expression;

        // MemberAccessExpression (e.g., query.SelectExpr)
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.Text == "SelectExpr";
        }

        return false;
    }

    private static SelectExprInfo? GetSelectExprInfo(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        // Get lambda expression from arguments
        if (invocation.ArgumentList.Arguments.Count == 0)
            return null;

        var lambdaArg = invocation.ArgumentList.Arguments[0].Expression;
        if (lambdaArg is not LambdaExpressionSyntax lambda)
            return null;

        // Check if lambda body is an object initializer
        var body = lambda.Body;
        switch (body)
        {
            case AnonymousObjectCreationExpressionSyntax anon:
                return GetAnonymousSelectExprInfo(context, anon);
            case ObjectCreationExpressionSyntax objCreation:
                return GetNamedSelectExprInfo(context, objCreation);
            default:
                return null;
        }
    }

    private static SelectExprInfoAnonymous? GetAnonymousSelectExprInfo(
        GeneratorSyntaxContext context,
        AnonymousObjectCreationExpressionSyntax anonymousObj
    )
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Get target type from MemberAccessExpression
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        // Get type information
        var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
        if (typeInfo.Type is not INamedTypeSymbol namedType)
            return null;

        // Get T from IQueryable<T>
        var sourceType = namedType.TypeArguments.FirstOrDefault();
        if (sourceType is null)
            return null;

        return new SelectExprInfoAnonymous
        {
            SourceType = sourceType,
            AnonymousObject = anonymousObj,
            SemanticModel = semanticModel,
            Invocation = invocation,
        };
    }

    private static SelectExprInfoNamed? GetNamedSelectExprInfo(
        GeneratorSyntaxContext context,
        ObjectCreationExpressionSyntax obj
    )
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Get target type from MemberAccessExpression
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        // Get type information
        var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
        if (typeInfo.Type is not INamedTypeSymbol namedType)
            return null;

        // Get T from IQueryable<T>
        var sourceType = namedType.TypeArguments.FirstOrDefault();
        if (sourceType is null)
            return null;

        return new SelectExprInfoNamed
        {
            SourceType = sourceType,
            ObjectCreation = obj,
            SemanticModel = semanticModel,
            Invocation = invocation,
        };
    }
}
