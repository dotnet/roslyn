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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructMemberReadOnly;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeStructMemberReadOnly), Shared]
internal sealed class CSharpMakeStructMemberReadOnlyCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMakeStructMemberReadOnlyCodeFixProvider()
    {
    }

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
        var declarations = diagnostics.Select(d => d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken));

        //using var _1 = PooledHashSet<MethodDeclarationSyntax>.GetInstance(out var methodsToUpdate);
        //using var _2 = PooledHashSet<PropertyDeclarationSyntax>.GetInstance(out var propertiesToUpdate);
        //using var _3 = PooledHashSet<AccessorDeclarationSyntax>.GetInstance(out var accessorsToUpdate);

        //foreach (var declaration in declarations)
        //{
        //    if (declaration is MethodDeclarationSyntax methodDeclaration)
        //    {
        //        methodsToUpdate.Add(methodDeclaration);
        //    }
        //    else if (declaration is PropertyDeclarationSyntax propertyDeclaration)
        //    {
        //        propertiesToUpdate.Add(propertyDeclaration);
        //    }
        //    else if (declaration is AccessorDeclarationSyntax { Parent: PropertyDeclarationSyntax property } accessorDeclaration)
        //    {
        //        if ()
        //    }
        //}

        // process from lower to higher, that way we will fixup a nested struct first before fixing the outer struct.
        foreach (var typeDeclaration in declarations.OrderByDescending(t => t.SpanStart))
        {
            editor.ReplaceNode(
                typeDeclaration,
                (current, generator) =>
                {
                    if (current is MethodDeclarationSyntax or PropertyDeclarationSyntax)
                        return generator.WithModifiers(current, generator.GetModifiers(current).WithIsReadOnly(true));

                    if ()

                    return current;
                }
        }

        return Task.CompletedTask;
    }
}
