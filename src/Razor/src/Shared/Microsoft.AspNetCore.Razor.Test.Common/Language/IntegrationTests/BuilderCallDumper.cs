// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

/// <summary>
/// Projects generated component C# down to the render-tree-builder calls it performs,
/// ordered by sequence number. For every <c>__builder.&lt;op&gt;(sequence, ...)</c> call
/// (OpenElement, AddAttribute, AddContent, OpenComponent, AddComponentParameter, ...)
/// it emits one normalized line; calls that carry no sequence number (CloseElement,
/// SetKey, AddNamedEvent, ...) are positional bookkeeping and are omitted.
/// </summary>
/// <remarks>
/// The sequence number is the compile-time, source-position identity Razor assigns each
/// render operation. Ordering the calls by it yields a canonical projection that is
/// invariant under cosmetic codegen reorganization -- which partial half a member lands
/// in, line-pragma layout, method extraction, whitespace -- yet still reflects any change
/// to what the component actually builds: a different element name, a dropped attribute,
/// a renumbered fragment.
/// </remarks>
internal static class BuilderCallDumper
{
    // Nested render fragments receive numbered builders: __builder, __builder2, __builder3.
    private static readonly Regex s_builderName = new("^__builder[0-9]*$", RegexOptions.Compiled);

    private static readonly Regex s_whitespace = new("[ \t\r\n]+", RegexOptions.Compiled);

    public static string Dump(IEnumerable<string?> generatedSources)
    {
        var calls = new List<(int Sequence, string Text)>();

        foreach (var source in generatedSources)
        {
            if (string.IsNullOrEmpty(source))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(SourceText.From(source)).GetRoot();
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                    memberAccess.Expression is not IdentifierNameSyntax receiver ||
                    !s_builderName.IsMatch(receiver.Identifier.ValueText))
                {
                    continue;
                }

                var arguments = invocation.ArgumentList.Arguments;
                if (arguments.Count == 0 ||
                    arguments[0].Expression is not LiteralExpressionSyntax literal ||
                    literal.Token.Value is not int sequence)
                {
                    continue;
                }

                var op = memberAccess.Name.ToString();
                var remaining = arguments.Count > 1
                    ? string.Join(", ", arguments.Skip(1).Select(a => NormalizeArgument(a.Expression)))
                    : string.Empty;

                calls.Add((sequence, $"[{sequence:D4}] {op}({remaining})"));
            }
        }

        var ordered = calls
            .OrderBy(c => c.Sequence)
            .ThenBy(c => c.Text, System.StringComparer.Ordinal)
            .Select(c => c.Text);

        return string.Join("\r\n", ordered);
    }

    private static string NormalizeArgument(ExpressionSyntax expression)
    {
        // Inserted C# arguments are frequently split across lines with interleaved
        // #line / #nullable / #pragma directives. Drop directive lines and collapse the
        // remainder so the projection depends only on the expression, not its
        // line-mapping decoration.
        var builder = new StringBuilder();
        foreach (var rawLine in expression.ToString().Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || IsDirectiveLine(line))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(line);
        }

        return s_whitespace.Replace(builder.ToString(), " ").Trim();
    }

    private static bool IsDirectiveLine(string trimmedLine) =>
        trimmedLine.StartsWith("#line", System.StringComparison.Ordinal) ||
        trimmedLine.StartsWith("#nullable", System.StringComparison.Ordinal) ||
        trimmedLine.StartsWith("#pragma", System.StringComparison.Ordinal) ||
        trimmedLine.StartsWith("#region", System.StringComparison.Ordinal) ||
        trimmedLine.StartsWith("#endregion", System.StringComparison.Ordinal);
}
