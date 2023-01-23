// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using System.Linq;
    using static CodeFixHelpers;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseIndexOperator), Shared]
    internal class CSharpUseIndexOperatorCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseIndexOperatorCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseIndexOperatorDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_index_operator, nameof(CSharpAnalyzersResources.Use_index_operator));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            // Process diagnostics from innermost to outermost in case any are nested.
            foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);

                editor.ReplaceNode(
                    node,
                    (currentNode, _) => IndexExpression(((BinaryExpressionSyntax)currentNode).Right));
            }

            return Task.CompletedTask;
        }
    }
}
