// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class RazorCodeDocumentExtensions
{
    private enum SpanKind
    {
        Transition,
        MetaCode,
        Comment,
        Code,
        Markup,
        None
    }

    private record struct ClassifiedSpan(SourceSpan Span, SpanKind Kind);

    private sealed class ClassifiedSpanVisitor : SyntaxWalker, IPoolableObject
    {
        private enum BlockKind
        {
            // Code
            Statement,
            Directive,
            Expression,

            // Markup
            Markup,
            Template,

            // Special
            Comment,
            Tag,
            HtmlComment
        }

        // Significantly larger than DefaultPool.MaximumObjectSize as there shouldn't be much concurrency
        // of these arrays (we limit the number of pooled items to 5) and they are commonly large
        public const int MaximumObjectSize = DefaultPool.DefaultMaximumObjectSize * 32;

        private static readonly ObjectPool<ClassifiedSpanVisitor> s_pool = DefaultPool.Create(static () => new ClassifiedSpanVisitor(), poolSize: 5);

        private readonly ImmutableArray<ClassifiedSpan>.Builder _spans;

        private RazorSourceDocument _source;
        private BlockKind _currentBlockKind;

        private ClassifiedSpanVisitor()
        {
            _spans = ImmutableArray.CreateBuilder<ClassifiedSpan>();
            _source = null!;
        }

        private void Initialize(RazorSourceDocument source)
        {
            _source = source;
            _currentBlockKind = BlockKind.Markup;
        }

        public static ImmutableArray<ClassifiedSpan> VisitRoot(RazorSyntaxTree syntaxTree)
        {
            using var _ = s_pool.GetPooledObject(out var visitor);

            visitor.Initialize(syntaxTree.Source);
            visitor.Visit(syntaxTree.Root);

            return visitor.GetSpansAndClear();
        }

        private ImmutableArray<ClassifiedSpan> GetSpansAndClear()
            => _spans.ToImmutableAndClear();

        public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
        {
            using (CommentBlock())
            {
                AddSpan(node.StartCommentTransition, SpanKind.Transition);
                AddSpan(node.StartCommentStar, SpanKind.MetaCode);

                var comment = node.Comment;

                if (comment.IsMissing)
                {
                    // We need to generate a classified span at this position. So insert a marker in its place.
                    comment = SyntaxFactory.Token(SyntaxKind.Marker, parent: node, position: node.StartCommentStar.EndPosition);
                }

                AddSpan(comment, SpanKind.Comment);

                AddSpan(node.EndCommentStar, SpanKind.MetaCode);
                AddSpan(node.EndCommentTransition, SpanKind.Transition);
            }
        }

        public override void VisitCSharpCodeBlock(CSharpCodeBlockSyntax node)
        {
            if (node.Parent is CSharpStatementBodySyntax or
                               CSharpExplicitExpressionBodySyntax or
                               CSharpImplicitExpressionBodySyntax or
                               RazorDirectiveBodySyntax ||
                (_currentBlockKind == BlockKind.Directive && node.Children is [CSharpStatementLiteralSyntax]))
            {
                base.VisitCSharpCodeBlock(node);
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

        public override void VisitMarkupTagHelperAttributeValue(MarkupTagHelperAttributeValueSyntax node)
        {
            // We don't generate a classified span when the attribute value is a simple literal value.
            // This is done so we maintain the classified spans generated in 2.x which
            // used ConditionalAttributeCollapser (combines markup literal attribute values into one span with no block parent).
            if (!IsSimpleLiteralValue(node))
            {
                base.VisitMarkupTagHelperAttributeValue(node);
                return;
            }

            using (MarkupBlock())
            {
                base.VisitMarkupTagHelperAttributeValue(node);
            }

            static bool IsSimpleLiteralValue(MarkupTagHelperAttributeValueSyntax node)
            {
                return node.Children is [MarkupDynamicAttributeValueSyntax] or { Count: > 1 };
            }
        }

        public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
        {
            using (TagBlock())
            {
                var children = SyntaxUtilities.GetRewrittenMarkupStartTagChildren(node, includeEditHandler: true);
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
                var children = SyntaxUtilities.GetRewrittenMarkupEndTagChildren(node, includeEditHandler: true);

                foreach (var child in children)
                {
                    Visit(child);
                }
            }
        }

        public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
        {
            using (TagBlock())
            {
                base.VisitMarkupTagHelperElement(node);
            }
        }

        public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
        {
            foreach (var child in node.Attributes)
            {
                if (child is MarkupTagHelperAttributeSyntax or
                             MarkupTagHelperDirectiveAttributeSyntax or
                             MarkupMinimizedTagHelperDirectiveAttributeSyntax)
                {
                    Visit(child);
                }
            }
        }

        public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
        {
            // We don't want to generate a classified span for a tag helper end tag. Do nothing.
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

                var sourceSpan = spanComputer.ToSourceSpan(_source);

                AddSpan(sourceSpan, SpanKind.Markup);

                // Visit the value and value suffix separately.
                Visit(node.Value);
                Visit(node.ValueSuffix);
            }
        }

        public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
        {
            Visit(node.Value);
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

                var sourceSpan = spanComputer.ToSourceSpan(_source);

                AddSpan(sourceSpan, SpanKind.Markup);
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

        public override void VisitRazorMetaCode(RazorMetaCodeSyntax node)
        {
            AddSpan(node, SpanKind.MetaCode);
            base.VisitRazorMetaCode(node);
        }

        public override void VisitCSharpTransition(CSharpTransitionSyntax node)
        {
            AddSpan(node, SpanKind.Transition);
            base.VisitCSharpTransition(node);
        }

        public override void VisitMarkupTransition(MarkupTransitionSyntax node)
        {
            AddSpan(node, SpanKind.Transition);
            base.VisitMarkupTransition(node);
        }

        public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
        {
            AddSpan(node, SpanKind.Code);
            base.VisitCSharpStatementLiteral(node);
        }

        public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
        {
            AddSpan(node, SpanKind.Code);
            base.VisitCSharpExpressionLiteral(node);
        }

        public override void VisitCSharpEphemeralTextLiteral(CSharpEphemeralTextLiteralSyntax node)
        {
            AddSpan(node, SpanKind.Code);
            base.VisitCSharpEphemeralTextLiteral(node);
        }

        public override void VisitUnclassifiedTextLiteral(UnclassifiedTextLiteralSyntax node)
        {
            AddSpan(node, SpanKind.None);
            base.VisitUnclassifiedTextLiteral(node);
        }

        public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
        {
            AddSpan(node, SpanKind.Markup);
            base.VisitMarkupLiteralAttributeValue(node);
        }

        public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
        {
            if (node.Parent is MarkupLiteralAttributeValueSyntax)
            {
                base.VisitMarkupTextLiteral(node);
                return;
            }

            AddSpan(node, SpanKind.Markup);
            base.VisitMarkupTextLiteral(node);
        }

        public override void VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
        {
            AddSpan(node, SpanKind.Markup);
            base.VisitMarkupEphemeralTextLiteral(node);
        }

        private BlockSaver CommentBlock()
            => Block(BlockKind.Comment);

        private BlockSaver DirectiveBlock()
            => Block(BlockKind.Directive);

        private BlockSaver ExpressionBlock()
            => Block(BlockKind.Expression);

        private BlockSaver HtmlCommentBlock()
            => Block(BlockKind.HtmlComment);

        private BlockSaver MarkupBlock()
            => Block(BlockKind.Markup);

        private BlockSaver StatementBlock()
            => Block(BlockKind.Statement);

        private BlockSaver TagBlock()
            => Block(BlockKind.Tag);

        private BlockSaver TemplateBlock()
            => Block(BlockKind.Template);

        private BlockSaver Block(BlockKind kind)
        {
            var saver = new BlockSaver(this);

            _currentBlockKind = kind;

            return saver;
        }

        private readonly ref struct BlockSaver(ClassifiedSpanVisitor visitor)
        {
            private readonly BlockKind _previousKind = visitor._currentBlockKind;

            public void Dispose()
            {
                visitor._currentBlockKind = _previousKind;
            }
        }

        private void AddSpan(SyntaxNode node, SpanKind kind)
        {
            if (node.IsMissing)
            {
                return;
            }

            var nodeSpan = node.GetSourceSpan(_source);

            AddSpan(nodeSpan, kind);
        }

        private void AddSpan(SyntaxToken token, SpanKind kind)
        {
            if (token.IsMissing)
            {
                return;
            }

            var tokenSpan = token.GetSourceSpan(_source);

            AddSpan(tokenSpan, kind);
        }

        private void AddSpan(SourceSpan span, SpanKind kind)
            => _spans.Add(new(span, kind));

        void IPoolableObject.Reset()
        {
            _spans.Clear();

            if (_spans.Capacity > MaximumObjectSize)
            {
                // Differs from ArrayBuilderPool.Policy's behavior as we allow our array to grow significantly larger
                _spans.Capacity = 0;
            }

            _source = null!;
            _currentBlockKind = BlockKind.Markup;
        }
    }
}
