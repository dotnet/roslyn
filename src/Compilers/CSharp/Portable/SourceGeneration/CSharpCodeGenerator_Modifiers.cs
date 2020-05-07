// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static SyntaxTokenList GenerateModifiers(
            Accessibility declaredAccessibility,
            SymbolModifiers symbolModifiers)
        {
            using var _ = GetArrayBuilder<SyntaxToken>(out var result);

            switch (declaredAccessibility)
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

            if ((symbolModifiers & SymbolModifiers.Static) != 0)
                result.Add(Token(SyntaxKind.StaticKeyword));

            if ((symbolModifiers & SymbolModifiers.Abstract) != 0)
                result.Add(Token(SyntaxKind.AbstractKeyword));

            if ((symbolModifiers & SymbolModifiers.New) != 0)
                result.Add(Token(SyntaxKind.NewKeyword));

            if ((symbolModifiers & SymbolModifiers.Unsafe) != 0)
                result.Add(Token(SyntaxKind.UnsafeKeyword));

            if ((symbolModifiers & SymbolModifiers.ReadOnly) != 0)
                result.Add(Token(SyntaxKind.ReadOnlyKeyword));

            if ((symbolModifiers & SymbolModifiers.Virtual) != 0)
                result.Add(Token(SyntaxKind.VirtualKeyword));

            if ((symbolModifiers & SymbolModifiers.Override) != 0)
                result.Add(Token(SyntaxKind.OverrideKeyword));

            if ((symbolModifiers & SymbolModifiers.Sealed) != 0)
                result.Add(Token(SyntaxKind.SealedKeyword));

            if ((symbolModifiers & SymbolModifiers.Const) != 0)
                result.Add(Token(SyntaxKind.ConstKeyword));

            if ((symbolModifiers & SymbolModifiers.Async) != 0)
                result.Add(Token(SyntaxKind.AsyncKeyword));

            if ((symbolModifiers & SymbolModifiers.Volatile) != 0)
                result.Add(Token(SyntaxKind.VolatileKeyword));

            if ((symbolModifiers & SymbolModifiers.Extern) != 0)
                result.Add(Token(SyntaxKind.ExternKeyword));

            if ((symbolModifiers & SymbolModifiers.Ref) != 0)
                result.Add(Token(SyntaxKind.RefKeyword));

            if ((symbolModifiers & SymbolModifiers.Partial) != 0)
                result.Add(Token(SyntaxKind.PartialKeyword));

            return TokenList(result);
        }
    }
}
