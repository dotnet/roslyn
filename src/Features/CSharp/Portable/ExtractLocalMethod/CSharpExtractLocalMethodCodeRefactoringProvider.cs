// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractLocalMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.ExtractLocalMethod), Shared]
    internal class CSharpExtractLocalMethodCodeRefactoringProvider : ExtractMethodCodeRefactoringProvider
    {
        [ImportingConstructor]
        public CSharpExtractLocalMethodCodeRefactoringProvider()
        {
        }

        protected override async Task<(CodeAction action, string methodBlock)> GetCodeActionAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var preferStatic = documentOptions.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction).Value;

            var result = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                extractLocalMethod: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);

            if (result.Succeeded || result.SucceededWithSuggestion)
            {
                // We insert an empty line between the generated local method and the previous statements if there is not one already.
                var (resultDocument, resultMethodDeclarationNode, resultInvocationNameToken) = await InsertNewLineBeforeLocalMethodIfNecessaryAsync(result, cancellationToken).ConfigureAwait(false);

                var description = FeaturesResources.Extract_Local_Method;

                var codeAction = new MyCodeAction(description, c => AddRenameAnnotationAsync(resultDocument, resultInvocationNameToken, c));
                var methodBlock = resultMethodDeclarationNode;

                return (codeAction, methodBlock.ToString());
            }

            return default;
        }

        private async Task<(Document resultDocument, SyntaxNode resultMethodDeclarationNode, SyntaxToken resultInvocationNameToken)> InsertNewLineBeforeLocalMethodIfNecessaryAsync(
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

            return (resultDocument, resultMethodDeclarationNode, resultInvocationNameToken);
        }
    }
}
