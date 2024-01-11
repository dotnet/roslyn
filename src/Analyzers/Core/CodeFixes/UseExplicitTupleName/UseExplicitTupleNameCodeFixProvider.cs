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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseExplicitTupleName;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.UseExplicitTupleName), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class UseExplicitTupleNameCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Use_explicitly_provided_tuple_name, nameof(AnalyzersResources.Use_explicitly_provided_tuple_name));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var generator = editor.Generator;

        foreach (var diagnostic in diagnostics)
        {
            var oldNameNode = diagnostic.Location.FindNode(
                getInnermostNodeForTie: true, cancellationToken: cancellationToken);

            var preferredName = diagnostic.Properties[nameof(UseExplicitTupleNameDiagnosticAnalyzer.ElementName)];
            Contract.ThrowIfNull(preferredName);

            var newNameNode = generator.IdentifierName(preferredName).WithTriviaFrom(oldNameNode);

            editor.ReplaceNode(oldNameNode, newNameNode);
        }

        return Task.CompletedTask;
    }
}
