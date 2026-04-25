// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal static class DocCommentHelpers
{
    public const string TagContentGroupName = "content";

    private static readonly Regex s_codeRegex = new Regex($"""<(?:c|code)>(?<{TagContentGroupName}>.*?)<\/(?:c|code)>""", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex s_crefRegex = new Regex($"""<(?:see|seealso)[\s]+cref="(?<{TagContentGroupName}>[^">]+)"[^>]*>""", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private static readonly char[] s_newLineChars = ['\n', '\r'];

    public static string ReduceCrefValue(string value)
    {
        // cref values come in the following formats:
        // Type = "T:Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName"
        // Property = "P:T:Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.AspAction"
        // Member = "M:T:Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeMethod(System.Collections.Generic.List{System.String})"

        if (value.Length < 2)
        {
            return string.Empty;
        }

        var type = value[0];
        value = value[2..];

        switch (type)
        {
            case 'T':
                var reducedCrefType = ReduceTypeName(value);
                return reducedCrefType;
            case 'P':
            case 'M':
                // TypeName.MemberName
                var reducedCrefProperty = ReduceMemberName(value);
                return reducedCrefProperty;
        }

        return value;
    }

    public static string ReduceTypeName(string content)
        => ReduceFullName(content, reduceWhenDotCount: 1);

    public static string ReduceMemberName(string content)
        => ReduceFullName(content, reduceWhenDotCount: 2);

    private static string ReduceFullName(string content, int reduceWhenDotCount)
    {
        // Starts searching backwards and then substrings everything when it finds enough dots. i.e.
        // ReduceFullName("Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName", 1) == "SomeTypeName"
        //
        // ReduceFullName("Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.AspAction", 2) == "SomeTypeName.AspAction"
        //
        // This is also smart enough to ignore nested dots in type generics[<>], methods[()], cref generics[{}].

        if (reduceWhenDotCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reduceWhenDotCount));
        }

        var dotsSeen = 0;
        var scope = 0;
        for (var i = content.Length - 1; i >= 0; i--)
        {
            do
            {
                if (content[i] == '}')
                {
                    scope++;
                }
                else if (content[i] == '{')
                {
                    scope--;
                }

                if (scope > 0)
                {
                    i--;
                }
            }
            while (scope != 0 && i >= 0);

            if (i < 0)
            {
                // Could not balance scope
                return content;
            }

            do
            {
                if (content[i] == ')')
                {
                    scope++;
                }
                else if (content[i] == '(')
                {
                    scope--;
                }

                if (scope > 0)
                {
                    i--;
                }
            }
            while (scope != 0 && i >= 0);

            if (i < 0)
            {
                // Could not balance scope
                return content;
            }

            do
            {
                if (content[i] == '>')
                {
                    scope++;
                }
                else if (content[i] == '<')
                {
                    scope--;
                }

                if (scope > 0)
                {
                    i--;
                }
            }
            while (scope != 0 && i >= 0);

            if (i < 0)
            {
                // Could not balance scope
                return content;
            }

            if (content[i] == '.')
            {
                dotsSeen++;
            }

            if (dotsSeen == reduceWhenDotCount)
            {
                var piece = content[(i + 1)..];
                return piece;
            }
        }

        // Could not reduce name
        return content;
    }

    public static bool TryExtractSummary(string? documentation, [NotNullWhen(true)] out string? summary)
    {
        const string SummaryStartTag = "<summary>";
        const string SummaryEndTag = "</summary>";

        if (documentation is null || documentation == string.Empty)
        {
            summary = null;
            return false;
        }

        documentation = documentation.Trim(s_newLineChars);

        var summaryTagStart = documentation.IndexOf(SummaryStartTag, StringComparison.OrdinalIgnoreCase);
        var summaryTagEndStart = documentation.IndexOf(SummaryEndTag, StringComparison.OrdinalIgnoreCase);
        if (summaryTagStart == -1 || summaryTagEndStart == -1)
        {
            // A really wrong but cheap way to check if this is XML
            if (!documentation.StartsWith("<", StringComparison.Ordinal) && !documentation.EndsWith(">", StringComparison.Ordinal))
            {
                // This doesn't look like a doc comment, we'll return it as-is.
                summary = documentation;
                return true;
            }

            summary = null;
            return false;
        }

        var summaryContentStart = summaryTagStart + SummaryStartTag.Length;
        var summaryContentLength = summaryTagEndStart - summaryContentStart;

        summary = documentation.Substring(summaryContentStart, summaryContentLength);
        return true;
    }

    public static List<Match> ExtractCodeMatches(string summaryContent)
    {
        var successfulMatches = ExtractSuccessfulMatches(s_codeRegex, summaryContent);
        return successfulMatches;
    }

    public static List<Match> ExtractCrefMatches(string summaryContent)
    {
        var successfulMatches = ExtractSuccessfulMatches(s_crefRegex, summaryContent);
        return successfulMatches;
    }

    private static List<Match> ExtractSuccessfulMatches(Regex regex, string summaryContent)
    {
        var matches = regex.Matches(summaryContent);
        var successfulMatches = new List<Match>();
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                successfulMatches.Add(match);
            }
        }

        return successfulMatches;
    }
}
