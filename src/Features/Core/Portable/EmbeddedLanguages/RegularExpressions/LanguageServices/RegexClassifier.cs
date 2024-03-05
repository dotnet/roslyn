// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;

using static EmbeddedSyntaxHelpers;

using RegexToken = EmbeddedSyntaxToken<RegexKind>;
using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

/// <summary>
/// Classifier impl for embedded regex strings.
/// </summary>
[ExtensionOrder(Before = PredefinedEmbeddedLanguageNames.Json)]
[ExportEmbeddedLanguageClassifier(
    PredefinedEmbeddedLanguageNames.Regex,
    [LanguageNames.CSharp, LanguageNames.VisualBasic],
    supportsUnannotatedAPIs: true, "Regex", "Regexp"), Shared]
internal sealed class RegexClassifier : IEmbeddedLanguageClassifier
{
    private static readonly ObjectPool<Visitor> s_visitorPool = SharedPools.Default<Visitor>();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RegexClassifier()
    {
    }

    public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
    {
        var info = context.Project.GetRequiredLanguageService<IEmbeddedLanguagesProvider>().EmbeddedLanguageInfo;

        var token = context.SyntaxToken;
        if (!info.IsAnyStringLiteral(token.RawKind))
            return;

        if (!context.Options.ColorizeRegexPatterns)
            return;

        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        var detector = RegexLanguageDetector.GetOrCreate(semanticModel.Compilation, info);
        var tree = detector.TryParseString(token, semanticModel, cancellationToken);
        if (tree == null)
            return;

        var visitor = s_visitorPool.Allocate();
        try
        {
            visitor.Context = context;
            AddClassifications(tree.Root, visitor, context);
        }
        finally
        {
            visitor.Context = default;
            s_visitorPool.Free(visitor);
        }
    }

    private static void AddClassifications(RegexNode node, Visitor visitor, EmbeddedLanguageClassificationContext context)
    {
        node.Accept(visitor);

        foreach (var child in node)
        {
            if (child.IsNode)
            {
                AddClassifications(child.Node, visitor, context);
            }
            else
            {
                AddTriviaClassifications(child.Token, context);
            }
        }
    }

    private static void AddTriviaClassifications(RegexToken token, EmbeddedLanguageClassificationContext context)
    {
        foreach (var trivia in token.LeadingTrivia)
            AddTriviaClassifications(trivia, context);
    }

    private static void AddTriviaClassifications(RegexTrivia trivia, EmbeddedLanguageClassificationContext context)
    {
        if (trivia.Kind == RegexKind.CommentTrivia &&
            trivia.VirtualChars.Length > 0)
        {
            context.AddClassification(ClassificationTypeNames.RegexComment, GetSpan(trivia.VirtualChars));
        }
    }

    private class Visitor : IRegexNodeVisitor
    {
        public EmbeddedLanguageClassificationContext Context;

        private void AddClassification(RegexToken token, string typeName)
        {
            if (!token.IsMissing)
                Context.AddClassification(typeName, token.GetSpan());
        }

        private void ClassifyWholeNode(RegexNode node, string typeName)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    ClassifyWholeNode(child.Node, typeName);
                }
                else
                {
                    AddClassification(child.Token, typeName);
                }
            }
        }

        public void Visit(RegexCompilationUnit node)
        {
            // Nothing to highlight.
        }

        public void Visit(RegexSequenceNode node)
        {
            // Nothing to highlight.   
        }

        #region Character classes

        public void Visit(RegexWildcardNode node)
            => AddClassification(node.DotToken, ClassificationTypeNames.RegexCharacterClass);

        public void Visit(RegexCharacterClassNode node)
        {
            AddClassification(node.OpenBracketToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CloseBracketToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RegexNegatedCharacterClassNode node)
        {
            AddClassification(node.OpenBracketToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CaretToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CloseBracketToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RegexCharacterClassRangeNode node)
            => AddClassification(node.MinusToken, ClassificationTypeNames.RegexCharacterClass);

        public void Visit(RegexCharacterClassSubtractionNode node)
            => AddClassification(node.MinusToken, ClassificationTypeNames.RegexCharacterClass);

        public void Visit(RegexCharacterClassEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexCharacterClass);

        public void Visit(RegexCategoryEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexCharacterClass);

        #endregion

        #region Quantifiers

        public void Visit(RegexZeroOrMoreQuantifierNode node)
            => AddClassification(node.AsteriskToken, ClassificationTypeNames.RegexQuantifier);

        public void Visit(RegexOneOrMoreQuantifierNode node)
            => AddClassification(node.PlusToken, ClassificationTypeNames.RegexQuantifier);

        public void Visit(RegexZeroOrOneQuantifierNode node)
            => AddClassification(node.QuestionToken, ClassificationTypeNames.RegexQuantifier);

        public void Visit(RegexLazyQuantifierNode node)
            => AddClassification(node.QuestionToken, ClassificationTypeNames.RegexQuantifier);

        public void Visit(RegexExactNumericQuantifierNode node)
        {
            AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
        }

        public void Visit(RegexOpenNumericRangeQuantifierNode node)
        {
            AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CommaToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
        }

        public void Visit(RegexClosedNumericRangeQuantifierNode node)
        {
            AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CommaToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.SecondNumberToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
        }

        #endregion

        #region Groupings

        public void Visit(RegexSimpleGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexSimpleOptionsGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexNestedOptionsGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexNonCapturingGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexPositiveLookaheadGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexNegativeLookaheadGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexPositiveLookbehindGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexNegativeLookbehindGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexAtomicGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexCaptureGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexBalancingGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexConditionalCaptureGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexConditionalExpressionGroupingNode node)
            => ClassifyGrouping(node);

        // Captures and backreferences refer to groups.  So we classify them the same way as groups.
        public void Visit(RegexCaptureEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexGrouping);

        public void Visit(RegexKCaptureEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexGrouping);

        public void Visit(RegexBackreferenceEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexGrouping);

        private void ClassifyGrouping(RegexGroupingNode node)
        {
            foreach (var child in node)
            {
                if (!child.IsNode)
                {
                    AddClassification(child.Token, ClassificationTypeNames.RegexGrouping);
                }
            }
        }

        #endregion

        #region Other Escapes

        public void Visit(RegexControlEscapeNode node)
            => ClassifyOtherEscape(node);

        public void Visit(RegexHexEscapeNode node)
            => ClassifyOtherEscape(node);

        public void Visit(RegexUnicodeEscapeNode node)
            => ClassifyOtherEscape(node);

        public void Visit(RegexOctalEscapeNode node)
            => ClassifyOtherEscape(node);

        public void ClassifyOtherEscape(RegexNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexOtherEscape);

        #endregion 

        #region Anchors

        public void Visit(RegexAnchorNode node)
            => AddClassification(node.AnchorToken, ClassificationTypeNames.RegexAnchor);

        public void Visit(RegexAnchorEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexAnchor);

        #endregion

        public void Visit(RegexTextNode node)
            => AddClassification(node.TextToken, ClassificationTypeNames.RegexText);

        public void Visit(RegexPosixPropertyNode node)
        {
            // The .NET parser just interprets the [ of the node, and skips the rest. So
            // classify the end part as a comment.
            Context.AddClassification(ClassificationTypeNames.RegexText, node.TextToken.VirtualChars[0].Span);
            Context.AddClassification(
                ClassificationTypeNames.RegexComment,
                GetSpan(node.TextToken.VirtualChars[1], node.TextToken.VirtualChars.Last()));
        }

        public void Visit(RegexAlternationNode node)
        {
            for (var i = 1; i < node.SequenceList.NodesAndTokens.Length; i += 2)
                AddClassification(node.SequenceList.NodesAndTokens[i].Token, ClassificationTypeNames.RegexAlternation);
        }

        public void Visit(RegexSimpleEscapeNode node)
            => ClassifyWholeNode(node, node.IsSelfEscape()
                ? ClassificationTypeNames.RegexSelfEscapedCharacter
                : ClassificationTypeNames.RegexOtherEscape);
    }
}
