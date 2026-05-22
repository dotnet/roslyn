// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public static class HtmlConventions
{
    private static readonly char[] InvalidNonWhitespaceHtmlCharacters =
        ['@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*'];

    internal static bool IsInvalidNonWhitespaceHtmlCharacters(char testChar)
    {
        foreach (var c in InvalidNonWhitespaceHtmlCharacters)
        {
            if (c == testChar)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts from pascal/camel case to lower kebab-case.
    /// </summary>
    /// <example>
    /// SomeThing => some-thing
    /// capsONInside => caps-on-inside
    /// CAPSOnOUTSIDE => caps-on-outside
    /// ALLCAPS => allcaps
    /// One1Two2Three3 => one1-two2-three3
    /// ONE1TWO2THREE3 => one1two2three3
    /// First_Second_ThirdHi => first_second_third-hi
    /// </example>
    public static string ToHtmlCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return TryGetKebabCaseString(input, out var result)
            ? result
            : input;
    }

    private static bool TryGetKebabCaseString(ReadOnlySpan<char> input, [NotNullWhen(true)] out string? result)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        var allLower = true;
        var i = 0;
        foreach (var c in input)
        {
            if (char.IsUpper(c))
            {
                allLower = false;

                if (ShouldInsertHyphenBeforeUppercase(input, i))
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }

            i++;
        }

        if (allLower)
        {
            // If the input is all lowercase, we don't need to realize the builder,
            // it will just be cleared when the pooled object is disposed.
            result = null;
            return false;
        }

        result = builder.ToString();
        return true;
    }

    private static bool ShouldInsertHyphenBeforeUppercase(ReadOnlySpan<char> input, int i)
    {
        Debug.Assert(char.IsUpper(input[i]));

        if (i == 0)
        {
            // First character is uppercase, no hyphen needed (e.g. This → this)
            return false;
        }

        var prev = input[i - 1];
        if (char.IsLower(prev))
        {
            // Lowercase followed by uppercase (e.g. someThing → some-thing)
            return true;
        }

        if ((char.IsUpper(prev) || char.IsDigit(prev)) &&
            (i + 1 < input.Length) && char.IsLower(input[i + 1]))
        {
            // Uppercase or digit followed by uppercase, followed by lowercase (e.g. CAPSOn → caps-on or ONE1Two → ONE1-Two)
            return true;
        }

        return false;
    }
}
