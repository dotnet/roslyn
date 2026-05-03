// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeFieldReadonly;

internal abstract class AbstractMakeFieldReadonlyCodeFixProvider<TSymbolSyntax, TFieldDeclarationSyntax>
    : SyntaxEditorBasedCodeFixProvider
    where TSymbolSyntax : SyntaxNode
    where TFieldDeclarationSyntax : SyntaxNode
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId];

    protected abstract SyntaxNode? GetInitializerNode(TSymbolSyntax declaration);
    protected abstract ImmutableList<TSymbolSyntax> GetVariableDeclarators(TFieldDeclarationSyntax declaration);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Add_readonly_modifier, nameof(AnalyzersResources.Add_readonly_modifier));
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        var declarators = new List<TSymbolSyntax>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            declarators.Add(root.FindNode(diagnosticSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<TSymbolSyntax>()!);
        }

        await MakeFieldReadonlyAsync(document, editor, declarators, cancellationToken).ConfigureAwait(false);
    }

    private async Task MakeFieldReadonlyAsync(
        Document document, SyntaxEditor editor, List<TSymbolSyntax> declarators, CancellationToken cancellationToken)
    {
        var generator = editor.Generator;
        var declaratorsByField = declarators.GroupBy(g => g.FirstAncestorOrSelf<TFieldDeclarationSyntax>()!);

        foreach (var fieldDeclarators in declaratorsByField)
        {
            var fieldDeclaration = fieldDeclarators.Key;
            var declarationDeclarators = GetVariableDeclarators(fieldDeclaration);

            if (declarationDeclarators.Count == fieldDeclarators.Count())
            {
                var modifiers = WithReadOnly(editor.Generator.GetModifiers(fieldDeclaration));
                editor.ReplaceNode(
                    fieldDeclaration,
                    generator.WithModifiers(fieldDeclaration.WithoutTrivia(), modifiers).WithTriviaFrom(fieldDeclaration));
            }
            else
            {
                var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                foreach (var declarator in declarationDeclarators.Reverse())
                {
                    var symbol = (IFieldSymbol?)model.GetDeclaredSymbol(declarator, cancellationToken);
                    Contract.ThrowIfNull(symbol);
                    var modifiers = generator.GetModifiers(fieldDeclaration);

                    var newDeclaration = generator
                        .FieldDeclaration(
                            symbol.Name,
                            generator.TypeExpression(symbol.Type),
                            Accessibility.Private,
                            fieldDeclarators.Contains(declarator)
                                ? WithReadOnly(modifiers)
                                : modifiers,
                            GetInitializerNode(declarator))
                        .WithAdditionalAnnotations(Formatter.Annotation);

                    editor.InsertAfter(fieldDeclaration, newDeclaration);
                }

                editor.RemoveNode(fieldDeclaration, SyntaxRemoveOptions.KeepLeadingTrivia);
            }
        }
    }

    private static DeclarationModifiers WithReadOnly(DeclarationModifiers modifiers)
        => (modifiers - DeclarationModifiers.Volatile) | DeclarationModifiers.ReadOnly;
}
