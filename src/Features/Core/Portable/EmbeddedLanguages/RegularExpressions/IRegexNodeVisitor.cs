// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;

internal interface IRegexNodeVisitor
{
    void Visit(RegexCompilationUnit node);
    void Visit(RegexSequenceNode node);
    void Visit(RegexTextNode node);
    void Visit(RegexCharacterClassNode node);
    void Visit(RegexNegatedCharacterClassNode node);
    void Visit(RegexCharacterClassRangeNode node);
    void Visit(RegexCharacterClassSubtractionNode node);
    void Visit(RegexPosixPropertyNode node);
    void Visit(RegexWildcardNode node);
    void Visit(RegexZeroOrMoreQuantifierNode node);
    void Visit(RegexOneOrMoreQuantifierNode node);
    void Visit(RegexZeroOrOneQuantifierNode node);
    void Visit(RegexLazyQuantifierNode node);
    void Visit(RegexExactNumericQuantifierNode node);
    void Visit(RegexOpenNumericRangeQuantifierNode node);
    void Visit(RegexClosedNumericRangeQuantifierNode node);
    void Visit(RegexAnchorNode node);
    void Visit(RegexAlternationNode node);
    void Visit(RegexSimpleGroupingNode node);
    void Visit(RegexSimpleOptionsGroupingNode node);
    void Visit(RegexNestedOptionsGroupingNode node);
    void Visit(RegexNonCapturingGroupingNode node);
    void Visit(RegexPositiveLookaheadGroupingNode node);
    void Visit(RegexNegativeLookaheadGroupingNode node);
    void Visit(RegexPositiveLookbehindGroupingNode node);
    void Visit(RegexNegativeLookbehindGroupingNode node);
    void Visit(RegexAtomicGroupingNode node);
    void Visit(RegexCaptureGroupingNode node);
    void Visit(RegexBalancingGroupingNode node);
    void Visit(RegexConditionalCaptureGroupingNode node);
    void Visit(RegexConditionalExpressionGroupingNode node);
    void Visit(RegexSimpleEscapeNode node);
    void Visit(RegexAnchorEscapeNode node);
    void Visit(RegexCharacterClassEscapeNode node);
    void Visit(RegexControlEscapeNode node);
    void Visit(RegexHexEscapeNode node);
    void Visit(RegexUnicodeEscapeNode node);
    void Visit(RegexCaptureEscapeNode node);
    void Visit(RegexKCaptureEscapeNode node);
    void Visit(RegexOctalEscapeNode node);
    void Visit(RegexBackreferenceEscapeNode node);
    void Visit(RegexCategoryEscapeNode node);
}
