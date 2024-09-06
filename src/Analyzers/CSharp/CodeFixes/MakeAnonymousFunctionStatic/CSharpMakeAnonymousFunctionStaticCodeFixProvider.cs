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

namespace Microsoft.CodeAnalysis.CSharp.MakeAnonymousFunctionStatic;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeAnonymousFunctionStatic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMakeAnonymousFunctionStaticCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.MakeAnonymousFunctionStaticDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(
            context,
            CSharpAnalyzersResources.Make_anonymous_function_static,
            nameof(CSharpAnalyzersResources.Make_anonymous_function_static),
            context.Diagnostics[0].Severity > DiagnosticSeverity.Hidden ? CodeActionPriority.Default : CodeActionPriority.Low);

        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var generator = editor.Generator;

        foreach (var diagnostic in diagnostics)
        {
            var anonymousFunction = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            editor.ReplaceNode(anonymousFunction, static (node, generator) => generator.WithModifiers(node, generator.GetModifiers(node).WithIsStatic(true)));
        }

        return Task.CompletedTask;
    }
}
