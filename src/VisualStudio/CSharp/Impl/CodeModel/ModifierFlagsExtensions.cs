// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel;

internal static class ModifierFlagsExtensions
{
    private static readonly SortedList<ModifierFlags, SyntaxKind> s_modifierDefinitions = new()
    {
        { ModifierFlags.Public, SyntaxKind.PublicKeyword },
        { ModifierFlags.Protected, SyntaxKind.ProtectedKeyword },
        { ModifierFlags.Internal, SyntaxKind.InternalKeyword },
        { ModifierFlags.Private, SyntaxKind.PrivateKeyword },
        { ModifierFlags.Virtual, SyntaxKind.VirtualKeyword },
        { ModifierFlags.Abstract, SyntaxKind.AbstractKeyword },
        { ModifierFlags.New, SyntaxKind.NewKeyword },
        { ModifierFlags.Override, SyntaxKind.OverrideKeyword },
        { ModifierFlags.Sealed, SyntaxKind.SealedKeyword },
        { ModifierFlags.Static, SyntaxKind.StaticKeyword },
        { ModifierFlags.Extern, SyntaxKind.ExternKeyword },
        { ModifierFlags.ReadOnly, SyntaxKind.ReadOnlyKeyword },
        { ModifierFlags.Const, SyntaxKind.ConstKeyword },
        { ModifierFlags.Volatile, SyntaxKind.VolatileKeyword },
        { ModifierFlags.Unsafe, SyntaxKind.UnsafeKeyword },
        { ModifierFlags.Async, SyntaxKind.AsyncKeyword },
        { ModifierFlags.Partial, SyntaxKind.PartialKeyword }
    };

    public static ModifierFlags GetModifierFlags(this MemberDeclarationSyntax member)
    {
        ModifierFlags result = 0;

        foreach (var modifier in member.GetModifiers())
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.PublicKeyword:
                    result |= ModifierFlags.Public;
                    break;
                case SyntaxKind.ProtectedKeyword:
                    result |= ModifierFlags.Protected;
                    break;
                case SyntaxKind.InternalKeyword:
                    result |= ModifierFlags.Internal;
                    break;
                case SyntaxKind.PrivateKeyword:
                    result |= ModifierFlags.Private;
                    break;
                case SyntaxKind.VirtualKeyword:
                    result |= ModifierFlags.Virtual;
                    break;
                case SyntaxKind.AbstractKeyword:
                    result |= ModifierFlags.Abstract;
                    break;
                case SyntaxKind.NewKeyword:
                    result |= ModifierFlags.New;
                    break;
                case SyntaxKind.OverrideKeyword:
                    result |= ModifierFlags.Override;
                    break;
                case SyntaxKind.SealedKeyword:
                    result |= ModifierFlags.Sealed;
                    break;
                case SyntaxKind.StaticKeyword:
                    result |= ModifierFlags.Static;
                    break;
                case SyntaxKind.ExternKeyword:
                    result |= ModifierFlags.Extern;
                    break;
                case SyntaxKind.ReadOnlyKeyword:
                    result |= ModifierFlags.ReadOnly;
                    break;
                case SyntaxKind.ConstKeyword:
                    result |= ModifierFlags.Const;
                    break;
                case SyntaxKind.VolatileKeyword:
                    result |= ModifierFlags.Volatile;
                    break;
                case SyntaxKind.UnsafeKeyword:
                    result |= ModifierFlags.Unsafe;
                    break;
                case SyntaxKind.AsyncKeyword:
                    result |= ModifierFlags.Async;
                    break;
                case SyntaxKind.PartialKeyword:
                    result |= ModifierFlags.Partial;
                    break;
            }
        }

        return result;
    }

    public static MemberDeclarationSyntax UpdateModifiers(this MemberDeclarationSyntax member, ModifierFlags flags)
    {
        // The starting token for this member may change, so we need to save
        // the leading trivia and reattach it after updating the modifiers.
        // We also need to remove it here to avoid duplicates.
        var leadingTrivia = member.GetLeadingTrivia();
        member = member.WithLeadingTrivia(SyntaxTriviaList.Empty);

        var newModifierList = new List<SyntaxToken>();
        foreach (var modifierDefinition in s_modifierDefinitions)
        {
            if ((flags & modifierDefinition.Key) != 0)
            {
                newModifierList.Add(SyntaxFactory.Token(modifierDefinition.Value));
            }
        }

        var newMember = member.WithModifiers([.. newModifierList]);
        return newMember.WithLeadingTrivia(leadingTrivia);
    }
}
