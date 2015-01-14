// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class RefKindExtensions
    {
        public static SyntaxToken GetToken(this RefKind refKind)
        {
            if (refKind == RefKind.Out)
            {
                return SyntaxFactory.Token(SyntaxKind.OutKeyword);
            }
            if (refKind == RefKind.Ref)
            {
                return SyntaxFactory.Token(SyntaxKind.RefKeyword);
            }
            return default(SyntaxToken);
        }

        public static RefKind GetRefKind(this SyntaxKind syntaxKind)
        {
            switch (syntaxKind)
            {
                case SyntaxKind.RefKeyword:
                    return RefKind.Ref;
                case SyntaxKind.OutKeyword:
                    return RefKind.Out;
                case SyntaxKind.None:
                    return RefKind.None;
                default:
                    throw ExceptionUtilities.UnexpectedValue(syntaxKind);
            }
        }

        internal static SpecialType GetSpecialType(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.VoidKeyword:
                    return SpecialType.System_Void;
                case SyntaxKind.BoolKeyword:
                    return SpecialType.System_Boolean;
                case SyntaxKind.ByteKeyword:
                    return SpecialType.System_Byte;
                case SyntaxKind.SByteKeyword:
                    return SpecialType.System_SByte;
                case SyntaxKind.ShortKeyword:
                    return SpecialType.System_Int16;
                case SyntaxKind.UShortKeyword:
                    return SpecialType.System_UInt16;
                case SyntaxKind.IntKeyword:
                    return SpecialType.System_Int32;
                case SyntaxKind.UIntKeyword:
                    return SpecialType.System_UInt32;
                case SyntaxKind.LongKeyword:
                    return SpecialType.System_Int64;
                case SyntaxKind.ULongKeyword:
                    return SpecialType.System_UInt64;
                case SyntaxKind.DoubleKeyword:
                    return SpecialType.System_Double;
                case SyntaxKind.FloatKeyword:
                    return SpecialType.System_Single;
                case SyntaxKind.DecimalKeyword:
                    return SpecialType.System_Decimal;
                case SyntaxKind.StringKeyword:
                    return SpecialType.System_String;
                case SyntaxKind.CharKeyword:
                    return SpecialType.System_Char;
                case SyntaxKind.ObjectKeyword:
                    return SpecialType.System_Object;
                default:
                    // Note that "dynamic" is a contextual keyword, so it should never show up here.
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
