// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(YieldCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(KeywordCompletionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class YieldCompletionProvider() : AbstractYieldCompletionProvider("yield", CSharpFeaturesResources.yield_return_statement)
{
    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options) ||
           CompletionUtilities.IsCompilerDirectiveTriggerCharacter(text, characterPosition);

    public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerCharacters.Add(' ');

    protected override bool IsYieldKeywordContext(SyntaxContext syntaxContext)
        => ((CSharpSyntaxContext)syntaxContext).IsStatementContext;

    protected override SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken leftToken, int position)
    {
        var parent = leftToken.Parent;
        switch (parent)
        {
            case null:
                return null;
            case NameSyntax { Parent: LocalFunctionStatementSyntax localFunction } name when localFunction.ReturnType == name:
                parent = localFunction.GetRequiredParent();
                break;
        }

        return parent.FirstAncestorOrSelf<SyntaxNode>(node =>
        {
            if (!node.IsAsyncSupportingFunctionSyntax())
                return false;

            return position > leftToken.FullSpan.End ? node.Span.Contains(position) : node.Span.IntersectsWith(position);
        });
    }

    protected override int GetAsyncKeywordInsertionPosition(SyntaxNode declaration)
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

    protected override Task<TextChange?> GetReturnTypeChangeAsync(Solution solution, SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken)
    {
        return SpecializedTasks.Default<TextChange?>();
    }

    protected override bool ShouldAddModifiers(SyntaxContext syntaxContext, SyntaxNode declaration, CancellationToken cancellationToken)
    {
        var semanticModel = syntaxContext.SemanticModel;

        var methodSymbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken) as IMethodSymbol;
        if (methodSymbol is null || methodSymbol.MethodKind is MethodKind.LambdaMethod or MethodKind.AnonymousFunction)
            return false;

        if (methodSymbol.IsAsync)
            return false;

        var returnType = methodSymbol.ReturnType;

        if (returnType is null or IErrorTypeSymbol)
            return false;

        if (returnType.Name is not ("IAsyncEnumerable" or "IAsyncEnumerator"))
            return false;

        var taskLikeTypes = new KnownTaskTypes(semanticModel.Compilation);
        return returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumerableOfTType) ||
               returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumeratorOfTType);
    }
}
