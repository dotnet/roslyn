// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

using Microsoft.AspNetCore.Razor.Language.Syntax;

internal sealed class FormattingVisitor : SyntaxWalker
{
    private const string HtmlTag = "html";

    private readonly ImmutableArray<FormattingSpan>.Builder _spans;
    private readonly bool _inGlobalNamespace;
    private FormattingBlockKind _currentBlockKind;
    private int _currentHtmlIndentationLevel = 0;
    private int _currentRazorIndentationLevel = 0;
    private int _currentComponentIndentationLevel = 0;
    private bool _isInClassBody = false;

    private FormattingVisitor(ImmutableArray<FormattingSpan>.Builder spans, bool inGlobalNamespace)
    {
        _inGlobalNamespace = inGlobalNamespace;
        _spans = spans;
        _currentBlockKind = FormattingBlockKind.Markup;
    }

    public static void VisitRoot(
        RazorSyntaxTree syntaxTree, ImmutableArray<FormattingSpan>.Builder spans, bool inGlobalNamespace)
    {
        var visitor = new FormattingVisitor(spans, inGlobalNamespace);
        visitor.Visit(syntaxTree.Root);
    }

    public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
    {
        using (CommentBlock())
        {
            // We only want to move the start of the comment into the right spot, so we only
            // create spans for the start.
            // The body of the comment, including whitespace before the "*@" is left exactly
            // as the user has it in the file.
            AddSpan(node.StartCommentTransition, FormattingSpanKind.Transition);
            AddSpan(node.StartCommentStar, FormattingSpanKind.MetaCode);
        }
    }

    public override void VisitCSharpCodeBlock(CSharpCodeBlockSyntax node)
    {
        if (node.Parent is CSharpStatementBodySyntax or
                           CSharpImplicitExpressionBodySyntax or
                           RazorDirectiveBodySyntax ||
            (_currentBlockKind == FormattingBlockKind.Directive && node.Parent?.Parent is RazorDirectiveBodySyntax))
        {
            // If we get here, it means we don't want this code block to be considered significant.
            // Without this, we would have double indentation in places where
            // CSharpCodeBlock is used as a wrapper block in the syntax tree.

            if (node.Parent is not RazorDirectiveBodySyntax)
            {
                _currentRazorIndentationLevel++;
            }

            var isInCodeBlockDirective =
                node.Parent?.Parent?.Parent is RazorDirectiveSyntax directive &&
                directive.IsDirectiveKind(DirectiveKind.CodeBlock);

            if (isInCodeBlockDirective)
            {
                // This means this is the code portion of an @code or @functions kind of block.
                _isInClassBody = true;
            }

            base.VisitCSharpCodeBlock(node);

            if (isInCodeBlockDirective)
            {
                // Finished visiting the code portion. We are no longer in it.
                _isInClassBody = false;
            }

            if (node.Parent is not RazorDirectiveBodySyntax)
            {
                _currentRazorIndentationLevel--;
            }

            return;
        }

        using (StatementBlock())
        {
            base.VisitCSharpCodeBlock(node);
        }
    }

    public override void VisitCSharpStatement(CSharpStatementSyntax node)
    {
        using (StatementBlock())
        {
            base.VisitCSharpStatement(node);
        }
    }

    public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
    {
        using (ExpressionBlock())
        {
            base.VisitCSharpExplicitExpression(node);
        }
    }

    public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
    {
        using (ExpressionBlock())
        {
            base.VisitCSharpImplicitExpression(node);
        }
    }

    public override void VisitRazorUsingDirective(RazorUsingDirectiveSyntax node)
    {
        using (DirectiveBlock())
        {
            base.VisitRazorUsingDirective(node);
        }
    }

    public override void VisitRazorDirective(RazorDirectiveSyntax node)
    {
        using (DirectiveBlock())
        {
            base.VisitRazorDirective(node);
        }
    }

    public override void VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
    {
        using (TemplateBlock())
        {
            base.VisitCSharpTemplateBlock(node);
        }
    }

    public override void VisitMarkupBlock(MarkupBlockSyntax node)
    {
        using (MarkupBlock())
        {
            base.VisitMarkupBlock(node);
        }
    }

    public override void VisitMarkupElement(MarkupElementSyntax node)
    {
        Visit(node.StartTag);

        // Void elements, like <meta> or <input> which don't need an end tag don't cause indentation.
        // We also cheat and treat the <html> tag as a void element, so it doesn't cause indentation,
        // as that's what the Html formatter does, to avoid one level of indentation in every html file.
        var voidElement = node.StartTag is { } startTag &&
            (startTag.IsVoidElement() || string.Equals(startTag.Name.Content, HtmlTag, StringComparison.OrdinalIgnoreCase));

        if (!voidElement)
        {
            _currentHtmlIndentationLevel++;
        }

        foreach (var child in node.Body)
        {
            Visit(child);
        }

        if (!voidElement)
        {
            _currentHtmlIndentationLevel--;
        }

        Visit(node.EndTag);
    }

    public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
    {
        using (TagBlock())
        {
            var children = SyntaxUtilities.GetRewrittenMarkupStartTagChildren(node);

            foreach (var child in children)
            {
                Visit(child);
            }
        }
    }

    public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
    {
        using (TagBlock())
        {
            var children = SyntaxUtilities.GetRewrittenMarkupEndTagChildren(node);

            foreach (var child in children)
            {
                Visit(child);
            }
        }
    }

    public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
    {
        var isComponent = IsComponentTagHelperNode(node);
        // Components with cascading type parameters cause an extra level of indentation
        var componentIndentationLevels = isComponent && HasUnspecifiedCascadingTypeParameter(node) ? 2 : 1;

        var causesIndentation = isComponent;
        if (node.Parent is MarkupTagHelperElementSyntax parentComponent &&
            IsComponentTagHelperNode(parentComponent) &&
            ParentHasProperty(parentComponent, node.TagHelperInfo?.TagName))
        {
            causesIndentation = false;
        }

        Visit(node.StartTag);

        _currentHtmlIndentationLevel++;
        if (causesIndentation)
        {
            _currentComponentIndentationLevel += componentIndentationLevels;
        }

        foreach (var child in node.Body)
        {
            Visit(child);
        }

        if (causesIndentation)
        {
            Debug.Assert(_currentComponentIndentationLevel > 0, "Component indentation level should not be at 0.");
            _currentComponentIndentationLevel -= componentIndentationLevels;
        }

        _currentHtmlIndentationLevel--;

        Visit(node.EndTag);

        static bool IsComponentTagHelperNode(MarkupTagHelperElementSyntax node)
        {
            return node.TagHelperInfo.BindingResult.TagHelpers is { Count: > 0 } descriptors &&
                   descriptors.Any(static d => d.IsComponentOrChildContentTagHelper());
        }

        static bool ParentHasProperty(MarkupTagHelperElementSyntax parentComponent, string? propertyName)
        {
            // If this is a child tag helper that match a property of its parent tag helper
            // then it means this specific node won't actually cause a change in indentation.
            // For example, the following two bits of Razor generate identical C# code, even though the code block is
            // nested in a different number of tag helper elements:
            //
            // <Component>
            //     @if (true)
            //     {
            //     }
            // </Component>
            //
            // and
            //
            // <Component>
            //     <ChildContent>
            //         @if (true)
            //         {
            //         }
            //     </ChildContent>
            // </Component>
            //
            // This code will not count "ChildContent" as causing indentation because its parent
            // has a property called "ChildContent".
            if (parentComponent.TagHelperInfo.BindingResult.TagHelpers.Any(d => d.BoundAttributes.Any(a => a.Name == propertyName)))
            {
                return true;
            }

            return false;
        }

        static bool HasUnspecifiedCascadingTypeParameter(MarkupTagHelperElementSyntax node)
        {
            if (node.TagHelperStartTag is not { } startTag)
            {
                return false;
            }

            if (node.TagHelperInfo.BindingResult.TagHelpers is not { Count: > 0 } descriptors)
            {
                return false;
            }

            // A cascading type parameter will mean the generated code will get a TypeInference class generated
            // for it, which we need to account for with an extra level of indentation in our expected C# indentation
            var hasCascadingGenericParameters = descriptors.Any(static d => d.SuppliesCascadingGenericParameters());
            if (!hasCascadingGenericParameters)
            {
                return false;
            }

            // BUT, because life wasn't mean to be easy, the indentation is only affected when the developer
            // doesn't specify any type parameter in the element itself as an attribute.

            // Get all type parameters for later use. Array is fine to use as the list should be tiny (I hope!!)
            var typeParameterNames = descriptors.SelectMany(d => d.GetTypeParameters().Select(p => p.Name)).ToArray();

            var attributes = startTag.Attributes.OfType<MarkupTagHelperAttributeSyntax>();
            foreach (var attribute in attributes)
            {
                if (attribute.TagHelperAttributeInfo.Bound)
                {
                    var name = attribute.TagHelperAttributeInfo.Name;
                    if (typeParameterNames.Contains(name))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
    {
        using (TagBlock())
        {
            foreach (var child in node.LegacyChildren)
            {
                Visit(child);
            }
        }
    }

    public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
    {
        using (TagBlock())
        {
            foreach (var child in node.LegacyChildren)
            {
                Visit(child);
            }
        }
    }

    public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
    {
        using (MarkupBlock())
        {
            // For attributes, we add a single span from the start of the name prefix to the end of the value prefix.
            var spanComputer = new SpanComputer();
            spanComputer.Add(node.NamePrefix);
            spanComputer.Add(node.Name);
            spanComputer.Add(node.NameSuffix);
            spanComputer.Add(node.EqualsToken);
            spanComputer.Add(node.ValuePrefix);

            var textSpan = spanComputer.ToTextSpan();

            AddSpan(textSpan, FormattingSpanKind.Markup);

            // Visit the value and value suffix separately.
            Visit(node.Value);
            Visit(node.ValueSuffix);
        }
    }

    public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
    {
        using (TagBlock())
        {
            // For attributes, we add a single span from the start of the name prefix to the end of the value prefix.
            var spanComputer = new SpanComputer();
            spanComputer.Add(node.NamePrefix);
            spanComputer.Add(node.Name);
            spanComputer.Add(node.NameSuffix);
            spanComputer.Add(node.EqualsToken);
            spanComputer.Add(node.ValuePrefix);

            var textSpan = spanComputer.ToTextSpan();

            AddSpan(textSpan, FormattingSpanKind.Markup);

            // Visit the value and value suffix separately.
            Visit(node.Value);
            Visit(node.ValueSuffix);
        }
    }

    public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
    {
        Visit(node.Transition);
        Visit(node.Colon);
        Visit(node.Value);
    }

    public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
    {
        Visit(node.Transition);
        Visit(node.Colon);
    }

    public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
    {
        using (MarkupBlock())
        {
            // For minimized attributes, we add a single span for the attribute name along with the name prefix.
            var spanComputer = new SpanComputer();
            spanComputer.Add(node.NamePrefix);
            spanComputer.Add(node.Name);

            var textSpan = spanComputer.ToTextSpan();

            AddSpan(textSpan, FormattingSpanKind.Markup);
        }
    }

    public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
    {
        using (HtmlCommentBlock())
        {
            base.VisitMarkupCommentBlock(node);
        }
    }

    public override void VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
    {
        using (MarkupBlock())
        {
            base.VisitMarkupDynamicAttributeValue(node);
        }
    }

    public override void VisitMarkupTagHelperAttributeValue(MarkupTagHelperAttributeValueSyntax node)
    {
        using (MarkupBlock())
        {
            base.VisitMarkupTagHelperAttributeValue(node);
        }
    }

    public override void VisitRazorMetaCode(RazorMetaCodeSyntax node)
    {
        if (node.Parent is MarkupTagHelperDirectiveAttributeSyntax { TagHelperAttributeInfo.Bound: true })
        {
            // For @bind attributes we want to pretend that we're in a Html context, so write this span as markup
            AddSpan(node, FormattingSpanKind.Markup);
        }
        else
        {
            AddSpan(node, FormattingSpanKind.MetaCode);
        }

        base.VisitRazorMetaCode(node);
    }

    public override void VisitCSharpTransition(CSharpTransitionSyntax node)
    {
        AddSpan(node, FormattingSpanKind.Transition);
        base.VisitCSharpTransition(node);
    }

    public override void VisitMarkupTransition(MarkupTransitionSyntax node)
    {
        AddSpan(node, FormattingSpanKind.Transition);
        base.VisitMarkupTransition(node);
    }

    public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
    {
        // Workaround for a quirk of runtime code gen, where an empty marker is inserted before the close brace
        // of an explicit expression. ie, at the $$ below:
        //
        // @{
        //     something
        // $$}
        //
        // Writing a span for this empty marker will cause the close brace to be incorrectly indented, because its seen as
        // being "inside" the block.
        if (node.LiteralTokens is not [{ Kind: SyntaxKind.Marker }])
        {
            AddSpan(node, FormattingSpanKind.Code);
        }

        base.VisitCSharpStatementLiteral(node);
    }

    public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
    {
        AddSpan(node, FormattingSpanKind.Code);
        base.VisitCSharpExpressionLiteral(node);
    }

    public override void VisitCSharpEphemeralTextLiteral(CSharpEphemeralTextLiteralSyntax node)
    {
        AddSpan(node, FormattingSpanKind.Code);
        base.VisitCSharpEphemeralTextLiteral(node);
    }

    public override void VisitUnclassifiedTextLiteral(UnclassifiedTextLiteralSyntax node)
    {
        AddSpan(node, FormattingSpanKind.None);
        base.VisitUnclassifiedTextLiteral(node);
    }

    public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
    {
        AddSpan(node, FormattingSpanKind.Markup);
        base.VisitMarkupLiteralAttributeValue(node);
    }

    public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
    {
        if (node.Parent is MarkupLiteralAttributeValueSyntax)
        {
            base.VisitMarkupTextLiteral(node);
            return;
        }

        AddSpan(node, FormattingSpanKind.Markup);
        base.VisitMarkupTextLiteral(node);
    }

    public override void VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
    {
        AddSpan(node, FormattingSpanKind.Markup);
        base.VisitMarkupEphemeralTextLiteral(node);
    }

    private BlockSaver CommentBlock()
        => Block(FormattingBlockKind.Comment);

    private BlockSaver DirectiveBlock()
        => Block(FormattingBlockKind.Directive);

    private BlockSaver ExpressionBlock()
        => Block(FormattingBlockKind.Expression);

    private BlockSaver HtmlCommentBlock()
        => Block(FormattingBlockKind.HtmlComment);

    private BlockSaver MarkupBlock()
        => Block(FormattingBlockKind.Markup);

    private BlockSaver StatementBlock()
        => Block(FormattingBlockKind.Statement);

    private BlockSaver TagBlock()
        => Block(FormattingBlockKind.Tag);

    private BlockSaver TemplateBlock()
        => Block(FormattingBlockKind.Template);

    private BlockSaver Block(FormattingBlockKind kind)
    {
        var saver = new BlockSaver(this);

        _currentBlockKind = kind;

        return saver;
    }

    private readonly ref struct BlockSaver(FormattingVisitor visitor)
    {
        private readonly FormattingBlockKind _previousKind = visitor._currentBlockKind;

        public void Dispose()
        {
            visitor._currentBlockKind = _previousKind;
        }
    }

    private void AddSpan(SyntaxNode node, FormattingSpanKind kind)
    {
        if (node.IsMissing)
        {
            return;
        }

        AddSpan(node.Span, kind);
    }

    private void AddSpan(SyntaxToken token, FormattingSpanKind kind)
    {
        if (token.IsMissing)
        {
            return;
        }

        AddSpan(token.Span, kind);
    }

    private void AddSpan(TextSpan textSpan, FormattingSpanKind kind)
    {
        if (textSpan.IsEmpty)
        {
            return;
        }

        var span = new FormattingSpan(
            textSpan,
            kind,
            _currentRazorIndentationLevel,
            _currentHtmlIndentationLevel,
            IsInGlobalNamespace: _inGlobalNamespace,
            IsInClassBody: _isInClassBody,
            _currentComponentIndentationLevel);

        _spans.Add(span);
    }
}
