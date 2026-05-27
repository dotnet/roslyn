// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal partial class RazorEditService
{
    private static void AddMemberChanges(ref PooledArrayBuilder<RazorTextChange> edits, RazorCodeDocument codeDocument, ImmutableArray<CSharpMember> addedMembers, RazorFormattingOptions options)
    {
        if (addedMembers.Length == 0)
        {
            return;
        }

        var tree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var firstDirective = tree.EnumerateDirectives<RazorDirectiveSyntax>(static dir => dir.IsCodeDirective() || dir.IsFunctionsDirective()).FirstOrDefault();

        var csharpCodeBlock = firstDirective?.DirectiveBody.CSharpCode;
        if (csharpCodeBlock is null ||
            !csharpCodeBlock.Children.TryGetOpenBraceNode(out var openBrace) ||
            !csharpCodeBlock.Children.TryGetCloseBraceNode(out var closeBrace))
        {
            AddMembersInNewCodeBlock(ref edits, codeDocument, addedMembers, options);
            return;
        }

        var source = codeDocument.Source;
        var sourceText = source.Text;
        var openBraceLine = openBrace.GetSourceLocation(source).LineIndex;
        var closeBraceLocation = closeBrace.GetSourceLocation(source);
        var closeBraceLine = closeBraceLocation.LineIndex;

        var insertAbsoluteIndex = closeBraceLocation.AbsoluteIndex;
        var insertLineIndex = closeBraceLine;

        if (openBraceLine != closeBraceLine && closeBraceLocation.AbsoluteIndex > 0)
        {
            var previousLineAbsoluteIndex = closeBraceLocation.AbsoluteIndex - closeBraceLocation.CharacterIndex - 1;
            var previousLinePosition = sourceText.GetLinePosition(previousLineAbsoluteIndex);
            var previousLine = sourceText.Lines[previousLinePosition.Line];

            if (IsLineEmpty(previousLine))
            {
                insertAbsoluteIndex = previousLine.End;
                insertLineIndex = previousLine.LineNumber;
            }
        }

        using var _ = StringBuilderPool.GetPooledObject(out var builder);
        AddMembersInExistingCodeBlock(builder, sourceText, addedMembers, options, openBraceLine, closeBraceLine, insertLineIndex);

        edits.Add(new RazorTextChange()
        {
            Span = new RazorTextSpan
            {
                Start = insertAbsoluteIndex,
                Length = 0
            },
            NewText = builder.ToString()
        });
    }

    private static void AddMembersInNewCodeBlock(ref PooledArrayBuilder<RazorTextChange> edits, RazorCodeDocument codeDocument, ImmutableArray<CSharpMember> members, RazorFormattingOptions options)
    {
        var sourceText = codeDocument.Source.Text;
        var lastLine = sourceText.Lines[^1];

        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        if (!IsLineEmpty(lastLine))
        {
            builder.AppendLine();
        }

        builder.Append('@');
        builder.Append(codeDocument.FileKind == RazorFileKind.Legacy
            ? FunctionsDirective.Directive.Directive
            : ComponentCodeDirective.Directive.Directive);
        if (options.CodeBlockBraceOnNextLine)
        {
            builder.AppendLine();
        }
        else
        {
            builder.Append(' ');
        }

        builder.Append('{');
        builder.AppendLine();
        AppendMembersText(builder, members, options);
        builder.AppendLine();
        builder.Append('}');

        edits.Add(new RazorTextChange()
        {
            Span = new RazorTextSpan
            {
                Start = lastLine.End,
                Length = 0
            },
            NewText = builder.ToString()
        });
    }

    private static void AddMembersInExistingCodeBlock(StringBuilder builder, SourceText sourceText, ImmutableArray<CSharpMember> addedMembers, RazorFormattingOptions options, int openBraceLineIndex, int closeBraceLineIndex, int insertLineIndex)
    {
        var lineAboveInsertionIsNotEmpty = insertLineIndex > 0 &&
            insertLineIndex - 1 != openBraceLineIndex &&
            !IsLineEmpty(sourceText.Lines[insertLineIndex - 1]);
        if (openBraceLineIndex == closeBraceLineIndex || lineAboveInsertionIsNotEmpty)
        {
            builder.AppendLine();
        }

        AppendMembersText(builder, addedMembers, options);

        if (openBraceLineIndex == closeBraceLineIndex || insertLineIndex == closeBraceLineIndex)
        {
            builder.AppendLine();
        }
    }

    private static void AppendMembersText(StringBuilder builder, ImmutableArray<CSharpMember> members, RazorFormattingOptions options)
    {
        var first = true;
        foreach (var member in members)
        {
            if (!first)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            first = false;

            AppendIndentedMember(builder, member, options);
        }
    }

    private static void AppendIndentedMember(StringBuilder builder, CSharpMember member, RazorFormattingOptions options)
    {
        // Roslyn will have indented the member by an appropriate amount for the generated file, but we need it to be placed nicely in the Razor
        // file, so we add each line one at a time, adjusting the indentation as we go.
        int? initialIndentation = null;
        var sourceText = member.Text;

        var endLine = member.GetEndLineNumber();
        for (var i = member.GetStartLineNumber(); i <= endLine; i++)
        {
            var line = sourceText.Lines[i];
            var currentIndentation = line.GetIndentationSize(options.TabSize);

            if (initialIndentation is null)
            {
                // The indentation of the first line is used as the baseline
                initialIndentation = currentIndentation;
            }
            else
            {
                builder.AppendLine();
            }

            if (line.GetFirstNonWhitespaceOffset() is int offset)
            {
                // New indentation is the Roslyn indentation, minus the baseline indentation, plus our desired indentation, which is just one
                // level, to nest inside the @code block.
                var newIndentation = options.TabSize + currentIndentation - initialIndentation.GetValueOrDefault();
                builder.Append(FormattingUtilities.GetIndentationString(Math.Max(0, newIndentation), options.InsertSpaces, options.TabSize));
                builder.Append(sourceText.ToString(TextSpan.FromBounds(line.Start + offset, line.End)));
            }
        }
    }

    private static ImmutableArray<CSharpMember> FindMembers(RoslynSyntaxNode syntaxRoot, SourceText sourceText)
    {
        if (!syntaxRoot.TryGetClassDeclaration(out var classDecl))
        {
            return [];
        }

        using var members = new PooledArrayBuilder<CSharpMember>();
        foreach (var member in classDecl.Members)
        {
            if (CSharpMember.TryCreate(member, sourceText) is { } csharpMember)
            {
                members.Add(csharpMember);
            }
        }

        return members.ToImmutableAndClear();
    }

    private static bool IsLineEmpty(TextLine textLine)
        => textLine.Start == textLine.End;

}
