// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class CSharpIdentifier
{
    public static string GetClassNameFromPath(string path)
    {
        var span = path.AsSpanOrDefault();

        if (span.Length == 0)
        {
            return path;
        }

        const string cshtmlExtension = ".cshtml";

        if (span.EndsWith(cshtmlExtension.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            span = span[..^cshtmlExtension.Length];
        }

        return SanitizeIdentifier(span);
    }

    public static string SanitizeIdentifier(ReadOnlySpan<char> inputName)
    {
        if (inputName.Length == 0)
        {
            return string.Empty;
        }

        using var _ = StringBuilderPool.GetPooledObject(out var builder);
        AppendSanitized(builder, inputName);
        return builder.ToString();
    }

    public static void AppendSanitized(StringBuilder builder, ReadOnlySpan<char> inputName)
    {
        if (inputName.Length == 0)
        {
            return;
        }

        var firstChar = inputName[0];
        if (!SyntaxFacts.IsIdentifierStartCharacter(firstChar) && SyntaxFacts.IsIdentifierPartCharacter(firstChar))
        {
            builder.SetCapacityIfLarger(builder.Length + inputName.Length + 1);
            builder.Append('_');
        }
        else
        {
            builder.SetCapacityIfLarger(builder.Length + inputName.Length);
        }

        for (int i = 0; i < inputName.Length; i++)
        {
            var ch = inputName[i];
            if (SyntaxFacts.IsIdentifierPartCharacter(ch))
            {
                builder.Append(ch);
            }
            else
            {
                // Not a valid identifier part, replace with underscore
                builder.Append('_');

                // If this is a high surrogate, skip the low surrogate as well
                // since a surrogate pair represents a single Unicode character
                // and we need to match the identifier mangling in the template
                // engine / VS
                if (char.IsHighSurrogate(ch) && i + 1 < inputName.Length && char.IsLowSurrogate(inputName[i + 1]))
                {
                    i++; // Skip the low surrogate
                }
            }
        }
    }
}
