// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.DotNet.FileBasedPrograms;

/// <summary>
/// Low-level primitives for parsing and formatting the values of file-based program <c>#:</c> directives.
/// These are source-shared between the CLI directive parser (<c>FileLevelDirectiveHelpers</c>)
/// and the analyzer that flags the deprecated unquoted form (<c>FileBasedProgramDirectiveQuoting</c>).
/// </summary>
internal static class FileBasedProgramDirectiveValueHelpers
{
    /// <summary>
    /// Returns whether <paramref name="name"/> contains a character that is not allowed in a directive
    /// or metadata name (whitespace or one of the separator characters <c>@</c>, <c>=</c>, <c>/</c>).
    /// </summary>
    public static bool ContainsDisallowedNameCharacter(string name)
    {
        foreach (var c in name)
        {
            if (char.IsWhiteSpace(c) || c is '@' or '=' or '/')
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that <paramref name="name"/> is a valid XML NCName,
    /// the constraint MSBuild applies to property and item-metadata names
    /// (an NCName additionally disallows the ':' that a plain XML name permits).
    /// </summary>
    public static bool IsValidMSBuildName(string name, [NotNullWhen(returnValue: false)] out string? errorMessage)
    {
        try
        {
            XmlConvert.VerifyNCName(name);
            errorMessage = null;
            return true;
        }
        catch (XmlException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Returns whether every token from <paramref name="start"/> onwards
    /// is a valid <c>Name=Value</c> item-metadata pair
    /// (a valid MSBuild name, then <c>'='</c>, then any value).
    /// </summary>
    public static bool AllValidMetadata(ImmutableArray<string> tokens, int start)
    {
        for (var i = start; i < tokens.Length; i++)
        {
            var token = tokens[i];
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0)
            {
                return false;
            }

            if (!IsValidMSBuildName(token.Substring(0, separatorIndex), out _))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Wraps <paramref name="value"/> in a C# string literal when it contains a character (whitespace or a double quote)
    /// that cannot appear in a bare directive token. Otherwise returns it unchanged.
    /// </summary>
    public static string QuoteIfNeeded(string value)
    {
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c) || c == '"')
            {
                return SymbolDisplay.FormatLiteral(value, quote: true);
            }
        }

        return value;
    }
}
