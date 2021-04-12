// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class SyntaxKindExtensions
    {
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
