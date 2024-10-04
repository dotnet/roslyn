// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertNamespace), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ConvertNamespaceCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseBlockScopedNamespaceDiagnosticId, IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        var (title, equivalenceKey) = ConvertNamespaceAnalysis.GetInfo(
            diagnostic.Id switch
            {
                IDEDiagnosticIds.UseBlockScopedNamespaceDiagnosticId => NamespaceDeclarationPreference.BlockScoped,
                IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId => NamespaceDeclarationPreference.FileScoped,
                _ => throw ExceptionUtilities.UnexpectedValue(diagnostic.Id),
            });

        context.RegisterCodeFix(
            CodeAction.Create(title, GetDocumentUpdater(context), equivalenceKey),
            context.Diagnostics);

        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var diagnostic = diagnostics.First();

        var namespaceDecl = (BaseNamespaceDeclarationSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);

        var options = await document.GetCSharpSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var converted = await ConvertNamespaceTransform.ConvertAsync(document, namespaceDecl, options, cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(
            editor.OriginalRoot,
            await converted.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
    }
}
