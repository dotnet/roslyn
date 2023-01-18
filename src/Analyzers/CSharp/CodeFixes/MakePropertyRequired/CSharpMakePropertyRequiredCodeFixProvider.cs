﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

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

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Required members are available in C# 11 or higher
        if (root.GetLanguageVersion() < LanguageVersion.CSharp11)
            return;

        var node = root.FindNode(span);

        if (node is PropertyDeclarationSyntax propertyDeclaration)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken);

            if (propertySymbol is null)
                return;

            var setMethod = propertySymbol.SetMethod;

            // Property must have a `set` or `init` accessor in order to be able to be required
            if (setMethod is null)
                return;

            var containingTypeVisibility = propertySymbol.ContainingType.GetResultantVisibility();
            var minimalAccessibility = (Accessibility)Math.Min((int)propertySymbol.DeclaredAccessibility, (int)setMethod.DeclaredAccessibility);

            if (!CanBeAccessed(containingTypeVisibility, minimalAccessibility))
                return;

            RegisterCodeFix(context, CSharpCodeFixesResources.Make_property_required, nameof(CSharpCodeFixesResources.Make_property_required));
        }
        else if (node is VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax })
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var fieldSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

            if (fieldSymbol is null)
                return;

            var containingTypeVisibility = fieldSymbol.ContainingType.GetResultantVisibility();
            var accessibility = fieldSymbol.DeclaredAccessibility;

            if (!CanBeAccessed(containingTypeVisibility, accessibility))
                return;

            RegisterCodeFix(context, CSharpCodeFixesResources.Make_field_required, nameof(CSharpCodeFixesResources.Make_field_required));
        }

        static bool CanBeAccessed(SymbolVisibility containingTypeVisibility, Accessibility accessibility) => containingTypeVisibility switch
        {
            SymbolVisibility.Public => accessibility is Accessibility.Public,
            SymbolVisibility.Internal => accessibility is >= Accessibility.Internal,
            SymbolVisibility.Private => accessibility is >= Accessibility.Internal,
            _ => throw ExceptionUtilities.Unreachable(),
        };
    }

    protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;
        var generator = editor.Generator;
        var visitedFieldDeclarations = new HashSet<FieldDeclarationSyntax>();

        foreach (var diagnostic in diagnostics)
        {
            var propertyDeclarationOrFieldVariableDeclarator = root.FindNode(diagnostic.Location.SourceSpan);

            // If we are fixing field, do not apply new declaration modifiers just to variable declarator, but to the whole field declaration.
            // This is observable when there are several variables in single filed declaration:
            // `public string _myField, _myField1;` -> `public required string _myField, _myField1;`
            // Without this branch the result would be:
            // ```
            // public required string _myField;
            // public required string _myField2;
            // ```
            if (propertyDeclarationOrFieldVariableDeclarator is VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax fieldDeclaration })
            {
                // Skip field declarations we already visited to not try changing the same declaration twice.
                // Otherwise we get an exception in fix-all scenario like this:
                // `public string _myField, _myField1`
                // Here when visiting diagnostic for `_myField1` we already changed this field declaration.
                // Trying to change it again throws in `SyntaxEditor` later, because it cannot find the node.
                if (visitedFieldDeclarations.Contains(fieldDeclaration))
                    continue;

                var declarationModifiers = generator.GetModifiers(fieldDeclaration);
                var newDeclarationModifiers = declarationModifiers.WithIsRequired(true);
                editor.ReplaceNode(fieldDeclaration, generator.WithModifiers(fieldDeclaration, newDeclarationModifiers));

                visitedFieldDeclarations.Add(fieldDeclaration);
            }
            else
            {
                var declarationModifiers = generator.GetModifiers(propertyDeclarationOrFieldVariableDeclarator);
                var newDeclarationModifiers = declarationModifiers.WithIsRequired(true);
                editor.ReplaceNode(propertyDeclarationOrFieldVariableDeclarator, generator.WithModifiers(propertyDeclarationOrFieldVariableDeclarator, newDeclarationModifiers));
            }
        }

        return Task.CompletedTask;
    }
}
