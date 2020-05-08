// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private SyntaxTokenList GenerateModifiers(ISymbol symbol)
        {
            using var _ = GetArrayBuilder<SyntaxToken>(out var result);

            var accessibility = GetAdjustedAccessibility(symbol);
            var symbolModifiers = GetAdjustedSymbolModifiers(symbol);

            switch (accessibility)
            {
                case Accessibility.Private:
                    result.Add(Token(SyntaxKind.PrivateKeyword));
                    break;
                case Accessibility.ProtectedAndInternal:
                    result.Add(Token(SyntaxKind.PrivateKeyword));
                    result.Add(Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.Protected:
                    result.Add(Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.Internal:
                    result.Add(Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.ProtectedOrInternal:
                    result.Add(Token(SyntaxKind.ProtectedKeyword));
                    result.Add(Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.Public:
                    result.Add(Token(SyntaxKind.PublicKeyword));
                    break;
            }

            if (symbolModifiers.HasFlag(SymbolModifiers.Static))
                result.Add(Token(SyntaxKind.StaticKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Abstract))
                result.Add(Token(SyntaxKind.AbstractKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.New))
                result.Add(Token(SyntaxKind.NewKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Unsafe))
                result.Add(Token(SyntaxKind.UnsafeKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.ReadOnly))
                result.Add(Token(SyntaxKind.ReadOnlyKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Virtual))
                result.Add(Token(SyntaxKind.VirtualKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Override))
                result.Add(Token(SyntaxKind.OverrideKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Sealed))
                result.Add(Token(SyntaxKind.SealedKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Const))
                result.Add(Token(SyntaxKind.ConstKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Async))
                result.Add(Token(SyntaxKind.AsyncKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Volatile))
                result.Add(Token(SyntaxKind.VolatileKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Extern))
                result.Add(Token(SyntaxKind.ExternKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Params))
                result.Add(Token(SyntaxKind.ParamsKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Ref))
                result.Add(Token(SyntaxKind.RefKeyword));

            if (symbolModifiers.HasFlag(SymbolModifiers.Partial))
                result.Add(Token(SyntaxKind.PartialKeyword));

            return TokenList(result);
        }

        private Accessibility GetAdjustedAccessibility(ISymbol symbol)
        {
            var accessibility = symbol.DeclaredAccessibility;

            // C# specific rules about what accessibility we should actually emit.

            return accessibility;
        }

        private SymbolModifiers GetAdjustedSymbolModifiers(ISymbol symbol)
        {
            var modifiers = symbol.GetModifiers();

            // C# specific rules about what modifiers we should actually emit.

            if (symbol is INamedTypeSymbol namedType)
            {
                if (namedType.TypeKind == TypeKind.Interface)
                {
                    // Never emit 'abstract' on an interface itself.
                    modifiers &= ~SymbolModifiers.Abstract;
                }

                if (_currentNamedType?.TypeKind == TypeKind.Interface)
                {
                    // It's redundant to declare interfaces members as abstract or virtual (even with DIM).
                    modifiers &= ~SymbolModifiers.Abstract;
                    modifiers &= ~SymbolModifiers.Virtual;
                }
            }

            return modifiers;
        }
    }
}
