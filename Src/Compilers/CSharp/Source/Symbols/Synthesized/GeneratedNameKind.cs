// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum GeneratedNameKind
    {
        None = 0,
        ThisProxy = 4,
        DisplayClassLocal = 8,
        DisplayClassType = 12,
    }

    internal static partial class GeneratedNames
    {
        // The type of generated name. Returns non-zero for names
        // starting with "<" or "CS$<" followed by ">c" where 'c' is in [1-9a-z].
        internal static GeneratedNameKind GetKind(string name)
        {
            if (name.StartsWith("<", StringComparison.Ordinal) || name.StartsWith("CS$<", StringComparison.Ordinal))
            {
                int depth = 0;
                for (int i = 0; i < name.Length - 1; i++)
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
                                int c = name[i + 1];
                                if ((c >= '1') && (c <= '9')) // Note '0' is not special.
                                {
                                    return (GeneratedNameKind)(c - '0');
                                }
                                else if ((c >= 'a') && (c <= 'z'))
                                {
                                    return (GeneratedNameKind)(c - 'a' + 10);
                                }
                                return GeneratedNameKind.None;
                            }
                            break;
                    }
                }
            }
            return GeneratedNameKind.None;
        }
    }
}
