// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertNamespace), Shared]
    internal class ConvertNamespaceCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertNamespaceCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            if (!span.IsEmpty)
                return;

            var position = span.Start;
            var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var namespaceDecl = token.GetAncestor<BaseNamespaceDeclarationSyntax>();
            if (namespaceDecl == null)
                return;

            if (!IsValidPosition(namespaceDecl, position))
                return;

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var title =
                ConvertNamespaceHelper.CanOfferUseBlockScoped(optionSet, namespaceDecl, forAnalyzer: false) ? CSharpFeaturesResources.Convert_to_block_scoped_namespace :
                ConvertNamespaceHelper.CanOfferUseFileScoped(optionSet, root, namespaceDecl, forAnalyzer: false) ? CSharpFeaturesResources.Convert_to_file_scoped_namespace : null;
            if (title == null)
                return;

            context.RegisterRefactoring(new MyCodeAction(
                title, c => ConvertNamespaceHelper.ConvertAsync(document, namespaceDecl, c)));
        }

        private static bool IsValidPosition(BaseNamespaceDeclarationSyntax baseDeclaration, int position)
        {
            if (position < baseDeclaration.SpanStart)
                return false;

            if (baseDeclaration is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
                return position <= fileScopedNamespace.SemicolonToken.Span.End;

            if (baseDeclaration is NamespaceDeclarationSyntax namespaceDeclaration)
                return position <= namespaceDeclaration.Name.Span.End;

            throw ExceptionUtilities.UnexpectedValue(baseDeclaration.Kind());
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
