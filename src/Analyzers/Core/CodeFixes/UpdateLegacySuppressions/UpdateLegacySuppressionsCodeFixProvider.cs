// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UpdateLegacySuppressions
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.UpdateLegacySuppressions), Shared]
    internal sealed class UpdateLegacySuppressionsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UpdateLegacySuppressionsCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.LegacyFormatSuppressMessageAttributeDiagnosticId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Properties?.ContainsKey(AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer.DocCommentIdKey) == true &&
                    root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) != null)
                {
                    RegisterCodeFix(context, CodeFixesResources.Update_suppression_format, nameof(CodeFixesResources.Update_suppression_format));
                }
            }
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                var newDocCommentId = diagnostic.Properties[AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer.DocCommentIdKey];
                editor.ReplaceNode(node, editor.Generator.LiteralExpression(newDocCommentId).WithTriviaFrom(node));
            }

            return Task.CompletedTask;
        }
    }
}
