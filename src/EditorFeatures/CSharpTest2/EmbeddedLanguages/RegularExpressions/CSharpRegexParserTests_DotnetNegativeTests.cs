// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.RegularExpressions;

// These tests came from tests found at:
// https://github.com/dotnet/corefx/blob/main/src/System.Text.RegularExpressions/tests/
public sealed partial class CSharpRegexParserTests
{
    [Fact]
    public void NegativeTest0()
        => Test("""
            @"cat([a-\d]*)dog"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>a</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <CharacterClassEscape>
                                <BackslashToken>\</BackslashToken>
                                <TextToken>d</TextToken>
                              </CharacterClassEscape>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <Text>
                    <TextToken>dog</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "d")}" Span="[17..19)" Text="\d" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="cat([a-\d]*)dog" />
                <Capture Name="1" Span="[13..22)" Text="([a-\d]*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest1()
        => Test("""
            @"\k<1"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;1</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\k&lt;1" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest2()
        => Test("""
            @"\k<"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_named_back_reference.Replace("<", "&lt;").Replace(">", "&gt;")}" Span="[10..12)" Text="\k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\k&lt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest3()
        => Test("""
            @"\k"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_named_back_reference.Replace("<", "&lt;").Replace(">", "&gt;")}" Span="[10..12)" Text="\k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\k" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest4()
        => Test("""
            @"\1"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, "1")}" Span="[11..12)" Text="1" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\1" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest5()
        => Test("""
            @"(?')"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken />
                    <SingleQuoteToken />
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[13..14)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="(?')" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest6()
        => Test("""
            @"(?<)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[13..14)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="(?&lt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest7()
        => Test("""
            @"(?)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>?</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[11..12)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest8()
        => Test("""
            @"(?>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <AtomicGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken />
                  </AtomicGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest9()
        => Test("""
            @"(?<!"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegativeLookbehindGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <ExclamationToken>!</ExclamationToken>
                    <Sequence />
                    <CloseParenToken />
                  </NegativeLookbehindGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="(?&lt;!" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest10()
        => Test("""
            @"(?<="
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <PositiveLookbehindGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <EqualsToken>=</EqualsToken>
                    <Sequence />
                    <CloseParenToken />
                  </PositiveLookbehindGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="(?&lt;=" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest11()
        => Test("""
            @"(?!"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegativeLookaheadGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <ExclamationToken>!</ExclamationToken>
                    <Sequence />
                    <CloseParenToken />
                  </NegativeLookaheadGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?!" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest12()
        => Test("""
            @"(?="
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <PositiveLookaheadGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <EqualsToken>=</EqualsToken>
                    <Sequence />
                    <CloseParenToken />
                  </PositiveLookaheadGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?=" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest13()
        => Test("""
            @"(?imn )"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>imn</OptionsToken>
                    <CloseParenToken />
                  </SimpleOptionsGrouping>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                  <Text>
                    <TextToken>)</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[10..11)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Too_many_close_parens}" Span="[16..17)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?imn )" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest14()
        => Test("""
            @"(?imn"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>imn</OptionsToken>
                    <CloseParenToken />
                  </SimpleOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[10..11)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="(?imn" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest15()
        => Test("""
            @"(?:"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NonCapturingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <ColonToken>:</ColonToken>
                    <Sequence />
                    <CloseParenToken />
                  </NonCapturingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?:" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest16()
        => Test("""
            @"(?'cat'"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <Sequence />
                    <CloseParenToken />
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[17..17)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?'cat'" />
                <Capture Name="1" Span="[10..17)" Text="(?'cat'" />
                <Capture Name="cat" Span="[10..17)" Text="(?'cat'" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest17()
        => Test("""
            @"(?'"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken />
                    <SingleQuoteToken />
                    <Sequence />
                    <CloseParenToken />
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[10..13)" Text="(?'" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?'" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest18()
        => Test("""
            @"[^"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegatedCharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <CaretToken>^</CaretToken>
                    <Sequence />
                    <CloseBracketToken />
                  </NegatedCharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="[^" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest19()
        => Test("""
            @"[cat"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="[cat" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest20()
        => Test("""
            @"[^cat"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegatedCharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <CaretToken>^</CaretToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken />
                  </NegatedCharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[^cat" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest21()
        => Test("""
            @"[a-"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken />
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="[a-" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest22()
        => Test("""
            @"\p{"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[10..12)" Text="\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\p{" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest23()
        => Test("""
            @"\p{cat"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{cat</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[10..12)" Text="\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\p{cat" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest24()
        => Test("""
            @"\k<cat"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;cat</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\k&lt;cat" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest25()
        => Test("""
            @"\p{cat}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CategoryEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                    <OpenBraceToken>{</OpenBraceToken>
                    <EscapeCategoryToken>cat</EscapeCategoryToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </CategoryEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Unknown_property_0, "cat")}}" Span="[13..16)" Text="cat" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="\p{cat}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest26()
        => Test("""
            @"\P{cat"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>P</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{cat</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[10..12)" Text="\P" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\P{cat" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest27()
        => Test("""
            @"\P{cat}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CategoryEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>P</TextToken>
                    <OpenBraceToken>{</OpenBraceToken>
                    <EscapeCategoryToken>cat</EscapeCategoryToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </CategoryEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Unknown_property_0, "cat")}}" Span="[13..16)" Text="cat" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="\P{cat}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest28()
        => Test("""
            @"("
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[11..11)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..11)" Text="(" />
                <Capture Name="1" Span="[10..11)" Text="(" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest29()
        => Test("""
            @"(?"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>?</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[10..11)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[11..12)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="(?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest30()
        => Test("""
            @"(?<"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken />
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[10..13)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?&lt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest31()
        => Test("""
            @"(?<cat>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken />
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[17..17)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?&lt;cat&gt;" />
                <Capture Name="1" Span="[10..17)" Text="(?&lt;cat&gt;" />
                <Capture Name="cat" Span="[10..17)" Text="(?&lt;cat&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest32()
        => Test("""
            @"\P{"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>P</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[10..12)" Text="\P" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\P{" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest33()
        => Test("""
            @"\k<>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;&gt;</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\k&lt;&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest34()
        => Test("""
            @"(?("
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence />
                      <CloseParenToken />
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?(" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest35()
        => Test("""
            @"(?()|"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence />
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Alternation>
                      <Sequence />
                      <BarToken>|</BarToken>
                      <Sequence />
                    </Alternation>
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="(?()|" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest36()
        => Test("""
            @"?(a|b)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Alternation>
                      <Sequence>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <Text>
                          <TextToken>b</TextToken>
                        </Text>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="?(a|b)" />
                <Capture Name="1" Span="[11..16)" Text="(a|b)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest37()
        => Test("""
            @"?((a)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <SimpleGrouping>
                        <OpenParenToken>(</OpenParenToken>
                        <Sequence>
                          <Text>
                            <TextToken>a</TextToken>
                          </Text>
                        </Sequence>
                        <CloseParenToken>)</CloseParenToken>
                      </SimpleGrouping>
                    </Sequence>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="?((a)" />
                <Capture Name="1" Span="[11..15)" Text="((a)" />
                <Capture Name="2" Span="[12..15)" Text="(a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest38()
        => Test("""
            @"?((a)a"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <SimpleGrouping>
                        <OpenParenToken>(</OpenParenToken>
                        <Sequence>
                          <Text>
                            <TextToken>a</TextToken>
                          </Text>
                        </Sequence>
                        <CloseParenToken>)</CloseParenToken>
                      </SimpleGrouping>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[16..16)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="?((a)a" />
                <Capture Name="1" Span="[11..16)" Text="((a)a" />
                <Capture Name="2" Span="[12..15)" Text="(a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest39()
        => Test("""
            @"?((a)a|"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Alternation>
                      <Sequence>
                        <SimpleGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <Sequence>
                            <Text>
                              <TextToken>a</TextToken>
                            </Text>
                          </Sequence>
                          <CloseParenToken>)</CloseParenToken>
                        </SimpleGrouping>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence />
                    </Alternation>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[17..17)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="?((a)a|" />
                <Capture Name="1" Span="[11..17)" Text="((a)a|" />
                <Capture Name="2" Span="[12..15)" Text="(a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest40()
        => Test("""
            @"?((a)a|b"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Alternation>
                      <Sequence>
                        <SimpleGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <Sequence>
                            <Text>
                              <TextToken>a</TextToken>
                            </Text>
                          </Sequence>
                          <CloseParenToken>)</CloseParenToken>
                        </SimpleGrouping>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <Text>
                          <TextToken>b</TextToken>
                        </Text>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[18..18)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="?((a)a|b" />
                <Capture Name="1" Span="[11..18)" Text="((a)a|b" />
                <Capture Name="2" Span="[12..15)" Text="(a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest41()
        => Test("""
            @"(?(?i))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>i</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?i))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest42()
        => Test("""
            @"?(a)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="?(a)" />
                <Capture Name="1" Span="[11..14)" Text="(a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest43()
        => Test("""
            @"(?(?I))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>I</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?I))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest44()
        => Test("""
            @"(?(?M))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>M</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?M))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest45()
        => Test("""
            @"(?(?s))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>s</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?s))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest46()
        => Test("""
            @"(?(?S))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>S</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?S))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest47()
        => Test("""
            @"(?(?x))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>x</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?x))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest48()
        => Test("""
            @"(?(?X))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>X</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?X))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest49()
        => Test("""
            @"(?(?n))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>n</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?n))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest50()
        => Test("""
            @"(?(?m))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>m</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?m))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest51()
        => Test("""
            @"[a"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="[a" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest52()
        => Test("""
            @"?(a:b)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>a:b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="?(a:b)" />
                <Capture Name="1" Span="[11..16)" Text="(a:b)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest53()
        => Test("""
            @"(?(?"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken />
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[12..13)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[13..14)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="(?(?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest54()
        => Test("""
            @"(?(cat"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>cat</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken />
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[16..16)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="(?(cat" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest55()
        => Test("""
            @"(?(cat)|"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>cat</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Alternation>
                      <Sequence />
                      <BarToken>|</BarToken>
                      <Sequence />
                    </Alternation>
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[18..18)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="(?(cat)|" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest56()
        => Test("""
            @"foo(?<0>bar)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>foo</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>bar</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Capture_number_cannot_be_zero}" Span="[16..17)" Text="0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="foo(?&lt;0&gt;bar)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest57()
        => Test("""
            @"foo(?'0'bar)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>foo</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <NumberToken value="0">0</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <Sequence>
                      <Text>
                        <TextToken>bar</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Capture_number_cannot_be_zero}" Span="[16..17)" Text="0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="foo(?'0'bar)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest58()
        => Test("""
            @"foo(?<1bar)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>foo</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>bar</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[17..18)" Text="b" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="foo(?&lt;1bar)" />
                <Capture Name="1" Span="[13..21)" Text="(?&lt;1bar)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest59()
        => Test("""
            @"foo(?'1bar)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>foo</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <NumberToken value="1">1</NumberToken>
                    <SingleQuoteToken />
                    <Sequence>
                      <Text>
                        <TextToken>bar</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[17..18)" Text="b" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="foo(?'1bar)" />
                <Capture Name="1" Span="[13..21)" Text="(?'1bar)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest60()
        => Test("""
            @"(?("
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence />
                      <CloseParenToken />
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="(?(" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest61()
        => Test("""
            @"\p{klsak"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{klsak</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[10..12)" Text="\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="\p{klsak" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest62()
        => Test("""
            @"(?c:cat)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>?</TextToken>
                      </Text>
                      <Text>
                        <TextToken>c:cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[10..11)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[11..12)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="(?c:cat)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest63()
        => Test("""
            @"(??e:cat)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrOneQuantifier>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <QuestionToken>?</QuestionToken>
                      </ZeroOrOneQuantifier>
                      <Text>
                        <TextToken>e:cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[10..11)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[11..12)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="(??e:cat)" />
                <Capture Name="1" Span="[10..19)" Text="(??e:cat)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest64()
        => Test("""
            @"[a-f-[]]+"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>f</TextToken>
                        </Text>
                      </CharacterClassRange>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>]</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                      <Text>
                        <TextToken>+</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.A_subtraction_must_be_the_last_element_in_a_character_class}" Span="[14..14)" Text="" />
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[19..19)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[a-f-[]]+" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest65()
        => Test("""
            @"[A-[]+"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>A</TextToken>
                      </Text>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>]+</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken />
                        </CharacterClass>
                      </CharacterClassSubtraction>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[16..16)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[A-[]+" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest66()
        => Test("""
            @"(?(?e))"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>e</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[12..13)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?e))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest67()
        => Test("""
            @"(?(?a)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[12..13)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[13..14)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[16..16)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="(?(?a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest68()
        => Test("""
            @"(?r:cat)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>?</TextToken>
                      </Text>
                      <Text>
                        <TextToken>r:cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[10..11)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[11..12)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="(?r:cat)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest69()
        => Test("""
            @"(?(?N))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>N</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="Unrecognized grouping construct" Span="[12..13)" Text="(" />
                <Diagnostic Message="Quantifier '?' following nothing" Span="[13..14)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(?(?N))" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest70()
        => Test("""
            @"[]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>]</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="[]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest71()
        => Test("""
            @"\x2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>2</TextToken>
                  </HexEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..13)" Text="\x2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\x2" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest72()
        => Test("""
            @"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>
                        <Trivia>
                          <WhitespaceTrivia> </WhitespaceTrivia>
                          <CommentTrivia>(?#cat)</CommentTrivia>
                          <WhitespaceTrivia>    </WhitespaceTrivia>
                        </Trivia>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                    <CommentTrivia>(?#followed by 1 or more whitespace</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_regex_comment}" Span="[31..66)" Text="(?#followed by 1 or more whitespace" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..66)" Text="(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void NegativeTest73()
        => Test("""
            @"cat(?(?afdcat)dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>afdcat</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[15..16)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[16..17)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..28)" Text="cat(?(?afdcat)dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest74()
        => Test("""
            @"cat(?(?<cat>cat)dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <LessThanToken>&lt;</LessThanToken>
                      <CaptureNameToken value="cat">cat</CaptureNameToken>
                      <GreaterThanToken>&gt;</GreaterThanToken>
                      <Sequence>
                        <Text>
                          <TextToken>cat</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </CaptureGrouping>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[13..14)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..30)" Text="cat(?(?&lt;cat&gt;cat)dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest75()
        => Test("""
            @"cat(?(?'cat'cat)dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <SingleQuoteToken>'</SingleQuoteToken>
                      <CaptureNameToken value="cat">cat</CaptureNameToken>
                      <SingleQuoteToken>'</SingleQuoteToken>
                      <Sequence>
                        <Text>
                          <TextToken>cat</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </CaptureGrouping>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[13..14)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..30)" Text="cat(?(?'cat'cat)dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest76()
        => Test("""
            @"cat(?(?#COMMENT)cat)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>?</TextToken>
                        </Text>
                        <Text>
                          <TextToken>#COMMENT</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_cannot_be_comments}" Span="[13..14)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[15..16)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[16..17)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..30)" Text="cat(?(?#COMMENT)cat)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest77()
        => Test("""
            @"(?<cat>cat)\w+(?<dog-()*!@>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>w</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <SimpleGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <Sequence />
                          <CloseParenToken>)</CloseParenToken>
                        </SimpleGrouping>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                      <Text>
                        <TextToken>!@&gt;dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[31..32)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..41)" Text="(?&lt;cat&gt;cat)\w+(?&lt;dog-()*!@&gt;dog)" />
                <Capture Name="1" Span="[31..33)" Text="()" />
                <Capture Name="2" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="3" Span="[24..41)" Text="(?&lt;dog-()*!@&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..41)" Text="(?&lt;dog-()*!@&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest78()
        => Test("""
            @"(?<cat>cat)\w+(?<dog-catdog>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>w</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken value="catdog">catdog</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_name_0, "catdog")}" Span="[31..37)" Text="catdog" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..42)" Text="(?&lt;cat&gt;cat)\w+(?&lt;dog-catdog&gt;dog)" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..42)" Text="(?&lt;dog-catdog&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..42)" Text="(?&lt;dog-catdog&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest79()
        => Test("""
            @"(?<cat>cat)\w+(?<dog-1uosn>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>w</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>uosn&gt;dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[32..33)" Text="u" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..41)" Text="(?&lt;cat&gt;cat)\w+(?&lt;dog-1uosn&gt;dog)" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..41)" Text="(?&lt;dog-1uosn&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..41)" Text="(?&lt;dog-1uosn&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest80()
        => Test("""
            @"(?<cat>cat)\w+(?<dog-16>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>w</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="16">16</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, "16")}" Span="[31..33)" Text="16" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..38)" Text="(?&lt;cat&gt;cat)\w+(?&lt;dog-16&gt;dog)" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..38)" Text="(?&lt;dog-16&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..38)" Text="(?&lt;dog-16&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest81()
        => Test("""
            @"cat(?<->dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken />
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[17..18)" Text="&gt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="cat(?&lt;-&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest82()
        => Test("""
            @"cat(?<>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[16..17)" Text="&gt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="cat(?&lt;&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest83()
        => Test("""
            @"cat(?<dog<>)_*>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>&lt;&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>_</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>&gt;dog</TextToken>
                  </Text>
                  <Text>
                    <TextToken>)</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[19..20)" Text="&lt;" />
                <Diagnostic Message="{FeaturesResources.Too_many_close_parens}" Span="[28..29)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..29)" Text="cat(?&lt;dog&lt;&gt;)_*&gt;dog)" />
                <Capture Name="1" Span="[13..22)" Text="(?&lt;dog&lt;&gt;)" />
                <Capture Name="dog" Span="[13..22)" Text="(?&lt;dog&lt;&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest84()
        => Test("""
            @"cat(?<dog >)_*>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken> &gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>_</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>&gt;dog</TextToken>
                  </Text>
                  <Text>
                    <TextToken>)</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[19..20)" Text=" " />
                <Diagnostic Message="{FeaturesResources.Too_many_close_parens}" Span="[28..29)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..29)" Text="cat(?&lt;dog &gt;)_*&gt;dog)" />
                <Capture Name="1" Span="[13..22)" Text="(?&lt;dog &gt;)" />
                <Capture Name="dog" Span="[13..22)" Text="(?&lt;dog &gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest85()
        => Test("""
            @"cat(?<dog!>)_*>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>!&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>_</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>&gt;dog</TextToken>
                  </Text>
                  <Text>
                    <TextToken>)</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[19..20)" Text="!" />
                <Diagnostic Message="{FeaturesResources.Too_many_close_parens}" Span="[28..29)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..29)" Text="cat(?&lt;dog!&gt;)_*&gt;dog)" />
                <Capture Name="1" Span="[13..22)" Text="(?&lt;dog!&gt;)" />
                <Capture Name="dog" Span="[13..22)" Text="(?&lt;dog!&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest86()
        => Test("""
            @"cat(?<dog)_*>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>_</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>&gt;dog</TextToken>
                  </Text>
                  <Text>
                    <TextToken>)</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[19..20)" Text=")" />
                <Diagnostic Message="{FeaturesResources.Too_many_close_parens}" Span="[26..27)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..27)" Text="cat(?&lt;dog)_*&gt;dog)" />
                <Capture Name="1" Span="[13..20)" Text="(?&lt;dog)" />
                <Capture Name="dog" Span="[13..20)" Text="(?&lt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest87()
        => Test("""
            @"cat(?<1dog>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>dog&gt;dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[17..18)" Text="d" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="cat(?&lt;1dog&gt;dog)" />
                <Capture Name="1" Span="[13..25)" Text="(?&lt;1dog&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest88()
        => Test("""
            @"cat(?<0>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Capture_number_cannot_be_zero}" Span="[16..17)" Text="0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="cat(?&lt;0&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest89()
        => Test("""
            @"([5-\D]*)dog"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>5</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <CharacterClassEscape>
                                <BackslashToken>\</BackslashToken>
                                <TextToken>D</TextToken>
                              </CharacterClassEscape>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <Text>
                    <TextToken>dog</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "D")}" Span="[14..16)" Text="\D" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="([5-\D]*)dog" />
                <Capture Name="1" Span="[10..19)" Text="([5-\D]*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest90()
        => Test("""
            @"cat([6-\s]*)dog"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>6</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <CharacterClassEscape>
                                <BackslashToken>\</BackslashToken>
                                <TextToken>s</TextToken>
                              </CharacterClassEscape>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <Text>
                    <TextToken>dog</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "s")}" Span="[17..19)" Text="\s" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="cat([6-\s]*)dog" />
                <Capture Name="1" Span="[13..22)" Text="([6-\s]*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest91()
        => Test("""
            @"cat([c-\S]*)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>c</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <CharacterClassEscape>
                                <BackslashToken>\</BackslashToken>
                                <TextToken>S</TextToken>
                              </CharacterClassEscape>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "S")}" Span="[17..19)" Text="\S" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="cat([c-\S]*)" />
                <Capture Name="1" Span="[13..22)" Text="([c-\S]*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest92()
        => Test("""
            @"cat([7-\w]*)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>7</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <CharacterClassEscape>
                                <BackslashToken>\</BackslashToken>
                                <TextToken>w</TextToken>
                              </CharacterClassEscape>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "w")}" Span="[17..19)" Text="\w" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="cat([7-\w]*)" />
                <Capture Name="1" Span="[13..22)" Text="([7-\w]*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest93()
        => Test("""
            @"cat([a-\W]*)dog"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>a</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <CharacterClassEscape>
                                <BackslashToken>\</BackslashToken>
                                <TextToken>W</TextToken>
                              </CharacterClassEscape>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <Text>
                    <TextToken>dog</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "W")}" Span="[17..19)" Text="\W" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="cat([a-\W]*)dog" />
                <Capture Name="1" Span="[13..22)" Text="([a-\W]*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest94()
        => Test("""
            @"([f-\p{Lu}]\w*)\s([\p{Lu}]\w*)"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <CharacterClassRange>
                            <Text>
                              <TextToken>f</TextToken>
                            </Text>
                            <MinusToken>-</MinusToken>
                            <CategoryEscape>
                              <BackslashToken>\</BackslashToken>
                              <TextToken>p</TextToken>
                              <OpenBraceToken>{</OpenBraceToken>
                              <EscapeCategoryToken>Lu</EscapeCategoryToken>
                              <CloseBraceToken>}</CloseBraceToken>
                            </CategoryEscape>
                          </CharacterClassRange>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <ZeroOrMoreQuantifier>
                        <CharacterClassEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>w</TextToken>
                        </CharacterClassEscape>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>s</TextToken>
                  </CharacterClassEscape>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <CategoryEscape>
                            <BackslashToken>\</BackslashToken>
                            <TextToken>p</TextToken>
                            <OpenBraceToken>{</OpenBraceToken>
                            <EscapeCategoryToken>Lu</EscapeCategoryToken>
                            <CloseBraceToken>}</CloseBraceToken>
                          </CategoryEscape>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <ZeroOrMoreQuantifier>
                        <CharacterClassEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>w</TextToken>
                        </CharacterClassEscape>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "p")}}" Span="[14..16)" Text="\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..40)" Text="([f-\p{Lu}]\w*)\s([\p{Lu}]\w*)" />
                <Capture Name="1" Span="[10..25)" Text="([f-\p{Lu}]\w*)" />
                <Capture Name="2" Span="[27..40)" Text="([\p{Lu}]\w*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest95()
        => Test("""
            @"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <CommentTrivia>(?#cat)</CommentTrivia>
                      </Trivia>    </TextToken>
                  </Text>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?#followed by 1 or more whitespace</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_regex_comment}" Span="[31..66)" Text="(?#followed by 1 or more whitespace" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..66)" Text="(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest96()
        => Test("""
            @"([1-\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <CharacterClassRange>
                            <Text>
                              <TextToken>1</TextToken>
                            </Text>
                            <MinusToken>-</MinusToken>
                            <CategoryEscape>
                              <BackslashToken>\</BackslashToken>
                              <TextToken>P</TextToken>
                              <OpenBraceToken>{</OpenBraceToken>
                              <EscapeCategoryToken>Ll</EscapeCategoryToken>
                              <CloseBraceToken>}</CloseBraceToken>
                            </CategoryEscape>
                          </CharacterClassRange>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CategoryEscape>
                              <BackslashToken>\</BackslashToken>
                              <TextToken>p</TextToken>
                              <OpenBraceToken>{</OpenBraceToken>
                              <EscapeCategoryToken>Ll</EscapeCategoryToken>
                              <CloseBraceToken>}</CloseBraceToken>
                            </CategoryEscape>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>s</TextToken>
                  </CharacterClassEscape>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <CategoryEscape>
                            <BackslashToken>\</BackslashToken>
                            <TextToken>P</TextToken>
                            <OpenBraceToken>{</OpenBraceToken>
                            <EscapeCategoryToken>Ll</EscapeCategoryToken>
                            <CloseBraceToken>}</CloseBraceToken>
                          </CategoryEscape>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CategoryEscape>
                              <BackslashToken>\</BackslashToken>
                              <TextToken>p</TextToken>
                              <OpenBraceToken>{</OpenBraceToken>
                              <EscapeCategoryToken>Ll</EscapeCategoryToken>
                              <CloseBraceToken>}</CloseBraceToken>
                            </CategoryEscape>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "P")}}" Span="[14..16)" Text="\P" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..52)" Text="([1-\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)" />
                <Capture Name="1" Span="[10..31)" Text="([1-\P{Ll}][\p{Ll}]*)" />
                <Capture Name="2" Span="[33..52)" Text="([\P{Ll}][\p{Ll}]*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest97()
        => Test("""
            @"[\P]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>P</TextToken>
                      </SimpleEscape>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Incomplete_character_escape}" Span="[11..13)" Text="\P" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="[\P]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest98()
        => Test("""
            @"([\pcat])"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <SimpleEscape>
                            <BackslashToken>\</BackslashToken>
                            <TextToken>p</TextToken>
                          </SimpleEscape>
                          <Text>
                            <TextToken>cat</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_character_escape}" Span="[12..14)" Text="\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="([\pcat])" />
                <Capture Name="1" Span="[10..19)" Text="([\pcat])" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest99()
        => Test("""
            @"([\Pcat])"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <SimpleEscape>
                            <BackslashToken>\</BackslashToken>
                            <TextToken>P</TextToken>
                          </SimpleEscape>
                          <Text>
                            <TextToken>cat</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_character_escape}" Span="[12..14)" Text="\P" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="([\Pcat])" />
                <Capture Name="1" Span="[10..19)" Text="([\Pcat])" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest100()
        => Test("""
            @"(\p{"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>p</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>{</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[11..13)" Text="\p" />
                <Diagnostic Message="{{FeaturesResources.Not_enough_close_parens}}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="(\p{" />
                <Capture Name="1" Span="[10..14)" Text="(\p{" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest101()
        => Test("""
            @"(\p{Ll"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>p</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>{Ll</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[11..13)" Text="\p" />
                <Diagnostic Message="{{FeaturesResources.Not_enough_close_parens}}" Span="[16..16)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="(\p{Ll" />
                <Capture Name="1" Span="[10..16)" Text="(\p{Ll" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest102()
        => Test("""
            @"(cat)([\o]*)(dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <SimpleEscape>
                              <BackslashToken>\</BackslashToken>
                              <TextToken>o</TextToken>
                            </SimpleEscape>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "o")}" Span="[18..19)" Text="o" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..27)" Text="(cat)([\o]*)(dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="2" Span="[15..22)" Text="([\o]*)" />
                <Capture Name="3" Span="[22..27)" Text="(dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest103()
        => Test("""
            @"[\p]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>p</TextToken>
                      </SimpleEscape>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Incomplete_character_escape}" Span="[11..13)" Text="\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="[\p]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest104()
        => Test("""
            @"(?<cat>cat)\s+(?<dog>dog)\kcat"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_named_back_reference.Replace("<", "&lt;").Replace(">", "&gt;")}" Span="[35..37)" Text="\k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..40)" Text="(?&lt;cat&gt;cat)\s+(?&lt;dog&gt;dog)\kcat" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest105()
        => Test("""
            @"(?<cat>cat)\s+(?<dog>dog)\k<cat2>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat2">cat2</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_name_0, "cat2")}" Span="[38..42)" Text="cat2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..43)" Text="(?&lt;cat&gt;cat)\s+(?&lt;dog&gt;dog)\k&lt;cat2&gt;" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest106()
        => Test("""
            @"(?<cat>cat)\s+(?<dog>dog)\k<8>cat"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="8">8</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, "8")}" Span="[38..39)" Text="8" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..43)" Text="(?&lt;cat&gt;cat)\s+(?&lt;dog&gt;dog)\k&lt;8&gt;cat" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest107()
        => Test("""
            @"^[abcd]{1}?*$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <LazyQuantifier>
                    <ExactNumericQuantifier>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Text>
                            <TextToken>abcd</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <OpenBraceToken>{</OpenBraceToken>
                      <NumberToken value="1">1</NumberToken>
                      <CloseBraceToken>}</CloseBraceToken>
                    </ExactNumericQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "*")}}" Span="[21..22)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="^[abcd]{1}?*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest108()
        => Test("""
            @"^[abcd]*+$"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <ZeroOrMoreQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>+</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "+")}" Span="[18..19)" Text="+" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="^[abcd]*+$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest109()
        => Test("""
            @"^[abcd]+*$"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <OneOrMoreQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "*")}" Span="[18..19)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="^[abcd]+*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest110()
        => Test("""
            @"^[abcd]?*$"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <ZeroOrOneQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <QuestionToken>?</QuestionToken>
                  </ZeroOrOneQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "*")}" Span="[18..19)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="^[abcd]?*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest111()
        => Test("""
            @"^[abcd]*?+$"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <LazyQuantifier>
                    <ZeroOrMoreQuantifier>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Text>
                            <TextToken>abcd</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <AsteriskToken>*</AsteriskToken>
                    </ZeroOrMoreQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                  <Text>
                    <TextToken>+</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "+")}" Span="[19..20)" Text="+" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="^[abcd]*?+$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest112()
        => Test("""
            @"^[abcd]+?*$"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <LazyQuantifier>
                    <OneOrMoreQuantifier>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Text>
                            <TextToken>abcd</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <PlusToken>+</PlusToken>
                    </OneOrMoreQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "*")}" Span="[19..20)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="^[abcd]+?*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest113()
        => Test("""
            @"^[abcd]{1,}?*$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <LazyQuantifier>
                    <OpenRangeNumericQuantifier>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Text>
                            <TextToken>abcd</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <OpenBraceToken>{</OpenBraceToken>
                      <NumberToken value="1">1</NumberToken>
                      <CommaToken>,</CommaToken>
                      <CloseBraceToken>}</CloseBraceToken>
                    </OpenRangeNumericQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "*")}}" Span="[22..23)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="^[abcd]{1,}?*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest114()
        => Test("""
            @"^[abcd]??*$"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <LazyQuantifier>
                    <ZeroOrOneQuantifier>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Text>
                            <TextToken>abcd</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <QuestionToken>?</QuestionToken>
                    </ZeroOrOneQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "*")}" Span="[19..20)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="^[abcd]??*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest115()
        => Test("""
            @"^[abcd]+{0,5}$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <OneOrMoreQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                  <Text>
                    <TextToken>0,5}</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "{")}}" Span="[18..19)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="^[abcd]+{0,5}$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest116()
        => Test("""
            @"^[abcd]?{0,5}$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <ZeroOrOneQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <QuestionToken>?</QuestionToken>
                  </ZeroOrOneQuantifier>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                  <Text>
                    <TextToken>0,5}</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "{")}}" Span="[18..19)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="^[abcd]?{0,5}$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest117()
        => Test("""
            @"\u"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken />
                  </UnicodeEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..12)" Text="\u" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\u" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest118()
        => Test("""
            @"\ua"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken>a</TextToken>
                  </UnicodeEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..13)" Text="\ua" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\ua" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest119()
        => Test("""
            @"\u0"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken>0</TextToken>
                  </UnicodeEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..13)" Text="\u0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\u0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest120()
        => Test("""
            @"\x"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken />
                  </HexEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..12)" Text="\x" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\x" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest121()
        => Test("""
            @"^[abcd]*{0,5}$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <ZeroOrMoreQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                  <Text>
                    <TextToken>0,5}</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "{")}}" Span="[18..19)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="^[abcd]*{0,5}$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest122()
        => Test("""
            @"["
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence />
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[11..11)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..11)" Text="[" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest123()
        => Test("""
            @"^[abcd]{0,16}?*$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <LazyQuantifier>
                    <ClosedRangeNumericQuantifier>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Text>
                            <TextToken>abcd</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </CharacterClass>
                      <OpenBraceToken>{</OpenBraceToken>
                      <NumberToken value="0">0</NumberToken>
                      <CommaToken>,</CommaToken>
                      <NumberToken value="16">16</NumberToken>
                      <CloseBraceToken>}</CloseBraceToken>
                    </ClosedRangeNumericQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "*")}}" Span="[24..25)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..26)" Text="^[abcd]{0,16}?*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest124()
        => Test("""
            @"^[abcd]{1,}*$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <OpenRangeNumericQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="1">1</NumberToken>
                    <CommaToken>,</CommaToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </OpenRangeNumericQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "*")}}" Span="[21..22)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="^[abcd]{1,}*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest125()
        => Test("""
            @"(?<cat>cat)\s+(?<dog>dog)\k<8>cat"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="8">8</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken>cat</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, "8")}" Span="[38..39)" Text="8" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..43)" Text="(?&lt;cat&gt;cat)\s+(?&lt;dog&gt;dog)\k&lt;8&gt;cat" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void NegativeTest126()
        => Test("""
            @"(?<cat>cat)\s+(?<dog>dog)\k8"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>8</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_named_back_reference.Replace("<", "&lt;").Replace(">", "&gt;")}" Span="[35..37)" Text="\k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..38)" Text="(?&lt;cat&gt;cat)\s+(?&lt;dog&gt;dog)\k8" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest127()
        => Test("""
            @"(?<cat>cat)\s+(?<dog>dog)\k8"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="cat">cat</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="dog">dog</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>8</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_named_back_reference.Replace("<", "&lt;").Replace(">", "&gt;")}" Span="[35..37)" Text="\k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..38)" Text="(?&lt;cat&gt;cat)\s+(?&lt;dog&gt;dog)\k8" />
                <Capture Name="1" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="2" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
                <Capture Name="cat" Span="[10..21)" Text="(?&lt;cat&gt;cat)" />
                <Capture Name="dog" Span="[24..35)" Text="(?&lt;dog&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void NegativeTest128()
        => Test("""
            @"(cat)(\7)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="7">7</NumberToken>
                      </BackreferenceEscape>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, "7")}" Span="[17..18)" Text="7" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="(cat)(\7)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="2" Span="[15..19)" Text="(\7)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest129()
        => Test("""
            @"(cat)\s+(?<2147483648>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="-2147483648">2147483648</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Quantifier_and_capture_group_numbers_must_be_less_than_or_equal_to_Int32_MaxValue}" Span="[21..31)" Text="2147483648" />
              </Diagnostics>
              <Captures>
                <Capture Name="-2147483648" Span="[18..36)" Text="(?&lt;2147483648&gt;dog)" />
                <Capture Name="0" Span="[10..36)" Text="(cat)\s+(?&lt;2147483648&gt;dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest130()
        => Test("""
            @"(cat)\s+(?<21474836481097>dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <OneOrMoreQuantifier>
                    <CharacterClassEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>s</TextToken>
                    </CharacterClassEscape>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="1097">21474836481097</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Quantifier_and_capture_group_numbers_must_be_less_than_or_equal_to_Int32_MaxValue}" Span="[21..35)" Text="21474836481097" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..40)" Text="(cat)\s+(?&lt;21474836481097&gt;dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="1097" Span="[18..40)" Text="(?&lt;21474836481097&gt;dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest131()
        => Test("""
            @"^[abcd]{1}*$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <ExactNumericQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="1">1</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ExactNumericQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "*")}}" Span="[20..21)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="^[abcd]{1}*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest132()
        => Test("""
            @"(cat)(\c*)(dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrMoreQuantifier>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken />
                        </ControlEscape>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[18..19)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="(cat)(\c*)(dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="2" Span="[15..20)" Text="(\c*)" />
                <Capture Name="3" Span="[20..25)" Text="(dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest133()
        => Test("""
            @"(cat)(\c *)(dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ControlEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>c</TextToken>
                        <TextToken />
                      </ControlEscape>
                      <ZeroOrMoreQuantifier>
                        <Text>
                          <TextToken> </TextToken>
                        </Text>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[18..19)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..26)" Text="(cat)(\c *)(dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="2" Span="[15..21)" Text="(\c *)" />
                <Capture Name="3" Span="[21..26)" Text="(dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest134()
        => Test("""
            @"(cat)(\c?*)(dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ZeroOrOneQuantifier>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken />
                        </ControlEscape>
                        <QuestionToken>?</QuestionToken>
                      </ZeroOrOneQuantifier>
                      <Text>
                        <TextToken>*</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[18..19)" Text="?" />
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "*")}" Span="[19..20)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..26)" Text="(cat)(\c?*)(dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="2" Span="[15..21)" Text="(\c?*)" />
                <Capture Name="3" Span="[21..26)" Text="(dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest135()
        => Test("""
            @"(cat)(\c`*)(dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ControlEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>c</TextToken>
                        <TextToken />
                      </ControlEscape>
                      <ZeroOrMoreQuantifier>
                        <Text>
                          <TextToken>`</TextToken>
                        </Text>
                        <AsteriskToken>*</AsteriskToken>
                      </ZeroOrMoreQuantifier>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[18..19)" Text="`" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..26)" Text="(cat)(\c`*)(dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="2" Span="[15..21)" Text="(\c`*)" />
                <Capture Name="3" Span="[21..26)" Text="(dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest136()
        => Test("""
            @"(cat)(\c\|*)(dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Alternation>
                      <Sequence>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>\</TextToken>
                        </ControlEscape>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <Text>
                          <TextToken>*</TextToken>
                        </Text>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>dog</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '*')}" Span="[20..21)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..27)" Text="(cat)(\c\|*)(dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="2" Span="[15..22)" Text="(\c\|*)" />
                <Capture Name="3" Span="[22..27)" Text="(dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest137()
        => Test("""
            @"(cat)(\c\[*)(dog)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ControlEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>c</TextToken>
                        <TextToken>\</TextToken>
                      </ControlEscape>
                      <CharacterClass>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Text>
                            <TextToken>*)(dog)</TextToken>
                          </Text>
                        </Sequence>
                        <CloseBracketToken />
                      </CharacterClass>
                    </Sequence>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[27..27)" Text="" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[27..27)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..27)" Text="(cat)(\c\[*)(dog)" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
                <Capture Name="2" Span="[15..27)" Text="(\c\[*)(dog)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest138()
        => Test("""
            @"^[abcd]{0,16}*$"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <StartAnchor>
                    <CaretToken>^</CaretToken>
                  </StartAnchor>
                  <ClosedRangeNumericQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>abcd</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="0">0</NumberToken>
                    <CommaToken>,</CommaToken>
                    <NumberToken value="16">16</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ClosedRangeNumericQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "*")}}" Span="[23..24)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="^[abcd]{0,16}*$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void NegativeTest139()
        => Test("""
            @"(cat)\c"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>cat</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken />
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Missing_control_character}" Span="[16..17)" Text="c" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="(cat)\c" />
                <Capture Name="1" Span="[10..15)" Text="(cat)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);
}
