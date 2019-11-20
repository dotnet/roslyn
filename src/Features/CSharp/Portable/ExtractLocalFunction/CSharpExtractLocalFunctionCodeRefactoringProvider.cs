// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractLocalFunction
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.ExtractLocalFunction), Shared]
    internal class CSharpExtractLocalFunctionCodeRefactoringProvider : ExtractMethodCodeRefactoringProvider
    {
        [ImportingConstructor]
        public CSharpExtractLocalFunctionCodeRefactoringProvider()
        {
        }

        protected override async Task<CodeAction> GetCodeActionAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var result = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                localFunction: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);

            if (result.Succeeded || result.SucceededWithSuggestion)
            {
                // We insert an empty line between the generated local method and the previous statements if there is not one already.
                var codeAction = new MyCodeAction(FeaturesResources.Extract_local_function, c => InsertNewLineBeforeLocalMethodIfNecessaryAsync(result, c));

                return codeAction;
            }

            return default;
        }

        private async Task<Document> InsertNewLineBeforeLocalMethodIfNecessaryAsync(
            ExtractMethodResult result,
            CancellationToken cancellationToken)
        {
            var resultDocument = result.Document;
            var resultMethodDeclarationNode = result.MethodDeclarationNode;
            var resultInvocationNameToken = result.InvocationNameToken;

            // Checking to see if there is already an empty line before the local method declaration.
            var leadingTrivia = result.MethodDeclarationNode.GetLeadingTrivia();
            if (!leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                resultMethodDeclarationNode = result.MethodDeclarationNode.WithPrependedLeadingTrivia(SpecializedCollections.SingletonEnumerable(SyntaxFactory.CarriageReturnLineFeed));

                // Generating the new document and associated variables.
                var root = await resultDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                resultDocument = resultDocument.WithSyntaxRoot(root.ReplaceNode(result.MethodDeclarationNode, resultMethodDeclarationNode));

                var newRoot = await resultDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                resultInvocationNameToken = newRoot.FindToken(result.InvocationNameToken.SpanStart);
            }

            return await AddRenameAnnotationAsync(resultDocument, resultInvocationNameToken, cancellationToken).ConfigureAwait(false);
        }
    }
}
