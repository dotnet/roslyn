// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Symbols;

internal static partial class CommonGeneratedNames
{
    /// <summary>
    /// Parses one or two debug ids that the specified <paramref name="metadataNameSuffix"/> ends with.
    /// 
    /// Returns true if <paramref name="metadataNameSuffix"/> ends with one or two well-formed debug ids.
    /// If two ids are present in the name then the first is <paramref name="methodId"/> and the second is <paramref name="entityId"/>.
    /// Otherwise, if <paramref name="isMethodIdOptional"/> is true then the single parsed id is returned in <paramref name="entityId"/>,
    /// otherwise in <paramref name="methodId"/>.
    /// </summary>
    /// <param name="metadataNameSuffix">Suffix of the metadata name following the suffix separator.</param>
    public static bool TryParseDebugIds(ReadOnlySpan<char> metadataNameSuffix, char idSeparator, bool isMethodIdOptional, out DebugId methodId, out DebugId entityId)
    {
        methodId = entityId = default;

        int generation = -1;
        long power = 1;
        long value = 0;

        DebugId? seenId = null;

        for (int i = metadataNameSuffix.Length - 1; i >= -1; i--)
        {
            var c = (i >= 0) ? metadataNameSuffix[i] : '\0';
            switch (c)
            {
                case >= '0' and <= '9':
                    value += (c - '0') * power;
                    if (value > int.MaxValue)
                    {
                        return false;
                    }

                    power *= 10;
                    break;

                case GenerationSeparator:
                    if (generation >= 0 || power == 1)
                    {
                        // bad format:
                        return false;
                    }

                    generation = (int)value;
                    value = 0;
                    power = 1;
                    break;

                default:
                    if (power == 1)
                    {
                        // no digits parsed
                        return false;
                    }

                    var id = new DebugId((int)value, (generation >= 0) ? generation : 0);
                    generation = -1;
                    value = 0;
                    power = 1;

                    if (seenId == null)
                    {
                        if (c == idSeparator)
                        {
                            // continue parsing, we have another id
                            seenId = id;
                            break;
                        }

                        // only a single id is present
                        if (isMethodIdOptional)
                        {
                            entityId = id;
                        }
                        else
                        {
                            methodId = id;
                        }

                        return true;
                    }

                    if (c == idSeparator)
                    {
                        // bad format - only two ids allowed
                        return false;
                    }

                    // two ids are present:
                    methodId = id;
                    entityId = seenId.Value;
                    return true;
            }
        }

        throw ExceptionUtilities.Unreachable();
    }
}
