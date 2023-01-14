// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseImplicitObjectCreation), Shared]
    internal class CSharpUseImplicitObjectCreationCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpUseImplicitObjectCreationCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseImplicitObjectCreationDiagnosticId);

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.IsSuppressed;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_new, nameof(CSharpAnalyzersResources.Use_new));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            // process from inside->out so that outer rewrites see the effects of inner changes.
            foreach (var diagnostic in diagnostics.OrderBy(d => d.Location.SourceSpan.End))
                FixOne(editor, diagnostic, cancellationToken);

            return Task.CompletedTask;
        }

        private static void FixOne(SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var node = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            editor.ReplaceNode(node, (current, _) =>
            {
                var currentObjectCreation = (ObjectCreationExpressionSyntax)current;
                return SyntaxFactory.ImplicitObjectCreationExpression(
                    WithoutTrailingWhitespace(currentObjectCreation.NewKeyword),
                    currentObjectCreation.ArgumentList ?? SyntaxFactory.ArgumentList(),
                    currentObjectCreation.Initializer);
            });
        }

        private static SyntaxToken WithoutTrailingWhitespace(SyntaxToken newKeyword)
        {
            return newKeyword.TrailingTrivia.All(t => t.IsWhitespace())
                ? newKeyword.WithoutTrailingTrivia()
                : newKeyword;
        }
    }
}
