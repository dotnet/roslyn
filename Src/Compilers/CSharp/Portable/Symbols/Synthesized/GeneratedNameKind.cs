// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum GeneratedNameKind
    {
        None = 0,

        // Used by EE:
        ThisProxy = '4',
        HoistedLocalField = '5',
        DisplayClassLocalOrField = '8',
        LambdaMethod = 'b',
        LambdaDisplayClassType = 'c',
        StateMachineType = 'd',

        // Used by EnC:
        AwaiterField = 'u',
        HoistedSynthesizedLocalField = 's',

        // Currently not parsed:
        DynamicCallSiteContainer = 'o',
        LambdaCacheField = '9',
    }

    internal static partial class GeneratedNames
    {
        // The type of generated name. See TryParseGeneratedName.
        internal static GeneratedNameKind GetKind(string name)
        {
            GeneratedNameKind kind;
            int openBracketOffset;
            int closeBracketOffset;
            return TryParseGeneratedName(name, out kind, out openBracketOffset, out closeBracketOffset) ? kind : GeneratedNameKind.None;
        }

        // Parse the generated name. Returns true for names of the form
        // [CS$]<[middle]>c[__[suffix]] where [CS$] is included for certain
        // generated names, where [middle] and [__[suffix]] are optional,
        // and where c is a single character in [1-9a-z]
        // (csharp\LanguageAnalysis\LIB\SpecialName.cpp).
        internal static bool TryParseGeneratedName(
            string name,
            out GeneratedNameKind kind,
            out int openBracketOffset,
            out int closeBracketOffset)
        {
            openBracketOffset = -1;
            if (name.StartsWith("CS$<", StringComparison.Ordinal))
            {
                openBracketOffset = 3;
            }
            else if (name.StartsWith("<", StringComparison.Ordinal))
            {
                openBracketOffset = 0;
            }

            if (openBracketOffset >= 0)
            {
                closeBracketOffset = name.IndexOfBalancedParenthesis(openBracketOffset, '>');
                if (closeBracketOffset >= 0 && closeBracketOffset + 1 < name.Length)
                {
                    int c = name[closeBracketOffset + 1];
                    if ((c >= '1' && c <= '9') || (c >= 'a' && c <= 'z')) // Note '0' is not special.
                    {
                        kind = (GeneratedNameKind)c;
                        return true;
                    }
                }
            }

            kind = GeneratedNameKind.None;
            openBracketOffset = -1;
            closeBracketOffset = -1;
            return false;
        }
    }
}
