// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(AwaitCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(KeywordCompletionProvider))]
[Shared]
internal sealed class AwaitCompletionProvider : AbstractAwaitCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public AwaitCompletionProvider()
        : base(CSharpSyntaxFacts.Instance)
    {
    }

    internal override string Language => LanguageNames.CSharp;
    public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerCharactersWithArgumentList;

    protected override bool IsAwaitKeywordContext(SyntaxContext syntaxContext)
        => base.IsAwaitKeywordContext(syntaxContext);

    /// <summary>
    /// Gets the span start where async keyword should go.
    /// </summary>
    protected override int GetSpanStart(SyntaxNode declaration)
    {
        return declaration switch
        {
            MethodDeclarationSyntax method => method.ReturnType.SpanStart,
            LocalFunctionStatementSyntax local => local.ReturnType.SpanStart,
            AnonymousMethodExpressionSyntax anonymous => anonymous.DelegateKeyword.SpanStart,
            // If we have an explicit lambda return type, async should go just before it. Otherwise, it should go before parameter list.
            // static [|async|] (a) => ....
            // static [|async|] ExplicitReturnType (a) => ....
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => (parenthesizedLambda.ReturnType as SyntaxNode ?? parenthesizedLambda.ParameterList).SpanStart,
            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.SpanStart,
            _ => throw ExceptionUtilities.UnexpectedValue(declaration.Kind())
        };
    }

    protected override SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token)
    {
        // In a case like
        //   someTask.$$
        //   await Test();
        // someTask.await Test() is parsed as a local function statement.
        // We skip this and look further up in the hierarchy.
        var parent = token.Parent;
        if (parent == null)
            return null;

        if (parent is QualifiedNameSyntax { Parent: LocalFunctionStatementSyntax localFunction } qualifiedName &&
            localFunction.ReturnType == qualifiedName)
        {
            parent = localFunction;
        }

        return parent.AncestorsAndSelf().FirstOrDefault(node => node.IsAsyncSupportingFunctionSyntax());
    }

    protected override SyntaxNode? GetExpressionToPlaceAwaitInFrontOf(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken);
        return dotToken?.Parent switch
        {
            // Don't support conditional access someTask?.$$ or c?.TaskReturning().$$ because there is no good completion until
            // await? is supported by the language https://github.com/dotnet/csharplang/issues/35
            MemberAccessExpressionSyntax memberAccess => memberAccess.GetParentConditionalAccessExpression() is null ? memberAccess : null,
            // someTask.$$.
            RangeExpressionSyntax range => range.LeftOperand,
            // special cases, where parsing is misleading. Such cases are handled in GetTypeSymbolOfExpression.
            QualifiedNameSyntax qualifiedName => qualifiedName.Left,
            _ => null,
        };
    }

    protected override SyntaxToken? GetDotTokenLeftOfPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        => CompletionUtilities.GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken);

    protected override ITypeSymbol? GetTypeSymbolOfExpression(SemanticModel semanticModel, SyntaxNode potentialAwaitableExpression, CancellationToken cancellationToken)
    {
        if (potentialAwaitableExpression is MemberAccessExpressionSyntax memberAccess)
        {
            var memberAccessExpression = memberAccess.Expression.WalkDownParentheses();
            // In cases like Task.$$ semanticModel.GetTypeInfo returns Task, but
            // we don't want to suggest await here. We look up the symbol of the "Task" part
            // and return null if it is a NamedType.
            var symbol = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol;
            return symbol is ITypeSymbol ? null : semanticModel.GetTypeInfo(memberAccessExpression, cancellationToken).Type;
        }
        else if (potentialAwaitableExpression is ExpressionSyntax expression &&
                 expression.ShouldNameExpressionBeTreatedAsExpressionInsteadOfType(semanticModel, out _, out var container))
        {
            return container;
        }
        else
        {
            return semanticModel.GetTypeInfo(potentialAwaitableExpression, cancellationToken).Type;
        }
    }
}
