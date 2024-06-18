// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InlineHints;

[ExportLanguageService(typeof(IInlineTypeHintsService), LanguageNames.CSharp), Shared]
internal sealed class CSharpInlineTypeHintsService : AbstractInlineTypeHintsService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpInlineTypeHintsService()
    {
    }

    protected override TypeHint? TryGetTypeHint(
        SemanticModel semanticModel,
        SyntaxNode node,
        bool displayAllOverride,
        bool forImplicitVariableTypes,
        bool forLambdaParameterTypes,
        bool forImplicitObjectCreation,
        bool forCollectionExpressions,
        CancellationToken cancellationToken)
    {
        if (forImplicitVariableTypes || displayAllOverride)
        {
            if (node is VariableDeclarationSyntax { Type.IsVar: true } variableDeclaration &&
                variableDeclaration.Variables.Count == 1 &&
                !variableDeclaration.Variables[0].Identifier.IsMissing)
            {
                var type = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type;
                if (IsValidType(type))
                    return CreateTypeHint(type, displayAllOverride, forImplicitVariableTypes, variableDeclaration.Type, variableDeclaration.Variables[0].Identifier);
            }

            // We handle individual variables of ParenthesizedVariableDesignationSyntax separately.
            // For example, in `var (x, y) = (0, "")`, we should `int` for `x` and `string` for `y`.
            // It's redundant to show `(int, string)` for `var`
            if (node is DeclarationExpressionSyntax { Type.IsVar: true, Designation: not ParenthesizedVariableDesignationSyntax } declarationExpression)
            {
                var type = semanticModel.GetTypeInfo(declarationExpression.Type, cancellationToken).Type;
                if (IsValidType(type))
                    return CreateTypeHint(type, displayAllOverride, forImplicitVariableTypes, declarationExpression.Type, declarationExpression.Designation);
            }
            else if (node is SingleVariableDesignationSyntax { Parent: not DeclarationPatternSyntax and not DeclarationExpressionSyntax } variableDesignation)
            {
                var local = semanticModel.GetDeclaredSymbol(variableDesignation, cancellationToken) as ILocalSymbol;
                var type = local?.Type;
                if (IsValidType(type))
                {
                    return node.Parent is VarPatternSyntax varPattern
                        ? CreateTypeHint(type, displayAllOverride, forImplicitVariableTypes, varPattern.VarKeyword, variableDesignation.Identifier)
                        : new(type, new TextSpan(variableDesignation.Identifier.SpanStart, 0), textChange: null, trailingSpace: true);
                }
            }
            else if (node is ForEachStatementSyntax { Type.IsVar: true } forEachStatement)
            {
                var info = semanticModel.GetForEachStatementInfo(forEachStatement);
                var type = info.ElementType;
                if (IsValidType(type))
                    return CreateTypeHint(type, displayAllOverride, forImplicitVariableTypes, forEachStatement.Type, forEachStatement.Identifier);
            }
        }

        if (forLambdaParameterTypes || displayAllOverride)
        {
            if (node is ParameterSyntax { Type: null } parameterNode)
            {
                var span = new TextSpan(parameterNode.Identifier.SpanStart, 0);
                var parameter = semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);
                if (parameter?.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } &&
                    IsValidType(parameter?.Type))
                {
                    return parameterNode.Parent?.Parent?.Kind() is SyntaxKind.ParenthesizedLambdaExpression
                        ? new TypeHint(parameter.Type, span, textChange: new TextChange(span, parameter.Type.ToDisplayString(s_minimalTypeStyle) + " "), trailingSpace: true)
                        : new TypeHint(parameter.Type, span, textChange: null, trailingSpace: true);
                }
            }
        }

        if (forImplicitObjectCreation || displayAllOverride)
        {
            if (node is ImplicitObjectCreationExpressionSyntax implicitNew)
            {
                var type = semanticModel.GetTypeInfo(implicitNew, cancellationToken).Type;
                if (IsValidType(type))
                {
                    var span = new TextSpan(implicitNew.NewKeyword.Span.End, 0);
                    return new(type, span, new TextChange(span, " " + type.ToDisplayString(s_minimalTypeStyle)), leadingSpace: true);
                }
            }
        }

        if (forCollectionExpressions || displayAllOverride)
        {
            if (node is CollectionExpressionSyntax collectionExpression)
            {
                var x = semanticModel.GetTypeInfo(collectionExpression, cancellationToken);
                var type = x.ConvertedType;
                if (IsValidType(type))
                {
                    var span = new TextSpan(collectionExpression.OpenBracketToken.SpanStart, 0);
                    return new(type, span, new TextChange(span, " " + type.ToDisplayString(s_minimalTypeStyle)), leadingSpace: true);
                }
            }
        }

        return null;
    }

    private static TypeHint CreateTypeHint(
        ITypeSymbol type,
        bool displayAllOverride,
        bool normalOption,
        SyntaxNodeOrToken displayAllSpan,
        SyntaxNodeOrToken normalSpan)
    {
        var span = GetSpan(displayAllOverride, normalOption, displayAllSpan, normalSpan);
        // if this is a hint that is placed in-situ (i.e. it's not overwriting text like 'var'), then place
        // a space after it to make things feel less cramped.
        var trailingSpace = span.Length == 0;
        return new TypeHint(type, span, new TextChange(displayAllSpan.Span, type.ToDisplayString(s_minimalTypeStyle)), trailingSpace: trailingSpace);
    }

    private static TextSpan GetSpan(
        bool displayAllOverride,
        bool normalOption,
        SyntaxNodeOrToken displayAllSpan,
        SyntaxNodeOrToken normalSpan)
    {
        // If we're showing this because the normal option is on, then place the hint prior to the node being marked.
        if (normalOption)
            return new TextSpan(normalSpan.SpanStart, 0);

        // Otherwise, we're showing because the user explicitly asked to see all hints.  In that case, overwrite the
        // provided span (i.e. overwrite 'var' with 'int') as this provides a cleaner view while the user is in this
        // mode.
        Contract.ThrowIfFalse(displayAllOverride);
        return displayAllSpan.Span;
    }

    private static bool IsValidType([NotNullWhen(true)] ITypeSymbol? type)
    {
        return type is not null or IErrorTypeSymbol && type.Name != "var";
    }
}
