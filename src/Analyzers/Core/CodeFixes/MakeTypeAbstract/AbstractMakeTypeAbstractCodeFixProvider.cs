// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeTypeAbstract;

internal abstract class AbstractMakeTypeAbstractCodeFixProvider<TTypeDeclarationSyntax> : SyntaxEditorBasedCodeFixProvider
    where TTypeDeclarationSyntax : SyntaxNode
{
    protected abstract bool IsValidRefactoringContext(SyntaxNode? node, [NotNullWhen(true)] out TTypeDeclarationSyntax? typeDeclaration);

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (IsValidRefactoringContext(context.Diagnostics[0].Location?.FindNode(context.CancellationToken), out _))
        {
            RegisterCodeFix(context, CodeFixesResources.Make_class_abstract, CodeFixesResources.Make_class_abstract);
        }

        return Task.CompletedTask;
    }

    protected sealed override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < diagnostics.Length; i++)
        {
            if (IsValidRefactoringContext(diagnostics[i].Location?.FindNode(cancellationToken), out var typeDeclaration))
            {
                editor.ReplaceNode(typeDeclaration,
                    (currentTypeDeclaration, generator) => generator.WithModifiers(currentTypeDeclaration, generator.GetModifiers(currentTypeDeclaration).WithIsAbstract(true)));
            }
        }

        return Task.CompletedTask;
    }
}
