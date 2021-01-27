﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseInterpolatedVerbatimString
{
    /// <summary>
    /// Converts a verbatim interpolated string @$"" to an interpolated verbatim string $@""
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseInterpolatedVerbatimStringCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseInterpolatedVerbatimStringCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create("CS8401");

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        private const string InterpolatedVerbatimText = "$@\"";

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, diagnostic, cancellationToken);
            }

            return Task.CompletedTask;
        }

        private static void AddEdits(
            SyntaxEditor editor,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var verbatimInterpolatedLocation = diagnostic.Location;
            var verbatimInterpolated = (InterpolatedStringExpressionSyntax)verbatimInterpolatedLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);

            var oldStartToken = verbatimInterpolated.StringStartToken;
            var newStartToken = SyntaxFactory.Token(oldStartToken.LeadingTrivia, SyntaxKind.InterpolatedVerbatimStringStartToken,
                InterpolatedVerbatimText, InterpolatedVerbatimText, oldStartToken.TrailingTrivia);

            var interpolatedVerbatim = verbatimInterpolated.WithStringStartToken(newStartToken);

            editor.ReplaceNode(verbatimInterpolated, interpolatedVerbatim);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_interpolated_verbatim_string, createChangedDocument)
            {
            }
        }
    }
}
