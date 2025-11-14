// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(ExtensionMemberImportCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(TypeImportCompletionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ExtensionMemberImportCompletionProvider() : AbstractExtensionMemberImportCompletionProvider
{
    internal override string Language => LanguageNames.CSharp;

    protected override string GenericSuffix => "<>";

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

    protected override bool IsFinalSemicolonOfUsingOrExtern(SyntaxNode directive, SyntaxToken token)
    {
        if (token.IsKind(SyntaxKind.None) || token.IsMissing)
            return false;

        return directive switch
        {
            UsingDirectiveSyntax usingDirective => usingDirective.SemicolonToken == token,
            ExternAliasDirectiveSyntax externAliasDirective => externAliasDirective.SemicolonToken == token,
            _ => false,
        };
    }

    protected override Task<bool> ShouldProvideParenthesisCompletionAsync(
        Document document,
        CompletionItem item,
        char? commitKey,
        CancellationToken cancellationToken)
        // Ideally we should check if the inferred type for this location is delegate to decide whether to add parenthesis or not
        // However, for an extension method like
        // static class C { public static int ToInt(this Bar b) => 1; }
        // it can only be used as like: bar.ToInt();
        // Func<int> x = bar.ToInt or Func<Bar, int> x = bar.ToInt is illegal. It can't be assign to delegate.
        // Therefore at here we always assume the user always wants to add parenthesis.
        => Task.FromResult(commitKey is ';' or '.');

    protected override bool TryGetReceiverTypeSymbol(
        SemanticModel semanticModel,
        SyntaxNode node,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out ITypeSymbol? receiverTypeSymbol)
    {
        // If we have:
        //
        //      X.Y.$$
        //      var v = ...
        //
        // Then this will seem as if we're asking for completion inside the error qualified type symbol 'X.Y.var' in
        // a qualified type name context (i.e. "types only"). To address that, we call into our helper which checks and
        // reinterprets that case as if we have `X.Y.` in an expression context instead.
        if (node is ExpressionSyntax expression &&
            expression.ShouldNameExpressionBeTreatedAsExpressionInsteadOfType(semanticModel, out _, out var container) &&
            container is not null and not IErrorTypeSymbol)
        {
            receiverTypeSymbol = container;
            return true;
        }

        return base.TryGetReceiverTypeSymbol(semanticModel, node, cancellationToken, out receiverTypeSymbol);
    }
}
