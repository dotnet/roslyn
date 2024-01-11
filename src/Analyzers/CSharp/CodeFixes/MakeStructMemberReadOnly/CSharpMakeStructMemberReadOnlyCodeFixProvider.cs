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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructMemberReadOnly;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeStructMemberReadOnly), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMakeStructMemberReadOnlyCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(IDEDiagnosticIds.MakeStructMemberReadOnlyDiagnosticId);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Make_member_readonly, nameof(CSharpAnalyzersResources.Make_member_readonly));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        var generator = editor.Generator;
        var declarations = diagnostics.Select(d => d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken));

        foreach (var declaration in declarations.OrderByDescending(t => t.SpanStart))
        {
            // Methods and properties are easy to directly update.  We just add the 'readonly' modifier directly to them.
            if (declaration is MethodDeclarationSyntax or BasePropertyDeclarationSyntax)
            {
                editor.ReplaceNode(
                    declaration,
                    UpdateReadOnlyModifier(declaration, add: true));
            }
            else if (declaration is AccessorDeclarationSyntax { Parent: AccessorListSyntax { Parent: BasePropertyDeclarationSyntax property } accessorList } accessor)
            {
                // Accessors have rules about how 'readonly' should be added to it vs the containing property.
                if (accessorList.Accessors.Count == 1)
                {
                    // `int X { readonly get { } }` is not legal.it has to be `readonly int X { get { } }`.
                    // So add the modifier to the property
                    editor.ReplaceNode(
                        property,
                        UpdateReadOnlyModifier(property, add: true));
                }
                else if (accessorList.Accessors.Count == 2)
                {
                    // `int X { readonly get { } readonly set { } }` is not legal.  Has to add the modifier to the property.
                    editor.ReplaceNode(
                        property,
                        (current, generator) =>
                        {
                            var currentProperty = (BasePropertyDeclarationSyntax)current;
                            var currentAccessorList = currentProperty.AccessorList;
                            Contract.ThrowIfNull(currentAccessorList);

                            var currentAccessor = currentAccessorList.Accessors.First(a => a.Kind() == accessor.Kind());
                            var otherAccessor = currentAccessorList.Accessors.Single(a => a != currentAccessor);

                            if (otherAccessor.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                            {
                                // both accessors would have 'readonly' on them.  Remove from the accessors and place on the property.
                                currentProperty = currentProperty.ReplaceNode(
                                    otherAccessor,
                                    UpdateReadOnlyModifier(otherAccessor, add: false));
                                return UpdateReadOnlyModifier(currentProperty, add: true);
                            }
                            else
                            {
                                // Otherwise, just add to this accessor alone.
                                return currentProperty.ReplaceNode(
                                    currentAccessor,
                                    UpdateReadOnlyModifier(currentAccessor, add: true));
                            }
                        });
                }
            }
        }

        return Task.CompletedTask;

        TNode UpdateReadOnlyModifier<TNode>(TNode node, bool add) where TNode : SyntaxNode
        {
            return (TNode)generator.WithModifiers(node, generator.GetModifiers(node).WithIsReadOnly(add));
        }
    }
}
