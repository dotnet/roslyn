// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        internal override async Task<(CodeAction action, string methodBlock)> GetCodeActionAsync(
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
                preferStatic: preferStatic,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);

            if (result.Succeeded || result.SucceededWithSuggestion)
            {
                // We want to ensure that there is an empty line in between the generated local method and the previous statements.
                var resultDocument = result.Document;
                var resultMethodDeclarationNode = result.MethodDeclarationNode;
                var resultInvocationNameToken = result.InvocationNameToken;

                var leadingTrivia = result.MethodDeclarationNode.GetLeadingTrivia();
                if (!leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
                {
                    resultMethodDeclarationNode = result.MethodDeclarationNode.WithPrependedLeadingTrivia(SpecializedCollections.SingletonEnumerable(SyntaxFactory.CarriageReturnLineFeed));

                    var root = await resultDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var newRoot = root.ReplaceNode(result.MethodDeclarationNode, resultMethodDeclarationNode);
                    resultDocument = resultDocument.WithSyntaxRoot(newRoot);

                    newRoot = await resultDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    resultInvocationNameToken = newRoot.FindToken(result.InvocationNameToken.SpanStart);
                }

                var description = documentOptions.GetOption(ExtractMethodOptions.AllowMovingDeclaration) ?
                                      FeaturesResources.Extract_Local_Method_plus_Local : FeaturesResources.Extract_Local_Method;

                var codeAction = new MyCodeAction(description, c => AddRenameAnnotationAsync(resultDocument, resultInvocationNameToken, c));
                var methodBlock = resultMethodDeclarationNode;

                return (codeAction, methodBlock.ToString());
            }

            return default;
        }
    }
}
