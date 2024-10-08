// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Formatting;

[ExportNewDocumentFormattingProvider(LanguageNames.CSharp), Shared]
internal class CSharpAccessibilityModifiersNewDocumentFormattingProvider : INewDocumentFormattingProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpAccessibilityModifiersNewDocumentFormattingProvider()
    {
    }

    public async Task<Document> FormatNewDocumentAsync(Document document, Document? hintDocument, CodeCleanupOptions options, CancellationToken cancellationToken)
    {
        var accessibilityPreferences = options.FormattingOptions.AccessibilityModifiersRequired;
        if (accessibilityPreferences == AccessibilityModifiersRequired.Never)
        {
            return document;
        }

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var typeDeclarations = root.DescendantNodes().Where(node => syntaxFacts.IsTypeDeclaration(node));
        var editor = new SyntaxEditor(root, document.Project.Solution.Services);

        var service = document.GetRequiredLanguageService<IAddAccessibilityModifiersService>();

        foreach (var declaration in typeDeclarations)
        {
            if (!service.ShouldUpdateAccessibilityModifier(CSharpAccessibilityFacts.Instance, declaration, accessibilityPreferences, out _, out _))
                continue;

            // Since we format each document as they are added to a project we can't assume we know about all
            // of the files that are coming, so we have to opt out of changing partial classes. This especially
            // manifests when creating new projects as we format before we have a project at all, so we could get a
            // situation like this:
            //
            // File1.cs:
            //    partial class C { }
            // File2.cs:
            //    public partial class C { }
            //
            // When we see File1, we don't know about File2, so would add an internal modifier, which would result in a compile
            // error.
            var modifiers = syntaxFacts.GetModifiers(declaration);
            CSharpAccessibilityFacts.GetAccessibilityAndModifiers(modifiers, out _, out var declarationModifiers, out _);
            if (declarationModifiers.IsPartial)
                continue;

            var type = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
            if (type == null)
                continue;

            AddAccessibilityModifiersHelpers.UpdateDeclaration(editor, type, declaration);
        }

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }
}
