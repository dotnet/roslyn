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
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateType;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddShebangDirective), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.AddShebangDirective)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class AddShebangDirectiveCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.AddShebangDirective];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Regardless of where the diagnostic occurs in the document, the fix is the same.
        // Just put a '#!' directive at the top.
        var directive = SyntaxFactory.ParseLeadingTrivia($"#!/usr/bin/env dotnet{formattingOptions.NewLine}");
        var newRoot = root.WithPrependedLeadingTrivia(directive);
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(CSharpAnalyzersResources.Add_shebang,
                    cancellationToken => Task.FromResult(document.WithSyntaxRoot(newRoot)),
                    equivalenceKey: nameof(CSharpAnalyzersResources.Add_shebang)),
                diagnostic);
        }
    }

    public override FixAllProvider? GetFixAllProvider()
        // Note that reporting/fixing multiple of this diagnostic in a project is not a realistic situation.
        // This is something which would be proposed to be added to a possible file-based program entry point only.
        => null;
}
