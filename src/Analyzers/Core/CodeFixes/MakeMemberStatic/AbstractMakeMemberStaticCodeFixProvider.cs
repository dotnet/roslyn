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

namespace Microsoft.CodeAnalysis.MakeMemberStatic;

internal abstract class AbstractMakeMemberStaticCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    protected abstract bool TryGetMemberDeclaration(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? memberDeclaration);

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (context.Diagnostics.Length == 1 &&
            TryGetMemberDeclaration(context.Diagnostics[0].Location.FindNode(context.CancellationToken), out _))
        {
            RegisterCodeFix(context, CodeFixesResources.Make_member_static, nameof(AbstractMakeMemberStaticCodeFixProvider));
        }

        return Task.CompletedTask;
    }

    protected sealed override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < diagnostics.Length; i++)
        {
            var declaration = diagnostics[i].Location.FindNode(cancellationToken);

            if (TryGetMemberDeclaration(declaration, out var memberDeclaration))
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var newNode = generator.WithModifiers(memberDeclaration, generator.GetModifiers(declaration).WithIsStatic(true));
                editor.ReplaceNode(declaration, newNode);
            }
        }

        return Task.CompletedTask;
    }
}
