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
        var node = leftToken.Parent;
        if (node == null)
            return null;

        var declaration = node.FirstAncestorOrSelf<SyntaxNode>(node =>
            node is MethodDeclarationSyntax or LocalFunctionStatementSyntax or LambdaExpressionSyntax or AnonymousMethodExpressionSyntax or AccessorDeclarationSyntax);

        if (declaration is MethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax)
        {
            var isMatch = position > leftToken.FullSpan.End ? declaration.Span.Contains(position) : declaration.Span.IntersectsWith(position);
            return isMatch ? declaration : null;
        }

        return null;
    }

    protected override int GetAsyncKeywordInsertionPosition(SyntaxNode declaration)
    {
        return declaration switch
        {
            MethodDeclarationSyntax method => method.ReturnType.SpanStart,
            LocalFunctionStatementSyntax local => local.ReturnType.SpanStart,
            _ => throw ExceptionUtilities.UnexpectedValue(declaration.Kind())
        };
    }

    protected override Task<TextChange?> GetReturnTypeChangeAsync(Solution solution, SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken)
    {
        return SpecializedTasks.Default<TextChange?>();
    }

    protected override bool IsValidContext(SyntaxNode declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not IMethodSymbol methodSymbol)
            return false;

        if (methodSymbol.ReturnsVoid)
            return false;

        var returnType = methodSymbol.ReturnType;
        if (returnType is null)
            return false;

        return CheckReturnType(returnType, semanticModel.Compilation);
    }

    private static bool CheckReturnType(ITypeSymbol returnType, Compilation compilation)
    {
        var taskLikeTypes = new KnownTaskTypes(compilation);

        if (returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumerableOfTType) ||
            returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumeratorOfTType))
        {
            return true;
        }

        var iEnumerableOfT = compilation.IEnumerableOfTType();
        var iEnumeratorOfT = compilation.IEnumeratorOfTType();
        var iEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
        var iEnumerator = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerator);

        if (returnType.OriginalDefinition.Equals(iEnumerableOfT) ||
            returnType.OriginalDefinition.Equals(iEnumeratorOfT) ||
            returnType.Equals(iEnumerable) ||
            returnType.Equals(iEnumerator))
        {
            return true;
        }

        return returnType.Name is "IEnumerable" or "IEnumerator" or "IAsyncEnumerable" or "IAsyncEnumerator";
    }

    protected override bool ShouldAddModifiers(SyntaxContext syntaxContext, SyntaxNode declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var methodSymbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken) as IMethodSymbol;
        if (methodSymbol is null || methodSymbol.MethodKind is MethodKind.LambdaMethod or MethodKind.AnonymousFunction or MethodKind.PropertyGet)
            return false;

        if (methodSymbol.IsAsync)
            return false;

        var returnType = methodSymbol.ReturnType;

        if (returnType is null)
            return false;

        if (CheckAsyncReturnType(returnType, semanticModel.Compilation))
            return true;

        return returnType.Name is "IAsyncEnumerable" or "IAsyncEnumerator";
    }

    private static bool CheckAsyncReturnType(ITypeSymbol returnType, Compilation compilation)
    {
        var taskLikeTypes = new KnownTaskTypes(compilation);
        return returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumerableOfTType) ||
               returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumeratorOfTType);
    }
}
