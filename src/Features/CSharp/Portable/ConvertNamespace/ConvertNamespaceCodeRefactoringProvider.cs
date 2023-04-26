// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    using static ConvertNamespaceAnalysis;
    using static ConvertNamespaceTransform;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertNamespace), Shared]
    internal class ConvertNamespaceCodeRefactoringProvider : SyntaxEditorBasedCodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public ConvertNamespaceCodeRefactoringProvider()
        {
        }

        protected override ImmutableArray<FixAllScope> SupportedFixAllScopes
            => ImmutableArray.Create(FixAllScope.Project, FixAllScope.Solution);

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

            var options = await document.GetCSharpCodeFixOptionsProviderAsync(context.Options, cancellationToken).ConfigureAwait(false);
            if (!CanOfferRefactoring(namespaceDecl, root, options, out var info))
                return;

            context.RegisterRefactoring(CodeAction.Create(
                info.Value.title, c => ConvertAsync(document, namespaceDecl, options.GetFormattingOptions(), c), info.Value.equivalenceKey));
        }

        private static bool CanOfferRefactoring(
            [NotNullWhen(true)] BaseNamespaceDeclarationSyntax? namespaceDecl,
            CompilationUnitSyntax root,
            CSharpCodeFixOptionsProvider options,
            [NotNullWhen(true)] out (string title, string equivalenceKey)? info)
        {
            info =
                CanOfferUseBlockScoped(options.NamespaceDeclarations, namespaceDecl, forAnalyzer: false) ? GetInfo(NamespaceDeclarationPreference.BlockScoped) :
                CanOfferUseFileScoped(options.NamespaceDeclarations, root, namespaceDecl, forAnalyzer: false) ? GetInfo(NamespaceDeclarationPreference.FileScoped) :
                null;

            return info != null;
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

        protected override async Task FixAllAsync(
            Document document,
            ImmutableArray<TextSpan> fixAllSpans,
            SyntaxEditor editor,
            CodeActionOptionsProvider optionsProvider,
            string? equivalenceKey,
            CancellationToken cancellationToken)
        {
            var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetCSharpCodeFixOptionsProviderAsync(optionsProvider, cancellationToken).ConfigureAwait(false);
            var namespaceDecl = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
            if (!CanOfferRefactoring(namespaceDecl, root, options, out var info)
                || info.Value.equivalenceKey != equivalenceKey)
            {
                return;
            }

            document = await ConvertAsync(document, namespaceDecl, options.GetFormattingOptions(), cancellationToken).ConfigureAwait(false);
            var newRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(editor.OriginalRoot, newRoot);
        }
    }
}
