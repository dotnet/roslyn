// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddBraces), Shared]
internal sealed class CSharpAddBracesCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpAddBracesCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.AddBracesDiagnosticId];

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Add_braces, nameof(CSharpAnalyzersResources.Add_braces));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;
        foreach (var diagnostic in diagnostics)
        {
            var statement = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            // Use the callback version of ReplaceNode so that we see the effects
            // of other replace calls.  i.e. we may have statements nested in statements,
            // we need to make sure that any inner edits are seen when we make the outer
            // replacement.
            editor.ReplaceNode(statement, (currentStatement, g) =>
            {
                var embeddedStatement = currentStatement.GetEmbeddedStatement();
                return embeddedStatement is null ? currentStatement : currentStatement.ReplaceNode(embeddedStatement, SyntaxFactory.Block(embeddedStatement));
            });
        }

        return Task.CompletedTask;
    }
}
