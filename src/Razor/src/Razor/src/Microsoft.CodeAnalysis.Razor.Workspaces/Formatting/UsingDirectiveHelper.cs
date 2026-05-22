// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Text;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal static class UsingDirectiveHelper
{
    private static readonly Regex s_addUsingVSCodeAction = new Regex("@?using ([^;]+);?$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly record struct RazorUsingDirective(RazorUsingDirectiveSyntax Node, AddImportChunkGenerator Statement);

    /// <summary>
    /// Extracts the namespace from a C# add using statement provided by Visual Studio
    /// </summary>
    /// <param name="csharpAddUsing">Add using statement of the form `using System.X;`</param>
    /// <param name="namespace">Extract namespace `System.X`</param>
    /// <param name="prefix">The prefix to show, before the namespace, if any</param>
    /// <returns></returns>
    public static bool TryExtractNamespace(string csharpAddUsing, out string @namespace, out string prefix)
    {
        // We must remove any leading/trailing new lines from the add using edit
        csharpAddUsing = csharpAddUsing.Trim();
        var regexMatchedTextEdit = s_addUsingVSCodeAction.Match(csharpAddUsing);
        if (!regexMatchedTextEdit.Success ||

            // Two Regex matching groups are expected
            // 1. `using namespace;`
            // 2. `namespace`
            regexMatchedTextEdit.Groups.Count != 2)
        {
            // Text edit in an unexpected format
            @namespace = string.Empty;
            prefix = string.Empty;
            return false;
        }

        @namespace = regexMatchedTextEdit.Groups[1].Value;
        prefix = csharpAddUsing[..regexMatchedTextEdit.Index];
        return true;
    }

    public static TextEdit CreateAddUsingTextEdit(string @namespace, RazorCodeDocument codeDocument)
    {
        /* The heuristic is as follows:
         *
         * - If no @using, @namespace, or @page directives are present, insert the statements at the top of the
         *   file in alphabetical order.
         * - If a @namespace or @page are present, the statements are inserted after the last line-wise in
         *   alphabetical order.
         * - If @using directives are present and alphabetized with System directives at the top, the statements
         *   will be placed in the correct locations according to that ordering.
         * - Otherwise it's kind of undefined; it's only geared to insert based on alphabetization.
         *
         * This is generally sufficient for our current situation (inserting a single @using statement to include a
         * component), however it has holes if we eventually use it for other purposes. If we want to deal with
         * that now I can come up with a more sophisticated heuristic (something along the lines of checking if
         * there's already an ordering, etc.).
         */

        using var usingDirectives = new PooledArrayBuilder<RazorUsingDirective>();
        CollectUsingDirectives(codeDocument, ref usingDirectives.AsRef());
        if (usingDirectives.Count > 0)
        {
            return GetInsertUsingTextEdit(codeDocument, @namespace, in usingDirectives);
        }

        return GetInsertUsingTextEdit(codeDocument, @namespace);
    }

    public static ImmutableArray<string> FindUsingDirectiveStrings(RoslynSyntaxNode csharpSyntaxRoot, SourceText csharpSourceText)
    {
        return csharpSyntaxRoot
            .DescendantNodes(static n => n is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax)
            .OfType<UsingDirectiveSyntax>()
            .Where(static u => u.Name is not null) // If the Name is null then this isn't a using directive, it's probably an alias for a tuple type
            .SelectAsArray(u => GetNamespaceFromDirective(u, csharpSourceText));

        static string GetNamespaceFromDirective(UsingDirectiveSyntax usingDirectiveSyntax, SourceText sourceText)
        {
            var nameSyntax = usingDirectiveSyntax.Name.AssumeNotNull();

            var end = nameSyntax.Span.End;

            // FullSpan to get the end of the trivia before the next
            // token. Testing shows that the trailing whitespace is always given
            // as trivia to the using keyword.
            var start = usingDirectiveSyntax.UsingKeyword.FullSpan.End;

            return sourceText.ToString(TextSpan.FromBounds(start, end));
        }
    }

    /// <summary>
    /// Generates a <see cref="TextEdit"/> to insert a new using directive into the Razor code document, at the right spot among existing using directives.
    /// </summary>
    private static TextEdit GetInsertUsingTextEdit(
        RazorCodeDocument codeDocument,
        string newUsingNamespace,
        ref readonly PooledArrayBuilder<RazorUsingDirective> existingUsingDirectives)
    {
        Debug.Assert(existingUsingDirectives.Count > 0);

        var newText = $"@using {newUsingNamespace}{Environment.NewLine}";

        foreach (var usingDirective in existingUsingDirectives)
        {
            // Skip System directives; if they're at the top we don't want to insert before them
            var usingDirectiveNamespace = usingDirective.Statement.ParsedNamespace;
            if (usingDirectiveNamespace.StartsWith("System", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.CompareOrdinal(newUsingNamespace, usingDirectiveNamespace) < 0)
            {
                var usingDirectiveLineIndex = codeDocument.Source.Text.GetLinePosition(usingDirective.Node.Span.Start).Line;
                return LspFactory.CreateTextEdit(line: usingDirectiveLineIndex, character: 0, newText);
            }
        }

        // If we haven't actually found a place to insert the using directive, do so at the end
        var endIndex = existingUsingDirectives[^1].Node.Span.End;
        var lineIndex = GetLineIndexOrEnd(codeDocument, endIndex - 1) + 1;
        return LspFactory.CreateTextEdit(line: lineIndex, character: 0, newText);
    }

    /// <summary>
    /// Generates a <see cref="TextEdit"/> to insert a new using directive into the Razor code document, at the top of the file.
    /// </summary>
    private static TextEdit GetInsertUsingTextEdit(
        RazorCodeDocument codeDocument,
        string newUsingNamespace)
    {
        var insertPosition = (0, 0);

        // If we don't have usings, insert after the last namespace or page directive, which ever comes later
        var root = codeDocument.GetRequiredSyntaxRoot();
        var lastNamespaceOrPageDirective = root
            .DescendantNodes()
            .LastOrDefault(IsNamespaceOrPageDirective);

        if (lastNamespaceOrPageDirective != null)
        {
            var lineIndex = GetLineIndexOrEnd(codeDocument, lastNamespaceOrPageDirective.Span.End - 1) + 1;
            insertPosition = (lineIndex, 0);
        }

        return LspFactory.CreateTextEdit(insertPosition, newText: $"@using {newUsingNamespace}{Environment.NewLine}");
    }

    private static int GetLineIndexOrEnd(RazorCodeDocument codeDocument, int endIndex)
    {
        if (endIndex < codeDocument.Source.Text.Length)
        {
            return codeDocument.Source.Text.GetLinePosition(endIndex).Line;
        }
        else
        {
            return codeDocument.Source.Text.Lines.Count;
        }
    }

    private static void CollectUsingDirectives(RazorCodeDocument codeDocument, ref PooledArrayBuilder<RazorUsingDirective> directives)
    {
        var root = codeDocument.GetRequiredSyntaxRoot();
        foreach (var node in root.DescendantNodes())
        {
            if (node is RazorUsingDirectiveSyntax directiveNode)
            {
                foreach (var child in directiveNode.DescendantNodes())
                {
                    if (child.GetChunkGenerator() is AddImportChunkGenerator { IsStatic: false } usingStatement)
                    {
                        directives.Add(new RazorUsingDirective(directiveNode, usingStatement));
                    }
                }
            }
        }
    }

    private static bool IsNamespaceOrPageDirective(RazorSyntaxNode node)
    {
        if (node is RazorDirectiveSyntax directiveNode)
        {
            return directiveNode.IsDirective(ComponentPageDirective.Directive) ||
                   directiveNode.IsDirective(NamespaceDirective.Directive) ||
                   directiveNode.IsDirective(PageDirective.Directive);
        }

        return false;
    }

    /// <summary>
    /// Determines whether the using directives in the document need sorting or consolidating.
    /// </summary>
    public static bool NeedsSortOrConsolidate(RazorCodeDocument codeDocument)
    {
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();

        var usingDirectives = syntaxTree.GetUsingDirectives();
        if (usingDirectives.Length <= 1)
        {
            return false;
        }

        var sourceText = codeDocument.Source.Text;

        // Check if namespaces are already in sorted order
        string? previousNamespace = null;
        foreach (var directive in usingDirectives)
        {
            if (RazorSyntaxFacts.TryGetNamespaceFromDirective(directive, out var ns))
            {
                if (previousNamespace is not null && UsingsStringComparer.Instance.Compare(previousNamespace, ns) > 0)
                {
                    return true;
                }

                previousNamespace = ns;
            }
        }

        // Check if usings are in multiple groups (non-consecutive lines)
        for (var i = 1; i < usingDirectives.Length; i++)
        {
            var prevLine = sourceText.GetLinePosition(usingDirectives[i - 1].Span.End).Line;
            var currentLine = sourceText.GetLinePosition(usingDirectives[i].Span.Start).Line;

            if (currentLine > prevLine + 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the text edits to sort and consolidate the using directives in the document
    /// into one group at the location of the first group. Each directive's original source
    /// text is preserved (including semicolons or other trailing content on the directive node).
    /// Any non-directive content that follows a using directive on the same line is left in place.
    /// If <paramref name="directivesToKeep"/> is provided, only those directives appear in the
    /// sorted output; all others are removed. If null, all directives are kept.
    /// </summary>
    public static ImmutableArray<TextEdit> GetSortAndConsolidateEdits(
        RazorCodeDocument codeDocument,
        ImmutableArray<RazorUsingDirectiveSyntax>? directivesToKeep = null)
    {
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var usingDirectives = syntaxTree.GetUsingDirectives();
        var sourceText = codeDocument.Source.Text;

        // Sort the directive nodes by namespace and build consolidated text from original source
        var sorted = (directivesToKeep ?? usingDirectives).Sort(UsingsNodeComparer.Instance);

        using var _ = StringBuilderPool.GetPooledObject(out var builder);
        foreach (var directive in sorted)
        {
            // Append the full directive text, so (optional) semi-colons are preserved
            builder.AppendLine(sourceText.ToString(directive.Span));
        }

        using var editBuilder = new PooledArrayBuilder<TextEdit>();

        // Remove every using directive. If the directive is the only content on its line,
        // remove the entire line (including the newline). Otherwise, only remove the
        // directive span so any trailing content on the line is preserved.
        foreach (var directive in usingDirectives)
        {
            editBuilder.Add(GetRemoveDirectiveEdit(sourceText, directive.Span));
        }

        // Insert all sorted usings at the position of the first directive's line
        var firstDirectiveLine = sourceText.GetLinePosition(usingDirectives[0].Span.Start).Line;
        var insertPosition = sourceText.Lines[firstDirectiveLine].Start;
        var insertRange = sourceText.GetRange(insertPosition, insertPosition);
        editBuilder.Add(LspFactory.CreateTextEdit(insertRange, builder.ToString()));

        return editBuilder.ToImmutableAndClear();
    }

    /// <summary>
    /// Creates a text edit to remove a directive from its line. If the directive is the only
    /// non-whitespace content on the line, the entire line including the line break is removed.
    /// Otherwise, only the directive span is removed.
    /// </summary>
    public static TextEdit GetRemoveDirectiveEdit(SourceText sourceText, TextSpan directiveSpan)
    {
        var directiveLineNumber = sourceText.GetLinePosition(directiveSpan.Start).Line;
        var lineSpan = sourceText.Lines[directiveLineNumber].SpanIncludingLineBreak;

        // If the non-whitespace content of the line and the directive match, then the directive is the only thing on the line
        // so we remove the whole line. Otherwise, just remove the directive.
        var removeSpan = sourceText.NonWhitespaceContentEquals(sourceText, lineSpan.Start, lineSpan.End, directiveSpan.Start, directiveSpan.End)
            ? lineSpan
            : directiveSpan;

        return LspFactory.CreateTextEdit(sourceText.GetRange(removeSpan), string.Empty);
    }
}
