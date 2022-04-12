// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveInKeyword
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveIn), Shared]
    internal class RemoveInKeywordCodeFixProvider : CodeFixProvider
    {
        private const string CS1615 = nameof(CS1615); // Argument 1 may not be passed with the 'in' keyword

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public RemoveInKeywordCodeFixProvider()
        {
        }

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS1615);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);

            var argumentSyntax = token.GetAncestor<ArgumentSyntax>();
            if (argumentSyntax == null || argumentSyntax.GetRefKind() != RefKind.In)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpFeaturesResources.Remove_in_keyword,
                    ct => FixAsync(context.Document, argumentSyntax, ct),
                    nameof(CSharpFeaturesResources.Remove_in_keyword)),
                context.Diagnostics);
        }

        private static async Task<Document> FixAsync(
            Document document,
            ArgumentSyntax argumentSyntax,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

            return document.WithSyntaxRoot(root.ReplaceNode(
                argumentSyntax,
                generator.Argument(generator.SyntaxFacts.GetExpressionOfArgument(argumentSyntax))));
        }
    }
}
