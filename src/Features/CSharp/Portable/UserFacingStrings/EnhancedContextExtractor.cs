// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Extracts enhanced context information for AI analysis while keeping basic context simple for cache keys.
/// Enhanced context includes symbol information, surrounding code patterns, and semantic analysis.
/// </summary>
internal static class EnhancedContextExtractor
{
    /// <summary>
    /// Extracts basic context information for the AI (descriptive and informative).
    /// </summary>
    public static string ExtractBasicContext(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;

        // Provide minimal context information for the AI
        return parent switch
        {
            ArgumentSyntax arg when arg.Parent?.Parent is InvocationExpressionSyntax invocation =>
                $"Argument to method: {invocation.Expression}",
            AssignmentExpressionSyntax assignment =>
                $"Assignment to: {assignment.Left}",
            VariableDeclaratorSyntax declarator =>
                $"Variable initialization: {declarator.Identifier}",
            ReturnStatementSyntax =>
                "Return statement",
            AttributeSyntax =>
                "Attribute value",
            ThrowStatementSyntax =>
                "Exception message",
            ThrowExpressionSyntax =>
                "Exception message",
            _ => "Other context"
        };
    }

    /// <summary>
    /// Extracts enhanced context for AI analysis (detailed and comprehensive).
    /// This includes symbol information, method signatures, surrounding patterns, etc.
    /// </summary>
    public static async Task<string> ExtractEnhancedContextAsync(
        LiteralExpressionSyntax stringLiteral,
        Document document,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return ExtractBasicContext(stringLiteral);

        var context = new StringBuilder();
        var node = stringLiteral.Parent;

        // Add basic context
        context.Append("Context: ").Append(ExtractBasicContext(stringLiteral));

        // Add containing member information
        var containingMember = node?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (containingMember != null)
        {
            context.Append(" | Member: ");
            switch (containingMember)
            {
                case MethodDeclarationSyntax method:
                    context.Append("Method ").Append(method.Identifier.ValueText);
                    if (method.ParameterList.Parameters.Count > 0)
                    {
                        context.Append("(").Append(string.Join(", ", method.ParameterList.Parameters.Select(p => p.Type?.ToString() ?? ""))).Append(")");
                    }
                    break;
                    
                case PropertyDeclarationSyntax property:
                    context.Append("Property ").Append(property.Identifier.ValueText);
                    break;
                    
                case ConstructorDeclarationSyntax constructor:
                    context.Append("Constructor ").Append(constructor.Identifier.ValueText);
                    break;
                    
                case FieldDeclarationSyntax field:
                    context.Append("Field ").Append(string.Join(", ", field.Declaration.Variables.Select(v => v.Identifier.ValueText)));
                    break;
                    
                default:
                    context.Append(containingMember.GetType().Name);
                    break;
            }
        }

        // Add containing type information
        var containingType = node?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingType != null)
        {
            context.Append(" | Type: ").Append(containingType.Identifier.ValueText);
            
            // Add type kind
            context.Append(" (").Append(containingType.Keyword.ValueText).Append(")");
        }

        // Add specific usage patterns
        AddUsagePattern(stringLiteral, context, semanticModel);

        // Add surrounding code context (preceding/following statements)
        AddSurroundingCodeContext(stringLiteral, context);

        return context.ToString();
    }

    private static void AddUsagePattern(
        LiteralExpressionSyntax stringLiteral,
        StringBuilder context,
        SemanticModel semanticModel)
    {
        var parent = stringLiteral.Parent;

        switch (parent)
        {
            case ArgumentSyntax argument:
                AddArgumentPattern(argument, context, semanticModel);
                break;
                
            case AssignmentExpressionSyntax assignment:
                AddAssignmentPattern(assignment, context, semanticModel);
                break;
                
            case ThrowStatementSyntax:
            case ThrowExpressionSyntax:
                context.Append(" | Pattern: Exception message");
                break;
                
            case ReturnStatementSyntax:
                context.Append(" | Pattern: Return value");
                break;
        }
    }

    private static void AddArgumentPattern(
        ArgumentSyntax argument, 
        StringBuilder context, 
        SemanticModel semanticModel)
    {
        if (argument.Parent?.Parent is InvocationExpressionSyntax invocation)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation.Expression);
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                context.Append(" | Method: ").Append(method.ContainingType?.Name).Append(".").Append(method.Name);
                
                // Add parameter information
                var argumentList = argument.Parent as ArgumentListSyntax;
                if (argumentList != null)
                {
                    var argumentIndex = argumentList.Arguments.IndexOf(argument);
                    if (argumentIndex >= 0 && argumentIndex < method.Parameters.Length)
                    {
                        var parameter = method.Parameters[argumentIndex];
                        context.Append(" | Parameter: ").Append(parameter.Name).Append(" (").Append(parameter.Type?.Name).Append(")");
                    }
                }

                // Check for UI/user-facing method patterns
                if (IsUserFacingMethod(method))
                {
                    context.Append(" | Pattern: User-facing");
                }
                else if (IsLoggingMethod(method))
                {
                    context.Append(" | Pattern: Logging");
                }
                else if (IsValidationMethod(method))
                {
                    context.Append(" | Pattern: Validation");
                }
            }
        }
    }

    private static void AddAssignmentPattern(
        AssignmentExpressionSyntax assignment, 
        StringBuilder context, 
        SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(assignment.Left);
        if (symbolInfo.Symbol != null)
        {
            context.Append(" | Variable: ").Append(symbolInfo.Symbol.Name);
            
            if (symbolInfo.Symbol is IPropertySymbol property)
            {
                context.Append(" (Property of ").Append(property.ContainingType?.Name).Append(")");
            }
            else if (symbolInfo.Symbol is IFieldSymbol field)
            {
                context.Append(" (Field of ").Append(field.ContainingType?.Name).Append(")");
            }
        }
    }

    private static void AddSurroundingCodeContext(LiteralExpressionSyntax stringLiteral, StringBuilder context)
    {
        var node = stringLiteral.Parent;
        
        // Look at preceding statement
        var statement = node?.FirstAncestorOrSelf<StatementSyntax>();
        if (statement?.Parent is BlockSyntax block)
        {
            var statements = block.Statements;
            var currentIndex = statements.IndexOf(statement);
            
            if (currentIndex > 0)
            {
                var previousStatement = statements[currentIndex - 1];
                var previousKind = previousStatement.Kind().ToString();
                context.Append(" | Previous: ").Append(previousKind);
            }
            
            if (currentIndex < statements.Count - 1)
            {
                var nextStatement = statements[currentIndex + 1];
                var nextKind = nextStatement.Kind().ToString();
                context.Append(" | Next: ").Append(nextKind);
            }
        }
    }

    private static bool IsMessageBoxCall(ArgumentSyntax argument)
    {
        if (argument.Parent?.Parent is InvocationExpressionSyntax invocation)
        {
            var expression = invocation.Expression.ToString();
            return expression.Contains("MessageBox", StringComparison.OrdinalIgnoreCase) ||
                   expression.Contains("ShowDialog", StringComparison.OrdinalIgnoreCase) ||
                   expression.Contains("ShowMessage", StringComparison.OrdinalIgnoreCase);
        }
        
        return false;
    }

    private static bool IsUserFacingMethod(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        var typeName = method.ContainingType?.Name?.ToLowerInvariant() ?? "";

        return methodName.Contains("show") ||
               methodName.Contains("display") ||
               methodName.Contains("dialog") ||
               methodName.Contains("message") ||
               methodName.Contains("prompt") ||
               typeName.Contains("messagebox") ||
               typeName.Contains("dialog") ||
               typeName.Contains("notification");
    }

    private static bool IsLoggingMethod(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        var typeName = method.ContainingType?.Name?.ToLowerInvariant() ?? "";

        return methodName.Contains("log") ||
               methodName.Contains("debug") ||
               methodName.Contains("trace") ||
               methodName.Contains("error") ||
               methodName.Contains("warn") ||
               typeName.Contains("log") ||
               typeName.Contains("trace");
    }

    private static bool IsValidationMethod(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();

        return methodName.Contains("validate") ||
               methodName.Contains("check") ||
               methodName.Contains("verify") ||
               methodName.Contains("ensure") ||
               methodName.StartsWith("throw");
    }
}
