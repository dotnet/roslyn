// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;
using RazorSyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal partial class CSharpFormattingPass
{
    /// <summary>
    /// Generates a C# document in order to get Roslyn formatting behaviour on a Razor document
    /// </summary>
    /// <remarks>
    /// <para>
    /// The general theory is to take a Razor syntax tree and convert it to something that looks to Roslyn like a C#
    /// document, in a way that accurately represents the indentation constructs that a Razor user is expressing when
    /// they write the Razor code.
    /// </para>
    /// <para>
    /// For example, given the following Razor file:
    /// <code>
    /// &lt;div&gt;
    ///     @if (true)
    ///     {
    ///         // Some code
    ///     }
    /// &lt;/div&gt;
    ///
    /// @code {
    ///     private string Name { get; set; }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// The generator will go through that syntax tree and produce the following C# document:
    /// <code>
    /// { // &lt;div&gt;
    ///     @if (true)
    ///     {
    ///         // Some code
    ///     }
    /// } // &lt;/div&gt;
    ///
    /// class F
    /// {
    ///     private string Name { get; set; }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// The class definition is clearly not present in the source Razor document, but it represents the intended
    /// indentation that the user would expect to see for the property declaration. The Html element start and
    /// end tags are emited as braces so they cause the right amount of indentation, and the indentation of the
    /// @if block is recorded, so that any quirks of Roslyn formatting are the same as what the user would expect
    /// when looking at equivalent code in C#. ie, there might only be one level of indentation in C# for "Some code"
    /// but conceptually the user is expecting two, due to the Html element.
    /// </para>
    /// <para>
    /// For more complete examples, the full test log for every formatting test includes the generated C# document.
    /// </para>
    /// <para>
    /// A final important note about this class, whilst it is a SyntaxVisitor, it is not intended to be a general
    /// purpose one, and things won't work as expected if the Visit method is called on arbitrary nodes. The visit
    /// methods are implemented with the assumption they will only see a node if it is the first one on a line of
    /// a Razor file.
    /// </para>
    /// </remarks>
    private sealed class CSharpDocumentGenerator
    {
        public static FormattedDocument Generate(RazorCodeDocument codeDocument, SyntaxNode csharpSyntaxRoot, RazorFormattingOptions options, IDocumentMappingService documentMappingService)
        {
            using var _1 = StringBuilderPool.GetPooledObject(out var builder);
            using var _2 = ArrayBuilderPool<LineInfo>.GetPooledObject(out var lineInfoBuilder);
            lineInfoBuilder.SetCapacityIfLarger(codeDocument.Source.Text.Lines.Count);

            var generator = new Generator(codeDocument, csharpSyntaxRoot, options, builder, lineInfoBuilder, documentMappingService);

            generator.Generate();

            var text = SourceText.From(builder.ToString());

            return new(text, lineInfoBuilder.ToImmutableAndClear());
        }

        private static string GetAdditionalLineComment(SourceSpan originalSpan)
        {
            // IMPORTANT: The format here needs to match the parse method below
            return $"// {originalSpan.AbsoluteIndex} {originalSpan.Length}";
        }

        public static bool TryParseAdditionalLineComment(TextLine line, out int start, out int length)
        {
            start = 0;
            length = 0;

            // We're looking for a line that matches "// {start} {length}", where start and length are integers.

            // Need at least 6 chars for two single digit integers separated by a space, plus "// "
            if (line.Span.Length < 6)
            {
                return false;
            }

            if (line.CharAt(0) != '/' ||
                line.CharAt(1) != '/' ||
                line.CharAt(2) != ' ')
            {
                return false;
            }

            var span = line.ToString().AsSpan();
            var toParse = span[3..];
            var space = toParse.IndexOf(' ');
            if (space == -1)
            {
                return false;
            }

            var startSpan = toParse[..space];
            var lengthSpan = toParse[(space + 1)..];

#if NET8_0_OR_GREATER
            return int.TryParse(startSpan, out start)
                && int.TryParse(lengthSpan, out length);
#else
            return int.TryParse(startSpan.ToString(), out start)
                && int.TryParse(lengthSpan.ToString(), out length);
#endif
        }

        private sealed class Generator(
            RazorCodeDocument codeDocument,
            SyntaxNode csharpSyntaxRoot,
            RazorFormattingOptions options,
            StringBuilder builder,
            ImmutableArray<LineInfo>.Builder lineInfoBuilder,
            IDocumentMappingService documentMappingService) : SyntaxVisitor<LineInfo>
        {
            private const string SyntheticLambdaBodyStart = "() => {";
            private const int SyntheticLambdaSignatureLength = 5;

            private readonly SourceText _sourceText = codeDocument.Source.Text;
            private readonly RazorCodeDocument _codeDocument = codeDocument;
            private readonly SyntaxNode _csharpSyntaxRoot = csharpSyntaxRoot;
            private readonly bool _insertSpaces = options.InsertSpaces;
            private readonly int _tabSize = options.TabSize;
            private readonly AttributeIndentStyle _attributeIndentStyle = options.AttributeIndentStyle;
            private readonly RazorCSharpSyntaxFormattingOptions? _csharpSyntaxFormattingOptions = options.CSharpSyntaxFormattingOptions;
            private readonly StringBuilder _builder = builder;
            private readonly ImmutableArray<LineInfo>.Builder _lineInfoBuilder = lineInfoBuilder;
            private readonly IDocumentMappingService _documentMappingService = documentMappingService;
            private readonly RazorCSharpDocument _csharpDocument = codeDocument.GetCSharpDocument().AssumeNotNull();

            private TextLine _currentLine;
            private int _currentFirstNonWhitespacePosition;

            private RazorSyntaxToken _currentToken;
            private RazorSyntaxToken _previousCurrentToken;

            /// <summary>
            /// The line number of the last line of an element, if we're inside one, where we care about the Html formatters indentation
            /// </summary>
            /// <remarks>
            /// This is used to track if the syntax node at the start of each line is parented by an element node, without
            /// having to do lots of tree traversal. We use this to make sure we format the contents of script and style tags as
            /// the Html formatter intends.
            /// </remarks>
            private int? _honourHtmlFormattingUntilLine;
            /// <summary>
            /// The line number of the last line of a block where formatting should be completely ignored
            /// </summary>
            /// <remarks>
            /// Some Html constructs, namely &lt;textarea&gt; and &lt;pre&gt;, should not be formatted at all, and we essentially
            /// need to treat them as multiline Razor comments. This field is used to track the line number of the last line of such
            /// an element, so we can ignore every line in it without having to do lots of tree traversal to check "are we parented
            /// by a pre tag" etc.
            /// </remarks>
            private int? _ignoreUntilLine;

            public void Generate()
            {
                using var _ = StringBuilderPool.GetPooledObject(out var additionalLinesBuilder);

                var root = _codeDocument.GetRequiredSyntaxRoot();
                var sourceMappings = _codeDocument.GetRequiredCSharpDocument().SourceMappingsSortedByOriginal;
                var iMapping = 0;
                foreach (var line in _sourceText.Lines)
                {
                    if (line.GetFirstNonWhitespacePosition() is int firstNonWhitespacePosition)
                    {
                        _previousCurrentToken = _currentToken;
                        _currentLine = line;
                        _currentFirstNonWhitespacePosition = firstNonWhitespacePosition;
                        _currentToken = root.FindToken(firstNonWhitespacePosition);

                        var length = _builder.Length;
                        _lineInfoBuilder.Add(Visit(_currentToken.Parent));
                        Debug.Assert(_builder.Length > length, "Didn't output any generated code!");

                        // If there are C# mappings on this line, we want to output additional lines that represent the C# blocks.
                        while (iMapping < sourceMappings.Length)
                        {
                            var originalSpan = sourceMappings[iMapping].OriginalSpan;
                            if (originalSpan.AbsoluteIndex < _currentFirstNonWhitespacePosition)
                            {
                                iMapping++;
                            }
                            else if (originalSpan.AbsoluteIndex > _currentFirstNonWhitespacePosition &&
                                (originalSpan.AbsoluteIndex + originalSpan.Length) <= line.Span.End)
                            {
                                // We've found a span mapping that means there is some C# on this line, so if its an explicit or implicit expression
                                // we need to format it, but separately to the rest of the document.
                                var node = root.FindInnermostNode(originalSpan.AbsoluteIndex);
                                if (node is CSharpExpressionLiteralSyntax)
                                {
                                    AddAdditionalLineFormattingContent(additionalLinesBuilder, node, originalSpan);
                                }

                                iMapping++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        _builder.AppendLine();
                        _lineInfoBuilder.Add(CreateLineInfo(processIndentation: false));
                    }

                    // If we're inside an element that ends on this line, clear the field that tracks it.
                    if (_honourHtmlFormattingUntilLine is { } endLine &&
                        endLine == line.LineNumber)
                    {
                        _honourHtmlFormattingUntilLine = null;
                    }

                    if (_ignoreUntilLine is { } endLine2 &&
                        endLine2 == line.LineNumber)
                    {
                        _ignoreUntilLine = null;
                    }
                }

                _builder.AppendLine();
                _builder.AppendLine(additionalLinesBuilder.ToString());
            }

            private void AddAdditionalLineFormattingContent(StringBuilder additionalLinesBuilder, RazorSyntaxNode node, SourceSpan originalSpan)
            {
                // Rather than bother to store more data about the formatted file, since we don't actually know where
                // these will end up in that file once it's all said and done, we are just going to use a simple comment
                // format that we can easily parse.

                // Special case, for attributes that represent generic type parameters, we want to output something such
                // that Roslyn knows to format it as a type. For example, the meaning and spacing around "?"s should be
                // what the user expects.
                if (node is { Parent.Parent: MarkupTagHelperAttributeSyntax attribute } &&
                    attribute is { Parent.Parent: MarkupTagHelperElementSyntax element } &&
                    element.TagHelperInfo.BindingResult.TagHelpers is [{ } descriptor, ..] &&
                    descriptor.IsGenericTypedComponent() &&
                    descriptor.BoundAttributes.FirstOrDefault(d => d.Name == attribute.TagHelperAttributeInfo.Name) is { } boundAttribute &&
                    boundAttribute.IsTypeParameterProperty())
                {
                    additionalLinesBuilder.AppendLine("F<");
                    additionalLinesBuilder.AppendLine(GetAdditionalLineComment(originalSpan));
                    additionalLinesBuilder.AppendLine(_sourceText.ToString(originalSpan.ToTextSpan()));
                    additionalLinesBuilder.AppendLine("> x;");
                    return;
                }

                additionalLinesBuilder.AppendLine("_ =");
                additionalLinesBuilder.AppendLine(GetAdditionalLineComment(originalSpan));
                additionalLinesBuilder.AppendLine(_sourceText.ToString(originalSpan.ToTextSpan()));
                additionalLinesBuilder.AppendLine(";");
            }

            public override LineInfo Visit(RazorSyntaxNode? node)
            {
                // Sometimes we are in a block where we want to do no formatting at all
                if (_ignoreUntilLine is not null)
                {
                    return EmitCurrentLineWithNoFormatting();
                }

                return base.Visit(node);
            }

            protected override LineInfo DefaultVisit(RazorSyntaxNode node)
            {
                return EmitCurrentLineAsCSharp();
            }

            public override LineInfo VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
            {
                if (_sourceText.GetLinePositionSpan(node.Span).SpansMultipleLines())
                {
                    return VisitMultilineCSharpExpressionLiteral(node);
                }

                Debug.Assert(node.LiteralTokens.Count > 0);
                return VisitCSharpLiteral(node, node.LiteralTokens[^1]);
            }

            private LineInfo VisitMultilineCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
            {
                // Literals that span multiple lines are interesting. eg, given:
                //
                // <div class="@(foo
                //              .bar)" />
                //
                // When we visit the second line, the first thing on the line is already the middle of the C# expression.
                // Roslyn still needs the previous line's `@(foo` context to indent `.Bar)` sensibly, so we may need to
                // carry that previous line forward into the generated C# document. In attribute cases like:
                //
                // <button class="btn"
                //         @onclick="@(async e =>
                //         {
                //
                // we also need to remember where that carried-forward line started so the mapped result can be applied
                // back onto the attribute text correctly.
                int? skippedPreviousLineOriginOffset = null;
                var nodeStartLine = GetLineNumber(node);
                var expressionStartsBlockLambda = IsBlockLambdaStart(node.SpanStart, _sourceText.Lines[nodeStartLine]);
                var previousTokenParent = _previousCurrentToken.Parent;
                var previousLineStartedWithAttributeName =
                    previousTokenParent is not null &&
                    RazorSyntaxFacts.IsAttributeName(previousTokenParent, out _);

                if (nodeStartLine == _currentLine.LineNumber - 1 &&
                    _sourceText.Lines[nodeStartLine] is { } previousLine &&
                    previousLine.GetFirstNonWhitespacePosition() != node.Position &&
                    (_previousCurrentToken.Kind != SyntaxKind.Transition || previousLineStartedWithAttributeName))
                {
                    var skippedPreviousLineText = _sourceText.ToString(TextSpan.FromBounds(node.SpanStart, previousLine.End));
                    _builder.AppendLine(skippedPreviousLineText);
                    skippedPreviousLineOriginOffset = node.SpanStart - previousLine.Start;
                }

                // The last line of this might not be entirely C#, so we have to trim off the end so as not to cause issues. For the
                // middle lines, we don't need to worry about that, but we do have to deal with quirks for Html attribute (see below)
                // so this code handles both cases:

                // We can't use node.Span because it can contain newlines from the line before, so we have to work a little.

                // First, emit the whitespace, so user spacing is honoured if possible
                _builder.Append(_sourceText.ToString(TextSpan.FromBounds(_currentLine.Start, _currentFirstNonWhitespacePosition)));

                // Now emit the contents of the line. If this is the last line of the expression literal, then we want to stop at the
                // end of the node, as there could be other contents afterwards, so work out if we're in that case first.
                var toEndOfNode = _sourceText.GetLinePosition(node.EndPosition).Line == _currentLine.LineNumber;

                var emitSemiColon = false;
                var end = _currentLine.End;

                if (toEndOfNode)
                {
                    // A special case here is if we're inside an explicit expression body, one of the bits of content after the node will
                    // be the final close parens, so we need to emit that or the C# expression won't be valid, and we can't trust the formatter.
                    var potentialExplicitExpression = node.Parent.Parent;
                    if (potentialExplicitExpression is CSharpExplicitExpressionBodySyntax or CSharpImplicitExpressionBodySyntax &&
                        _sourceText.GetLinePosition(potentialExplicitExpression.EndPosition).Line == _currentLine.LineNumber)
                    {
                        emitSemiColon = true;
                        end = potentialExplicitExpression.EndPosition;
                    }
                    else
                    {
                        end = node.EndPosition;
                        // Semi-colons are vitally important for block bodied lambdas to not affect subsequent lines
                        emitSemiColon = expressionStartsBlockLambda && end != _currentLine.End;
                    }
                }

                var span = TextSpan.FromBounds(_currentFirstNonWhitespacePosition, end);
                _builder.Append(_sourceText.ToString(span));

                // Keep track of how many characters we add to the end of the line, so the formatter knows to ignore them.
                var offsetFromEnd = 0;

                // If we're at the end of an explicit expression, we want to add a semi-colon to the end of the line, so that C# after the
                // expression is formatted correctly. ie, we need to "close" the expression.
                if (emitSemiColon)
                {
                    offsetFromEnd++;
                    _builder.Append(';');
                }

                // Append a comment at the end so whitespace isn't removed, as Roslyn thinks its the end of the line, but it might not be.
                // eg, given "4, 5, @<div></div>", we want Roslyn to keep the space after the last comma, because there is something after it,
                // but we can't let Roslyn see the "@<div>" that it is.
                // We use a multi-line comment because Roslyn has a desire to line up "//" comments with the previous line, which we could interpret
                // as Roslyn suggesting we indent some trailing Html.
                if (end != _currentLine.End)
                {
                    const string EndOfLineComment = " /* */";
                    offsetFromEnd += EndOfLineComment.Length;
                    _builder.Append(EndOfLineComment);
                }

                _builder.AppendLine();

                // Final quirk: If we're inside an Html attribute, it means the Html formatter won't have formatted this line, as multi-line
                // Html attributes are not valid.
                // TODO: The traverse up the tree here is not ideal. See comments in https://github.com/dotnet/razor/issues/11371
                var htmlIndentLevel = 0;
                int? additionalIndentation = null;
                var attributeNode = node.Ancestors().FirstOrDefault(n => n.IsAnyAttributeSyntax())
                    ?? (previousLineStartedWithAttributeName ? previousTokenParent?.Parent : null);
                if (attributeNode?.Parent is BaseMarkupStartTagSyntax attributeStartTag)
                {
                    GetAttributeIndentation(attributeStartTag, out htmlIndentLevel, out var attributeAdditionalIndentation);
                    additionalIndentation = attributeAdditionalIndentation;
                }

                if (offsetFromEnd == 0)
                {
                    // If we're not doing any extra emitting of our own, then we can safely check for newlines
                    return CreateLineInfo(
                        skippedPreviousLineOriginOffset: skippedPreviousLineOriginOffset,
                        processFormatting: true,
                        htmlIndentLevel: htmlIndentLevel,
                        additionalIndentation: additionalIndentation,
                        checkForNewLines: true);
                }

                return CreateLineInfo(
                    skippedPreviousLineOriginOffset: skippedPreviousLineOriginOffset,
                    processFormatting: true,
                    formattedLength: span.Length,
                    formattedOffsetFromEndOfLine: offsetFromEnd,
                    htmlIndentLevel: htmlIndentLevel,
                    additionalIndentation: additionalIndentation,
                    checkForNewLines: false);
            }

            public override LineInfo VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
            {
                Debug.Assert(node.LiteralTokens.Count > 0);

                // If this is the end of a multi-line CSharp template (ie, RenderFragment) then we need to close
                // out the lambda expression that we started when we opened it.
                // When there are two templates directly following each other, the parser will put the semicolon of one
                // and the declaration of the next in the same literal, so we have to be careful to only do this when the
                // semicolon we find is on the line we're trying to process.
                if (node.LiteralTokens.FirstOrDefault(static t => !t.IsWhitespace()) is { Content: ";" } semicolon &&
                    GetLineNumber(semicolon) == GetLineNumber(_currentToken) &&
                    node.TryGetPreviousSibling(out var previousSibling) &&
                    previousSibling is CSharpTemplateBlockSyntax &&
                    GetLineNumber(previousSibling.GetFirstToken()) != GetLineNumber(previousSibling.GetLastToken()))
                {
                    _builder.AppendLine(";");
                    return CreateLineInfo();
                }

                return VisitCSharpLiteral(node, node.LiteralTokens[^1]);
            }

            private LineInfo VisitCSharpLiteral(RazorSyntaxNode node, RazorSyntaxToken lastToken)
            {
                // If we get here we have a line of code which starts in C#, but Razor being Razor means we can't assume it
                // is entirely C#. For example it could be:
                //
                // Render(@<div></div>);
                //
                // In these situations we simply stop at the transition away from C# and output the line as C#. The only
                // interesting thing we have to do is tell the formatter that we've done that, so it doesn't expect the
                // line contents to match the original line entirely. The Html formatter will have dealt with the bits after
                // the transition anyway.
                //
                // The final quirk is that the node in question can span multiple lines, but the transition can only
                // possibly be on the last line.
                if (lastToken.GetNextToken() is { Kind: SyntaxKind.Transition } token &&
                    GetLineNumber(token) == GetLineNumber(_currentToken))
                {
                    // We can't use node.Span because it can contain newlines from the line before.
                    // Emit the whitespace, so user spacing is honoured if possible
                    _builder.Append(_sourceText.ToString(TextSpan.FromBounds(_currentLine.Start, _currentFirstNonWhitespacePosition)));
                    // Now emit the contents
                    var span = TextSpan.FromBounds(_currentFirstNonWhitespacePosition, node.EndPosition);
                    _builder.Append(_sourceText.ToString(span));

                    // If the template is multiline we emit a block-bodied lambda opener so Roslyn has a statement-friendly
                    // context for the template body. For example:
                    //
                    //     Render(@<div>
                    //         @if (showDetails)
                    //         {
                    //             <p>Hello</p>
                    //         }
                    //     </div>);
                    //
                    // The `@<div> ... </div>` template is in an expression position, but its body can contain statements.
                    // For single-line templates we can get away with a `null` placeholder instead.
                    if (token.Parent?.Parent.Parent is CSharpTemplateBlockSyntax template &&
                        _sourceText.GetLinePositionSpan(template.Span).SpansMultipleLines())
                    {
                        var charsAppended = AppendSyntheticLambdaBodyStart();
                        return CreateLineInfo(
                            skipNextLineIfBrace: true,
                            formattedLength: span.Length,
                            formattedOffsetFromEndOfLine: charsAppended,
                            processFormatting: true,
                            // We turn off check for new lines because that only works if the content doesn't change from the original,
                            // but we're deliberately leaving out a bunch of the original file because it would confuse the Roslyn formatter.
                            checkForNewLines: false);
                    }

                    // Putting a semi-colon on the end might make for invalid C#, but it means this line won't cause indentation,
                    // which is all we need. If we're in an explicit expression body though, we don't want to do this, as the
                    // close paren of the expression will do the same job (and the semi-colon would confuse that).
                    var emitSemiColon = node.Parent.Parent is not CSharpExplicitExpressionBodySyntax;
                    _builder.AppendLine("null");

                    if (emitSemiColon)
                    {
                        _builder.AppendLine(";");
                    }

                    return CreateLineInfo(
                        skipNextLine: emitSemiColon,
                        formattedLength: span.Length,
                        formattedOffsetFromEndOfLine: 4, // "null".Length
                        processFormatting: true,
                        // We turn off check for new lines because that only works if the content doesn't change from the original,
                        // but we're deliberately leaving out a bunch of the original file because it would confuse the Roslyn formatter.
                        checkForNewLines: false);
                }

                // If we're here, it means this is a "normal" line of C#, so we can just emit it as is. The exception to this is
                // when we're inside a string literal. We still want to emit it as is, but we need to make sure we tell the formatter
                // to ignore any existing indentation too.
                if (_documentMappingService.TryMapToCSharpDocumentPosition(_csharpDocument, _currentToken.SpanStart, out _, out var csharpIndex) &&
                    _csharpSyntaxRoot.FindNode(new TextSpan(csharpIndex, 0), getInnermostNodeForTie: true) is { } csharpNode &&
                    csharpNode.IsStringLiteral(multilineOnly: true))
                {
                    _builder.AppendLine(_currentLine.ToString());
                    return CreateLineInfo(processIndentation: false, processFormatting: true, checkForNewLines: true);
                }

                return EmitCurrentLineAsCSharp();
            }

            public override LineInfo VisitMarkupStartTag(MarkupStartTagSyntax node)
            {
                return VisitStartTag(node);
            }

            public override LineInfo VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
            {
                return VisitStartTag(node);
            }

            private LineInfo VisitStartTag(BaseMarkupStartTagSyntax node)
            {
                var closeAngle = node.GetEndTag()?.CloseAngle ?? node.CloseAngle;
                if (ElementHasSignificantWhitespace(node))
                {
                    // The contents of some html tags is significant, so we never want any formatting to happen in their contents
                    if (GetLineNumber(node) == GetLineNumber(node.CloseAngle))
                    {
                        _ignoreUntilLine = GetLineNumber(closeAngle);
                    }

                    return EmitCurrentLineAsComment();
                }

                var lineInfo = ElementCausesIndentation(node)
                    ? EmitOpenBraceLine()          // When an element causes indentation, we emit an open brace to tell the C# formatter to indent.
                    : EmitCurrentLineAsComment();  // This is a single line element, so it doesn't cause indentation

                if (RazorSyntaxFacts.IsScriptOrStyleBlock(node.ParentElement) &&
                    _honourHtmlFormattingUntilLine is null)
                {
                    // If this is an element at the root level, we want to record where it ends. We can't rely on the Visit method
                    // for it, because it might not be at the start of a line. We only care about contents though, so thats why
                    // we are doing this after emitting this line, and subtracting one from the end element line number.
                    _honourHtmlFormattingUntilLine = GetLineNumber(closeAngle) - 1;
                }

                return lineInfo;
            }

            public override LineInfo VisitMarkupEndTag(MarkupEndTagSyntax node)
            {
                return VisitEndTag(node);
            }

            public override LineInfo VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
            {
                return VisitEndTag(node);
            }

            private LineInfo VisitEndTag(BaseMarkupEndTagSyntax node)
            {
                // End tags are emited as close braces, to remove the indent their start tag caused. We don't need to worry
                // about single-line elements here, because these Visit methods only ever see the first node on a line.
                _builder.Append('}');

                // Close multiline template lambdas when the template ends on this line.
                if (TryGetMultilineTemplateClosure(node, out var appendSemicolon) && appendSemicolon)
                {
                    _builder.Append(';');
                }

                _builder.AppendLine();
                return CreateLineInfo();
            }

            private bool ElementCausesIndentation(BaseMarkupStartTagSyntax node)
            {
                if (node.Name.Content.Equals("html", StringComparison.OrdinalIgnoreCase))
                {
                    // <html> doesn't cause indentation in Visual Studio or VS Code, so we honour that.
                    return false;
                }

                if (node.IsSelfClosing())
                {
                    // Self-closing elements don't cause indentation
                    return false;
                }

                if (node.IsVoidElement())
                {
                    // Void elements don't cause indentation
                    return false;
                }

                if (node.GetEndTag() is { } endTag &&
                    GetLineNumber(endTag) == GetLineNumber(node.CloseAngle))
                {
                    // End tag is on the same line as the start tag's close angle bracket
                    return false;
                }

                return true;
            }

            private static bool ElementHasSignificantWhitespace(BaseMarkupStartTagSyntax node)
            {
                return node.Name.Content.Equals("textarea", StringComparison.OrdinalIgnoreCase)
                    || node.Name.Content.Equals("pre", StringComparison.OrdinalIgnoreCase);
            }

            public override LineInfo VisitRazorMetaCode(RazorMetaCodeSyntax node)
            {
                // This could be a directive attribute, like @bind-Value="asdf"
                if (TryVisitAttribute(node) is { } result)
                {
                    return result;
                }

                // Meta code is a few things, and mostly they're valid C#, but one case we have to specifically handle is
                // bound attributes that start on their own line, eg the second line of:
                //
                // <Thing foo="bar"
                //        @bind-value="baz" />
                if (node.MetaCode is [{ Kind: SyntaxKind.Transition }, ..])
                {
                    // This is not C# so we just need to avoid the default visit
                    return EmitCurrentLineAsComment();
                }

                if (node is
                    {
                        Parent: CSharpExplicitExpressionBodySyntax,
                        MetaCode: [{ Kind: SyntaxKind.RightParenthesis } paren]
                    } &&
                    paren.GetPreviousToken() is { Parent: CSharpExpressionLiteralSyntax literal })
                {
                    // This is the close bracket of a multi-line "@( .. )" expression at the start of a line, ie the last line of:
                    //
                    //    @(DateTime.
                    //      Now
                    //    )
                    //
                    // This needs some funky handling because there could be non-C# after the close parens, but fortunately
                    // the method we already have that handles the intervening lines has all of the right code.
                    return VisitMultilineCSharpExpressionLiteral(literal);
                }

                return EmitCurrentLineAsCSharp();
            }

            public override LineInfo VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
            {
                // A MarkupEphemeralTextLiteral is an escaped @ sign, eg in CSS "@@font-face". We just treat it like markup text
                return EmitCurrentLineAsComment();
            }

            public override LineInfo VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
            {
                if (node.Parent is MarkupCommentBlockSyntax comment)
                {
                    return VisitMarkupCommentBlock(comment);
                }

                if (TryVisitAttribute(node) is { } result)
                {
                    return result;
                }

                return EmitCurrentLineAsComment();
            }

            public override LineInfo VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
            {
                // The contents of html comments might be significant (eg tables or ASCII flow charts),
                // so we never want any formatting to happen inside them.
                _ignoreUntilLine = _sourceText.Lines.GetLineFromPosition(node.EndPosition).LineNumber;

                return EmitCurrentLineAsComment();
            }

            private LineInfo? TryVisitAttribute(RazorSyntaxNode node)
            {
                if (RazorSyntaxFacts.IsAttributeName(node, out var startTag))
                {
                    GetAttributeIndentation(startTag, out var htmlIndentLevel, out var additionalIndentation);

                    // Self-closing tags can be the last line of a multiline template, so emit the synthetic close here.
                    if (startTag.IsSelfClosing() &&
                        GetLineNumber(startTag.CloseAngle) == _currentLine.LineNumber &&
                        TryGetMultilineTemplateClosure(startTag, out _))
                    {
                        return EmitSyntheticLambdaBodyCloseLine(startTag, htmlIndentLevel, additionalIndentation);
                    }

                    if (ElementHasSignificantWhitespace(startTag) &&
                        GetLineNumber(node) == GetLineNumber(startTag.CloseAngle))
                    {
                        // If this is the last line of a tag that shouldn't be indented, honour that
                        _ignoreUntilLine = GetLineNumber(startTag.GetEndTag()?.CloseAngle ?? startTag.CloseAngle);
                    }

                    return EmitCurrentLineAsComment(htmlIndentLevel: htmlIndentLevel, additionalIndentation: additionalIndentation);
                }

                return null;
            }

            private void GetAttributeIndentation(BaseMarkupStartTagSyntax startTag, out int htmlIndentLevel, out int additionalIndentation)
            {
                additionalIndentation = 0;
                var startTagAddsIndentation = ElementCausesIndentation(startTag) && !ElementHasSignificantWhitespace(startTag);
                var firstAttribute = startTag.Attributes[0];
                var firstAttributeNameSpan = RazorSyntaxFacts.GetFullAttributeNameSpan(firstAttribute);

                if (_attributeIndentStyle == AttributeIndentStyle.IndentByOne)
                {
                    // Indent attributes by one level to match child elements.
                    htmlIndentLevel = startTagAddsIndentation ? 0 : 1;
                }
                else if (_attributeIndentStyle == AttributeIndentStyle.IndentByTwo)
                {
                    // Indent attributes by two levels to differentiate them from child elements.
                    htmlIndentLevel = startTagAddsIndentation ? 1 : 2;
                }
                else if (_attributeIndentStyle == AttributeIndentStyle.AlignWithFirst)
                {
                    // Align attributes with the first attribute in their tag.
                    // We need to line up with the first attribute, but the start tag might not be the first thing on the line,
                    // so it's really relative to the first non-whitespace character on the line. We use the line that the attribute
                    // is on, just in case it's not on the same line as the start tag.
                    var lineStart = _sourceText.Lines[GetLineNumber(firstAttributeNameSpan)].GetFirstNonWhitespacePosition().GetValueOrDefault();
                    htmlIndentLevel = FormattingUtilities.GetIndentationLevel(firstAttributeNameSpan.Start - lineStart, _tabSize, out additionalIndentation);

                    if (startTagAddsIndentation &&
                        GetLineNumber(firstAttributeNameSpan) == GetLineNumber(startTag.Name))
                    {
                        // If the element has caused indentation, then we'll want to take one level off our attribute indentation to
                        // compensate.
                        htmlIndentLevel--;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Unknown attribute indentation style '{_attributeIndentStyle}'.");
                }

                if (TemplateStartAddsIndentation(startTag, firstAttributeNameSpan) && htmlIndentLevel > 0)
                {
                    // Inline multiline templates already contribute a continuation indent through the synthetic lambda body.
                    htmlIndentLevel--;
                }
            }

            private bool TemplateStartAddsIndentation(BaseMarkupStartTagSyntax startTag, TextSpan firstAttributeNameSpan)
            {
                if (GetContainingTemplate(startTag) is not { } template ||
                    !_sourceText.GetLinePositionSpan(template.Span).SpansMultipleLines() ||
                    GetLineNumber(template.GetFirstToken()) != GetLineNumber(startTag.Name) ||
                    GetLineNumber(firstAttributeNameSpan) != GetLineNumber(startTag.Name))
                {
                    return false;
                }

                var templateLine = _sourceText.Lines[GetLineNumber(template.GetFirstToken())];
                return templateLine.GetFirstNonWhitespacePosition() is int firstNonWhitespacePosition &&
                    template.GetFirstToken().Position > firstNonWhitespacePosition;
            }

            public override LineInfo VisitMarkupTransition(MarkupTransitionSyntax node)
            {
                // A transition to Html means the start of a RenderFragment. These are challenging because conceptually
                // they are like a Write() call, because their contents are sent to the output, but they can also contain
                // statements. eg:
                //
                // RenderFragment f =
                //     @<div>
                //          @if (true)
                //          {
                //              <p>Some text</p>
                //          }
                //     </div>;
                //
                // If we convert that to C# the way we normally do, we end up with statements in a C# context where only
                // expressions are valid. To avoid that, we need to emit C# such that we can be sure we're in a context
                // where statements are valid. To do this we emit a block bodied lambda expression. Ironically this whole
                // formatting engine arguably exists because the compiler loves to emit lambda expressions, but they're
                // really annoying to format. This just happens to be the one case where a lambda is the right choice.

                // Emit the whitespace, so user spacing is honoured if possible
                _builder.Append(_sourceText.ToString(TextSpan.FromBounds(_currentLine.Start, _currentFirstNonWhitespacePosition)));

                // If its a one-line render fragment, then we don't need to worry.
                if (GetLineNumber(node.Parent.GetLastToken()) == GetLineNumber(_currentToken))
                {
                    _builder.AppendLine("null;");
                    return CreateLineInfo();
                }

                // Roslyn may move the opening brace to the next line, depending on its options. Unlike with code block
                // formatting where we put the opening brace on the next line ourselves (and Roslyn might bring it back)
                // if we do that for lambdas, Roslyn won't adjust the opening brace position at all. See, told you lambdas
                // were annoying to format.
                return EmitSyntheticLambdaBodyStartLine();
            }

            public override LineInfo VisitRazorCommentBlock(RazorCommentBlockSyntax node)
            {
                // Every line of a multiline Razor comment will hit this method, but we have two different ways to handle it.
                // For the start comment, we output a C# comment, so it gets indented as normal. For all of the other lines,
                // we just tell the formatter to completely skip this line. The Html formatter also skips comment lines, so
                // they will be left exactly as the user wrote them.
                if (_currentToken.Kind == SyntaxKind.RazorCommentTransition)
                {
                    return EmitCurrentLineAsComment();
                }

                // Do nothing for any lines inside the comment
                return EmitCurrentLineWithNoFormatting();
            }

            public override LineInfo VisitCSharpTransition(CSharpTransitionSyntax node)
            {
                // Empty transition we just emit as nothing interesting
                if (node.Parent is null)
                {
                    return EmitCurrentLineAsComment();
                }

                // Other transitions, we decide based on the parent
                return base.Visit(node.Parent);
            }

            public override LineInfo VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
            {
                // This matches like @DateTime.Now, which you would think we want to format as C#, but there can be multiple of them
                // on the same line, and they don't have to be the first thing on the line. We handle these above in GetCSharpDocumentContents,
                // so we can actually just emit these lines as a comment so the indentation is correct, and then let the code above
                // handle them. Essentially, whether these are at the start or int he middle of a line is irrelevant.

                // The exception to this is if the implicit expressions are multi-line. In that case, it's possible that the contents
                // of this line (ie, the first line) will affect the indentation of subsequent lines. Emitting this as a comment, won't
                // help when we emit the following lines in their original form. So lets do that for this line too. Since it's multi-line
                // we know, by definition, there can't be more than one on this line anyway.

                if (_sourceText.GetLinePositionSpan(node.Span).SpansMultipleLines())
                {
                    var csharpCode = ((CSharpImplicitExpressionBodySyntax)node.Body).CSharpCode;
                    return VisitCSharpCodeBlock(node, csharpCode);
                }

                return EmitCurrentLineAsComment();
            }

            public override LineInfo VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
            {
                // If this is a single line expression, we handle it like we do for implicit expressions, irrelevant
                // of whether its at the start or in the middle of the line.
                var body = (CSharpExplicitExpressionBodySyntax)node.Body;
                var closeParen = body.CloseParen;
                var csharpCode = body.CSharpCode;
                if (GetLineNumber(closeParen) == GetLineNumber(node))
                {
                    return EmitCurrentLineAsComment();
                }

                return VisitCSharpCodeBlock(node, csharpCode);
            }

            private LineInfo VisitCSharpCodeBlock(RazorSyntaxNode node, CSharpCodeBlockSyntax csharpCode)
            {
                // If this spans multiple lines however, the indentation of this line will affect the next, so we handle it in the
                // same way we handle a C# literal syntax. That includes checking if the C# doesn't go to the end of the line.
                // If the whole explicit expression is C#, then the children will be a single CSharpExpressionLiteral. If not, there
                // will be multiple children, and the second one is not C#, so thats the one we need to exclude from the generated
                // document.
                if (csharpCode.Children is [_, { } secondChild, ..] &&
                    GetLineNumber(secondChild) == GetLineNumber(node))
                {
                    // Emit the whitespace, so user spacing is honoured if possible
                    _builder.Append(_sourceText.ToString(TextSpan.FromBounds(_currentLine.Start, _currentFirstNonWhitespacePosition)));
                    var span = TextSpan.FromBounds(_currentFirstNonWhitespacePosition + 1, secondChild.Position);
                    _builder.Append(_sourceText.ToString(span));

                    if (secondChild is CSharpTemplateBlockSyntax template &&
                        _sourceText.GetLinePositionSpan(template.Span).SpansMultipleLines())
                    {
                        // This is the explicit-expression version of the RenderFragment case above, e.g.
                        //
                        //     @(BuildFragment(@<div>
                        //         @if (showDetails)
                        //         {
                        //             <p>Hello</p>
                        //         }
                        //     </div>))
                        //
                        // The C# before the template stays on this line, but the template body still needs a synthetic
                        // lambda opener so Roslyn can format any statements nested inside the template.
                        var charsAppended = AppendSyntheticLambdaBodyStart();
                        return CreateLineInfo(
                            skipNextLineIfBrace: true,
                            originOffset: 1,
                            formattedLength: span.Length,
                            formattedOffsetFromEndOfLine: charsAppended,
                            processFormatting: true,
                            // We turn off check for new lines because that only works if the content doesn't change from the original,
                            // but we're deliberately leaving out a bunch of the original file because it would confuse the Roslyn formatter.
                            checkForNewLines: false);
                    }

                    // Append a comment at the end so whitespace isn't removed, as Roslyn thinks its the end of the line, but we know it isn't.
                    _builder.AppendLine(" //");

                    return CreateLineInfo(
                        formattedLength: span.Length,
                        formattedOffsetFromEndOfLine: 3,
                        originOffset: 1,
                        processFormatting: true,
                        // We turn off check for new lines because that only works if the content doesn't change from the original,
                        // but we're deliberately leaving out a bunch of the original file because it would confuse the Roslyn formatter.
                        checkForNewLines: false);
                }

                // Multi-line expressions are often not formatted by Roslyn much at all, but it will often move subsequent lines
                // relative to the first, so make sure we include the users indentation so everything moves together, and is stable.
                _builder.Append(_sourceText.ToString(TextSpan.FromBounds(_currentLine.Start, _currentFirstNonWhitespacePosition)));
                _builder.AppendLine(_sourceText.ToString(TextSpan.FromBounds(_currentToken.Position + 1, _currentLine.End)));
                return CreateLineInfo(
                    processFormatting: true,
                    checkForNewLines: true,
                    originOffset: 1,
                    formattedOffset: 0);
            }

            public override LineInfo VisitCSharpCodeBlock(CSharpCodeBlockSyntax node)
            {
                // Matches things like @if, so skip the first character, but output as C# otherwise
                // Make sure to output leading whitespace, if any, as Roslyn can move multi-line constructs relative to the first
                // line, and if we don't maintain input whitespace, we're effectively de-denting, which means when it re-indents,
                // Roslyn will indent other lines, causing them to migrate right over time.
                _builder.Append(_sourceText.ToString(TextSpan.FromBounds(_currentLine.Start, _currentFirstNonWhitespacePosition)));
                _builder.AppendLine(_sourceText.ToString(TextSpan.FromBounds(_currentToken.Position + 1, _currentLine.End)));

                return CreateLineInfo(
                    processFormatting: true,
                    checkForNewLines: true,
                    originOffset: 1,
                    formattedOffset: 0);
            }

            public override LineInfo VisitCSharpStatement(CSharpStatementSyntax node)
            {
                // Matches "@{".
                // Logically we can just output an open brace, but there is one quirk we have to handle, which is if there is nothing but
                // whitespace between the open and close braces, then we have to check if they're on the same line. Otherwise we'd output
                // and open brace and there would be no matching close. Fortunately in this situation we don't actually have to do anything
                // because an empty set of braces has no flow on effects. If we wanted to be opinionated and shrink the whitespace down
                // to a single space, that would be a job for the RazorFormattingPass.
                var body = (CSharpStatementBodySyntax)node.Body;
                if (GetLineNumber(body.OpenBrace) == GetLineNumber(body.CloseBrace))
                {
                    return EmitCurrentLineAsComment();
                }

                // We don't need to worry about formatting, or offsetting, because the RazorFormattingPass will
                // have ensured this node is followed by a newline, and if there was a space between the "@" and "{"
                // then it wouldn't be a CSharpStatementSyntax so we wouldn't be here!
                return EmitOpenBraceLine();
            }

            public override LineInfo VisitRazorDirective(RazorDirectiveSyntax node)
            {
                // Unfortunately the Razor syntax tree doesn't distinguish many different directives with different syntax node types,
                // so this method is handles way more cases that ideally it would. Sorry! I've split it up into separate methods
                // so we can pretend, for readability of those methods, if not this one.

                if (node.IsAttributeDirective(out var attribute))
                {
                    return VisitAttributeDirective(attribute);
                }

                if (node.IsConstrainedTypeParamDirective(out var typeParam, out var conditions))
                {
                    return VisitTypeParamDirective(typeParam, conditions);
                }

                if (node.IsCodeDirective() ||
                    node.IsFunctionsDirective())
                {
                    return VisitCodeOrFunctionsDirective();
                }

                // All other directives that have braces are handled here
                if (node.DirectiveBody.CSharpCode.Children.TryGetOpenBraceToken(out var brace) &&
                    // If the open brace is on the same line as the directive, then we need to ensure the contents are indented.
                    GetLineNumber(brace) == GetLineNumber(_currentToken))
                {
                    return EmitOpenBraceLine();
                }

                // If the brace is on a different line, then we don't need to do anything, as the brace will be output when
                // processing the next line.
                return EmitCurrentLineAsComment();
            }

            private LineInfo VisitCodeOrFunctionsDirective()
            {
                // If its an @code or @functions we want to wrap the contents in a class so that access modifiers
                // on any members declared within it are valid, and will be formatted as appropriate.
                // We let the users content be the class name, as it will either be "@code" or "@functions", which
                // are both valid, and it might have an open brace after it, or that might be on the next line,
                // but if we just let that flow to the generated document, we don't need to do any fancy checking.
                _builder.Append("class ");
                _builder.AppendLine(_currentLine.ToString());

                return CreateLineInfo(skipNextLineIfBrace: true);
            }

            public override LineInfo VisitRazorUsingDirective(RazorUsingDirectiveSyntax node)
            {
                // For @using we just skip over the @ and format as a C# using directive
                // "@using System" to "using System"
                // Roslyn's parser is smart enough to not care about missing semicolons.
                _builder.AppendLine(_sourceText.ToString(TextSpan.FromBounds(_currentToken.Position + 1, _currentLine.End)));
                return CreateLineInfo(
                    processFormatting: true,
                    originOffset: 1,
                    formattedOffset: 0);
            }

            private LineInfo VisitTypeParamDirective(RazorSyntaxNode typeParam, RazorSyntaxNode conditions)
            {
                // For @typeparam we just need C# to format things after the "where", so we construct a local function that looks right
                // "@typeparam T where T : IDisposable" to "void F<T>() where T : IDisposable"
                // This is one of the weirder ones.
                var methodDef = $"void F<{typeParam.GetContent()}>() ";
                _builder.Append(methodDef);
                _builder.AppendLine(conditions.GetContent());
                _builder.AppendLine("=> null");

                return CreateLineInfo(
                    skipNextLine: true,
                    processFormatting: true,
                    originOffset: conditions.SpanStart - _currentToken.Position,
                    formattedOffset: methodDef.Length);
            }

            private LineInfo VisitAttributeDirective(RazorSyntaxNode attribute)
            {
                // For @attribute we skip over the directive itself and Roslyn can handle the rest
                // "@attribute [AttributeUsage(AttributeTargets.All)]" to "[AttributeUsage(AttributeTargets.All)]"
                // Roslyn's parser doesn't care whether the attribute is on a valid member, at least for formatting purposes
                _builder.AppendLine(attribute.GetContent());
                return CreateLineInfo(
                    processFormatting: true,
                    originOffset: attribute.SpanStart - _currentToken.Position,
                    formattedOffset: 0);
            }

            private int GetLineNumber(TextSpan span)
                => _sourceText.GetLinePositionSpan(span).Start.Line;

            private int GetLineNumber(RazorSyntaxNode node)
                => _sourceText.Lines.GetLineFromPosition(node.Position).LineNumber;

            private int GetLineNumber(RazorSyntaxToken token)
                => _sourceText.Lines.GetLineFromPosition(token.Position).LineNumber;

            private bool IsBlockLambdaStart(int startPosition, TextLine line)
            {
                // The lambda body can start on the same line (`() => {`) or on the next line (`() =>` + `{`).
                // Scan the current line backwards to the last lambda arrow, then walk forward to the next
                // non-whitespace character in the source text so we cover both shapes without allocating substrings.
                var i = line.End - 1;
                for (; i > startPosition; i--)
                {
                    if (_sourceText[i] == '>' && _sourceText[i - 1] == '=')
                    {
                        break;
                    }
                }

                if (i <= startPosition)
                {
                    return false;
                }

                for (i++; i < _sourceText.Length; i++)
                {
                    if (!char.IsWhiteSpace(_sourceText[i]))
                    {
                        return _sourceText[i] == '{';
                    }
                }

                return false;
            }

            private LineInfo EmitCurrentLineAsCSharp()
            {
                _builder.AppendLine(_currentLine.ToString());
                return CreateLineInfo(processFormatting: true, checkForNewLines: true);
            }

            private LineInfo EmitCurrentLineAsComment(int htmlIndentLevel = 0, int? additionalIndentation = null)
            {
                _builder.AppendLine($"//");
                return CreateLineInfo(htmlIndentLevel: htmlIndentLevel, additionalIndentation: additionalIndentation);
            }

            private LineInfo EmitSyntheticLambdaBodyStartLine()
            {
                // Used when Razor markup is being formatted in an expression position but still needs a statement-friendly
                // body, such as a multiline RenderFragment or `@<div> ... </div>` template.
                AppendSyntheticLambdaBodyStart();
                return CreateLineInfo(skipNextLineIfBrace: true);
            }

            private LineInfo EmitSyntheticLambdaBodyCloseLine(BaseMarkupStartTagSyntax startTag, int htmlIndentLevel, int? additionalIndentation)
            {
                _builder.Append('}');

                // Preserve any same-line C# that follows the self-closing tag, such as the `);` in
                // `Render(@<Component />);`.
                var closeAngleEnd = startTag.CloseAngle.Position + startTag.CloseAngle.Content.Length;
                if (closeAngleEnd < _currentLine.End)
                {
                    _builder.Append(_sourceText.ToString(TextSpan.FromBounds(closeAngleEnd, _currentLine.End)));
                }

                _builder.AppendLine();

                // Roslyn indents the synthetic `}` one level shallower than the attribute lines it closes, so compensate
                // here to keep the last attribute aligned with the preceding ones when we map indentation back to Razor.
                return CreateLineInfo(htmlIndentLevel: htmlIndentLevel + 1, additionalIndentation: additionalIndentation);
            }

            private int AppendSyntheticLambdaBodyStart()
            {
                _builder.AppendLine(SyntheticLambdaBodyStart);

                // Roslyn may keep the opener as `() => {` or rewrite it to:
                //
                //     () =>
                //     {
                //
                // The formatted offset tells the mapping code to ignore whichever representation Roslyn chose, because
                // the synthetic lambda opener is scaffolding for formatting and should not be copied back into Razor.
                return _csharpSyntaxFormattingOptions?.NewLines.IsFlagSet(RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody) ?? true
                    ? SyntheticLambdaSignatureLength
                    : SyntheticLambdaBodyStart.Length;
            }

            private bool TryGetMultilineTemplateClosure(RazorSyntaxNode node, out bool appendSemicolon)
            {
                appendSemicolon = false;

                // Only the last line of a multiline C# template closes the synthetic lambda body.
                if (GetContainingTemplate(node) is not { } template ||
                    GetLineNumber(template.GetFirstToken()) == GetLineNumber(template.GetLastToken()) ||
                    GetLineNumber(template.GetLastToken()) != _currentLine.LineNumber)
                {
                    return false;
                }

                // Preserve a same-line semicolon when the template ends as `</div>);` or `/>);`.
                appendSemicolon = template.GetLastToken().GetNextToken() is { } semiColonToken &&
                    semiColonToken.Content == ";" &&
                    GetLineNumber(semiColonToken) == _currentLine.LineNumber;

                return true;
            }

            private static CSharpTemplateBlockSyntax? GetContainingTemplate(RazorSyntaxNode node)
            {
                for (var current = node; current is not null; current = current.Parent as RazorSyntaxNode)
                {
                    if (current is CSharpTemplateBlockSyntax template)
                    {
                        return template;
                    }
                }

                return null;
            }

            private LineInfo EmitOpenBraceLine()
            {
                // Any open brace we emit that represents something "real" must have something after it to avoid
                // us skipping it due to SkipNextLineIfOpenBrace on the previous line.
                _builder.AppendLine("{ /* */");
                return CreateLineInfo();
            }

            private LineInfo EmitCurrentLineWithNoFormatting()
            {
                _builder.AppendLine();
                return CreateLineInfo(processIndentation: false);
            }

            private LineInfo CreateLineInfo(
                bool processIndentation = true,
                bool processFormatting = false,
                bool checkForNewLines = false,
                int? skippedPreviousLineOriginOffset = null,
                bool skipNextLine = false,
                bool skipNextLineIfBrace = false,
                int htmlIndentLevel = 0,
                int originOffset = 0,
                int formattedLength = 0,
                int formattedOffset = 0,
                int formattedOffsetFromEndOfLine = 0,
                int? additionalIndentation = null)
            {
                // We sometimes want to honour the indentation that the Html formatter supplied, when inside the right type of tag
                // but we will also have added our own C# indentation on top of that, so we need to subtract one level to compensate.
                if (additionalIndentation is null &&
                    htmlIndentLevel == 0 &&
                    _honourHtmlFormattingUntilLine is { } endLine &&
                    endLine >= _currentLine.LineNumber)
                {
                    htmlIndentLevel = FormattingUtilities.GetIndentationLevel(_currentLine, _currentFirstNonWhitespacePosition, _insertSpaces, _tabSize, out var calculatedAdditionalIndentation) - 1;
                    additionalIndentation = calculatedAdditionalIndentation;
                }

                return new(
                    ProcessIndentation: processIndentation,
                    ProcessFormatting: processFormatting,
                    CheckForNewLines: checkForNewLines,
                    SkippedPreviousLineOriginOffset: skippedPreviousLineOriginOffset,
                    SkipNextLine: skipNextLine,
                    SkipNextLineIfBrace: skipNextLineIfBrace,
                    FixedIndentLevel: htmlIndentLevel,
                    OriginOffset: originOffset,
                    FormattedLength: formattedLength,
                    FormattedOffset: formattedOffset,
                    FormattedOffsetFromEndOfLine: formattedOffsetFromEndOfLine,
                    AdditionalIndentation: additionalIndentation);
            }
        }
    }
}
