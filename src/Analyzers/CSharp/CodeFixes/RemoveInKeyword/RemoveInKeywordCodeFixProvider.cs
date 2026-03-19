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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveInKeyword;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveIn), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class RemoveInKeywordCodeFixProvider() : CodeFixProvider
{
    private const string CS1615 = nameof(CS1615); // Argument 1 may not be passed with the 'in' keyword

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override ImmutableArray<string> FixableDiagnosticIds => [CS1615];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var token = root.FindToken(diagnosticSpan.Start);

        var argumentSyntax = token.GetAncestor<ArgumentSyntax>();
        if (argumentSyntax == null || argumentSyntax.GetRefKind() != RefKind.In)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpCodeFixesResources.Remove_in_keyword,
                cancellationToken => FixAsync(context.Document, argumentSyntax, cancellationToken),
                nameof(CSharpCodeFixesResources.Remove_in_keyword)),
            context.Diagnostics);
    }

    private static async Task<Document> FixAsync(
        Document document,
        ArgumentSyntax argumentSyntax,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        return document.WithSyntaxRoot(root.ReplaceNode(
            argumentSyntax,
            generator.Argument(syntaxFacts.GetExpressionOfArgument(argumentSyntax))));
    }
}
