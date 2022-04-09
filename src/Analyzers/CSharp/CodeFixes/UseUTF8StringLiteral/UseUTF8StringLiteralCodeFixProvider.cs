// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseUTF8StringLiteral), Shared]
    internal sealed class UseUTF8StringLiteralCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const char QuoteCharacter = '"';
        private const string Suffix = "u8";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UseUTF8StringLiteralCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseUTF8StringLiteralDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(CodeAction.Create(
                CSharpAnalyzersResources.Use_UTF8_string_literal,
                c => FixAsync(context.Document, context.Diagnostics[0], c),
                nameof(CSharpAnalyzersResources.Use_UTF8_string_literal)),
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

                var arrayNode = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                var stringValue = diagnostic.Properties[UseUTF8StringLiteralDiagnosticAnalyzer.StringValuePropertyName]!;

                editor.ReplaceNode(arrayNode, CreateUTF8String(arrayNode, stringValue));
            }

            return Task.CompletedTask;
        }

        private static SyntaxNode CreateUTF8String(SyntaxNode arrayNode, string stringValue)
        {
            var literal = SyntaxFactory.Token(
                    leading: arrayNode.GetLeadingTrivia(),
                    kind: SyntaxKind.UTF8StringLiteralToken,
                    text: QuoteCharacter + stringValue + QuoteCharacter + Suffix,
                    valueText: "",
                    trailing: arrayNode.GetTrailingTrivia());

            return SyntaxFactory.LiteralExpression(SyntaxKind.UTF8StringLiteralExpression, literal);
        }
    }
}
