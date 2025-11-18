// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Text.RegularExpressions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.RegularExpressions;

// These tests were created by trying to enumerate all codepaths in the lexer/parser.
public sealed partial class CSharpRegexParserTests
{
    [Fact]
    public void TestEmpty()
        => Test("""
            ""
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[0..0)" Text="" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOneWhitespace_IgnorePatternWhitespace()
        => Test("""
            " "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text=" " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestTwoWhitespace_IgnorePatternWhitespace()
        => Test("""
            "  "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia>  </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="  " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestEmptyParenComment()
        => Test("""
            "(?#)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?#)</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?#)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestSimpleParenComment()
        => Test("""
            "(?# )"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?# )</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?# )" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnterminatedParenComment1()
        => Test("""
            "(?#"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?#</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_regex_comment}" Span="[9..12)" Text="(?#" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(?#" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnterminatedParenComment2()
        => Test("""
            "(?# "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?# </CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_regex_comment}" Span="[9..13)" Text="(?# " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?# " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestMultipleComments1()
        => Test("""
            "(?#)(?#)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?#)</CommentTrivia>
                    <CommentTrivia>(?#)</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?#)(?#)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestMultipleComments2()
        => Test("""
            "(?#)(?#)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?#)</CommentTrivia>
                    <CommentTrivia>(?#)</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?#)(?#)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestMultipleComments3()
        => Test("""
            "(?#) (?#)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <CommentTrivia>(?#)</CommentTrivia>
                      </Trivia> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?#)</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?#) (?#)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestMultipleComments4()
        => Test("""
            "(?#) (?#)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>(?#)</CommentTrivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                    <CommentTrivia>(?#)</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?#) (?#)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestDoNotTreatAsCommentAfterEscapeInCharacterClass1()
        => Test("""
            @"[a\p{Lu}(?#)b]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <CategoryEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>p</TextToken>
                        <OpenBraceToken>{</OpenBraceToken>
                        <EscapeCategoryToken>Lu</EscapeCategoryToken>
                        <CloseBraceToken>}</CloseBraceToken>
                      </CategoryEscape>
                      <Text>
                        <TextToken>(?#)b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="[a\p{Lu}(?#)b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestDoNotTreatAsCommentAfterEscapeInCharacterClass2()
        => Test("""
            @"[a\0(?#)b]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <OctalEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>0</TextToken>
                      </OctalEscape>
                      <Text>
                        <TextToken>(?#)b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[a\0(?#)b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestDoNotTreatAsCommentAfterEscapeInCharacterClass3()
        => Test("""
            @"[a\a(?#)b]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>a</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>(?#)b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[a\a(?#)b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestDoNotTreatAsCommentAfterEscapeInCharacterClass4()
        => Test("""
            @"[a\x00(?#)b]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <HexEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>x</TextToken>
                        <TextToken>00</TextToken>
                      </HexEscape>
                      <Text>
                        <TextToken>(?#)b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="[a\x00(?#)b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestDoNotTreatAsCommentAfterEscapeInCharacterClass5()
        => Test("""
            @"[a\u0000(?#)b]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <UnicodeEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>u</TextToken>
                        <TextToken>0000</TextToken>
                      </UnicodeEscape>
                      <Text>
                        <TextToken>(?#)b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="[a\u0000(?#)b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestDoNotTreatAsCommentAfterEscapeInCharacterClass6()
        => Test("""
            @"[a\](?#)b]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>]</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>(?#)b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[a\](?#)b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOpenQuestion1()
        => Test("""
            "(?"
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
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[11..11)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="(?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOpenQuestion2()
        => Test("""
            "(?"
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
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[11..11)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="(?" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestOpenQuestion3()
        => Test("""
            "(? "
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
                        <TextToken> </TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken />
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(? " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOpenQuestion4()
        => Test("""
            "(? "
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
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(? " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestSimpleOptionsNode1()
        => Test("""
            "(?i)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>i</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?i)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestSimpleOptionsNode2()
        => Test("""
            "(?im)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>im</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?im)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestSimpleOptionsNode3()
        => Test("""
            "(?im-x)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>im-x</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?im-x)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestSimpleOptionsNode4()
        => Test("""
            "(?im-x+n)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>im-x+n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?im-x+n)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOptionThatDoesNotChangeWhitespaceScanning()
        => Test("""
            "(?i) "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>i</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?i) " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOptionThatDoesChangeWhitespaceScanning()
        => Test("""
            "(?x) "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>x</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?x) " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOptionThatDoesChangeWhitespaceScanning2()
        => Test("""
            " (?x) "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>x</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text=" (?x) " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOptionThatDoesChangeWhitespaceScanning3()
        => Test("""
            " (?-x) "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text=" (?-x) " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestOptionRestoredWhenGroupPops()
        => Test("""
            " ( (?-x) ) "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>(</OpenParenToken>
                    <Sequence>
                      <SimpleOptionsGrouping>
                        <OpenParenToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>(</OpenParenToken>
                        <QuestionToken>?</QuestionToken>
                        <OptionsToken>-x</OptionsToken>
                        <CloseParenToken>)</CloseParenToken>
                      </SimpleOptionsGrouping>
                      <Text>
                        <TextToken> </TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..20)" Text=" ( (?-x) ) " />
                <Capture Name="1" Span="[10..19)" Text="( (?-x) )" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNestedOptionGroup1()
        => Test("""
            " (?-x:) "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NestedOptionsGrouping>
                    <OpenParenToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text=" (?-x:) " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNestedOptionGroup2()
        => Test("""
            " (?-x: ) "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NestedOptionsGrouping>
                    <OpenParenToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence>
                      <Text>
                        <TextToken> </TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text=" (?-x: ) " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNestedOptionGroup3()
        => Test("""
            " (?-x: (?+x: ) ) "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NestedOptionsGrouping>
                    <OpenParenToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence>
                      <Text>
                        <TextToken> </TextToken>
                      </Text>
                      <NestedOptionsGrouping>
                        <OpenParenToken>(</OpenParenToken>
                        <QuestionToken>?</QuestionToken>
                        <OptionsToken>+x</OptionsToken>
                        <ColonToken>:</ColonToken>
                        <Sequence />
                        <CloseParenToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>)</CloseParenToken>
                      </NestedOptionsGrouping>
                      <Text>
                        <TextToken> </TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                </Sequence>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..26)" Text=" (?-x: (?+x: ) ) " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestIncompleteOptionsGroup1()
        => Test("""
            "(?-x"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
                    <CloseParenToken />
                  </SimpleOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?-x" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestIncompleteOptionsGroup2()
        => Test("""
            "(?-x "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
                    <CloseParenToken />
                  </SimpleOptionsGrouping>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?-x " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestIncorrectOptionsGroup3()
        => Test("""
            "(?-x :"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
                    <CloseParenToken />
                  </SimpleOptionsGrouping>
                  <Text>
                    <TextToken> :</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?-x :" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestIncorrectOptionsGroup4()
        => Test("""
            "(?-x )"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
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
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Too_many_close_parens}" Span="[14..15)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?-x )" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestIncorrectOptionsGroup5()
        => Test("""
            "(?-x :)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-x</OptionsToken>
                    <CloseParenToken />
                  </SimpleOptionsGrouping>
                  <Text>
                    <TextToken> :</TextToken>
                  </Text>
                  <Text>
                    <TextToken>)</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Too_many_close_parens}" Span="[15..16)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?-x :)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestCloseParen()
        => Test("""
            ")"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>)</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Too_many_close_parens}" Span="[9..10)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text=")" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestSingleChar()
        => Test("""
            "a"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text="a" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestTwoCharsChar()
        => Test("""
            "ab"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>ab</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="ab" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestAsteriskQuantifier()
        => Test("""
            "a*"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="a*" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestAsteriskQuestionQuantifier()
        => Test("""
            "a*?"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <LazyQuantifier>
                    <ZeroOrMoreQuantifier>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <AsteriskToken>*</AsteriskToken>
                    </ZeroOrMoreQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="a*?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPlusQuantifier()
        => Test("""
            "a+"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OneOrMoreQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <PlusToken>+</PlusToken>
                  </OneOrMoreQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="a+" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPlusQuestionQuantifier()
        => Test("""
            "a+?"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <LazyQuantifier>
                    <OneOrMoreQuantifier>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <PlusToken>+</PlusToken>
                    </OneOrMoreQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="a+?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestQuestionQuantifier()
        => Test("""
            "a?"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ZeroOrOneQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <QuestionToken>?</QuestionToken>
                  </ZeroOrOneQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="a?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestQuestionQuestionQuantifier()
        => Test("""
            "a??"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <LazyQuantifier>
                    <ZeroOrOneQuantifier>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <QuestionToken>?</QuestionToken>
                    </ZeroOrOneQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="a??" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestEmptySimpleGroup()
        => Test("""
            "()"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="()" />
                <Capture Name="1" Span="[9..11)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestGroupWithSingleElement()
        => Test("""
            "(a)"
            """, """
            <Tree>
              <CompilationUnit>
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
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(a)" />
                <Capture Name="1" Span="[9..12)" Text="(a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestGroupWithMissingCloseParen()
        => Test("""
            "("
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[10..10)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text="(" />
                <Capture Name="1" Span="[9..10)" Text="(" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestGroupWithElementWithMissingCloseParen()
        => Test("""
            "(a"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[11..11)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="(a" />
                <Capture Name="1" Span="[9..11)" Text="(a" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void JustBar()
        => Test("""
            "|"
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Sequence />
                  <BarToken>|</BarToken>
                  <Sequence />
                </Alternation>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text="|" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void SpaceBar()
        => Test("""
            " |"
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Sequence>
                    <Text>
                      <TextToken> </TextToken>
                    </Text>
                  </Sequence>
                  <BarToken>|</BarToken>
                  <Sequence />
                </Alternation>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text=" |" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void BarSpace()
        => Test("""
            "| "
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Sequence />
                  <BarToken>|</BarToken>
                  <Sequence>
                    <Text>
                      <TextToken> </TextToken>
                    </Text>
                  </Sequence>
                </Alternation>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="| " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void SpaceBarSpace()
        => Test("""
            " | "
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Sequence>
                    <Text>
                      <TextToken> </TextToken>
                    </Text>
                  </Sequence>
                  <BarToken>|</BarToken>
                  <Sequence>
                    <Text>
                      <TextToken> </TextToken>
                    </Text>
                  </Sequence>
                </Alternation>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text=" | " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void JustBar_IgnoreWhitespace()
        => Test("""
            "|"
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Sequence />
                  <BarToken>|</BarToken>
                  <Sequence />
                </Alternation>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text="|" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void SpaceBar_IgnoreWhitespace()
        => Test("""
            " |"
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Sequence />
                  <BarToken>
                    <Trivia>
                      <WhitespaceTrivia> </WhitespaceTrivia>
                    </Trivia>|</BarToken>
                  <Sequence />
                </Alternation>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text=" |" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void BarSpace_IgnoreWhitespace()
        => Test("""
            "| "
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Sequence />
                  <BarToken>|</BarToken>
                  <Sequence />
                </Alternation>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="| " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void SpaceBarSpace_IgnoreWhitespace()
        => Test("""
            " | "
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Sequence />
                  <BarToken>
                    <Trivia>
                      <WhitespaceTrivia> </WhitespaceTrivia>
                    </Trivia>|</BarToken>
                  <Sequence />
                </Alternation>
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text=" | " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void DoubleBar()
        => Test("""
            "||"
            """, """
            <Tree>
              <CompilationUnit>
                <Alternation>
                  <Alternation>
                    <Sequence />
                    <BarToken>|</BarToken>
                    <Sequence />
                  </Alternation>
                  <BarToken>|</BarToken>
                  <Sequence />
                </Alternation>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="||" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void BarInGroup()
        => Test("""
            "(|)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Alternation>
                      <Sequence />
                      <BarToken>|</BarToken>
                      <Sequence />
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(|)" />
                <Capture Name="1" Span="[9..12)" Text="(|)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestExactNumericQuantifier()
        => Test("""
            "a{0}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ExactNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ExactNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="a{0}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOpenRangeNumericQuantifier()
        => Test("""
            "a{0,}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OpenRangeNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="0">0</NumberToken>
                    <CommaToken>,</CommaToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </OpenRangeNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="a{0,}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestClosedRangeNumericQuantifier()
        => Test("""
            "a{0,1}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ClosedRangeNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="0">0</NumberToken>
                    <CommaToken>,</CommaToken>
                    <NumberToken value="1">1</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ClosedRangeNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="a{0,1}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLargeExactRangeNumericQuantifier1()
        => Test("""
            "a{2147483647}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ExactNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="2147483647">2147483647</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ExactNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="a{2147483647}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLargeExactRangeNumericQuantifier2()
        => Test("""
            "a{2147483648}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ExactNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="-2147483648">2147483648</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ExactNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Quantifier_and_capture_group_numbers_must_be_less_than_or_equal_to_Int32_MaxValue}}" Span="[11..21)" Text="2147483648" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="a{2147483648}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLargeOpenRangeNumericQuantifier1()
        => Test("""
            "a{2147483647,}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OpenRangeNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="2147483647">2147483647</NumberToken>
                    <CommaToken>,</CommaToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </OpenRangeNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..23)" Text="a{2147483647,}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLargeOpenRangeNumericQuantifier2()
        => Test("""
            "a{2147483648,}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OpenRangeNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="-2147483648">2147483648</NumberToken>
                    <CommaToken>,</CommaToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </OpenRangeNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Quantifier_and_capture_group_numbers_must_be_less_than_or_equal_to_Int32_MaxValue}}" Span="[11..21)" Text="2147483648" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..23)" Text="a{2147483648,}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLargeClosedRangeNumericQuantifier1()
        => Test("""
            "a{0,2147483647}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ClosedRangeNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="0">0</NumberToken>
                    <CommaToken>,</CommaToken>
                    <NumberToken value="2147483647">2147483647</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ClosedRangeNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..24)" Text="a{0,2147483647}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLargeClosedRangeNumericQuantifier2()
        => Test("""
            "a{0,2147483648}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ClosedRangeNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="0">0</NumberToken>
                    <CommaToken>,</CommaToken>
                    <NumberToken value="-2147483648">2147483648</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ClosedRangeNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Quantifier_and_capture_group_numbers_must_be_less_than_or_equal_to_Int32_MaxValue}}" Span="[13..23)" Text="2147483648" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..24)" Text="a{0,2147483648}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestBadMinMaxClosedRangeNumericQuantifier()
        => Test("""
            "a{1,0}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ClosedRangeNumericQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="1">1</NumberToken>
                    <CommaToken>,</CommaToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ClosedRangeNumericQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Illegal_x_y_with_x_less_than_y.Replace(">", "&gt;")}}" Span="[13..14)" Text="0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="a{1,0}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLazyExactNumericQuantifier()
        => Test("""
            "a{0}?"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <LazyQuantifier>
                    <ExactNumericQuantifier>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <OpenBraceToken>{</OpenBraceToken>
                      <NumberToken value="0">0</NumberToken>
                      <CloseBraceToken>}</CloseBraceToken>
                    </ExactNumericQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="a{0}?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLazyOpenNumericQuantifier()
        => Test("""
            "a{0,}?"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <LazyQuantifier>
                    <OpenRangeNumericQuantifier>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <OpenBraceToken>{</OpenBraceToken>
                      <NumberToken value="0">0</NumberToken>
                      <CommaToken>,</CommaToken>
                      <CloseBraceToken>}</CloseBraceToken>
                    </OpenRangeNumericQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="a{0,}?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLazyClosedNumericQuantifier()
        => Test("""
            "a{0,1}?"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <LazyQuantifier>
                    <ClosedRangeNumericQuantifier>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <OpenBraceToken>{</OpenBraceToken>
                      <NumberToken value="0">0</NumberToken>
                      <CommaToken>,</CommaToken>
                      <NumberToken value="1">1</NumberToken>
                      <CloseBraceToken>}</CloseBraceToken>
                    </ClosedRangeNumericQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="a{0,1}?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestIncompleteNumericQuantifier1()
        => Test("""
            "a{"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="a{" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestIncompleteNumericQuantifier2()
        => Test("""
            "a{0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="a{0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestIncompleteNumericQuantifier3()
        => Test("""
            "a{0,"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0,</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="a{0," />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestIncompleteNumericQuantifier4()
        => Test("""
            "a{0,1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0,1</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="a{0,1" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNotNumericQuantifier1()
        => Test("""
            "a{0 }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0</TextToken>
                  </Text>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="a{0 }" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNotNumericQuantifier2()
        => Test("""
            "a{0, }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0,</TextToken>
                  </Text>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="a{0, }" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNotNumericQuantifier3()
        => Test("""
            "a{0 ,}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0</TextToken>
                  </Text>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>,}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="a{0 ,}" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNotNumericQuantifier4()
        => Test("""
            "a{0 ,1}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0</TextToken>
                  </Text>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>,1}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="a{0 ,1}" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNotNumericQuantifier5()
        => Test("""
            "a{0, 1}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0,</TextToken>
                  </Text>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>1}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="a{0, 1}" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNotNumericQuantifier6()
        => Test("""
            "a{0,1 }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>a{0,1</TextToken>
                  </Text>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="a{0,1 }" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41425")]
    public void TestLegalOpenCloseBrace1()
        => Test("""
            @"{}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>{}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="{}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41425")]
    public void TestLegalOpenCloseBrace2()
        => Test("""
            @"{1, 2}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>{1, 2}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="{1, 2}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41425")]
    public void TestDanglingNumericQuantifier1()
        => Test("""
            @"{1}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                  <Text>
                    <TextToken>1}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Quantifier_0_following_nothing, '{')}}" Span="[10..11)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="{1}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41425")]
    public void TestDanglingNumericQuantifier2()
        => Test("""
            @"{1,2}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                  <Text>
                    <TextToken>1,2}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Quantifier_0_following_nothing, '{')}}" Span="[10..11)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="{1,2}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestLazyQuantifierDueToIgnoredWhitespace()
        => Test("""
            "a* ?"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <LazyQuantifier>
                    <ZeroOrMoreQuantifier>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <AsteriskToken>*</AsteriskToken>
                    </ZeroOrMoreQuantifier>
                    <QuestionToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>?</QuestionToken>
                  </LazyQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="a* ?" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNonLazyQuantifierDueToNonIgnoredWhitespace()
        => Test("""
            "a* ?"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <ZeroOrOneQuantifier>
                    <Text>
                      <TextToken> </TextToken>
                    </Text>
                    <QuestionToken>?</QuestionToken>
                  </ZeroOrOneQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="a* ?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestAsteriskQuantifierAtStart()
        => Test("""
            "*"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '*')}" Span="[9..10)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text="*" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestAsteriskQuantifierAtStartOfGroup()
        => Test("""
            "(*)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>*</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '*')}" Span="[10..11)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(*)" />
                <Capture Name="1" Span="[9..12)" Text="(*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestAsteriskQuantifierAfterQuantifier()
        => Test("""
            "a**"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>*</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "*")}" Span="[11..12)" Text="*" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="a**" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPlusQuantifierAtStart()
        => Test("""
            "+"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>+</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '+')}" Span="[9..10)" Text="+" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text="+" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPlusQuantifierAtStartOfGroup()
        => Test("""
            "(+)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>+</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '+')}" Span="[10..11)" Text="+" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(+)" />
                <Capture Name="1" Span="[9..12)" Text="(+)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPlusQuantifierAfterQuantifier()
        => Test("""
            "a*+"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>+</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "+")}" Span="[11..12)" Text="+" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="a*+" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestQuestionQuantifierAtStart()
        => Test("""
            "?"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[9..10)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..10)" Text="?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestQuestionQuantifierAtStartOfGroup()
        => Test("""
            "(?)"
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
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[10..11)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(?)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestQuestionQuantifierAfterQuantifier()
        => Test("""
            "a*??"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <LazyQuantifier>
                    <ZeroOrMoreQuantifier>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <AsteriskToken>*</AsteriskToken>
                    </ZeroOrMoreQuantifier>
                    <QuestionToken>?</QuestionToken>
                  </LazyQuantifier>
                  <Text>
                    <TextToken>?</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Nested_quantifier_0, "?")}" Span="[12..13)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="a*??" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNumericQuantifierAtStart()
        => Test("""
            "{0}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                  <Text>
                    <TextToken>0}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Quantifier_0_following_nothing, '{')}}" Span="[9..10)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="{0}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNumericQuantifierAtStartOfGroup()
        => Test("""
            "({0})"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>{</TextToken>
                      </Text>
                      <Text>
                        <TextToken>0}</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Quantifier_0_following_nothing, '{')}}" Span="[10..11)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="({0})" />
                <Capture Name="1" Span="[9..14)" Text="({0})" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNumericQuantifierAfterQuantifier()
        => Test("""
            "a*{0}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                  <Text>
                    <TextToken>0}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Nested_quantifier_0, "{")}}" Span="[11..12)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="a*{0}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNonNumericQuantifierAtStart()
        => Test("""
            "{0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>{0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="{0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNonNumericQuantifierAtStartOfGroup()
        => Test("""
            "({0)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>{0</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="({0)" />
                <Capture Name="1" Span="[9..13)" Text="({0)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNonNumericQuantifierAfterQuantifier()
        => Test("""
            "a*{0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ZeroOrMoreQuantifier>
                    <Text>
                      <TextToken>a</TextToken>
                    </Text>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                  <Text>
                    <TextToken>{0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="a*{0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestEscapeAtEnd1()
        => Test("""
            @"\"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken />
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Illegal_backslash_at_end_of_pattern}" Span="[10..11)" Text="\" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..11)" Text="\" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestEscapeAtEnd2()
        => Test("""
            "\\"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken />
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Illegal_backslash_at_end_of_pattern}" Span="[9..11)" Text="\\" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..11)" Text="\\" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestSimpleEscape()
        => Test("""
            @"\w"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </CharacterClassEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\w" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPrimaryEscapes1()
        => Test("""
            @"\b\B\A\G\Z\z\w\W\s\W\s\S\d\D"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <AnchorEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>b</TextToken>
                  </AnchorEscape>
                  <AnchorEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>B</TextToken>
                  </AnchorEscape>
                  <AnchorEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>A</TextToken>
                  </AnchorEscape>
                  <AnchorEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>G</TextToken>
                  </AnchorEscape>
                  <AnchorEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>Z</TextToken>
                  </AnchorEscape>
                  <AnchorEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>z</TextToken>
                  </AnchorEscape>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </CharacterClassEscape>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>W</TextToken>
                  </CharacterClassEscape>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>s</TextToken>
                  </CharacterClassEscape>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>W</TextToken>
                  </CharacterClassEscape>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>s</TextToken>
                  </CharacterClassEscape>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>S</TextToken>
                  </CharacterClassEscape>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>d</TextToken>
                  </CharacterClassEscape>
                  <CharacterClassEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>D</TextToken>
                  </CharacterClassEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..38)" Text="\b\B\A\G\Z\z\w\W\s\W\s\S\d\D" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape1()
        => Test("""
            @"\c"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken />
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Missing_control_character}" Span="[11..12)" Text="c" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\c" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape2()
        => Test("""
            @"\c<"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken />
                  </ControlEscape>
                  <Text>
                    <TextToken>&lt;</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[12..13)" Text="&lt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c&lt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape3()
        => Test("""
            @"\ca"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>a</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\ca" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape4()
        => Test("""
            @"\cA"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>A</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\cA" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape5()
        => Test("""
            @"\c A"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken />
                  </ControlEscape>
                  <Text>
                    <TextToken> A</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[12..13)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\c A" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape6()
        => Test("""
            @"\c(a)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken />
                  </ControlEscape>
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
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[12..13)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\c(a)" />
                <Capture Name="1" Span="[12..15)" Text="(a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape7()
        => Test("""
            @"\c>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken />
                  </ControlEscape>
                  <Text>
                    <TextToken>&gt;</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[12..13)" Text="&gt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape8()
        => Test("""
            @"\c?"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ZeroOrOneQuantifier>
                    <ControlEscape>
                      <BackslashToken>\</BackslashToken>
                      <TextToken>c</TextToken>
                      <TextToken />
                    </ControlEscape>
                    <QuestionToken>?</QuestionToken>
                  </ZeroOrOneQuantifier>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[12..13)" Text="?" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c?" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape9()
        => Test("""
            @"\c@"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>@</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c@" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape10()
        => Test("""
            @"\c^"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>^</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c^" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape11()
        => Test("""
            @"\c_"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>_</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c_" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape12()
        => Test("""
            @"\c`"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken />
                  </ControlEscape>
                  <Text>
                    <TextToken>`</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[12..13)" Text="`" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c`" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape13()
        => Test("""
            @"\c{"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken />
                  </ControlEscape>
                  <Text>
                    <TextToken>{</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Unrecognized_control_character}}" Span="[12..13)" Text="{" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c{" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape14()
        => Test("""
            @"\ca"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>a</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\ca" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape15()
        => Test("""
            @"\cA"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>A</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\cA" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape16()
        => Test("""
            @"\cz"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>z</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\cz" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape17()
        => Test("""
            @"\cZ"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>Z</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\cZ" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape18()
        => Test("""
            @"\c\"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>\</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c\" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestControlEscape19()
        => Test("""
            @"\c]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ControlEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>c</TextToken>
                    <TextToken>]</TextToken>
                  </ControlEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\c]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnknownEscape1()
        => Test("""
            @"\m"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>m</TextToken>
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "m")}" Span="[11..12)" Text="m" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\m" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape1()
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
    public void TestHexEscape2()
        => Test("""
            @"\x "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken />
                  </HexEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..12)" Text="\x" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\x " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape3()
        => Test("""
            @"\x0"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>0</TextToken>
                  </HexEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..13)" Text="\x0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\x0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape4()
        => Test("""
            @"\x0 "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>0</TextToken>
                  </HexEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..13)" Text="\x0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\x0 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape5()
        => Test("""
            @"\x00"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>00</TextToken>
                  </HexEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\x00" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape6()
        => Test("""
            @"\x00 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>00</TextToken>
                  </HexEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\x00 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape7()
        => Test("""
            @"\x000"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>00</TextToken>
                  </HexEscape>
                  <Text>
                    <TextToken>0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\x000" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape8()
        => Test("""
            @"\xff"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>ff</TextToken>
                  </HexEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\xff" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape9()
        => Test("""
            @"\xFF"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>FF</TextToken>
                  </HexEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\xFF" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape10()
        => Test("""
            @"\xfF"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>fF</TextToken>
                  </HexEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\xfF" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape11()
        => Test("""
            @"\xfff"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>ff</TextToken>
                  </HexEscape>
                  <Text>
                    <TextToken>f</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\xfff" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestHexEscape12()
        => Test("""
            @"\xgg"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken />
                  </HexEscape>
                  <Text>
                    <TextToken>gg</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..12)" Text="\x" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\xgg" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnknownEscape2()
        => Test("""
            @"\m "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>m</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "m")}" Span="[11..12)" Text="m" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\m " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnicodeEscape1()
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
    public void TestUnicodeEscape2()
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
    public void TestUnicodeEscape3()
        => Test("""
            @"\u00"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken>00</TextToken>
                  </UnicodeEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..14)" Text="\u00" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\u00" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnicodeEscape4()
        => Test("""
            @"\u000"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken>000</TextToken>
                  </UnicodeEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..15)" Text="\u000" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\u000" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnicodeEscape5()
        => Test("""
            @"\u0000"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken>0000</TextToken>
                  </UnicodeEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\u0000" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnicodeEscape6()
        => Test("""
            @"\u0000 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken>0000</TextToken>
                  </UnicodeEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="\u0000 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnicodeEscape7()
        => Test("""
            @"\u "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken />
                  </UnicodeEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..12)" Text="\u" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\u " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnicodeEscape8()
        => Test("""
            @"\u0 "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken>0</TextToken>
                  </UnicodeEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..13)" Text="\u0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\u0 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestUnicodeEscape9()
        => Test("""
            @"\ugggg"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <UnicodeEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>u</TextToken>
                    <TextToken />
                  </UnicodeEscape>
                  <Text>
                    <TextToken>gggg</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[10..12)" Text="\u" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\ugggg" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape1()
        => Test("""
            @"\0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>0</TextToken>
                  </OctalEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape2()
        => Test("""
            @"\0 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>0</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\0 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape3()
        => Test("""
            @"\00"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>00</TextToken>
                  </OctalEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\00" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape4()
        => Test("""
            @"\00 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>00</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\00 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape5()
        => Test("""
            @"\000"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>000</TextToken>
                  </OctalEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\000" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape6()
        => Test("""
            @"\000 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>000</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\000 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape7()
        => Test("""
            @"\0000"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>000</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken>0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\0000" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape8()
        => Test("""
            @"\0000 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>000</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken>0 </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\0000 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape9()
        => Test("""
            @"\7"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="7">7</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 7)}" Span="[11..12)" Text="7" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\7" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape10()
        => Test("""
            @"\78"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>7</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken>8</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\78" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscape11()
        => Test("""
            @"\8"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="8">8</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 8)}" Span="[11..12)" Text="8" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\8" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestOctalEscapeEcmascript1()
        => Test("""
            @"\40"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>40</TextToken>
                  </OctalEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\40" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestOctalEscapeEcmascript2()
        => Test("""
            @"\401"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>40</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken>1</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\401" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestOctalEscapeEcmascript3()
        => Test("""
            @"\37"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>37</TextToken>
                  </OctalEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\37" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestOctalEscapeEcmascript4()
        => Test("""
            @"\371"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>371</TextToken>
                  </OctalEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\371" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestOctalEscapeEcmascript5()
        => Test("""
            @"\0000"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>000</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken>0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\0000" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureEscape1()
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
    public void TestKCaptureEscape2()
        => Test("""
            @"\k "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_named_back_reference.Replace("<", "&lt;").Replace(">", "&gt;")}" Span="[10..12)" Text="\k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\k " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEscape3()
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
    public void TestKCaptureEscape4()
        => Test("""
            @"\k< "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt; </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\k&lt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEscape5()
        => Test("""
            @"\k<0"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\k&lt;0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEscape6()
        => Test("""
            @"\k<0 "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;0 </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\k&lt;0 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEscape7()
        => Test("""
            @"\k<0>"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\k&lt;0&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEscape8()
        => Test("""
            @"\k<0> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\k&lt;0&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEscape9()
        => Test("""
            @"\k<00> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">00</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="\k&lt;00&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEscape10()
        => Test("""
            @"\k<a> "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_name_0, "a")}" Span="[13..14)" Text="a" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\k&lt;a&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEscape11()
        => Test("""
            @"(?<a>)\k<a> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="(?&lt;a&gt;)\k&lt;a&gt; " />
                <Capture Name="1" Span="[10..16)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[10..16)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureEcmaEscape1()
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
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureEcmaEscape2()
        => Test("""
            @"\k "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_named_back_reference.Replace("<", "&lt;").Replace(">", "&gt;")}" Span="[10..12)" Text="\k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\k " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureEcmaEscape3()
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
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureEcmaEscape4()
        => Test("""
            @"\k< "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt; </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\k&lt; " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureEcmaEscape5()
        => Test("""
            @"\k<0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\k&lt;0" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureEcmaEscape6()
        => Test("""
            @"\k<0 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;0 </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\k&lt;0 " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureEcmaEscape7()
        => Test("""
            @"\k<0>"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\k&lt;0&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureEcmaEscape8()
        => Test("""
            @"\k<0> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\k&lt;0&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestKCaptureQuoteEscape3()
        => Test("""
            @"\k'"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>'</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Malformed_named_back_reference.Replace("<", "&lt;").Replace(">", "&gt;")}" Span="[10..12)" Text="\k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\k'" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureQuoteEscape4()
        => Test("""
            @"\k' "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>' </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\k' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureQuoteEscape5()
        => Test("""
            @"\k'0"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>'0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\k'0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureQuoteEscape6()
        => Test("""
            @"\k'0 "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>'0 </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\k'0 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureQuoteEscape7()
        => Test("""
            @"\k'0'"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <NumberToken value="0">0</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </KCaptureEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\k'0'" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureQuoteEscape8()
        => Test("""
            @"\k'0' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <NumberToken value="0">0</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\k'0' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureQuoteEscape9()
        => Test("""
            @"\k'00' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <NumberToken value="0">00</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="\k'00' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureQuoteEscape10()
        => Test("""
            @"\k'a' "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_name_0, "a")}" Span="[13..14)" Text="a" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\k'a' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureQuoteEscape11()
        => Test("""
            @"(?<a>)\k'a' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <KCaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </KCaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="(?&lt;a&gt;)\k'a' " />
                <Capture Name="1" Span="[10..16)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[10..16)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureWrongQuote1()
        => Test("""
            @"\k<0' "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>&lt;0' </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\k&lt;0' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestKCaptureWrongQuote2()
        => Test("""
            @"\k'0> "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>k</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>'0&gt; </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Unrecognized_escape_sequence_0, "k")}" Span="[11..12)" Text="k" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\k'0&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape1()
        => Test("""
            @"\"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken />
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Illegal_backslash_at_end_of_pattern}" Span="[10..11)" Text="\" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..11)" Text="\" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape2()
        => Test("""
            @"\ "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken> </TextToken>
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\ " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape3()
        => Test("""
            @"\<"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\&lt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape4()
        => Test("""
            @"\< "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\&lt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape5()
        => Test("""
            @"\<0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\&lt;0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape6()
        => Test("""
            @"\<0 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>0 </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\&lt;0 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape7()
        => Test("""
            @"\<0>"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </CaptureEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\&lt;0&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape8()
        => Test("""
            @"\<0> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\&lt;0&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape9()
        => Test("""
            @"\<00> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">00</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\&lt;00&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape10()
        => Test("""
            @"\<a> "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_name_0, "a")}" Span="[12..13)" Text="a" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\&lt;a&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEscape11()
        => Test("""
            @"(?<a>)\<a> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="(?&lt;a&gt;)\&lt;a&gt; " />
                <Capture Name="1" Span="[10..16)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[10..16)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureEcmaEscape1()
        => Test("""
            @"\"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken />
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Illegal_backslash_at_end_of_pattern}" Span="[10..11)" Text="\" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..11)" Text="\" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCaptureEcmaEscape2()
        => Test("""
            @"\ "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken> </TextToken>
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\ " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCaptureEcmaEscape3()
        => Test("""
            @"\<"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\&lt;" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCaptureEcmaEscape4()
        => Test("""
            @"\< "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\&lt; " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCaptureEcmaEscape5()
        => Test("""
            @"\<0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\&lt;0" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCaptureEcmaEscape6()
        => Test("""
            @"\<0 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>0 </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\&lt;0 " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCaptureEcmaEscape7()
        => Test("""
            @"\<0>"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </CaptureEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\&lt;0&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCaptureEcmaEscape8()
        => Test("""
            @"\<0> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\&lt;0&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCaptureQuoteEscape3()
        => Test("""
            @"\'"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>'</TextToken>
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\'" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureQuoteEscape4()
        => Test("""
            @"\' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>'</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureQuoteEscape5()
        => Test("""
            @"\'0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>'</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\'0" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureQuoteEscape6()
        => Test("""
            @"\'0 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>'</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>0 </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\'0 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureQuoteEscape7()
        => Test("""
            @"\'0'"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <NumberToken value="0">0</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </CaptureEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="\'0'" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureQuoteEscape8()
        => Test("""
            @"\'0' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <NumberToken value="0">0</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\'0' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureQuoteEscape9()
        => Test("""
            @"\'00' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <NumberToken value="0">00</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="\'00' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureQuoteEscape10()
        => Test("""
            @"\'a' "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_name_0, "a")}" Span="[12..13)" Text="a" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\'a' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureQuoteEscape11()
        => Test("""
            @"(?<a>)\'a' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <CaptureEscape>
                    <BackslashToken>\</BackslashToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                  </CaptureEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="(?&lt;a&gt;)\'a' " />
                <Capture Name="1" Span="[10..16)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[10..16)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureWrongQuote1()
        => Test("""
            @"\<0' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>&lt;</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>0' </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\&lt;0' " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureWrongQuote2()
        => Test("""
            @"\'0> "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>'</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>0&gt; </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="\'0&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestDefinedCategoryEscape()
        => Test("""
            "\\p{Cc}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CategoryEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                    <OpenBraceToken>{</OpenBraceToken>
                    <EscapeCategoryToken>Cc</EscapeCategoryToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </CategoryEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="\\p{Cc}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestDefinedCategoryEscapeWithSpaces1()
        => Test("""
            "\\p{ Cc }"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{ Cc }</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="\\p{ Cc }" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestDefinedCategoryEscapeWithSpaces2()
        => Test("""
            "\\p{ Cc }"
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
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>Cc</TextToken>
                  </Text>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="\\p{ Cc }" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestDefinedCategoryEscapeWithSpaces3()
        => Test("""
            "\\p {Cc}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>{Cc}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Malformed_character_escape}}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="\\p {Cc}" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestUndefinedCategoryEscape()
        => Test("""
            "\\p{xxx}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CategoryEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                    <OpenBraceToken>{</OpenBraceToken>
                    <EscapeCategoryToken>xxx</EscapeCategoryToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </CategoryEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Unknown_property_0, "xxx")}}" Span="[13..16)" Text="xxx" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="\\p{xxx}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestTooShortCategoryEscape1()
        => Test("""
            "\\p"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Incomplete_character_escape}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="\\p" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestTooShortCategoryEscape2()
        => Test("""
            "\\p{"
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
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="\\p{" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestTooShortCategoryEscape3()
        => Test("""
            "\\p{}"
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="\\p{}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestTooShortCategoryEscape4()
        => Test("""
            "\\p{} "
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{} </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Unknown_property}}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="\\p{} " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestTooShortCategoryEscape5()
        => Test("""
            "\\p {} "
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken> {} </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Malformed_character_escape}}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="\\p {} " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestTooShortCategoryEscape6()
        => Test("""
            "\\p{Cc "
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                  </SimpleEscape>
                  <Text>
                    <TextToken>{Cc </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{FeaturesResources.Incomplete_character_escape}}" Span="[9..12)" Text="\\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="\\p{Cc " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCategoryNameWithDash()
        => Test("""
            "\\p{IsArabicPresentationForms-A}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CategoryEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>p</TextToken>
                    <OpenBraceToken>{</OpenBraceToken>
                    <EscapeCategoryToken>IsArabicPresentationForms-A</EscapeCategoryToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </CategoryEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..41)" Text="\\p{IsArabicPresentationForms-A}" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNonCapturingGrouping1()
        => Test("""
            "(?:)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NonCapturingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <ColonToken>:</ColonToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </NonCapturingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?:)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNonCapturingGrouping2()
        => Test("""
            "(?:a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NonCapturingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <ColonToken>:</ColonToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NonCapturingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?:a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNonCapturingGrouping3()
        => Test("""
            "(?:"
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(?:" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNonCapturingGrouping4()
        => Test("""
            "(?: "
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
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?: " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestPositiveLookaheadGrouping1()
        => Test("""
            "(?=)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <PositiveLookaheadGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <EqualsToken>=</EqualsToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </PositiveLookaheadGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?=)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPositiveLookaheadGrouping2()
        => Test("""
            "(?=a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <PositiveLookaheadGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <EqualsToken>=</EqualsToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </PositiveLookaheadGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?=a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPositiveLookaheadGrouping3()
        => Test("""
            "(?="
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(?=" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPositiveLookaheadGrouping4()
        => Test("""
            "(?= "
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
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?= " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNegativeLookaheadGrouping1()
        => Test("""
            "(?!)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegativeLookaheadGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <ExclamationToken>!</ExclamationToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </NegativeLookaheadGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?!)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNegativeLookaheadGrouping2()
        => Test("""
            "(?!a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegativeLookaheadGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <ExclamationToken>!</ExclamationToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NegativeLookaheadGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?!a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNegativeLookaheadGrouping3()
        => Test("""
            "(?!"
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(?!" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNegativeLookaheadGrouping4()
        => Test("""
            "(?! "
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
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?! " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestAtomicGrouping1()
        => Test("""
            "(?>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <AtomicGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </AtomicGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestAtomicGrouping2()
        => Test("""
            "(?>a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <AtomicGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </AtomicGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?&gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestAtomicGrouping3()
        => Test("""
            "(?>"
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(?&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestAtomicGrouping4()
        => Test("""
            "(?> "
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
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?&gt; " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestPositiveLookbehindGrouping1()
        => Test("""
            "(?<=)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <PositiveLookbehindGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <EqualsToken>=</EqualsToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </PositiveLookbehindGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?&lt;=)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPositiveLookbehindGrouping2()
        => Test("""
            "(?<=a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <PositiveLookbehindGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <EqualsToken>=</EqualsToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </PositiveLookbehindGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?&lt;=a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPositiveLookbehindGrouping3()
        => Test("""
            "(?<="
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?&lt;=" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestPositiveLookbehindGrouping4()
        => Test("""
            "(?<= "
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
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?&lt;= " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNegativeLookbehindGrouping1()
        => Test("""
            "(?<!)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegativeLookbehindGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <ExclamationToken>!</ExclamationToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </NegativeLookbehindGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?&lt;!)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNegativeLookbehindGrouping2()
        => Test("""
            "(?<!a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegativeLookbehindGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <ExclamationToken>!</ExclamationToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NegativeLookbehindGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?&lt;!a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNegativeLookbehindGrouping3()
        => Test("""
            "(?<!"
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?&lt;!" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNegativeLookbehindGrouping4()
        => Test("""
            "(?<! "
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
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?&lt;! " />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture1()
        => Test("""
            "(?<"
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
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..12)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(?&lt;" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture2()
        => Test("""
            "(?<>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken />
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[12..13)" Text="&gt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?&lt;&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture3()
        => Test("""
            "(?<a"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken />
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..12)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?&lt;a" />
                <Capture Name="1" Span="[9..13)" Text="(?&lt;a" />
                <Capture Name="a" Span="[9..13)" Text="(?&lt;a" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture4()
        => Test("""
            "(?<a>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken />
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?&lt;a&gt;" />
                <Capture Name="1" Span="[9..14)" Text="(?&lt;a&gt;" />
                <Capture Name="a" Span="[9..14)" Text="(?&lt;a&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture5()
        => Test("""
            "(?<a>a"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken />
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?&lt;a&gt;a" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;a&gt;a" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a&gt;a" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture6()
        => Test("""
            "(?<a>a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;a&gt;a)" />
                <Capture Name="1" Span="[9..16)" Text="(?&lt;a&gt;a)" />
                <Capture Name="a" Span="[9..16)" Text="(?&lt;a&gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture7()
        => Test("""
            "(?<a >a)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken> &gt;a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[13..14)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;a &gt;a)" />
                <Capture Name="1" Span="[9..17)" Text="(?&lt;a &gt;a)" />
                <Capture Name="a" Span="[9..17)" Text="(?&lt;a &gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNamedCapture8()
        => Test("""
            "(?<a >a)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>&gt;a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[13..14)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;a &gt;a)" />
                <Capture Name="1" Span="[9..17)" Text="(?&lt;a &gt;a)" />
                <Capture Name="a" Span="[9..17)" Text="(?&lt;a &gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture9()
        => Test("""
            "(?< a>a)"
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
                    <Sequence>
                      <Text>
                        <TextToken> a&gt;a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[12..13)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt; a&gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNamedCapture10()
        => Test("""
            "(?< a>a)"
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
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>a&gt;a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[12..13)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt; a&gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture11()
        => Test("""
            "(?< a >a)"
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
                    <Sequence>
                      <Text>
                        <TextToken> a &gt;a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[12..13)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?&lt; a &gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNamedCapture12()
        => Test("""
            "(?< a >a)"
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
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>a</TextToken>
                      </Text>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>&gt;a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[12..13)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?&lt; a &gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedCapture13()
        => Test("""
            "(?<ab>a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="ab">ab</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;ab&gt;a)" />
                <Capture Name="1" Span="[9..17)" Text="(?&lt;ab&gt;a)" />
                <Capture Name="ab" Span="[9..17)" Text="(?&lt;ab&gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestZeroNumberCapture()
        => Test("""
            "(?<0>a)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Capture_number_cannot_be_zero}" Span="[12..13)" Text="0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;0&gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNumericNumberCapture1()
        => Test("""
            "(?<1>a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;1&gt;a)" />
                <Capture Name="1" Span="[9..16)" Text="(?&lt;1&gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNumericNumberCapture2()
        => Test("""
            "(?<10>a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="10">10</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;10&gt;a)" />
                <Capture Name="10" Span="[9..17)" Text="(?&lt;10&gt;a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNumericNumberCapture3()
        => Test("""
            "(?<1>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?&lt;1&gt;)" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;1&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNumericNumberCapture4()
        => Test("""
            "(?<1> )"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence>
                      <Text>
                        <TextToken> </TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;1&gt; )" />
                <Capture Name="1" Span="[9..16)" Text="(?&lt;1&gt; )" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNumericNumberCapture6()
        => Test("""
            "(?<1> )"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;1&gt; )" />
                <Capture Name="1" Span="[9..16)" Text="(?&lt;1&gt; )" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping1()
        => Test("""
            "(?<-"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken />
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[13..13)" Text="" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..12)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?&lt;-" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping2()
        => Test("""
            "(?<-0"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken />
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..12)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?&lt;-0" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping3()
        => Test("""
            "(?<-0)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[14..15)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?&lt;-0)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping4()
        => Test("""
            "(?<-0>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken />
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?&lt;-0&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping5()
        => Test("""
            "(?<-0>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;-0&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping6()
        => Test("""
            "(?<-0 >)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[14..15)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;-0 &gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping7()
        => Test("""
            "(?<- 0 >)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>0</TextToken>
                      </Text>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[13..14)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?&lt;- 0 &gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping8()
        => Test("""
            "(?<- 0>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>0&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[13..14)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;- 0&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping9()
        => Test("""
            "(?<-00>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">00</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;-00&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping10()
        => Test("""
            "(?<a-"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken />
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[14..14)" Text="" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..12)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?&lt;a-" />
                <Capture Name="1" Span="[9..14)" Text="(?&lt;a-" />
                <Capture Name="a" Span="[9..14)" Text="(?&lt;a-" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping11()
        => Test("""
            "(?<a-0"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken />
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[9..12)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?&lt;a-0" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;a-0" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a-0" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping12()
        => Test("""
            "(?<a-0)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken />
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[15..16)" Text=")" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;a-0)" />
                <Capture Name="1" Span="[9..16)" Text="(?&lt;a-0)" />
                <Capture Name="a" Span="[9..16)" Text="(?&lt;a-0)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping13()
        => Test("""
            "(?<a-0>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken />
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[16..16)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;a-0&gt;" />
                <Capture Name="1" Span="[9..16)" Text="(?&lt;a-0&gt;" />
                <Capture Name="a" Span="[9..16)" Text="(?&lt;a-0&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping14()
        => Test("""
            "(?<a-0>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;a-0&gt;)" />
                <Capture Name="1" Span="[9..17)" Text="(?&lt;a-0&gt;)" />
                <Capture Name="a" Span="[9..17)" Text="(?&lt;a-0&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping15()
        => Test("""
            "(?<a-0 >)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[15..16)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?&lt;a-0 &gt;)" />
                <Capture Name="1" Span="[9..18)" Text="(?&lt;a-0 &gt;)" />
                <Capture Name="a" Span="[9..18)" Text="(?&lt;a-0 &gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping16()
        => Test("""
            "(?<a- 0 >)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>0</TextToken>
                      </Text>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[14..15)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..19)" Text="(?&lt;a- 0 &gt;)" />
                <Capture Name="1" Span="[9..19)" Text="(?&lt;a- 0 &gt;)" />
                <Capture Name="a" Span="[9..19)" Text="(?&lt;a- 0 &gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping17()
        => Test("""
            "(?<a- 0>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken />
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>
                          <Trivia>
                            <WhitespaceTrivia> </WhitespaceTrivia>
                          </Trivia>0&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[14..15)" Text=" " />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?&lt;a- 0&gt;)" />
                <Capture Name="1" Span="[9..18)" Text="(?&lt;a- 0&gt;)" />
                <Capture Name="a" Span="[9..18)" Text="(?&lt;a- 0&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGrouping18()
        => Test("""
            "(?<a-00>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">00</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?&lt;a-00&gt;)" />
                <Capture Name="1" Span="[9..18)" Text="(?&lt;a-00&gt;)" />
                <Capture Name="a" Span="[9..18)" Text="(?&lt;a-00&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingUndefinedReference1()
        => Test("""
            "(?<-1>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[13..14)" Text="1" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?&lt;-1&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingDefinedReferenceBehind()
        => Test("""
            "()(?<-1>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="()(?&lt;-1&gt;)" />
                <Capture Name="1" Span="[9..11)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingDefinedReferenceAhead()
        => Test("""
            "(?<-1>)()"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?&lt;-1&gt;)()" />
                <Capture Name="1" Span="[16..18)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingNamedReferenceBehind()
        => Test("""
            "(?<a>)(?<-a>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="(?&lt;a&gt;)(?&lt;-a&gt;)" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingNamedReferenceAhead()
        => Test("""
            "(?<-a>)(?<a>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="(?&lt;-a&gt;)(?&lt;a&gt;)" />
                <Capture Name="1" Span="[16..22)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[16..22)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingNumberedReferenceBehind()
        => Test("""
            "(?<4>)(?<-4>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="4">4</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="4">4</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="(?&lt;4&gt;)(?&lt;-4&gt;)" />
                <Capture Name="4" Span="[9..15)" Text="(?&lt;4&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingNumberedReferenceAhead()
        => Test("""
            "(?<-4>)(?<4>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="4">4</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <NumberToken value="4">4</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="(?&lt;-4&gt;)(?&lt;4&gt;)" />
                <Capture Name="4" Span="[16..22)" Text="(?&lt;4&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumberedExists()
        => Test("""
            "(?<a>)(?<b>)(?<-1>)(?<-2>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="b">b</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..35)" Text="(?&lt;a&gt;)(?&lt;b&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="2" Span="[15..21)" Text="(?&lt;b&gt;)" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="b" Span="[15..21)" Text="(?&lt;b&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers()
        => Test("""
            "()()(?<-0>)(?<-1>)(?<-2>)(?<-3>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[38..39)" Text="3" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..41)" Text="()()(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)" />
                <Capture Name="1" Span="[9..11)" Text="()" />
                <Capture Name="2" Span="[11..13)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers1()
        => Test("""
            "()(?<a>)(?<-0>)(?<-1>)(?<-2>)(?<-3>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[42..43)" Text="3" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..45)" Text="()(?&lt;a&gt;)(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)" />
                <Capture Name="1" Span="[9..11)" Text="()" />
                <Capture Name="2" Span="[11..17)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[11..17)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers2()
        => Test("""
            "(?<a>)()(?<-0>)(?<-1>)(?<-2>)(?<-3>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[42..43)" Text="3" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..45)" Text="(?&lt;a&gt;)()(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)" />
                <Capture Name="1" Span="[15..17)" Text="()" />
                <Capture Name="2" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers3()
        => Test("""
            "(?<a>)(?<b>)(?<-0>)(?<-1>)(?<-2>)(?<-3>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="b">b</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[46..47)" Text="3" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..49)" Text="(?&lt;a&gt;)(?&lt;b&gt;)(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="2" Span="[15..21)" Text="(?&lt;b&gt;)" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="b" Span="[15..21)" Text="(?&lt;b&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers4()
        => Test("""
            "(?<-0>)(?<-1>)(?<-2>)(?<-3>)()()"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[34..35)" Text="3" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..41)" Text="(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)()()" />
                <Capture Name="1" Span="[37..39)" Text="()" />
                <Capture Name="2" Span="[39..41)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers5_1()
        => Test("""
            "(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
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
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[27..28)" Text="2" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[34..35)" Text="3" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[39..40)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[40..41)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[41..41)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..41)" Text="(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)()(?" />
                <Capture Name="1" Span="[37..39)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers5()
        => Test("""
            "(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?<a>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[34..35)" Text="3" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..45)" Text="(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)()(?&lt;a&gt;)" />
                <Capture Name="1" Span="[37..39)" Text="()" />
                <Capture Name="2" Span="[39..45)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[39..45)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers6()
        => Test("""
            "(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)()"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[34..35)" Text="3" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..45)" Text="(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)(?&lt;a&gt;)()" />
                <Capture Name="1" Span="[43..45)" Text="()" />
                <Capture Name="2" Span="[37..43)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[37..43)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers7_1()
        => Test("""
            "(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)(?"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
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
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[27..28)" Text="2" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[34..35)" Text="3" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[43..44)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[44..45)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[45..45)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..45)" Text="(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)(?&lt;a&gt;)(?" />
                <Capture Name="1" Span="[37..43)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[37..43)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBalancingGroupingAutoNumbers7()
        => Test("""
            "(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)(?<b>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="1">1</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="2">2</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="3">3</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="b">b</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 3)}" Span="[34..35)" Text="3" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..49)" Text="(?&lt;-0&gt;)(?&lt;-1&gt;)(?&lt;-2&gt;)(?&lt;-3&gt;)(?&lt;a&gt;)(?&lt;b&gt;)" />
                <Capture Name="1" Span="[37..43)" Text="(?&lt;a&gt;)" />
                <Capture Name="2" Span="[43..49)" Text="(?&lt;b&gt;)" />
                <Capture Name="a" Span="[37..43)" Text="(?&lt;a&gt;)" />
                <Capture Name="b" Span="[43..49)" Text="(?&lt;b&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestReferenceToBalancingGroupCaptureName1()
        => Test("""
            "(?<a-0>)(?<b-a>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="b">b</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..25)" Text="(?&lt;a-0&gt;)(?&lt;b-a&gt;)" />
                <Capture Name="1" Span="[9..17)" Text="(?&lt;a-0&gt;)" />
                <Capture Name="2" Span="[17..25)" Text="(?&lt;b-a&gt;)" />
                <Capture Name="a" Span="[9..17)" Text="(?&lt;a-0&gt;)" />
                <Capture Name="b" Span="[17..25)" Text="(?&lt;b-a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestReferenceToBalancingGroupCaptureName2()
        => Test("""
            "(?<a-0>)(?<-a>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..24)" Text="(?&lt;a-0&gt;)(?&lt;-a&gt;)" />
                <Capture Name="1" Span="[9..17)" Text="(?&lt;a-0&gt;)" />
                <Capture Name="a" Span="[9..17)" Text="(?&lt;a-0&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestReferenceToSameBalancingGroup()
        => Test("""
            "(?<a-a>)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;a-a&gt;)" />
                <Capture Name="1" Span="[9..17)" Text="(?&lt;a-a&gt;)" />
                <Capture Name="a" Span="[9..17)" Text="(?&lt;a-a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestQuoteNamedCapture()
        => Test("""
            "(?'a')"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?'a')" />
                <Capture Name="1" Span="[9..15)" Text="(?'a')" />
                <Capture Name="a" Span="[9..15)" Text="(?'a')" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestQuoteBalancingCapture1()
        => Test("""
            "(?'-0')"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken />
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?'-0')" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestQuoteBalancingCapture2()
        => Test("""
            "(?'a-0')"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?'a-0')" />
                <Capture Name="1" Span="[9..17)" Text="(?'a-0')" />
                <Capture Name="a" Span="[9..17)" Text="(?'a-0')" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestMismatchedOpenCloseCapture1()
        => Test("""
            "(?<a-0')"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <GreaterThanToken />
                    <Sequence>
                      <Text>
                        <TextToken>'</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[15..16)" Text="'" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?&lt;a-0')" />
                <Capture Name="1" Span="[9..17)" Text="(?&lt;a-0')" />
                <Capture Name="a" Span="[9..17)" Text="(?&lt;a-0')" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestMismatchedOpenCloseCapture2()
        => Test("""
            "(?'a-0>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SingleQuoteToken>'</SingleQuoteToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <NumberToken value="0">0</NumberToken>
                    <SingleQuoteToken />
                    <Sequence>
                      <Text>
                        <TextToken>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Invalid_group_name_Group_names_must_begin_with_a_word_character}" Span="[15..16)" Text="&gt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?'a-0&gt;)" />
                <Capture Name="1" Span="[9..17)" Text="(?'a-0&gt;)" />
                <Capture Name="a" Span="[9..17)" Text="(?'a-0&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture1()
        => Test("""
            "(?("
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
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..12)" Text="(?(" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture2()
        => Test("""
            "(?(0"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken />
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Conditional_alternation_is_missing_a_closing_parenthesis_after_the_group_number_0, 0)}" Span="[12..13)" Text="0" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[13..13)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..13)" Text="(?(0" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture3()
        => Test("""
            "(?(0)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?(0)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture4()
        => Test("""
            "(?(0))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?(0))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture5()
        => Test("""
            "(?(0)a)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?(0)a)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture6()
        => Test("""
            "(?(0)a|)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Alternation>
                      <Sequence>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence />
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?(0)a|)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture7()
        => Test("""
            "(?(0)a|b)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
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
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?(0)a|b)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture8()
        => Test("""
            "(?(0)a|b|)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Alternation>
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
                      <BarToken>|</BarToken>
                      <Sequence />
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Too_many_bars_in_conditional_grouping}" Span="[17..18)" Text="|" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..19)" Text="(?(0)a|b|)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture9()
        => Test("""
            "(?(0)a|b|c)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Alternation>
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
                      <BarToken>|</BarToken>
                      <Sequence>
                        <Text>
                          <TextToken>c</TextToken>
                        </Text>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Too_many_bars_in_conditional_grouping}" Span="[17..18)" Text="|" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..20)" Text="(?(0)a|b|c)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture10()
        => Test("""
            "(?(0 )"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken />
                    <Sequence />
                    <CloseParenToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Conditional_alternation_is_missing_a_closing_parenthesis_after_the_group_number_0, 0)}" Span="[12..13)" Text="0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?(0 )" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture11()
        => Test("""
            "(?(1))"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="1">1</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Conditional_alternation_refers_to_an_undefined_group_number_0, 1)}" Span="[12..13)" Text="1" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?(1))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture12()
        => Test("""
            "(?(00))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">00</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?(00))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture13()
        => Test("""
            "(?(0)a|b|c|d)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Alternation>
                      <Alternation>
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
                        <BarToken>|</BarToken>
                        <Sequence>
                          <Text>
                            <TextToken>c</TextToken>
                          </Text>
                        </Sequence>
                      </Alternation>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <Text>
                          <TextToken>d</TextToken>
                        </Text>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Too_many_bars_in_conditional_grouping}" Span="[17..18)" Text="|" />
                <Diagnostic Message="{FeaturesResources.Too_many_bars_in_conditional_grouping}" Span="[19..20)" Text="|" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="(?(0)a|b|c|d)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestConditionalCapture14()
        => Test("""
            "(?(0)a|b|c|d|e)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <NumberToken value="0">0</NumberToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Alternation>
                      <Alternation>
                        <Alternation>
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
                          <BarToken>|</BarToken>
                          <Sequence>
                            <Text>
                              <TextToken>c</TextToken>
                            </Text>
                          </Sequence>
                        </Alternation>
                        <BarToken>|</BarToken>
                        <Sequence>
                          <Text>
                            <TextToken>d</TextToken>
                          </Text>
                        </Sequence>
                      </Alternation>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <Text>
                          <TextToken>e</TextToken>
                        </Text>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Too_many_bars_in_conditional_grouping}" Span="[17..18)" Text="|" />
                <Diagnostic Message="{FeaturesResources.Too_many_bars_in_conditional_grouping}" Span="[19..20)" Text="|" />
                <Diagnostic Message="{FeaturesResources.Too_many_bars_in_conditional_grouping}" Span="[21..22)" Text="|" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..24)" Text="(?(0)a|b|c|d|e)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedConditionalCapture1()
        => Test("""
            "(?(a))"
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
                          <TextToken>a</TextToken>
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
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?(a))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedConditionalCapture2()
        => Test("""
            "(?<a>)(?(a))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ConditionalCaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OpenParenToken>(</OpenParenToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <CloseParenToken>)</CloseParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalCaptureGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..21)" Text="(?&lt;a&gt;)(?(a))" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedConditionalCapture3()
        => Test("""
            "(?<a>)(?(a ))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </Sequence>
                      <CloseParenToken>
                        <Trivia>
                          <WhitespaceTrivia> </WhitespaceTrivia>
                        </Trivia>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="(?&lt;a&gt;)(?(a ))" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNamedConditionalCapture4()
        => Test("""
            "(?<a>)(?( a))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <LessThanToken>&lt;</LessThanToken>
                    <CaptureNameToken value="a">a</CaptureNameToken>
                    <GreaterThanToken>&gt;</GreaterThanToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <Text>
                          <TextToken>
                            <Trivia>
                              <WhitespaceTrivia> </WhitespaceTrivia>
                            </Trivia>a</TextToken>
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
              <Captures>
                <Capture Name="0" Span="[9..22)" Text="(?&lt;a&gt;)(?( a))" />
                <Capture Name="1" Span="[9..15)" Text="(?&lt;a&gt;)" />
                <Capture Name="a" Span="[9..15)" Text="(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestNestedGroupsInConditionalGrouping1()
        => Test("""
            "(?(()a()))"
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
                        <SimpleGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <Sequence />
                          <CloseParenToken>)</CloseParenToken>
                        </SimpleGrouping>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                        <SimpleGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <Sequence />
                          <CloseParenToken>)</CloseParenToken>
                        </SimpleGrouping>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..19)" Text="(?(()a()))" />
                <Capture Name="1" Span="[12..14)" Text="()" />
                <Capture Name="2" Span="[15..17)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNestedGroupsInConditionalGrouping2()
        => Test("""
            "(?((?<x>)a(?<y>)))"
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
                        <CaptureGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <QuestionToken>?</QuestionToken>
                          <LessThanToken>&lt;</LessThanToken>
                          <CaptureNameToken value="x">x</CaptureNameToken>
                          <GreaterThanToken>&gt;</GreaterThanToken>
                          <Sequence />
                          <CloseParenToken>)</CloseParenToken>
                        </CaptureGrouping>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                        <CaptureGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <QuestionToken>?</QuestionToken>
                          <LessThanToken>&lt;</LessThanToken>
                          <CaptureNameToken value="y">y</CaptureNameToken>
                          <GreaterThanToken>&gt;</GreaterThanToken>
                          <Sequence />
                          <CloseParenToken>)</CloseParenToken>
                        </CaptureGrouping>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..27)" Text="(?((?&lt;x&gt;)a(?&lt;y&gt;)))" />
                <Capture Name="1" Span="[12..18)" Text="(?&lt;x&gt;)" />
                <Capture Name="2" Span="[19..25)" Text="(?&lt;y&gt;)" />
                <Capture Name="x" Span="[12..18)" Text="(?&lt;x&gt;)" />
                <Capture Name="y" Span="[19..25)" Text="(?&lt;y&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptureInConditionalGrouping1()
        => Test("""
            "(?(?'"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <SingleQuoteToken>'</SingleQuoteToken>
                      <CaptureNameToken />
                      <SingleQuoteToken />
                      <Sequence />
                      <CloseParenToken />
                    </CaptureGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[11..14)" Text="(?'" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?(?'" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestCaptureInConditionalGrouping2()
        => Test("""
            "(?(?'x'))"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <SingleQuoteToken>'</SingleQuoteToken>
                      <CaptureNameToken value="x">x</CaptureNameToken>
                      <SingleQuoteToken>'</SingleQuoteToken>
                      <Sequence />
                      <CloseParenToken>)</CloseParenToken>
                    </CaptureGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[9..10)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?(?'x'))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestCommentInConditionalGrouping1()
        => Test("""
            "(?(?#"
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
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>#</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_regex_comment}" Span="[11..14)" Text="(?#" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[11..12)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[12..13)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?(?#" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestCommentInConditionalGrouping2()
        => Test("""
            "(?(?#)"
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
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>#)</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_cannot_be_comments}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[11..12)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[12..13)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?(?#)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestCommentInConditionalGrouping3()
        => Test("""
            "(?(?#))"
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
                <EndOfFile>
                  <Trivia>
                    <CommentTrivia>#))</CommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_cannot_be_comments}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[11..12)" Text="(" />
                <Diagnostic Message="{string.Format(FeaturesResources.Quantifier_0_following_nothing, '?')}" Span="[12..13)" Text="?" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[16..16)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?(?#))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestAngleCaptureInConditionalGrouping1()
        => Test("""
            "(?(?<"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <LessThanToken>&lt;</LessThanToken>
                      <CaptureNameToken />
                      <GreaterThanToken />
                      <Sequence />
                      <CloseParenToken />
                    </CaptureGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[11..14)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..14)" Text="(?(?&lt;" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestAngleCaptureInConditionalGrouping2()
        => Test("""
            "(?(?<a"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <LessThanToken>&lt;</LessThanToken>
                      <CaptureNameToken value="a">a</CaptureNameToken>
                      <GreaterThanToken />
                      <Sequence />
                      <CloseParenToken />
                    </CaptureGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_grouping_construct}" Span="[11..14)" Text="(?&lt;" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..15)" Text="(?(?&lt;a" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestAngleCaptureInConditionalGrouping3()
        => Test("""
            "(?(?<a>"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <LessThanToken>&lt;</LessThanToken>
                      <CaptureNameToken value="a">a</CaptureNameToken>
                      <GreaterThanToken>&gt;</GreaterThanToken>
                      <Sequence />
                      <CloseParenToken />
                    </CaptureGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[16..16)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..16)" Text="(?(?&lt;a&gt;" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestAngleCaptureInConditionalGrouping4()
        => Test("""
            "(?(?<a>)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <LessThanToken>&lt;</LessThanToken>
                      <CaptureNameToken value="a">a</CaptureNameToken>
                      <GreaterThanToken>&gt;</GreaterThanToken>
                      <Sequence />
                      <CloseParenToken>)</CloseParenToken>
                    </CaptureGrouping>
                    <Sequence />
                    <CloseParenToken />
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[9..10)" Text="(" />
                <Diagnostic Message="{FeaturesResources.Not_enough_close_parens}" Span="[17..17)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?(?&lt;a&gt;)" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestAngleCaptureInConditionalGrouping5()
        => Test("""
            "(?(?<a>))"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <CaptureGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <LessThanToken>&lt;</LessThanToken>
                      <CaptureNameToken value="a">a</CaptureNameToken>
                      <GreaterThanToken>&gt;</GreaterThanToken>
                      <Sequence />
                      <CloseParenToken>)</CloseParenToken>
                    </CaptureGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Alternation_conditions_do_not_capture_and_cannot_be_named}" Span="[9..10)" Text="(" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[9..18)" Text="(?(?&lt;a&gt;))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestLookbehindAssertionInConditionalGrouping1()
        => Test("""
            "(?(?<=))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <PositiveLookbehindGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <LessThanToken>&lt;</LessThanToken>
                      <EqualsToken>=</EqualsToken>
                      <Sequence />
                      <CloseParenToken>)</CloseParenToken>
                    </PositiveLookbehindGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?(?&lt;=))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestLookbehindAssertionInConditionalGrouping2()
        => Test("""
            "(?(?<!))"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <NegativeLookbehindGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <QuestionToken>?</QuestionToken>
                      <LessThanToken>&lt;</LessThanToken>
                      <ExclamationToken>!</ExclamationToken>
                      <Sequence />
                      <CloseParenToken>)</CloseParenToken>
                    </NegativeLookbehindGrouping>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[9..17)" Text="(?(?&lt;!))" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnorePatternWhitespace);

    [Fact]
    public void TestBackreference1()
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
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[11..12)" Text="1" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\1" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestBackreference2()
        => Test("""
            @"\1 "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[11..12)" Text="1" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\1 " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestBackreference3()
        => Test("""
            @"()\1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="()\1" />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestBackreference4()
        => Test("""
            @"()\1 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="()\1 " />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestBackreference5()
        => Test("""
            @"()\10 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>10</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="()\10 " />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestEcmascriptBackreference1()
        => Test("""
            @"\1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>1</TextToken>
                  </OctalEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..12)" Text="\1" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestEcmascriptBackreference2()
        => Test("""
            @"\1 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <OctalEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>1</TextToken>
                  </OctalEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="\1 " />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestEcmascriptBackreference3()
        => Test("""
            @"()\1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="()\1" />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestEcmaBackreference4()
        => Test("""
            @"()\1 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="()\1 " />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestEcmascriptBackreference5()
        => Test("""
            @"()\10 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="10">1</NumberToken>
                  </BackreferenceEscape>
                  <Text>
                    <TextToken>0 </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="()\10 " />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestEcmascriptBackreference6()
        => Test("""
            @"()()()()()()()()()()\10 "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="10">10</NumberToken>
                  </BackreferenceEscape>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..34)" Text="()()()()()()()()()()\10 " />
                <Capture Name="1" Span="[10..12)" Text="()" />
                <Capture Name="2" Span="[12..14)" Text="()" />
                <Capture Name="3" Span="[14..16)" Text="()" />
                <Capture Name="4" Span="[16..18)" Text="()" />
                <Capture Name="5" Span="[18..20)" Text="()" />
                <Capture Name="6" Span="[20..22)" Text="()" />
                <Capture Name="7" Span="[22..24)" Text="()" />
                <Capture Name="8" Span="[24..26)" Text="()" />
                <Capture Name="9" Span="[26..28)" Text="()" />
                <Capture Name="10" Span="[28..30)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.ECMAScript);

    [Fact]
    public void TestCharacterClass1()
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
    public void TestCharacterClass2()
        => Test("""
            @"[ "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken> </TextToken>
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
                <Capture Name="0" Span="[10..12)" Text="[ " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass3()
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
    public void TestCharacterClass4()
        => Test("""
            @"[] "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>] </TextToken>
                      </Text>
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
                <Capture Name="0" Span="[10..13)" Text="[] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass5()
        => Test("""
            @"[a]"
            """, """
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
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="[a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass6()
        => Test("""
            @"[a] "
            """, """
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
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="[a] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass7()
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
    public void TestCharacterClass8()
        => Test("""
            @"[a- "
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
                          <TextToken> </TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[14..14)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="[a- " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass9()
        => Test("""
            @"[a-]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a-</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="[a-]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass10()
        => Test("""
            @"[a-] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a-</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[a-] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass11()
        => Test("""
            @"[a-b]"
            """, """
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
                          <TextToken>b</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[a-b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass12()
        => Test("""
            @"[a-b] "
            """, """
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
                          <TextToken>b</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[a-b] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass13()
        => Test("""
            @"[a-[b]] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>b</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[a-[b]] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass14()
        => Test("""
            @"[a-b-[c]] "
            """, """
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
                          <TextToken>b</TextToken>
                        </Text>
                      </CharacterClassRange>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>c</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[a-b-[c]] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass15()
        => Test("""
            @"[a-[b]-c] "
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
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>b</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                      <Text>
                        <TextToken>-c</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.A_subtraction_must_be_the_last_element_in_a_character_class}" Span="[12..12)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[a-[b]-c] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass16()
        => Test("""
            @"[[a]-b] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>[a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken>-b] </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[[a]-b] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass17()
        => Test("""
            @"[[a]-[b]] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>[a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken>-</TextToken>
                  </Text>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken>] </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[[a]-[b]] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass18()
        => Test("""
            @"[\w-a] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>w</TextToken>
                      </CharacterClassEscape>
                      <Text>
                        <TextToken>-a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[\w-a] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass19()
        => Test("""
            @"[a-\w] "
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
                        <CharacterClassEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>w</TextToken>
                        </CharacterClassEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "w")}" Span="[13..15)" Text="\w" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[a-\w] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass20()
        => Test("""
            @"[\p{llll}-a] "
            """, $$"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CategoryEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>p</TextToken>
                        <OpenBraceToken>{</OpenBraceToken>
                        <EscapeCategoryToken>llll</EscapeCategoryToken>
                        <CloseBraceToken>}</CloseBraceToken>
                      </CategoryEscape>
                      <Text>
                        <TextToken>-a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Unknown_property_0, "llll")}}" Span="[14..18)" Text="llll" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="[\p{llll}-a] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass21()
        => Test("""
            @"[\p{Lu}-a] "
            """, """
            <Tree>
              <CompilationUnit>
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
                      <Text>
                        <TextToken>-a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="[\p{Lu}-a] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass22()
        => Test("""
            @"[a-\p{Lu}] "
            """, $$"""
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
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{{string.Format(FeaturesResources.Cannot_include_class_0_in_character_range, "p")}}" Span="[13..15)" Text="\p" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="[a-\p{Lu}] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass23()
        => Test("""
            @"[a-[:Ll:]] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>:Ll:</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="[a-[:Ll:]] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass24()
        => Test("""
            @"[a-[:Ll]] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>:Ll</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[a-[:Ll]] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass25()
        => Test("""
            @"[a-[:"
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
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>:</TextToken>
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
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[a-[:" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass26()
        => Test("""
            @"[a-[:L"
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
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>:L</TextToken>
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
                <Capture Name="0" Span="[10..16)" Text="[a-[:L" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass27()
        => Test("""
            @"[a-[:L:"
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
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>:L:</TextToken>
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
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[17..17)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[a-[:L:" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass28()
        => Test("""
            @"[a-[:L:]"
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
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>:L:</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[18..18)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[a-[:L:]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass29()
        => Test("""
            @"[\-]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>-</TextToken>
                      </SimpleEscape>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="[\-]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass30()
        => Test("""
            @"[a-b-c] "
            """, """
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
                          <TextToken>b</TextToken>
                        </Text>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>-c</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[a-b-c] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass31()
        => Test("""
            @"[-b-c] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>-</TextToken>
                      </Text>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>b</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>c</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[-b-c] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass32()
        => Test("""
            @"[-[b] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>-[b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[-[b] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass33()
        => Test("""
            @"[-[b]] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>-[b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken>] </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[-[b]] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass34()
        => Test("""
            @"[--b "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>-</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>b</TextToken>
                        </Text>
                      </CharacterClassRange>
                      <Text>
                        <TextToken> </TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[15..15)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[--b " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass35()
        => Test("""
            @"[--b] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>-</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>b</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[--b] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass36()
        => Test("""
            @"[--[b "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>-</TextToken>
                      </Text>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>b </TextToken>
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
                <Capture Name="0" Span="[10..16)" Text="[--[b " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass37()
        => Test("""
            @"[--[b] "
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>-</TextToken>
                      </Text>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>b</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                      <Text>
                        <TextToken> </TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.A_subtraction_must_be_the_last_element_in_a_character_class}" Span="[12..12)" Text="" />
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[17..17)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[--[b] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass38()
        => Test("""
            @"[--[b]] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>-</TextToken>
                      </Text>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>b</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[--[b]] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass39()
        => Test("""
            @"[a--[b "
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
                          <TextToken>-</TextToken>
                        </Text>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>[b </TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken />
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
                <Diagnostic Message="{FeaturesResources.Unterminated_character_class_set}" Span="[17..17)" Text="" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[a--[b " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass40()
        => Test("""
            @"[,--[a] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>,</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>-</TextToken>
                        </Text>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>[a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken> </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[,--[a] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass41()
        => Test("""
            @"[,--[a]] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>,</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>-</TextToken>
                        </Text>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>[a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken>] </TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[,--[a]] " />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass42()
        => Test("""
            @"[\s-a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>s</TextToken>
                      </CharacterClassEscape>
                      <Text>
                        <TextToken>-a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[\s-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass43()
        => Test("""
            @"[\p{Lu}-a]"
            """, """
            <Tree>
              <CompilationUnit>
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
                      <Text>
                        <TextToken>-a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[\p{Lu}-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClass44()
        => Test("""
            @"[\p{Lu}-a]"
            """, """
            <Tree>
              <CompilationUnit>
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
                      <Text>
                        <TextToken>-a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[\p{Lu}-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestNegatedCharacterClass1()
        => Test("""
            @"[a]"
            """, """
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
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..13)" Text="[a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange1()
        => Test("""
            @"[\c<-\c>]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <ControlEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>c</TextToken>
                        <TextToken />
                      </ControlEscape>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>&lt;</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken />
                        </ControlEscape>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[13..14)" Text="&lt;" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[17..18)" Text="&gt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\c&lt;-\c&gt;]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange2()
        => Test("""
            @"[\c>-\c<]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <ControlEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>c</TextToken>
                        <TextToken />
                      </ControlEscape>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>&gt;</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken />
                        </ControlEscape>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>&lt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[13..14)" Text="&gt;" />
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[17..18)" Text="&lt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\c&gt;-\c&lt;]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange3()
        => Test("""
            @"[\c>-a]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <ControlEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>c</TextToken>
                        <TextToken />
                      </ControlEscape>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>&gt;</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[13..14)" Text="&gt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[\c&gt;-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange4()
        => Test("""
            @"[a-\c>]"
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
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken />
                        </ControlEscape>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Unrecognized_control_character}" Span="[15..16)" Text="&gt;" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[a-\c&gt;]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange5()
        => Test("""
            @"[a--]"
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
                          <TextToken>-</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[a--]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange6()
        => Test("""
            @"[--a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>-</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[--a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange7()
        => Test("""
            @"[a-\-]"
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
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[a-\-]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange8()
        => Test("""
            @"[\--a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>-</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>-a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[\--a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange9()
        => Test("""
            @"[\0-\1]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>0</TextToken>
                        </OctalEscape>
                        <MinusToken>-</MinusToken>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>1</TextToken>
                        </OctalEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[\0-\1]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange10()
        => Test("""
            @"[\1-\0]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>1</TextToken>
                        </OctalEscape>
                        <MinusToken>-</MinusToken>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>0</TextToken>
                        </OctalEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[13..14)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[\1-\0]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange11()
        => Test("""
            @"[\0-\01]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>0</TextToken>
                        </OctalEscape>
                        <MinusToken>-</MinusToken>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>01</TextToken>
                        </OctalEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\0-\01]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange12()
        => Test("""
            @"[\01-\0]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>01</TextToken>
                        </OctalEscape>
                        <MinusToken>-</MinusToken>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>0</TextToken>
                        </OctalEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[14..15)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\01-\0]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange13()
        => Test("""
            @"[[:x:]-a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>[:x:</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken>-a]</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[[:x:]-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange14()
        => Test("""
            @"[a-[:x:]]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                      <CharacterClassSubtraction>
                        <MinusToken>-</MinusToken>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>:x:</TextToken>
                            </Text>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </CharacterClassSubtraction>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[a-[:x:]]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange15()
        => Test("""
            @"[\0-\ca]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>0</TextToken>
                        </OctalEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>a</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\0-\ca]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange16()
        => Test("""
            @"[\ca-\0]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>a</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>0</TextToken>
                        </OctalEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[14..15)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\ca-\0]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange17()
        => Test("""
            @"[\ca-\cA]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>a</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>A</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\ca-\cA]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange18()
        => Test("""
            @"[\cA-\ca]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>A</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>a</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\cA-\ca]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange19()
        => Test("""
            @"[\u0-\u1]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0</TextToken>
                        </UnicodeEscape>
                        <MinusToken>-</MinusToken>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>1</TextToken>
                        </UnicodeEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[11..14)" Text="\u0" />
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[15..18)" Text="\u1" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\u0-\u1]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange20()
        => Test("""
            @"[\u1-\u0]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>1</TextToken>
                        </UnicodeEscape>
                        <MinusToken>-</MinusToken>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0</TextToken>
                        </UnicodeEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[11..14)" Text="\u1" />
                <Diagnostic Message="{FeaturesResources.Insufficient_or_invalid_hexadecimal_digits}" Span="[15..18)" Text="\u0" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\u1-\u0]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange21()
        => Test("""
            @"[\u0000-\u0000]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0000</TextToken>
                        </UnicodeEscape>
                        <MinusToken>-</MinusToken>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0000</TextToken>
                        </UnicodeEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="[\u0000-\u0000]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange22()
        => Test("""
            @"[\u0000-\u0001]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0000</TextToken>
                        </UnicodeEscape>
                        <MinusToken>-</MinusToken>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0001</TextToken>
                        </UnicodeEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="[\u0000-\u0001]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange23()
        => Test("""
            @"[\u0001-\u0000]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0001</TextToken>
                        </UnicodeEscape>
                        <MinusToken>-</MinusToken>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0000</TextToken>
                        </UnicodeEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[17..18)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..25)" Text="[\u0001-\u0000]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange24()
        => Test("""
            @"[\u0001-a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0001</TextToken>
                        </UnicodeEscape>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[\u0001-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange25()
        => Test("""
            @"[a-\u0001]"
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
                        <UnicodeEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>u</TextToken>
                          <TextToken>0001</TextToken>
                        </UnicodeEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[a-\u0001]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange26()
        => Test("""
            @"[a-a]"
            """, """
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
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[a-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange27()
        => Test("""
            @"[a-A]"
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
                          <TextToken>A</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[a-A]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange28()
        => Test("""
            @"[A-a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>A</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[A-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange29()
        => Test("""
            @"[a-a]"
            """, """
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
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[a-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnoreCase);

    [Fact]
    public void TestCharacterClassRange30()
        => Test("""
            @"[a-A]"
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
                          <TextToken>A</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[a-A]" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnoreCase);

    [Fact]
    public void TestCharacterClassRange31()
        => Test("""
            @"[A-a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>A</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..15)" Text="[A-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.IgnoreCase);

    [Fact]
    public void TestCharacterClassRange32()
        => Test("""
            @"[a-\x61]"
            """, """
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
                        <HexEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>x</TextToken>
                          <TextToken>61</TextToken>
                        </HexEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[a-\x61]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange33()
        => Test("""
            @"[\x61-a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <HexEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>x</TextToken>
                          <TextToken>61</TextToken>
                        </HexEscape>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\x61-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange34()
        => Test("""
            @"[a-\x60]"
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
                        <HexEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>x</TextToken>
                          <TextToken>60</TextToken>
                        </HexEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[a-\x60]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange35()
        => Test("""
            @"[\x62-a]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <HexEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>x</TextToken>
                          <TextToken>62</TextToken>
                        </HexEscape>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[15..16)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\x62-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange36()
        => Test("""
            @"[a-\x62]"
            """, """
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
                        <HexEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>x</TextToken>
                          <TextToken>62</TextToken>
                        </HexEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[a-\x62]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange37()
        => Test("""
            @"[\x62-a]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <HexEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>x</TextToken>
                          <TextToken>62</TextToken>
                        </HexEscape>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[15..16)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\x62-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange38()
        => Test("""
            @"[\3-\cc]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>3</TextToken>
                        </OctalEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>c</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\3-\cc]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange39()
        => Test("""
            @"[\cc-\3]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>c</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>3</TextToken>
                        </OctalEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\cc-\3]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange40()
        => Test("""
            @"[\2-\cc]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>2</TextToken>
                        </OctalEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>c</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\2-\cc]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange41()
        => Test("""
            @"[\cc-\2]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>c</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>2</TextToken>
                        </OctalEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[14..15)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\cc-\2]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange42()
        => Test("""
            @"[\4-\cc]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>4</TextToken>
                        </OctalEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>c</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[13..14)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\4-\cc]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange43()
        => Test("""
            @"[\cc-\4]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>c</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <OctalEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>4</TextToken>
                        </OctalEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[\cc-\4]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange44()
        => Test("""
            @"[\ca-\cb]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>a</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>b</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\ca-\cb]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange45()
        => Test("""
            @"[\ca-\cB]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>a</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>B</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\ca-\cB]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange46()
        => Test("""
            @"[\cA-\cb]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>A</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>b</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\cA-\cb]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange47()
        => Test("""
            @"[\cA-\cB]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>A</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>B</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\cA-\cB]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange48()
        => Test("""
            @"[\cb-\ca]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>b</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>a</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[14..15)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\cb-\ca]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange49()
        => Test("""
            @"[\cb-\cA]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>b</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>A</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[14..15)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\cb-\cA]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange50()
        => Test("""
            @"[\cB-\ca]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>B</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>a</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[14..15)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\cB-\ca]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange51()
        => Test("""
            @"[\cB-\cA]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>B</TextToken>
                        </ControlEscape>
                        <MinusToken>-</MinusToken>
                        <ControlEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>c</TextToken>
                          <TextToken>A</TextToken>
                        </ControlEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[14..15)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[\cB-\cA]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange52()
        => Test("""
            @"[\--a]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>-</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>-a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[\--a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange53()
        => Test("""
            @"[\--#]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>-</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>-#</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[\--#]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange54()
        => Test("""
            @"[a-\-]"
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
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="[a-\-]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange55()
        => Test("""
            @"[a-\-b]"
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
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[a-\-b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange56()
        => Test("""
            @"[a-\-\-b]"
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
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>-</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>b</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[a-\-\-b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange57()
        => Test("""
            @"[b-\-a]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>b</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[b-\-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange58()
        => Test("""
            @"[b-\-\-a]"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>b</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>-</TextToken>
                      </SimpleEscape>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[b-\-\-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange59()
        => Test("""
            @"[a-\-\D]"
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
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                      <CharacterClassEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>D</TextToken>
                      </CharacterClassEscape>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[a-\-\D]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange60()
        => Test("""
            @"[a-\-\-\D]"
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
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>-</TextToken>
                      </SimpleEscape>
                      <CharacterClassEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>D</TextToken>
                      </CharacterClassEscape>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[12..13)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..20)" Text="[a-\-\-\D]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange61()
        => Test("""
            @"[a -\-\b]"
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
                      <CharacterClassRange>
                        <Text>
                          <TextToken> </TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>b</TextToken>
                      </SimpleEscape>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..19)" Text="[a -\-\b]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCharacterClassRange62()
        => Test("""
            @"[ab-\-a]"
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
                      <CharacterClassRange>
                        <Text>
                          <TextToken>b</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>-</TextToken>
                        </SimpleEscape>
                      </CharacterClassRange>
                      <Text>
                        <TextToken>a</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{FeaturesResources.x_y_range_in_reverse_order}" Span="[13..14)" Text="-" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..18)" Text="[ab-\-a]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures1()
        => Test("""
            @"()\1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="()\1" />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures2()
        => Test("""
            @"()\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[13..14)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="()\2" />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures3()
        => Test("""
            @"()()\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="()()\2" />
                <Capture Name="1" Span="[10..12)" Text="()" />
                <Capture Name="2" Span="[12..14)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures4()
        => Test("""
            @"()\1"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[13..14)" Text="1" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="()\1" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures5()
        => Test("""
            @"()\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[13..14)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..14)" Text="()\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures6()
        => Test("""
            @"()()\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[15..16)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..16)" Text="()()\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures7()
        => Test("""
            @"()()(?n)\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="()()(?n)\1\2" />
                <Capture Name="1" Span="[10..12)" Text="()" />
                <Capture Name="2" Span="[12..14)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures8()
        => Test("""
            @"()(?n)()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[21..22)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="()(?n)()\1\2" />
                <Capture Name="1" Span="[10..12)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures9()
        => Test("""
            @"(?n)()()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[19..20)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[21..22)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="(?n)()()\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures10()
        => Test("""
            @"()()(?n)\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[19..20)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[21..22)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="()()(?n)\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures11()
        => Test("""
            @"()(?n)()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[19..20)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[21..22)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="()(?n)()\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures12()
        => Test("""
            @"(?n)()()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[19..20)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[21..22)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..22)" Text="(?n)()()\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures13()
        => Test("""
            @"()()(?-n)\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="()()(?-n)\1\2" />
                <Capture Name="1" Span="[10..12)" Text="()" />
                <Capture Name="2" Span="[12..14)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures14()
        => Test("""
            @"()(?-n)()\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="()(?-n)()\1\2" />
                <Capture Name="1" Span="[10..12)" Text="()" />
                <Capture Name="2" Span="[17..19)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures15()
        => Test("""
            @"(?-n)()()\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="(?-n)()()\1\2" />
                <Capture Name="1" Span="[15..17)" Text="()" />
                <Capture Name="2" Span="[17..19)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures16()
        => Test("""
            @"()()(?-n)\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[20..21)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[22..23)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="()()(?-n)\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures17()
        => Test("""
            @"()(?-n)()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[22..23)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="()(?-n)()\1\2" />
                <Capture Name="1" Span="[17..19)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures18()
        => Test("""
            @"(?-n)()()\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="(?-n)()()\1\2" />
                <Capture Name="1" Span="[15..17)" Text="()" />
                <Capture Name="2" Span="[17..19)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures19()
        => Test("""
            @"()()(?n:\1\2)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <NestedOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="1">1</NumberToken>
                      </BackreferenceEscape>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="2">2</NumberToken>
                      </BackreferenceEscape>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="()()(?n:\1\2)" />
                <Capture Name="1" Span="[10..12)" Text="()" />
                <Capture Name="2" Span="[12..14)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures20()
        => Test("""
            @"()()(?n:\1\2)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <NestedOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="1">1</NumberToken>
                      </BackreferenceEscape>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="2">2</NumberToken>
                      </BackreferenceEscape>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[19..20)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[21..22)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="()()(?n:\1\2)" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures21()
        => Test("""
            @"()()(?-n:\1\2)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <NestedOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="1">1</NumberToken>
                      </BackreferenceEscape>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="2">2</NumberToken>
                      </BackreferenceEscape>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="()()(?-n:\1\2)" />
                <Capture Name="1" Span="[10..12)" Text="()" />
                <Capture Name="2" Span="[12..14)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures22()
        => Test("""
            @"()()(?-n:\1\2)"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <NestedOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="1">1</NumberToken>
                      </BackreferenceEscape>
                      <BackreferenceEscape>
                        <BackslashToken>\</BackslashToken>
                        <NumberToken value="2">2</NumberToken>
                      </BackreferenceEscape>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[20..21)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[22..23)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="()()(?-n:\1\2)" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures23()
        => Test("""
            @"(?n:)()()\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NestedOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="(?n:)()()\1\2" />
                <Capture Name="1" Span="[15..17)" Text="()" />
                <Capture Name="2" Span="[17..19)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures24()
        => Test("""
            @"(?n:)()()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NestedOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[20..21)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[22..23)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..23)" Text="(?n:)()()\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures25()
        => Test("""
            @"(?-n:)()()\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NestedOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="(?-n:)()()\1\2" />
                <Capture Name="1" Span="[16..18)" Text="()" />
                <Capture Name="2" Span="[18..20)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures26()
        => Test("""
            @"(?-n:)()()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NestedOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <ColonToken>:</ColonToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </NestedOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[21..22)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[23..24)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..24)" Text="(?-n:)()()\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures27()
        => Test("""
            @"(?n)(?-n)()()\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..27)" Text="(?n)(?-n)()()\1\2" />
                <Capture Name="1" Span="[19..21)" Text="()" />
                <Capture Name="2" Span="[21..23)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures28()
        => Test("""
            @"(?n)(?-n)()()\1\2"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..27)" Text="(?n)(?-n)()()\1\2" />
                <Capture Name="1" Span="[19..21)" Text="()" />
                <Capture Name="2" Span="[21..23)" Text="()" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestCaptures29()
        => Test("""
            @"(?-n)(?n)()()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[24..25)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[26..27)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..27)" Text="(?-n)(?n)()()\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestCaptures30()
        => Test("""
            @"(?-n)(?n)()()\1\2"
            """, $"""
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>-n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleOptionsGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <OptionsToken>n</OptionsToken>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleOptionsGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="1">1</NumberToken>
                  </BackreferenceEscape>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value="2">2</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Diagnostics>
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 1)}" Span="[24..25)" Text="1" />
                <Diagnostic Message="{string.Format(FeaturesResources.Reference_to_undefined_group_number_0, 2)}" Span="[26..27)" Text="2" />
              </Diagnostics>
              <Captures>
                <Capture Name="0" Span="[10..27)" Text="(?-n)(?n)()()\1\2" />
              </Captures>
            </Tree>
            """, RegexOptions.ExplicitCapture);

    [Fact]
    public void TestComplex1()
        => Test($"""
            @"{And(".*[0-9].*[0-9].*", ".*[A-Z].*[A-Z].*", Not(".*(01|12).*"))}"
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
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>0</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <Text>
                                <TextToken>9</TextToken>
                              </Text>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>0</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <Text>
                                <TextToken>9</TextToken>
                              </Text>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Alternation>
                      <Sequence>
                        <ConditionalExpressionGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <QuestionToken>?</QuestionToken>
                          <SimpleGrouping>
                            <OpenParenToken>(</OpenParenToken>
                            <Sequence>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <CharacterClassRange>
                                    <Text>
                                      <TextToken>A</TextToken>
                                    </Text>
                                    <MinusToken>-</MinusToken>
                                    <Text>
                                      <TextToken>Z</TextToken>
                                    </Text>
                                  </CharacterClassRange>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <CharacterClassRange>
                                    <Text>
                                      <TextToken>A</TextToken>
                                    </Text>
                                    <MinusToken>-</MinusToken>
                                    <Text>
                                      <TextToken>Z</TextToken>
                                    </Text>
                                  </CharacterClassRange>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                            </Sequence>
                            <CloseParenToken>)</CloseParenToken>
                          </SimpleGrouping>
                          <Alternation>
                            <Sequence>
                              <SimpleGrouping>
                                <OpenParenToken>(</OpenParenToken>
                                <Sequence>
                                  <ConditionalExpressionGrouping>
                                    <OpenParenToken>(</OpenParenToken>
                                    <QuestionToken>?</QuestionToken>
                                    <SimpleGrouping>
                                      <OpenParenToken>(</OpenParenToken>
                                      <Sequence>
                                        <ZeroOrMoreQuantifier>
                                          <Wildcard>
                                            <DotToken>.</DotToken>
                                          </Wildcard>
                                          <AsteriskToken>*</AsteriskToken>
                                        </ZeroOrMoreQuantifier>
                                        <SimpleGrouping>
                                          <OpenParenToken>(</OpenParenToken>
                                          <Alternation>
                                            <Sequence>
                                              <Text>
                                                <TextToken>01</TextToken>
                                              </Text>
                                            </Sequence>
                                            <BarToken>|</BarToken>
                                            <Sequence>
                                              <Text>
                                                <TextToken>12</TextToken>
                                              </Text>
                                            </Sequence>
                                          </Alternation>
                                          <CloseParenToken>)</CloseParenToken>
                                        </SimpleGrouping>
                                        <ZeroOrMoreQuantifier>
                                          <Wildcard>
                                            <DotToken>.</DotToken>
                                          </Wildcard>
                                          <AsteriskToken>*</AsteriskToken>
                                        </ZeroOrMoreQuantifier>
                                      </Sequence>
                                      <CloseParenToken>)</CloseParenToken>
                                    </SimpleGrouping>
                                    <Alternation>
                                      <Sequence>
                                        <CharacterClass>
                                          <OpenBracketToken>[</OpenBracketToken>
                                          <Sequence>
                                            <Text>
                                              <TextToken>0</TextToken>
                                            </Text>
                                            <CharacterClassSubtraction>
                                              <MinusToken>-</MinusToken>
                                              <CharacterClass>
                                                <OpenBracketToken>[</OpenBracketToken>
                                                <Sequence>
                                                  <Text>
                                                    <TextToken>0</TextToken>
                                                  </Text>
                                                </Sequence>
                                                <CloseBracketToken>]</CloseBracketToken>
                                              </CharacterClass>
                                            </CharacterClassSubtraction>
                                          </Sequence>
                                          <CloseBracketToken>]</CloseBracketToken>
                                        </CharacterClass>
                                      </Sequence>
                                      <BarToken>|</BarToken>
                                      <Sequence>
                                        <ZeroOrMoreQuantifier>
                                          <Wildcard>
                                            <DotToken>.</DotToken>
                                          </Wildcard>
                                          <AsteriskToken>*</AsteriskToken>
                                        </ZeroOrMoreQuantifier>
                                      </Sequence>
                                    </Alternation>
                                    <CloseParenToken>)</CloseParenToken>
                                  </ConditionalExpressionGrouping>
                                </Sequence>
                                <CloseParenToken>)</CloseParenToken>
                              </SimpleGrouping>
                            </Sequence>
                            <BarToken>|</BarToken>
                            <Sequence>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                  <CharacterClassSubtraction>
                                    <MinusToken>-</MinusToken>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <Text>
                                          <TextToken>0</TextToken>
                                        </Text>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                  </CharacterClassSubtraction>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </Sequence>
                          </Alternation>
                          <CloseParenToken>)</CloseParenToken>
                        </ConditionalExpressionGrouping>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>0</TextToken>
                            </Text>
                            <CharacterClassSubtraction>
                              <MinusToken>-</MinusToken>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </CharacterClassSubtraction>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..96)" Text="(?(.*[0-9].*[0-9].*)(?(.*[A-Z].*[A-Z].*)((?(.*(01|12).*)[0-[0]]|.*))|[0-[0]])|[0-[0]])" />
                <Capture Name="1" Span="[50..78)" Text="((?(.*(01|12).*)[0-[0]]|.*))" />
                <Capture Name="2" Span="[56..63)" Text="(01|12)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestComplex2()
        => Test($"""
            @"{And(".*a.*", ".*b.*")}"
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
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                        <Text>
                          <TextToken>a</TextToken>
                        </Text>
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Alternation>
                      <Sequence>
                        <SimpleGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <Sequence>
                            <ZeroOrMoreQuantifier>
                              <Wildcard>
                                <DotToken>.</DotToken>
                              </Wildcard>
                              <AsteriskToken>*</AsteriskToken>
                            </ZeroOrMoreQuantifier>
                            <Text>
                              <TextToken>b</TextToken>
                            </Text>
                            <ZeroOrMoreQuantifier>
                              <Wildcard>
                                <DotToken>.</DotToken>
                              </Wildcard>
                              <AsteriskToken>*</AsteriskToken>
                            </ZeroOrMoreQuantifier>
                          </Sequence>
                          <CloseParenToken>)</CloseParenToken>
                        </SimpleGrouping>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>0</TextToken>
                            </Text>
                            <CharacterClassSubtraction>
                              <MinusToken>-</MinusToken>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </CharacterClassSubtraction>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..35)" Text="(?(.*a.*)(.*b.*)|[0-[0]])" />
                <Capture Name="1" Span="[19..26)" Text="(.*b.*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestComplex3()
        => Test($"""
            @"{And(".*[a-z].*", ".*[A-Z].*", ".*[0-9].*", ".{2,4}")}"
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
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>a</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <Text>
                                <TextToken>z</TextToken>
                              </Text>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Alternation>
                      <Sequence>
                        <ConditionalExpressionGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <QuestionToken>?</QuestionToken>
                          <SimpleGrouping>
                            <OpenParenToken>(</OpenParenToken>
                            <Sequence>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <CharacterClassRange>
                                    <Text>
                                      <TextToken>A</TextToken>
                                    </Text>
                                    <MinusToken>-</MinusToken>
                                    <Text>
                                      <TextToken>Z</TextToken>
                                    </Text>
                                  </CharacterClassRange>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                            </Sequence>
                            <CloseParenToken>)</CloseParenToken>
                          </SimpleGrouping>
                          <Alternation>
                            <Sequence>
                              <ConditionalExpressionGrouping>
                                <OpenParenToken>(</OpenParenToken>
                                <QuestionToken>?</QuestionToken>
                                <SimpleGrouping>
                                  <OpenParenToken>(</OpenParenToken>
                                  <Sequence>
                                    <ZeroOrMoreQuantifier>
                                      <Wildcard>
                                        <DotToken>.</DotToken>
                                      </Wildcard>
                                      <AsteriskToken>*</AsteriskToken>
                                    </ZeroOrMoreQuantifier>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <CharacterClassRange>
                                          <Text>
                                            <TextToken>0</TextToken>
                                          </Text>
                                          <MinusToken>-</MinusToken>
                                          <Text>
                                            <TextToken>9</TextToken>
                                          </Text>
                                        </CharacterClassRange>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                    <ZeroOrMoreQuantifier>
                                      <Wildcard>
                                        <DotToken>.</DotToken>
                                      </Wildcard>
                                      <AsteriskToken>*</AsteriskToken>
                                    </ZeroOrMoreQuantifier>
                                  </Sequence>
                                  <CloseParenToken>)</CloseParenToken>
                                </SimpleGrouping>
                                <Alternation>
                                  <Sequence>
                                    <SimpleGrouping>
                                      <OpenParenToken>(</OpenParenToken>
                                      <Sequence>
                                        <ClosedRangeNumericQuantifier>
                                          <Wildcard>
                                            <DotToken>.</DotToken>
                                          </Wildcard>
                                          <OpenBraceToken>{</OpenBraceToken>
                                          <NumberToken value="2">2</NumberToken>
                                          <CommaToken>,</CommaToken>
                                          <NumberToken value="4">4</NumberToken>
                                          <CloseBraceToken>}</CloseBraceToken>
                                        </ClosedRangeNumericQuantifier>
                                      </Sequence>
                                      <CloseParenToken>)</CloseParenToken>
                                    </SimpleGrouping>
                                  </Sequence>
                                  <BarToken>|</BarToken>
                                  <Sequence>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <Text>
                                          <TextToken>0</TextToken>
                                        </Text>
                                        <CharacterClassSubtraction>
                                          <MinusToken>-</MinusToken>
                                          <CharacterClass>
                                            <OpenBracketToken>[</OpenBracketToken>
                                            <Sequence>
                                              <Text>
                                                <TextToken>0</TextToken>
                                              </Text>
                                            </Sequence>
                                            <CloseBracketToken>]</CloseBracketToken>
                                          </CharacterClass>
                                        </CharacterClassSubtraction>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                  </Sequence>
                                </Alternation>
                                <CloseParenToken>)</CloseParenToken>
                              </ConditionalExpressionGrouping>
                            </Sequence>
                            <BarToken>|</BarToken>
                            <Sequence>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                  <CharacterClassSubtraction>
                                    <MinusToken>-</MinusToken>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <Text>
                                          <TextToken>0</TextToken>
                                        </Text>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                  </CharacterClassSubtraction>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </Sequence>
                          </Alternation>
                          <CloseParenToken>)</CloseParenToken>
                        </ConditionalExpressionGrouping>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>0</TextToken>
                            </Text>
                            <CharacterClassSubtraction>
                              <MinusToken>-</MinusToken>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </CharacterClassSubtraction>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..84)" Text="(?(.*[a-z].*)(?(.*[A-Z].*)(?(.*[0-9].*)(.{2,4})|[0-[0]])|[0-[0]])|[0-[0]])" />
                <Capture Name="1" Span="[49..57)" Text="(.{2,4})" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestComplex4()
        => Test($"""
            @"{And(".*[a-z].*", ".*[A-Z].*", ".*[0-9].*", ".{4,8}",
                    Not(".*(01|12|23|34|45|56|67|78|89).*"))}"
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
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>a</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <Text>
                                <TextToken>z</TextToken>
                              </Text>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Alternation>
                      <Sequence>
                        <ConditionalExpressionGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <QuestionToken>?</QuestionToken>
                          <SimpleGrouping>
                            <OpenParenToken>(</OpenParenToken>
                            <Sequence>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <CharacterClassRange>
                                    <Text>
                                      <TextToken>A</TextToken>
                                    </Text>
                                    <MinusToken>-</MinusToken>
                                    <Text>
                                      <TextToken>Z</TextToken>
                                    </Text>
                                  </CharacterClassRange>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                            </Sequence>
                            <CloseParenToken>)</CloseParenToken>
                          </SimpleGrouping>
                          <Alternation>
                            <Sequence>
                              <ConditionalExpressionGrouping>
                                <OpenParenToken>(</OpenParenToken>
                                <QuestionToken>?</QuestionToken>
                                <SimpleGrouping>
                                  <OpenParenToken>(</OpenParenToken>
                                  <Sequence>
                                    <ZeroOrMoreQuantifier>
                                      <Wildcard>
                                        <DotToken>.</DotToken>
                                      </Wildcard>
                                      <AsteriskToken>*</AsteriskToken>
                                    </ZeroOrMoreQuantifier>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <CharacterClassRange>
                                          <Text>
                                            <TextToken>0</TextToken>
                                          </Text>
                                          <MinusToken>-</MinusToken>
                                          <Text>
                                            <TextToken>9</TextToken>
                                          </Text>
                                        </CharacterClassRange>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                    <ZeroOrMoreQuantifier>
                                      <Wildcard>
                                        <DotToken>.</DotToken>
                                      </Wildcard>
                                      <AsteriskToken>*</AsteriskToken>
                                    </ZeroOrMoreQuantifier>
                                  </Sequence>
                                  <CloseParenToken>)</CloseParenToken>
                                </SimpleGrouping>
                                <Alternation>
                                  <Sequence>
                                    <ConditionalExpressionGrouping>
                                      <OpenParenToken>(</OpenParenToken>
                                      <QuestionToken>?</QuestionToken>
                                      <SimpleGrouping>
                                        <OpenParenToken>(</OpenParenToken>
                                        <Sequence>
                                          <ClosedRangeNumericQuantifier>
                                            <Wildcard>
                                              <DotToken>.</DotToken>
                                            </Wildcard>
                                            <OpenBraceToken>{</OpenBraceToken>
                                            <NumberToken value="4">4</NumberToken>
                                            <CommaToken>,</CommaToken>
                                            <NumberToken value="8">8</NumberToken>
                                            <CloseBraceToken>}</CloseBraceToken>
                                          </ClosedRangeNumericQuantifier>
                                        </Sequence>
                                        <CloseParenToken>)</CloseParenToken>
                                      </SimpleGrouping>
                                      <Alternation>
                                        <Sequence>
                                          <SimpleGrouping>
                                            <OpenParenToken>(</OpenParenToken>
                                            <Sequence>
                                              <ConditionalExpressionGrouping>
                                                <OpenParenToken>(</OpenParenToken>
                                                <QuestionToken>?</QuestionToken>
                                                <SimpleGrouping>
                                                  <OpenParenToken>(</OpenParenToken>
                                                  <Sequence>
                                                    <ZeroOrMoreQuantifier>
                                                      <Wildcard>
                                                        <DotToken>.</DotToken>
                                                      </Wildcard>
                                                      <AsteriskToken>*</AsteriskToken>
                                                    </ZeroOrMoreQuantifier>
                                                    <SimpleGrouping>
                                                      <OpenParenToken>(</OpenParenToken>
                                                      <Alternation>
                                                        <Alternation>
                                                          <Alternation>
                                                            <Alternation>
                                                              <Alternation>
                                                                <Alternation>
                                                                  <Alternation>
                                                                    <Alternation>
                                                                      <Sequence>
                                                                        <Text>
                                                                          <TextToken>01</TextToken>
                                                                        </Text>
                                                                      </Sequence>
                                                                      <BarToken>|</BarToken>
                                                                      <Sequence>
                                                                        <Text>
                                                                          <TextToken>12</TextToken>
                                                                        </Text>
                                                                      </Sequence>
                                                                    </Alternation>
                                                                    <BarToken>|</BarToken>
                                                                    <Sequence>
                                                                      <Text>
                                                                        <TextToken>23</TextToken>
                                                                      </Text>
                                                                    </Sequence>
                                                                  </Alternation>
                                                                  <BarToken>|</BarToken>
                                                                  <Sequence>
                                                                    <Text>
                                                                      <TextToken>34</TextToken>
                                                                    </Text>
                                                                  </Sequence>
                                                                </Alternation>
                                                                <BarToken>|</BarToken>
                                                                <Sequence>
                                                                  <Text>
                                                                    <TextToken>45</TextToken>
                                                                  </Text>
                                                                </Sequence>
                                                              </Alternation>
                                                              <BarToken>|</BarToken>
                                                              <Sequence>
                                                                <Text>
                                                                  <TextToken>56</TextToken>
                                                                </Text>
                                                              </Sequence>
                                                            </Alternation>
                                                            <BarToken>|</BarToken>
                                                            <Sequence>
                                                              <Text>
                                                                <TextToken>67</TextToken>
                                                              </Text>
                                                            </Sequence>
                                                          </Alternation>
                                                          <BarToken>|</BarToken>
                                                          <Sequence>
                                                            <Text>
                                                              <TextToken>78</TextToken>
                                                            </Text>
                                                          </Sequence>
                                                        </Alternation>
                                                        <BarToken>|</BarToken>
                                                        <Sequence>
                                                          <Text>
                                                            <TextToken>89</TextToken>
                                                          </Text>
                                                        </Sequence>
                                                      </Alternation>
                                                      <CloseParenToken>)</CloseParenToken>
                                                    </SimpleGrouping>
                                                    <ZeroOrMoreQuantifier>
                                                      <Wildcard>
                                                        <DotToken>.</DotToken>
                                                      </Wildcard>
                                                      <AsteriskToken>*</AsteriskToken>
                                                    </ZeroOrMoreQuantifier>
                                                  </Sequence>
                                                  <CloseParenToken>)</CloseParenToken>
                                                </SimpleGrouping>
                                                <Alternation>
                                                  <Sequence>
                                                    <CharacterClass>
                                                      <OpenBracketToken>[</OpenBracketToken>
                                                      <Sequence>
                                                        <Text>
                                                          <TextToken>0</TextToken>
                                                        </Text>
                                                        <CharacterClassSubtraction>
                                                          <MinusToken>-</MinusToken>
                                                          <CharacterClass>
                                                            <OpenBracketToken>[</OpenBracketToken>
                                                            <Sequence>
                                                              <Text>
                                                                <TextToken>0</TextToken>
                                                              </Text>
                                                            </Sequence>
                                                            <CloseBracketToken>]</CloseBracketToken>
                                                          </CharacterClass>
                                                        </CharacterClassSubtraction>
                                                      </Sequence>
                                                      <CloseBracketToken>]</CloseBracketToken>
                                                    </CharacterClass>
                                                  </Sequence>
                                                  <BarToken>|</BarToken>
                                                  <Sequence>
                                                    <ZeroOrMoreQuantifier>
                                                      <Wildcard>
                                                        <DotToken>.</DotToken>
                                                      </Wildcard>
                                                      <AsteriskToken>*</AsteriskToken>
                                                    </ZeroOrMoreQuantifier>
                                                  </Sequence>
                                                </Alternation>
                                                <CloseParenToken>)</CloseParenToken>
                                              </ConditionalExpressionGrouping>
                                            </Sequence>
                                            <CloseParenToken>)</CloseParenToken>
                                          </SimpleGrouping>
                                        </Sequence>
                                        <BarToken>|</BarToken>
                                        <Sequence>
                                          <CharacterClass>
                                            <OpenBracketToken>[</OpenBracketToken>
                                            <Sequence>
                                              <Text>
                                                <TextToken>0</TextToken>
                                              </Text>
                                              <CharacterClassSubtraction>
                                                <MinusToken>-</MinusToken>
                                                <CharacterClass>
                                                  <OpenBracketToken>[</OpenBracketToken>
                                                  <Sequence>
                                                    <Text>
                                                      <TextToken>0</TextToken>
                                                    </Text>
                                                  </Sequence>
                                                  <CloseBracketToken>]</CloseBracketToken>
                                                </CharacterClass>
                                              </CharacterClassSubtraction>
                                            </Sequence>
                                            <CloseBracketToken>]</CloseBracketToken>
                                          </CharacterClass>
                                        </Sequence>
                                      </Alternation>
                                      <CloseParenToken>)</CloseParenToken>
                                    </ConditionalExpressionGrouping>
                                  </Sequence>
                                  <BarToken>|</BarToken>
                                  <Sequence>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <Text>
                                          <TextToken>0</TextToken>
                                        </Text>
                                        <CharacterClassSubtraction>
                                          <MinusToken>-</MinusToken>
                                          <CharacterClass>
                                            <OpenBracketToken>[</OpenBracketToken>
                                            <Sequence>
                                              <Text>
                                                <TextToken>0</TextToken>
                                              </Text>
                                            </Sequence>
                                            <CloseBracketToken>]</CloseBracketToken>
                                          </CharacterClass>
                                        </CharacterClassSubtraction>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                  </Sequence>
                                </Alternation>
                                <CloseParenToken>)</CloseParenToken>
                              </ConditionalExpressionGrouping>
                            </Sequence>
                            <BarToken>|</BarToken>
                            <Sequence>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                  <CharacterClassSubtraction>
                                    <MinusToken>-</MinusToken>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <Text>
                                          <TextToken>0</TextToken>
                                        </Text>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                  </CharacterClassSubtraction>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </Sequence>
                          </Alternation>
                          <CloseParenToken>)</CloseParenToken>
                        </ConditionalExpressionGrouping>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>0</TextToken>
                            </Text>
                            <CharacterClassSubtraction>
                              <MinusToken>-</MinusToken>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </CharacterClassSubtraction>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..144)" Text="(?(.*[a-z].*)(?(.*[A-Z].*)(?(.*[0-9].*)(?(.{4,8})((?(.*(01|12|23|34|45|56|67|78|89).*)[0-[0]]|.*))|[0-[0]])|[0-[0]])|[0-[0]])|[0-[0]])" />
                <Capture Name="1" Span="[59..108)" Text="((?(.*(01|12|23|34|45|56|67|78|89).*)[0-[0]]|.*))" />
                <Capture Name="2" Span="[65..93)" Text="(01|12|23|34|45|56|67|78|89)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestComplex5()
    {
        var twoLower = ".*[a-z].*[a-z].*";
        var twoUpper = ".*[A-Z].*[A-Z].*";
        var threeDigits = ".*[0-9].*[0-9].*[0-9].*";
        var oneSpecial = @".*[\x21-\x2F\x3A-\x40\x5B-x60\x7B-\x7E].*";
        var Not_countUp = Not(".*(012|123|234|345|456|567|678|789).*");
        var Not_countDown = Not(".*(987|876|765|654|543|432|321|210).*");
        var length = "[!-~]{8,12}";
        var contains_first_P_and_then_r = ".*X.*r.*";
        var all = And(twoLower, twoUpper, threeDigits, oneSpecial, Not_countUp, Not_countDown, length, contains_first_P_and_then_r);

        Test($"""
            @"\b{all}\b"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <AnchorEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>b</TextToken>
                  </AnchorEscape>
                  <ConditionalExpressionGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <SimpleGrouping>
                      <OpenParenToken>(</OpenParenToken>
                      <Sequence>
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>a</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <Text>
                                <TextToken>z</TextToken>
                              </Text>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <CharacterClassRange>
                              <Text>
                                <TextToken>a</TextToken>
                              </Text>
                              <MinusToken>-</MinusToken>
                              <Text>
                                <TextToken>z</TextToken>
                              </Text>
                            </CharacterClassRange>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                        <ZeroOrMoreQuantifier>
                          <Wildcard>
                            <DotToken>.</DotToken>
                          </Wildcard>
                          <AsteriskToken>*</AsteriskToken>
                        </ZeroOrMoreQuantifier>
                      </Sequence>
                      <CloseParenToken>)</CloseParenToken>
                    </SimpleGrouping>
                    <Alternation>
                      <Sequence>
                        <ConditionalExpressionGrouping>
                          <OpenParenToken>(</OpenParenToken>
                          <QuestionToken>?</QuestionToken>
                          <SimpleGrouping>
                            <OpenParenToken>(</OpenParenToken>
                            <Sequence>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <CharacterClassRange>
                                    <Text>
                                      <TextToken>A</TextToken>
                                    </Text>
                                    <MinusToken>-</MinusToken>
                                    <Text>
                                      <TextToken>Z</TextToken>
                                    </Text>
                                  </CharacterClassRange>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <CharacterClassRange>
                                    <Text>
                                      <TextToken>A</TextToken>
                                    </Text>
                                    <MinusToken>-</MinusToken>
                                    <Text>
                                      <TextToken>Z</TextToken>
                                    </Text>
                                  </CharacterClassRange>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                              <ZeroOrMoreQuantifier>
                                <Wildcard>
                                  <DotToken>.</DotToken>
                                </Wildcard>
                                <AsteriskToken>*</AsteriskToken>
                              </ZeroOrMoreQuantifier>
                            </Sequence>
                            <CloseParenToken>)</CloseParenToken>
                          </SimpleGrouping>
                          <Alternation>
                            <Sequence>
                              <ConditionalExpressionGrouping>
                                <OpenParenToken>(</OpenParenToken>
                                <QuestionToken>?</QuestionToken>
                                <SimpleGrouping>
                                  <OpenParenToken>(</OpenParenToken>
                                  <Sequence>
                                    <ZeroOrMoreQuantifier>
                                      <Wildcard>
                                        <DotToken>.</DotToken>
                                      </Wildcard>
                                      <AsteriskToken>*</AsteriskToken>
                                    </ZeroOrMoreQuantifier>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <CharacterClassRange>
                                          <Text>
                                            <TextToken>0</TextToken>
                                          </Text>
                                          <MinusToken>-</MinusToken>
                                          <Text>
                                            <TextToken>9</TextToken>
                                          </Text>
                                        </CharacterClassRange>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                    <ZeroOrMoreQuantifier>
                                      <Wildcard>
                                        <DotToken>.</DotToken>
                                      </Wildcard>
                                      <AsteriskToken>*</AsteriskToken>
                                    </ZeroOrMoreQuantifier>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <CharacterClassRange>
                                          <Text>
                                            <TextToken>0</TextToken>
                                          </Text>
                                          <MinusToken>-</MinusToken>
                                          <Text>
                                            <TextToken>9</TextToken>
                                          </Text>
                                        </CharacterClassRange>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                    <ZeroOrMoreQuantifier>
                                      <Wildcard>
                                        <DotToken>.</DotToken>
                                      </Wildcard>
                                      <AsteriskToken>*</AsteriskToken>
                                    </ZeroOrMoreQuantifier>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <CharacterClassRange>
                                          <Text>
                                            <TextToken>0</TextToken>
                                          </Text>
                                          <MinusToken>-</MinusToken>
                                          <Text>
                                            <TextToken>9</TextToken>
                                          </Text>
                                        </CharacterClassRange>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                    <ZeroOrMoreQuantifier>
                                      <Wildcard>
                                        <DotToken>.</DotToken>
                                      </Wildcard>
                                      <AsteriskToken>*</AsteriskToken>
                                    </ZeroOrMoreQuantifier>
                                  </Sequence>
                                  <CloseParenToken>)</CloseParenToken>
                                </SimpleGrouping>
                                <Alternation>
                                  <Sequence>
                                    <ConditionalExpressionGrouping>
                                      <OpenParenToken>(</OpenParenToken>
                                      <QuestionToken>?</QuestionToken>
                                      <SimpleGrouping>
                                        <OpenParenToken>(</OpenParenToken>
                                        <Sequence>
                                          <ZeroOrMoreQuantifier>
                                            <Wildcard>
                                              <DotToken>.</DotToken>
                                            </Wildcard>
                                            <AsteriskToken>*</AsteriskToken>
                                          </ZeroOrMoreQuantifier>
                                          <CharacterClass>
                                            <OpenBracketToken>[</OpenBracketToken>
                                            <Sequence>
                                              <CharacterClassRange>
                                                <HexEscape>
                                                  <BackslashToken>\</BackslashToken>
                                                  <TextToken>x</TextToken>
                                                  <TextToken>21</TextToken>
                                                </HexEscape>
                                                <MinusToken>-</MinusToken>
                                                <HexEscape>
                                                  <BackslashToken>\</BackslashToken>
                                                  <TextToken>x</TextToken>
                                                  <TextToken>2F</TextToken>
                                                </HexEscape>
                                              </CharacterClassRange>
                                              <CharacterClassRange>
                                                <HexEscape>
                                                  <BackslashToken>\</BackslashToken>
                                                  <TextToken>x</TextToken>
                                                  <TextToken>3A</TextToken>
                                                </HexEscape>
                                                <MinusToken>-</MinusToken>
                                                <HexEscape>
                                                  <BackslashToken>\</BackslashToken>
                                                  <TextToken>x</TextToken>
                                                  <TextToken>40</TextToken>
                                                </HexEscape>
                                              </CharacterClassRange>
                                              <CharacterClassRange>
                                                <HexEscape>
                                                  <BackslashToken>\</BackslashToken>
                                                  <TextToken>x</TextToken>
                                                  <TextToken>5B</TextToken>
                                                </HexEscape>
                                                <MinusToken>-</MinusToken>
                                                <Text>
                                                  <TextToken>x</TextToken>
                                                </Text>
                                              </CharacterClassRange>
                                              <Text>
                                                <TextToken>60</TextToken>
                                              </Text>
                                              <CharacterClassRange>
                                                <HexEscape>
                                                  <BackslashToken>\</BackslashToken>
                                                  <TextToken>x</TextToken>
                                                  <TextToken>7B</TextToken>
                                                </HexEscape>
                                                <MinusToken>-</MinusToken>
                                                <HexEscape>
                                                  <BackslashToken>\</BackslashToken>
                                                  <TextToken>x</TextToken>
                                                  <TextToken>7E</TextToken>
                                                </HexEscape>
                                              </CharacterClassRange>
                                            </Sequence>
                                            <CloseBracketToken>]</CloseBracketToken>
                                          </CharacterClass>
                                          <ZeroOrMoreQuantifier>
                                            <Wildcard>
                                              <DotToken>.</DotToken>
                                            </Wildcard>
                                            <AsteriskToken>*</AsteriskToken>
                                          </ZeroOrMoreQuantifier>
                                        </Sequence>
                                        <CloseParenToken>)</CloseParenToken>
                                      </SimpleGrouping>
                                      <Alternation>
                                        <Sequence>
                                          <ConditionalExpressionGrouping>
                                            <OpenParenToken>(</OpenParenToken>
                                            <QuestionToken>?</QuestionToken>
                                            <SimpleGrouping>
                                              <OpenParenToken>(</OpenParenToken>
                                              <Sequence>
                                                <ConditionalExpressionGrouping>
                                                  <OpenParenToken>(</OpenParenToken>
                                                  <QuestionToken>?</QuestionToken>
                                                  <SimpleGrouping>
                                                    <OpenParenToken>(</OpenParenToken>
                                                    <Sequence>
                                                      <ZeroOrMoreQuantifier>
                                                        <Wildcard>
                                                          <DotToken>.</DotToken>
                                                        </Wildcard>
                                                        <AsteriskToken>*</AsteriskToken>
                                                      </ZeroOrMoreQuantifier>
                                                      <SimpleGrouping>
                                                        <OpenParenToken>(</OpenParenToken>
                                                        <Alternation>
                                                          <Alternation>
                                                            <Alternation>
                                                              <Alternation>
                                                                <Alternation>
                                                                  <Alternation>
                                                                    <Alternation>
                                                                      <Sequence>
                                                                        <Text>
                                                                          <TextToken>012</TextToken>
                                                                        </Text>
                                                                      </Sequence>
                                                                      <BarToken>|</BarToken>
                                                                      <Sequence>
                                                                        <Text>
                                                                          <TextToken>123</TextToken>
                                                                        </Text>
                                                                      </Sequence>
                                                                    </Alternation>
                                                                    <BarToken>|</BarToken>
                                                                    <Sequence>
                                                                      <Text>
                                                                        <TextToken>234</TextToken>
                                                                      </Text>
                                                                    </Sequence>
                                                                  </Alternation>
                                                                  <BarToken>|</BarToken>
                                                                  <Sequence>
                                                                    <Text>
                                                                      <TextToken>345</TextToken>
                                                                    </Text>
                                                                  </Sequence>
                                                                </Alternation>
                                                                <BarToken>|</BarToken>
                                                                <Sequence>
                                                                  <Text>
                                                                    <TextToken>456</TextToken>
                                                                  </Text>
                                                                </Sequence>
                                                              </Alternation>
                                                              <BarToken>|</BarToken>
                                                              <Sequence>
                                                                <Text>
                                                                  <TextToken>567</TextToken>
                                                                </Text>
                                                              </Sequence>
                                                            </Alternation>
                                                            <BarToken>|</BarToken>
                                                            <Sequence>
                                                              <Text>
                                                                <TextToken>678</TextToken>
                                                              </Text>
                                                            </Sequence>
                                                          </Alternation>
                                                          <BarToken>|</BarToken>
                                                          <Sequence>
                                                            <Text>
                                                              <TextToken>789</TextToken>
                                                            </Text>
                                                          </Sequence>
                                                        </Alternation>
                                                        <CloseParenToken>)</CloseParenToken>
                                                      </SimpleGrouping>
                                                      <ZeroOrMoreQuantifier>
                                                        <Wildcard>
                                                          <DotToken>.</DotToken>
                                                        </Wildcard>
                                                        <AsteriskToken>*</AsteriskToken>
                                                      </ZeroOrMoreQuantifier>
                                                    </Sequence>
                                                    <CloseParenToken>)</CloseParenToken>
                                                  </SimpleGrouping>
                                                  <Alternation>
                                                    <Sequence>
                                                      <CharacterClass>
                                                        <OpenBracketToken>[</OpenBracketToken>
                                                        <Sequence>
                                                          <Text>
                                                            <TextToken>0</TextToken>
                                                          </Text>
                                                          <CharacterClassSubtraction>
                                                            <MinusToken>-</MinusToken>
                                                            <CharacterClass>
                                                              <OpenBracketToken>[</OpenBracketToken>
                                                              <Sequence>
                                                                <Text>
                                                                  <TextToken>0</TextToken>
                                                                </Text>
                                                              </Sequence>
                                                              <CloseBracketToken>]</CloseBracketToken>
                                                            </CharacterClass>
                                                          </CharacterClassSubtraction>
                                                        </Sequence>
                                                        <CloseBracketToken>]</CloseBracketToken>
                                                      </CharacterClass>
                                                    </Sequence>
                                                    <BarToken>|</BarToken>
                                                    <Sequence>
                                                      <ZeroOrMoreQuantifier>
                                                        <Wildcard>
                                                          <DotToken>.</DotToken>
                                                        </Wildcard>
                                                        <AsteriskToken>*</AsteriskToken>
                                                      </ZeroOrMoreQuantifier>
                                                    </Sequence>
                                                  </Alternation>
                                                  <CloseParenToken>)</CloseParenToken>
                                                </ConditionalExpressionGrouping>
                                              </Sequence>
                                              <CloseParenToken>)</CloseParenToken>
                                            </SimpleGrouping>
                                            <Alternation>
                                              <Sequence>
                                                <ConditionalExpressionGrouping>
                                                  <OpenParenToken>(</OpenParenToken>
                                                  <QuestionToken>?</QuestionToken>
                                                  <SimpleGrouping>
                                                    <OpenParenToken>(</OpenParenToken>
                                                    <Sequence>
                                                      <ConditionalExpressionGrouping>
                                                        <OpenParenToken>(</OpenParenToken>
                                                        <QuestionToken>?</QuestionToken>
                                                        <SimpleGrouping>
                                                          <OpenParenToken>(</OpenParenToken>
                                                          <Sequence>
                                                            <ZeroOrMoreQuantifier>
                                                              <Wildcard>
                                                                <DotToken>.</DotToken>
                                                              </Wildcard>
                                                              <AsteriskToken>*</AsteriskToken>
                                                            </ZeroOrMoreQuantifier>
                                                            <SimpleGrouping>
                                                              <OpenParenToken>(</OpenParenToken>
                                                              <Alternation>
                                                                <Alternation>
                                                                  <Alternation>
                                                                    <Alternation>
                                                                      <Alternation>
                                                                        <Alternation>
                                                                          <Alternation>
                                                                            <Sequence>
                                                                              <Text>
                                                                                <TextToken>987</TextToken>
                                                                              </Text>
                                                                            </Sequence>
                                                                            <BarToken>|</BarToken>
                                                                            <Sequence>
                                                                              <Text>
                                                                                <TextToken>876</TextToken>
                                                                              </Text>
                                                                            </Sequence>
                                                                          </Alternation>
                                                                          <BarToken>|</BarToken>
                                                                          <Sequence>
                                                                            <Text>
                                                                              <TextToken>765</TextToken>
                                                                            </Text>
                                                                          </Sequence>
                                                                        </Alternation>
                                                                        <BarToken>|</BarToken>
                                                                        <Sequence>
                                                                          <Text>
                                                                            <TextToken>654</TextToken>
                                                                          </Text>
                                                                        </Sequence>
                                                                      </Alternation>
                                                                      <BarToken>|</BarToken>
                                                                      <Sequence>
                                                                        <Text>
                                                                          <TextToken>543</TextToken>
                                                                        </Text>
                                                                      </Sequence>
                                                                    </Alternation>
                                                                    <BarToken>|</BarToken>
                                                                    <Sequence>
                                                                      <Text>
                                                                        <TextToken>432</TextToken>
                                                                      </Text>
                                                                    </Sequence>
                                                                  </Alternation>
                                                                  <BarToken>|</BarToken>
                                                                  <Sequence>
                                                                    <Text>
                                                                      <TextToken>321</TextToken>
                                                                    </Text>
                                                                  </Sequence>
                                                                </Alternation>
                                                                <BarToken>|</BarToken>
                                                                <Sequence>
                                                                  <Text>
                                                                    <TextToken>210</TextToken>
                                                                  </Text>
                                                                </Sequence>
                                                              </Alternation>
                                                              <CloseParenToken>)</CloseParenToken>
                                                            </SimpleGrouping>
                                                            <ZeroOrMoreQuantifier>
                                                              <Wildcard>
                                                                <DotToken>.</DotToken>
                                                              </Wildcard>
                                                              <AsteriskToken>*</AsteriskToken>
                                                            </ZeroOrMoreQuantifier>
                                                          </Sequence>
                                                          <CloseParenToken>)</CloseParenToken>
                                                        </SimpleGrouping>
                                                        <Alternation>
                                                          <Sequence>
                                                            <CharacterClass>
                                                              <OpenBracketToken>[</OpenBracketToken>
                                                              <Sequence>
                                                                <Text>
                                                                  <TextToken>0</TextToken>
                                                                </Text>
                                                                <CharacterClassSubtraction>
                                                                  <MinusToken>-</MinusToken>
                                                                  <CharacterClass>
                                                                    <OpenBracketToken>[</OpenBracketToken>
                                                                    <Sequence>
                                                                      <Text>
                                                                        <TextToken>0</TextToken>
                                                                      </Text>
                                                                    </Sequence>
                                                                    <CloseBracketToken>]</CloseBracketToken>
                                                                  </CharacterClass>
                                                                </CharacterClassSubtraction>
                                                              </Sequence>
                                                              <CloseBracketToken>]</CloseBracketToken>
                                                            </CharacterClass>
                                                          </Sequence>
                                                          <BarToken>|</BarToken>
                                                          <Sequence>
                                                            <ZeroOrMoreQuantifier>
                                                              <Wildcard>
                                                                <DotToken>.</DotToken>
                                                              </Wildcard>
                                                              <AsteriskToken>*</AsteriskToken>
                                                            </ZeroOrMoreQuantifier>
                                                          </Sequence>
                                                        </Alternation>
                                                        <CloseParenToken>)</CloseParenToken>
                                                      </ConditionalExpressionGrouping>
                                                    </Sequence>
                                                    <CloseParenToken>)</CloseParenToken>
                                                  </SimpleGrouping>
                                                  <Alternation>
                                                    <Sequence>
                                                      <ConditionalExpressionGrouping>
                                                        <OpenParenToken>(</OpenParenToken>
                                                        <QuestionToken>?</QuestionToken>
                                                        <SimpleGrouping>
                                                          <OpenParenToken>(</OpenParenToken>
                                                          <Sequence>
                                                            <ClosedRangeNumericQuantifier>
                                                              <CharacterClass>
                                                                <OpenBracketToken>[</OpenBracketToken>
                                                                <Sequence>
                                                                  <CharacterClassRange>
                                                                    <Text>
                                                                      <TextToken>!</TextToken>
                                                                    </Text>
                                                                    <MinusToken>-</MinusToken>
                                                                    <Text>
                                                                      <TextToken>~</TextToken>
                                                                    </Text>
                                                                  </CharacterClassRange>
                                                                </Sequence>
                                                                <CloseBracketToken>]</CloseBracketToken>
                                                              </CharacterClass>
                                                              <OpenBraceToken>{</OpenBraceToken>
                                                              <NumberToken value="8">8</NumberToken>
                                                              <CommaToken>,</CommaToken>
                                                              <NumberToken value="12">12</NumberToken>
                                                              <CloseBraceToken>}</CloseBraceToken>
                                                            </ClosedRangeNumericQuantifier>
                                                          </Sequence>
                                                          <CloseParenToken>)</CloseParenToken>
                                                        </SimpleGrouping>
                                                        <Alternation>
                                                          <Sequence>
                                                            <SimpleGrouping>
                                                              <OpenParenToken>(</OpenParenToken>
                                                              <Sequence>
                                                                <ZeroOrMoreQuantifier>
                                                                  <Wildcard>
                                                                    <DotToken>.</DotToken>
                                                                  </Wildcard>
                                                                  <AsteriskToken>*</AsteriskToken>
                                                                </ZeroOrMoreQuantifier>
                                                                <Text>
                                                                  <TextToken>X</TextToken>
                                                                </Text>
                                                                <ZeroOrMoreQuantifier>
                                                                  <Wildcard>
                                                                    <DotToken>.</DotToken>
                                                                  </Wildcard>
                                                                  <AsteriskToken>*</AsteriskToken>
                                                                </ZeroOrMoreQuantifier>
                                                                <Text>
                                                                  <TextToken>r</TextToken>
                                                                </Text>
                                                                <ZeroOrMoreQuantifier>
                                                                  <Wildcard>
                                                                    <DotToken>.</DotToken>
                                                                  </Wildcard>
                                                                  <AsteriskToken>*</AsteriskToken>
                                                                </ZeroOrMoreQuantifier>
                                                              </Sequence>
                                                              <CloseParenToken>)</CloseParenToken>
                                                            </SimpleGrouping>
                                                          </Sequence>
                                                          <BarToken>|</BarToken>
                                                          <Sequence>
                                                            <CharacterClass>
                                                              <OpenBracketToken>[</OpenBracketToken>
                                                              <Sequence>
                                                                <Text>
                                                                  <TextToken>0</TextToken>
                                                                </Text>
                                                                <CharacterClassSubtraction>
                                                                  <MinusToken>-</MinusToken>
                                                                  <CharacterClass>
                                                                    <OpenBracketToken>[</OpenBracketToken>
                                                                    <Sequence>
                                                                      <Text>
                                                                        <TextToken>0</TextToken>
                                                                      </Text>
                                                                    </Sequence>
                                                                    <CloseBracketToken>]</CloseBracketToken>
                                                                  </CharacterClass>
                                                                </CharacterClassSubtraction>
                                                              </Sequence>
                                                              <CloseBracketToken>]</CloseBracketToken>
                                                            </CharacterClass>
                                                          </Sequence>
                                                        </Alternation>
                                                        <CloseParenToken>)</CloseParenToken>
                                                      </ConditionalExpressionGrouping>
                                                    </Sequence>
                                                    <BarToken>|</BarToken>
                                                    <Sequence>
                                                      <CharacterClass>
                                                        <OpenBracketToken>[</OpenBracketToken>
                                                        <Sequence>
                                                          <Text>
                                                            <TextToken>0</TextToken>
                                                          </Text>
                                                          <CharacterClassSubtraction>
                                                            <MinusToken>-</MinusToken>
                                                            <CharacterClass>
                                                              <OpenBracketToken>[</OpenBracketToken>
                                                              <Sequence>
                                                                <Text>
                                                                  <TextToken>0</TextToken>
                                                                </Text>
                                                              </Sequence>
                                                              <CloseBracketToken>]</CloseBracketToken>
                                                            </CharacterClass>
                                                          </CharacterClassSubtraction>
                                                        </Sequence>
                                                        <CloseBracketToken>]</CloseBracketToken>
                                                      </CharacterClass>
                                                    </Sequence>
                                                  </Alternation>
                                                  <CloseParenToken>)</CloseParenToken>
                                                </ConditionalExpressionGrouping>
                                              </Sequence>
                                              <BarToken>|</BarToken>
                                              <Sequence>
                                                <CharacterClass>
                                                  <OpenBracketToken>[</OpenBracketToken>
                                                  <Sequence>
                                                    <Text>
                                                      <TextToken>0</TextToken>
                                                    </Text>
                                                    <CharacterClassSubtraction>
                                                      <MinusToken>-</MinusToken>
                                                      <CharacterClass>
                                                        <OpenBracketToken>[</OpenBracketToken>
                                                        <Sequence>
                                                          <Text>
                                                            <TextToken>0</TextToken>
                                                          </Text>
                                                        </Sequence>
                                                        <CloseBracketToken>]</CloseBracketToken>
                                                      </CharacterClass>
                                                    </CharacterClassSubtraction>
                                                  </Sequence>
                                                  <CloseBracketToken>]</CloseBracketToken>
                                                </CharacterClass>
                                              </Sequence>
                                            </Alternation>
                                            <CloseParenToken>)</CloseParenToken>
                                          </ConditionalExpressionGrouping>
                                        </Sequence>
                                        <BarToken>|</BarToken>
                                        <Sequence>
                                          <CharacterClass>
                                            <OpenBracketToken>[</OpenBracketToken>
                                            <Sequence>
                                              <Text>
                                                <TextToken>0</TextToken>
                                              </Text>
                                              <CharacterClassSubtraction>
                                                <MinusToken>-</MinusToken>
                                                <CharacterClass>
                                                  <OpenBracketToken>[</OpenBracketToken>
                                                  <Sequence>
                                                    <Text>
                                                      <TextToken>0</TextToken>
                                                    </Text>
                                                  </Sequence>
                                                  <CloseBracketToken>]</CloseBracketToken>
                                                </CharacterClass>
                                              </CharacterClassSubtraction>
                                            </Sequence>
                                            <CloseBracketToken>]</CloseBracketToken>
                                          </CharacterClass>
                                        </Sequence>
                                      </Alternation>
                                      <CloseParenToken>)</CloseParenToken>
                                    </ConditionalExpressionGrouping>
                                  </Sequence>
                                  <BarToken>|</BarToken>
                                  <Sequence>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <Text>
                                          <TextToken>0</TextToken>
                                        </Text>
                                        <CharacterClassSubtraction>
                                          <MinusToken>-</MinusToken>
                                          <CharacterClass>
                                            <OpenBracketToken>[</OpenBracketToken>
                                            <Sequence>
                                              <Text>
                                                <TextToken>0</TextToken>
                                              </Text>
                                            </Sequence>
                                            <CloseBracketToken>]</CloseBracketToken>
                                          </CharacterClass>
                                        </CharacterClassSubtraction>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                  </Sequence>
                                </Alternation>
                                <CloseParenToken>)</CloseParenToken>
                              </ConditionalExpressionGrouping>
                            </Sequence>
                            <BarToken>|</BarToken>
                            <Sequence>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                  <CharacterClassSubtraction>
                                    <MinusToken>-</MinusToken>
                                    <CharacterClass>
                                      <OpenBracketToken>[</OpenBracketToken>
                                      <Sequence>
                                        <Text>
                                          <TextToken>0</TextToken>
                                        </Text>
                                      </Sequence>
                                      <CloseBracketToken>]</CloseBracketToken>
                                    </CharacterClass>
                                  </CharacterClassSubtraction>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </Sequence>
                          </Alternation>
                          <CloseParenToken>)</CloseParenToken>
                        </ConditionalExpressionGrouping>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <CharacterClass>
                          <OpenBracketToken>[</OpenBracketToken>
                          <Sequence>
                            <Text>
                              <TextToken>0</TextToken>
                            </Text>
                            <CharacterClassSubtraction>
                              <MinusToken>-</MinusToken>
                              <CharacterClass>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Text>
                                    <TextToken>0</TextToken>
                                  </Text>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </CharacterClass>
                            </CharacterClassSubtraction>
                          </Sequence>
                          <CloseBracketToken>]</CloseBracketToken>
                        </CharacterClass>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </ConditionalExpressionGrouping>
                  <AnchorEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>b</TextToken>
                  </AnchorEscape>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..326)" Text="\b(?(.*[a-z].*[a-z].*)(?(.*[A-Z].*[A-Z].*)(?(.*[0-9].*[0-9].*[0-9].*)(?(.*[\x21-\x2F\x3A-\x40\x5B-x60\x7B-\x7E].*)(?((?(.*(012|123|234|345|456|567|678|789).*)[0-[0]]|.*))(?((?(.*(987|876|765|654|543|432|321|210).*)[0-[0]]|.*))(?([!-~]{8,12})(.*X.*r.*)|[0-[0]])|[0-[0]])|[0-[0]])|[0-[0]])|[0-[0]])|[0-[0]])|[0-[0]])\b" />
                <Capture Name="1" Span="[132..165)" Text="(012|123|234|345|456|567|678|789)" />
                <Capture Name="2" Span="[188..221)" Text="(987|876|765|654|543|432|321|210)" />
                <Capture Name="3" Span="[251..261)" Text="(.*X.*r.*)" />
              </Captures>
            </Tree>
            """, RegexOptions.None);
    }

    [Fact]
    public void TestComplex6()
        => Test("""
            @"pa[5\$s]{2}w[o0]rd$"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>pa</TextToken>
                  </Text>
                  <ExactNumericQuantifier>
                    <CharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <Sequence>
                        <Text>
                          <TextToken>5</TextToken>
                        </Text>
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>$</TextToken>
                        </SimpleEscape>
                        <Text>
                          <TextToken>s</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </CharacterClass>
                    <OpenBraceToken>{</OpenBraceToken>
                    <NumberToken value="2">2</NumberToken>
                    <CloseBraceToken>}</CloseBraceToken>
                  </ExactNumericQuantifier>
                  <Text>
                    <TextToken>w</TextToken>
                  </Text>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>o0</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                  <Text>
                    <TextToken>rd</TextToken>
                  </Text>
                  <EndAnchor>
                    <DollarToken>$</DollarToken>
                  </EndAnchor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..29)" Text="pa[5\$s]{2}w[o0]rd$" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76731")]
    public void TestCategoryWithNumber()
        => Test("""
            @"[\p{IsLatin-1Supplement}]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CategoryEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>p</TextToken>
                        <OpenBraceToken>{</OpenBraceToken>
                        <EscapeCategoryToken>IsLatin-1Supplement</EscapeCategoryToken>
                        <CloseBraceToken>}</CloseBraceToken>
                      </CategoryEscape>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..35)" Text="[\p{IsLatin-1Supplement}]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76731")]
    public void TestCategoryWithUnderscore()
        => Test("""
            @"[\p{_xmlW}]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CategoryEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>p</TextToken>
                        <OpenBraceToken>{</OpenBraceToken>
                        <EscapeCategoryToken>_xmlW</EscapeCategoryToken>
                        <CloseBraceToken>}</CloseBraceToken>
                      </CategoryEscape>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..21)" Text="[\p{_xmlW}]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);

    [Fact]
    public void TestMinusAsCharacterClassStart()
        => Test("""
            @"[-[:L:]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Text>
                        <TextToken>-[:L:</TextToken>
                      </Text>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
              <Captures>
                <Capture Name="0" Span="[10..17)" Text="[-[:L:]" />
              </Captures>
            </Tree>
            """, RegexOptions.None);
}
