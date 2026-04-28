// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal partial class RazorEditService
{
    private static void AddMethodChanges(ref PooledArrayBuilder<RazorTextChange> edits, RazorCodeDocument codeDocument, ImmutableArray<CSharpMethod> addedMethods, RazorFormattingOptions options)
    {
        if (addedMethods.Length == 0)
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
            AddMethodsInNewCodeBlock(ref edits, codeDocument, addedMethods, options);
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
        AddMethodsInExistingCodeBlock(builder, sourceText, addedMethods, options, openBraceLine, closeBraceLine, insertLineIndex);

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

    private static void AddMethodsInNewCodeBlock(ref PooledArrayBuilder<RazorTextChange> edits, RazorCodeDocument codeDocument, ImmutableArray<CSharpMethod> methods, RazorFormattingOptions options)
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
        AppendMethodsText(builder, methods, options);
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

    private static void AddMethodsInExistingCodeBlock(StringBuilder builder, SourceText sourceText, ImmutableArray<CSharpMethod> addedMethods, RazorFormattingOptions options, int openBraceLineIndex, int closeBraceLineIndex, int insertLineIndex)
    {
        var lineAboveInsertionIsNotEmpty = insertLineIndex > 0 &&
            insertLineIndex - 1 != openBraceLineIndex &&
            !IsLineEmpty(sourceText.Lines[insertLineIndex - 1]);
        if (openBraceLineIndex == closeBraceLineIndex || lineAboveInsertionIsNotEmpty)
        {
            builder.AppendLine();
        }

        AppendMethodsText(builder, addedMethods, options);

        if (openBraceLineIndex == closeBraceLineIndex || insertLineIndex == closeBraceLineIndex)
        {
            builder.AppendLine();
        }
    }

    private static void AppendMethodsText(StringBuilder builder, ImmutableArray<CSharpMethod> methods, RazorFormattingOptions options)
    {
        var first = true;
        foreach (var method in methods)
        {
            if (!first)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            first = false;

            AppendIndentedMethod(builder, method, options);
        }
    }

    private static void AppendIndentedMethod(StringBuilder builder, CSharpMethod method, RazorFormattingOptions options)
    {
        // Roslyn will have indented the method by an appropriate amount for the generated file, but we need it to be placed nicely in the Razor
        // file, so we add each line of the method one at a time, adjusting the indentation as we go.
        int? initialIndentation = null;
        var sourceText = method.Text;

        var endLine = method.GetEndLineNumber();
        for (var i = method.GetStartLineNumber(); i <= endLine; i++)
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

    private static ImmutableArray<CSharpMethod> FindMethods(RoslynSyntaxNode syntaxRoot, SourceText sourceText)
    {
        if (!syntaxRoot.TryGetClassDeclaration(out var classDecl))
        {
            return [];
        }

        return classDecl.Members.OfType<MethodDeclarationSyntax>().SelectAsArray(method => new CSharpMethod(method, sourceText));
    }

    private static bool IsLineEmpty(TextLine textLine)
        => textLine.Start == textLine.End;

    private sealed record CSharpMethod(MethodDeclarationSyntax Method, SourceText Text) : IEquatable<CSharpMethod>
    {
        public bool Equals(CSharpMethod? other)
        {
            if (other is null)
            {
                return false;
            }

            // Since we only want to know about additions, we need to ignore any body changes, so we end our comparison span
            // before the body, or expression body, starts. This prevents changes inside method bodies that are entirely unmapped
            // causing us to add that method. Since an existing unmapped method can only be present if the Razor compiler emitted
            // it, we never want those in the Razor file.
            // Strictly speaking this is comparing more than necessary - since a C# method can't be overloaded by return type for
            // example, having that as part of the comparison is redundant. Same for visibility modifiers, which would seem to show
            // a bug in this logic: If Roslyn changes a method from public to private via a code action, that would appear to this
            // logic as an addition. In reality though, such a change would have to be in a mappable region to be a valid code action,
            // so the edits will have been processed already, and not seen by this code. For a method to go from public to private
            // in an unmappable region means Roslyn is changing one of the Razor compiler generated methods, which the user can
            // never see or interact with.
            // If the user has an incomplete method, then we are safe to just use the end of the method node.
            if (((SyntaxNode?)Method.Body ?? Method.ExpressionBody)?.SpanStart is not { } spanEnd)
            {
                spanEnd = Method.Span.End;
            }

            if (((SyntaxNode?)other.Method.Body ?? other.Method.ExpressionBody)?.SpanStart is not { } otherSpanEnd)
            {
                otherSpanEnd = other.Method.Span.End;
            }

            return Text.NonWhitespaceContentEquals(other.Text, Method.SpanStart, spanEnd, other.Method.SpanStart, otherSpanEnd);
        }

        public override int GetHashCode()
        {
            // Given the gymnastics we are doing to construct a modified generated document, we want to always fallback to the Equals check
            // as that is the only actual trustworthy comparison we can do. Constructing a string from the source text without whitespace just
            // to get the hashcode seems like overkill for the amount of methods we expect to be added/removed in a typical code action.
            return 0;
        }

        // We don't want trivia, because it will include generated artifacts like #line directives, so using Span instead of FullSpan in the two
        // methods below is deliberate
        public int GetStartLineNumber()
            => Text.Lines.GetLineFromPosition(Method.SpanStart).LineNumber;

        public int GetEndLineNumber()
            => Text.Lines.GetLineFromPosition(Math.Max(Method.SpanStart, Method.Span.End - 1)).LineNumber;
    }
}
