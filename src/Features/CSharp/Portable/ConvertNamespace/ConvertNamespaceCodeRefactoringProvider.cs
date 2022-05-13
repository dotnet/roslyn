// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    using static ConvertNamespaceAnalysis;
    using static ConvertNamespaceTransform;

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
            var info =
                CanOfferUseBlockScoped(optionSet, namespaceDecl, forAnalyzer: false) ? GetInfo(NamespaceDeclarationPreference.BlockScoped) :
                CanOfferUseFileScoped(optionSet, root, namespaceDecl, forAnalyzer: false) ? GetInfo(NamespaceDeclarationPreference.FileScoped) :
                ((string title, string equivalenceKey)?)null;
            if (info == null)
                return;

            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(context.Options, cancellationToken).ConfigureAwait(false);

            context.RegisterRefactoring(CodeAction.Create(
                info.Value.title, c => ConvertAsync(document, namespaceDecl, formattingOptions, c), info.Value.equivalenceKey));
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
    }
}
