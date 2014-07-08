// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum GeneratedNameKind
    {
        None = 0,
        ThisProxy = '4',
        HoistedLocalField = '5',
        DisplayClassLocalOrField = '8',
        LambdaDisplayClassType = 'c',
        StateMachineType = 'd',
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
        // [CS$]<[middle]>c__[suffix] where [CS$] is included for certain
        // generated names, where [middle] and [suffix] are optional,
        // and where c is a single character in [1-9a-z].
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
                closeBracketOffset = -1;
                int depth = 1;
                // Find matching '>'. Since a valid generated name
                // ends with ">c__[suffix]" we only need to search
                // up to 3 characters from the end.
                for (int i = openBracketOffset + 1; i < name.Length - 3; i++)
                {
                    switch (name[i])
                    {
                        case '<':
                            depth++;
                            break;
                        case '>':
                            depth--;
                            if (depth == 0)
                            {
                                closeBracketOffset = i;
                                goto found;
                            }
                            break;
                    }
                }

found:
                if (closeBracketOffset > openBracketOffset &&
                    name[closeBracketOffset + 2] == '_' &&
                    name[closeBracketOffset + 3] == '_') // Not out of range since loop ended early.
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
