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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.MakePropertyRequired;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakePropertyRequired), Shared]
internal sealed class CSharpMakePropertyRequiredCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    private const string CS8618 = nameof(CS8618); // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMakePropertyRequiredCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(CS8618);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var span = context.Span;
        var cancellationToken = context.CancellationToken;

        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var node = root.FindNode(span);

        if (node is not PropertyDeclarationSyntax propertyDeclaration)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel.Compilation;

        // 1. Required members are available in C# 11 or higher
        // 2. `RequiredMember` attribute must be present in the metadata in order to emit required members
        if (compilation.LanguageVersion() < LanguageVersion.CSharp11 ||
            compilation.RequiredMemberAttributeType() is null)
        {
            return;
        }

        var propertySymbol = compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(propertyDeclaration, cancellationToken);

        if (propertySymbol is null)
            return;

        var setMethod = propertySymbol.SetMethod;

        // Property must have a `set` or `init` accessor in order to be able to be required
        if (setMethod is null)
            return;

        var containingTypeAccessibility = propertySymbol.ContainingType.DeclaredAccessibility;

        // Property itself and its set/init accessor must have
        // at least equal accessibility as the type they belong to
        // in order to be able to be required
        if (propertySymbol.DeclaredAccessibility < containingTypeAccessibility ||
            setMethod.DeclaredAccessibility < containingTypeAccessibility)
        {
            return;
        }

        RegisterCodeFix(context, CSharpCodeFixesResources.Make_property_required, nameof(CSharpCodeFixesResources.Make_property_required));
    }

    protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;
        var generator = editor.Generator;

        foreach (var diagnostic in diagnostics)
        {
            var propertyDeclaration = root.FindNode(diagnostic.Location.SourceSpan);
            var declarationModifiers = generator.GetModifiers(propertyDeclaration);
            var newDeclarationModifiers = declarationModifiers.WithIsRequired(true);
            editor.ReplaceNode(propertyDeclaration, generator.WithModifiers(propertyDeclaration, newDeclarationModifiers));
        }

        return Task.CompletedTask;
    }
}
