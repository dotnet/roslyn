// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static SyntaxNode GenerateNamedType(INamedTypeSymbol symbol)
        {
            if (symbol.SpecialType != SpecialType.None)
                return GenerateSpecialType(symbol);

            throw new NotImplementedException();
        }

        private static SyntaxNode GenerateSpecialType(INamedTypeSymbol symbol)
        {
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Object: return PredefinedType(Token(SyntaxKind.ObjectKeyword));
                case SpecialType.System_Boolean: return PredefinedType(Token(SyntaxKind.BoolKeyword));
                case SpecialType.System_Char: return PredefinedType(Token(SyntaxKind.CharKeyword));
                case SpecialType.System_SByte: return PredefinedType(Token(SyntaxKind.SByteKeyword));
                case SpecialType.System_Byte: return PredefinedType(Token(SyntaxKind.ByteKeyword));
                case SpecialType.System_Int16: return PredefinedType(Token(SyntaxKind.ShortKeyword));
                case SpecialType.System_UInt16: return PredefinedType(Token(SyntaxKind.UShortKeyword));
                case SpecialType.System_Int32: return PredefinedType(Token(SyntaxKind.IntKeyword));
                case SpecialType.System_UInt32: return PredefinedType(Token(SyntaxKind.UIntKeyword));
                case SpecialType.System_Int64: return PredefinedType(Token(SyntaxKind.LongKeyword));
                case SpecialType.System_UInt64: return PredefinedType(Token(SyntaxKind.ULongKeyword));
                case SpecialType.System_Decimal: return PredefinedType(Token(SyntaxKind.DecimalKeyword));
                case SpecialType.System_Single: return PredefinedType(Token(SyntaxKind.FloatKeyword));
                case SpecialType.System_Double: return PredefinedType(Token(SyntaxKind.DoubleKeyword));
                case SpecialType.System_String: return PredefinedType(Token(SyntaxKind.StringKeyword));
            }

            throw new NotImplementedException();
        }
    }
}
