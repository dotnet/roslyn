// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddNew), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class HideBaseCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    internal const string CS0108 = nameof(CS0108); // 'SomeClass.SomeMember' hides inherited member 'SomeClass.SomeMember'. Use the new keyword if hiding was intended.

    public override ImmutableArray<string> FixableDiagnosticIds => [CS0108];

    private static SyntaxNode? GetOriginalNode(SyntaxNode root, Diagnostic diagnostic)
    {
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var token = root.FindToken(diagnosticSpan.Start);

        var originalNode = token.GetAncestor<PropertyDeclarationSyntax>() ??
            token.GetAncestor<MethodDeclarationSyntax>() ??
            (SyntaxNode?)token.GetAncestor<FieldDeclarationSyntax>();
        return originalNode;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var originalNode = GetOriginalNode(root, context.Diagnostics.First());
        if (originalNode == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            CSharpCodeFixesResources.Hide_base_member,
            cancellationToken => GetChangedDocumentAsync(context.Document, originalNode, cancellationToken),
            nameof(CSharpCodeFixesResources.Hide_base_member)),
            context.Diagnostics);
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var modifierOrder = await GetModifierOrderAsync(document, cancellationToken).ConfigureAwait(false);

        var root = editor.OriginalRoot;
        foreach (var diagnostic in diagnostics)
        {
            var originalNode = GetOriginalNode(root, diagnostic);
            if (originalNode == null)
                continue;

            var newNode = GetNewNode(originalNode, modifierOrder);
            editor.ReplaceNode(originalNode, newNode);
        }
    }

    private static async Task<Dictionary<int, int>?> GetModifierOrderAsync(Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var modifierOrder = configOptions.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder).Value;
        CSharpOrderModifiersHelper.Instance.TryGetOrComputePreferredOrder(modifierOrder, out var preferredOrder);
        return preferredOrder;
    }
}
