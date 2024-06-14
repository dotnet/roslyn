// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Navigation;

[ExportLanguageService(typeof(IDefinitionLocationService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class CSharpDefinitionLocationService(
    IThreadingContext threadingContext,
    IStreamingFindUsagesPresenter streamingPresenter)
    : AbstractDefinitionLocationService(threadingContext, streamingPresenter)
{
    protected override async Task<ISymbol?> GetInterceptorSymbolAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (span.Start >= root.FullWidth())
            return null;

        var token = root.FindToken(span.Start);
        if (!token.IsKind(SyntaxKind.IdentifierToken) ||
            token.Parent is not SimpleNameSyntax simpleName)
        {
            return null;
        }

        var expression = simpleName.Parent switch
        {
            MemberAccessExpressionSyntax memberAccess when memberAccess.Name == simpleName => memberAccess,
            MemberBindingExpressionSyntax memberBinding when memberBinding.Name == simpleName => memberBinding,
            _ => (ExpressionSyntax)simpleName,
        };

        if (expression.Parent is not InvocationExpressionSyntax invocationExpression)
            return null;

        return await GetInterceptorSymbolAsync(document, invocationExpression, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ISymbol?> GetInterceptorSymbolAsync(
        Document document,
        InvocationExpressionSyntax invocationExpression,
        CancellationToken cancellationToken)
    {
        var contentHash = await document.GetContentHashAsync(cancellationToken).ConfigureAwait(false);
        var position = invocationExpression.FullSpan.Start;
        var interceptsLocationData = new InterceptsLocationData(contentHash, position);

        // We only look for interceptors in generated source documents.  Interceptors cannot reasonably be written by
        // hand (as they involve embedded an encoded version of a file's content hash, position, and other debugging
        // information).  So the only realistic way to create them is by asking the compiler to create the attribute
        // using SemanticModel.GetInterceptableLocation as part of a generator.
        foreach (var generatedDocument in await document.Project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
        {
            var syntaxIndex = await generatedDocument.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);
            if (!syntaxIndex.TryGetInterceptsLocation(interceptsLocationData, out var methodDeclarationSpan))
                continue;

            var root = await generatedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!root.FullSpan.Contains(methodDeclarationSpan))
                continue;

            var methodDeclaration = root.FindNode(methodDeclarationSpan);
            var semanticModel = await generatedDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        }

        return null;
    }
}
