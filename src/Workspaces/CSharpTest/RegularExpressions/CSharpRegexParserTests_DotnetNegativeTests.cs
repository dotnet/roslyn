// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.RegularExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.RegularExpressions
{
    public partial class CSharpRegexParserTests
    {
#if false
        [Theory]
        [InlineData(@"(\w)\1+.\b", RegexOptions.None)]
        public void PosTests(string val, RegexOptions options)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Fact]");
            builder.AppendLine("public void ReferenceTest" + testNum++ + "()");
            builder.AppendLine("{");
            builder.Append(@"    Test(" + '@' + '"' + '@' + '"' + '"');
            builder.Append(val);

            builder.Append("" + '"' + '"' + '"' + ',' + '@' + '"');

            var stringText = "" + '@' + '"' + val + '"';
            var token = GetStringToken(stringText);
            var allChars = _service.TryConvertToVirtualChars(token);
            var tree = RegexParser.Parse(allChars, options);

            var actual = TreeToText(tree).Replace("\"", "\"\"");
            builder.Append(actual);

            builder.AppendLine("" + '"' + ", RegexOptions." + options.ToString() + ");");
            builder.AppendLine("}");
            builder.AppendLine();

            File.AppendAllText(@"c:\temp\tests.txt", builder.ToString());
        }

        static int testNum = 72;

        static CSharpRegexParserTests()
        {
            File.Delete(@"c:\temp\tests.txt");
        }
#endif

        [Fact]
        public void NegativeTest0()
        {
            Test(@"@""cat([a-\d]*)dog""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
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
        <TextToken>d</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Cannot include class \d in character range"" Start=""17"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..25)"" />
    <Capture Name=""1"" Span=""[13..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest1()
        {
            Test(@"@""\k<1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken>1</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest2()
        {
            Test(@"@""\k<""", @"<Tree>
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
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest3()
        {
            Test(@"@""\k""", @"<Tree>
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
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..12)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest4()
        {
            Test(@"@""\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""11"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..12)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest5()
        {
            Test(@"@""(?')""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken />
        <QuoteToken />
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""13"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest6()
        {
            Test(@"@""(?<)""", @"<Tree>
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
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""13"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest7()
        {
            Test(@"@""(?)""", @"<Tree>
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
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""11"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest8()
        {
            Test(@"@""(?>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonBacktrackingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken />
      </NonBacktrackingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest9()
        {
            Test(@"@""(?<!""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest10()
        {
            Test(@"@""(?<=""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest11()
        {
            Test(@"@""(?!""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest12()
        {
            Test(@"@""(?=""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest13()
        {
            Test(@"@""(?imn )""", @"<Tree>
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
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Too many )'s"" Start=""16"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest14()
        {
            Test(@"@""(?imn""", @"<Tree>
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
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""10"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest15()
        {
            Test(@"@""(?:""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest16()
        {
            Test(@"@""(?'cat'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <QuoteToken>'</QuoteToken>
        <Sequence />
        <CloseParenToken />
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""17"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
    <Capture Name=""1"" Span=""[10..17)"" />
    <Capture Name=""cat"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest17()
        {
            Test(@"@""(?'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken />
        <QuoteToken />
        <Sequence />
        <CloseParenToken />
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""10"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest18()
        {
            Test(@"@""[^""", @"<Tree>
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
    <Diagnostic Message=""Unterminated [] set"" Start=""12"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..12)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest19()
        {
            Test(@"@""[cat""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""14"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest20()
        {
            Test(@"@""[^cat""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </NegatedCharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""15"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest21()
        {
            Test(@"@""[a-""", @"<Tree>
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
    <Diagnostic Message=""Unterminated [] set"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest22()
        {
            Test(@"@""\p{""", @"<Tree>
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
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest23()
        {
            Test(@"@""\p{cat""", @"<Tree>
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
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest24()
        {
            Test(@"@""\k<cat""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest25()
        {
            Test(@"@""\p{cat}""", @"<Tree>
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
    <Diagnostic Message=""Unknown property 'cat'"" Start=""13"" Length=""3"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest26()
        {
            Test(@"@""\P{cat""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>P</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest27()
        {
            Test(@"@""\P{cat}""", @"<Tree>
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
    <Diagnostic Message=""Unknown property 'cat'"" Start=""13"" Length=""3"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest28()
        {
            Test(@"@""(""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""11"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..11)"" />
    <Capture Name=""1"" Span=""[10..11)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest29()
        {
            Test(@"@""(?""", @"<Tree>
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
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""11"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..12)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest30()
        {
            Test(@"@""(?<""", @"<Tree>
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
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""10"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest31()
        {
            Test(@"@""(?<cat>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken />
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""17"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
    <Capture Name=""1"" Span=""[10..17)"" />
    <Capture Name=""cat"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest32()
        {
            Test(@"@""\P{""", @"<Tree>
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
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest33()
        {
            Test(@"@""\k<>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken>&gt;</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest34()
        {
            Test(@"@""(?(""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
    <Capture Name=""1"" Span=""[12..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest35()
        {
            Test(@"@""(?()|""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""15"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..15)"" />
    <Capture Name=""1"" Span=""[12..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest36()
        {
            Test(@"@""?(a|b)""", @"<Tree>
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
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
    <Capture Name=""1"" Span=""[11..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest37()
        {
            Test(@"@""?((a)""", @"<Tree>
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
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""15"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..15)"" />
    <Capture Name=""1"" Span=""[11..15)"" />
    <Capture Name=""2"" Span=""[12..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest38()
        {
            Test(@"@""?((a)a""", @"<Tree>
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
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""16"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
    <Capture Name=""1"" Span=""[11..16)"" />
    <Capture Name=""2"" Span=""[12..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest39()
        {
            Test(@"@""?((a)a|""", @"<Tree>
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
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""17"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
    <Capture Name=""1"" Span=""[11..17)"" />
    <Capture Name=""2"" Span=""[12..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest40()
        {
            Test(@"@""?((a)a|b""", @"<Tree>
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
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""18"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..18)"" />
    <Capture Name=""1"" Span=""[11..18)"" />
    <Capture Name=""2"" Span=""[12..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest41()
        {
            Test(@"@""(?(?i))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>i</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest42()
        {
            Test(@"@""?(a)""", @"<Tree>
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
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
    <Capture Name=""1"" Span=""[11..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest43()
        {
            Test(@"@""(?(?I))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>I</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest44()
        {
            Test(@"@""(?(?M))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>M</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest45()
        {
            Test(@"@""(?(?s))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>s</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest46()
        {
            Test(@"@""(?(?S))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>S</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest47()
        {
            Test(@"@""(?(?x))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>x</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest48()
        {
            Test(@"@""(?(?X))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>X</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest49()
        {
            Test(@"@""(?(?n))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>n</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest50()
        {
            Test(@"@""(?(?m))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>m</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest51()
        {
            Test(@"@""[a""", @"<Tree>
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
    <Diagnostic Message=""Unterminated [] set"" Start=""12"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..12)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest52()
        {
            Test(@"@""?(a:b)""", @"<Tree>
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
          <Text>
            <TextToken>:</TextToken>
          </Text>
          <Text>
            <TextToken>b</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
    <Capture Name=""1"" Span=""[11..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest53()
        {
            Test(@"@""(?(?""", @"<Tree>
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
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""13"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest54()
        {
            Test(@"@""(?(cat""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>t</TextToken>
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
    <Diagnostic Message=""Not enough )'s"" Start=""16"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
    <Capture Name=""1"" Span=""[12..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest55()
        {
            Test(@"@""(?(cat)|""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>t</TextToken>
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
    <Diagnostic Message=""Not enough )'s"" Start=""18"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..18)"" />
    <Capture Name=""1"" Span=""[12..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest56()
        {
            Test(@"@""foo(?<0>bar)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>f</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>b</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>r</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture number cannot be zero"" Start=""16"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest57()
        {
            Test(@"@""foo(?'0'bar)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>f</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <NumberToken value=""0"">0</NumberToken>
        <QuoteToken>'</QuoteToken>
        <Sequence>
          <Text>
            <TextToken>b</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>r</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture number cannot be zero"" Start=""16"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest58()
        {
            Test(@"@""foo(?<1bar)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>f</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>b</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>r</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""17"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..21)"" />
    <Capture Name=""1"" Span=""[13..21)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest59()
        {
            Test(@"@""foo(?'1bar)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>f</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <NumberToken value=""1"">1</NumberToken>
        <QuoteToken />
        <Sequence>
          <Text>
            <TextToken>b</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>r</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""17"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..21)"" />
    <Capture Name=""1"" Span=""[13..21)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest60()
        {
            Test(@"@""(?(""", @"<Tree>
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
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
    <Capture Name=""1"" Span=""[12..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest61()
        {
            Test(@"@""\p{klsak""", @"<Tree>
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
        <TextToken>k</TextToken>
      </Text>
      <Text>
        <TextToken>l</TextToken>
      </Text>
      <Text>
        <TextToken>s</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>k</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..18)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest62()
        {
            Test(@"@""(?c:cat)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>:</TextToken>
          </Text>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""11"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..18)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest63()
        {
            Test(@"@""(??e:cat)""", @"<Tree>
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
            <TextToken>e</TextToken>
          </Text>
          <Text>
            <TextToken>:</TextToken>
          </Text>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""11"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..19)"" />
    <Capture Name=""1"" Span=""[10..19)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest64()
        {
            Test(@"@""[a-f-[]]+""", @"<Tree>
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
    <Diagnostic Message=""A subtraction must be the last element in a character class"" Start=""14"" Length=""0"" />
    <Diagnostic Message=""Unterminated [] set"" Start=""19"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..19)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest65()
        {
            Test(@"@""[A-[]+""", @"<Tree>
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
                  <TextToken>]</TextToken>
                </Text>
                <Text>
                  <TextToken>+</TextToken>
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
    <Diagnostic Message=""Unterminated [] set"" Start=""16"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest66()
        {
            Test(@"@""(?(?e))""", @"<Tree>
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
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""13"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest67()
        {
            Test(@"@""(?(?a)""", @"<Tree>
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
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""13"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""16"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest68()
        {
            Test(@"@""(?r:cat)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
          <Text>
            <TextToken>r</TextToken>
          </Text>
          <Text>
            <TextToken>:</TextToken>
          </Text>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""11"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..18)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest69()
        {
            Test(@"@""(?(?N))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>N</OptionsToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleOptionsGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest70()
        {
            Test(@"@""[]""", @"<Tree>
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
    <Diagnostic Message=""Unterminated [] set"" Start=""12"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..12)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest71()
        {
            Test(@"@""\x2""", @"<Tree>
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
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""3"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest72()
        {
            Test(@"@""(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
    <Diagnostic Message=""Unterminated (?#...) comment"" Start=""31"" Length=""35"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..66)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
  </Captures>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void NegativeTest73()
        {
            Test(@"@""cat(?(?afdcat)dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>f</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>t</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""15"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""16"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..28)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest74()
        {
            Test(@"@""cat(?(?<cat>cat)dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <CaptureNameToken value=""cat"">cat</CaptureNameToken>
          <GreaterThanToken>&gt;</GreaterThanToken>
          <Sequence>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>t</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </CaptureGrouping>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""13"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..30)"" />
    <Capture Name=""1"" Span=""[15..26)"" />
    <Capture Name=""cat"" Span=""[15..26)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest75()
        {
            Test(@"@""cat(?(?'cat'cat)dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <QuoteToken>'</QuoteToken>
          <CaptureNameToken value=""cat"">cat</CaptureNameToken>
          <QuoteToken>'</QuoteToken>
          <Sequence>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>t</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </CaptureGrouping>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""13"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..30)"" />
    <Capture Name=""1"" Span=""[15..26)"" />
    <Capture Name=""cat"" Span=""[15..26)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest76()
        {
            Test(@"@""cat(?(?#COMMENT)cat)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
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
              <TextToken>#</TextToken>
            </Text>
            <Text>
              <TextToken>C</TextToken>
            </Text>
            <Text>
              <TextToken>O</TextToken>
            </Text>
            <Text>
              <TextToken>M</TextToken>
            </Text>
            <Text>
              <TextToken>M</TextToken>
            </Text>
            <Text>
              <TextToken>E</TextToken>
            </Text>
            <Text>
              <TextToken>N</TextToken>
            </Text>
            <Text>
              <TextToken>T</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions cannot be comments"" Start=""13"" Length=""1"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""15"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""16"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..30)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest77()
        {
            Test(@"@""(?<cat>cat)\w+(?<dog-()*!@>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
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
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>@</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""31"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..41)"" />
    <Capture Name=""1"" Span=""[31..33)"" />
    <Capture Name=""2"" Span=""[10..21)"" />
    <Capture Name=""3"" Span=""[24..41)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..41)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest78()
        {
            Test(@"@""(?<cat>cat)\w+(?<dog-catdog>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <CaptureNameToken value=""catdog"">catdog</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group name catdog"" Start=""31"" Length=""6"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..42)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..42)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..42)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest79()
        {
            Test(@"@""(?<cat>cat)\w+(?<dog-1uosn>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>u</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>s</TextToken>
          </Text>
          <Text>
            <TextToken>n</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""32"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..41)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..41)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..41)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest80()
        {
            Test(@"@""(?<cat>cat)\w+(?<dog-16>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""16"">16</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 16"" Start=""31"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..38)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..38)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..38)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest81()
        {
            Test(@"@""cat(?<->dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
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
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""17"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest82()
        {
            Test(@"@""cat(?<>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""16"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..21)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest83()
        {
            Test(@"@""cat(?<dog<>)_*>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>&lt;</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
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
        <TextToken>&gt;</TextToken>
      </Text>
      <Text>
        <TextToken>d</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>)</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Too many )'s"" Start=""28"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..29)"" />
    <Capture Name=""1"" Span=""[13..22)"" />
    <Capture Name=""dog"" Span=""[13..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest84()
        {
            Test(@"@""cat(?<dog >)_*>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
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
        <TextToken>&gt;</TextToken>
      </Text>
      <Text>
        <TextToken>d</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>)</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Too many )'s"" Start=""28"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..29)"" />
    <Capture Name=""1"" Span=""[13..22)"" />
    <Capture Name=""dog"" Span=""[13..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest85()
        {
            Test(@"@""cat(?<dog!>)_*>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
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
        <TextToken>&gt;</TextToken>
      </Text>
      <Text>
        <TextToken>d</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>)</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Too many )'s"" Start=""28"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..29)"" />
    <Capture Name=""1"" Span=""[13..22)"" />
    <Capture Name=""dog"" Span=""[13..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest86()
        {
            Test(@"@""cat(?<dog)_*>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
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
        <TextToken>&gt;</TextToken>
      </Text>
      <Text>
        <TextToken>d</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>)</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Too many )'s"" Start=""26"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..27)"" />
    <Capture Name=""1"" Span=""[13..20)"" />
    <Capture Name=""dog"" Span=""[13..20)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest87()
        {
            Test(@"@""cat(?<1dog>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""17"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..25)"" />
    <Capture Name=""1"" Span=""[13..25)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest88()
        {
            Test(@"@""cat(?<0>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture number cannot be zero"" Start=""16"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest89()
        {
            Test(@"@""([5-\D]*)dog""", @"<Tree>
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
        <TextToken>d</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Cannot include class \D in character range"" Start=""14"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..22)"" />
    <Capture Name=""1"" Span=""[10..19)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest90()
        {
            Test(@"@""cat([6-\s]*)dog""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
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
        <TextToken>d</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Cannot include class \s in character range"" Start=""17"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..25)"" />
    <Capture Name=""1"" Span=""[13..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest91()
        {
            Test(@"@""cat([c-\S]*)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
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
    <Diagnostic Message=""Cannot include class \S in character range"" Start=""17"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..22)"" />
    <Capture Name=""1"" Span=""[13..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest92()
        {
            Test(@"@""cat([7-\w]*)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
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
    <Diagnostic Message=""Cannot include class \w in character range"" Start=""17"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..22)"" />
    <Capture Name=""1"" Span=""[13..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest93()
        {
            Test(@"@""cat([a-\W]*)dog""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
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
        <TextToken>d</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Cannot include class \W in character range"" Start=""17"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..25)"" />
    <Capture Name=""1"" Span=""[13..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest94()
        {
            Test(@"@""([f-\p{Lu}]\w*)\s([\p{Lu}]\w*)""", @"<Tree>
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
    <Diagnostic Message=""Cannot include class \p in character range"" Start=""14"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..40)"" />
    <Capture Name=""1"" Span=""[10..25)"" />
    <Capture Name=""2"" Span=""[27..40)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest95()
        {
            Test(@"@""(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
          </Trivia> </TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
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
    <Diagnostic Message=""Unterminated (?#...) comment"" Start=""31"" Length=""35"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..66)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest96()
        {
            Test(@"@""([1-\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)""", @"<Tree>
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
    <Diagnostic Message=""Cannot include class \P in character range"" Start=""14"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..52)"" />
    <Capture Name=""1"" Span=""[10..31)"" />
    <Capture Name=""2"" Span=""[33..52)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest97()
        {
            Test(@"@""[\P]""", @"<Tree>
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
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""11"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest98()
        {
            Test(@"@""([\pcat])""", @"<Tree>
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
                <TextToken>c</TextToken>
              </Text>
              <Text>
                <TextToken>a</TextToken>
              </Text>
              <Text>
                <TextToken>t</TextToken>
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
    <Diagnostic Message=""Malformed \p{X} character escape"" Start=""12"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..19)"" />
    <Capture Name=""1"" Span=""[10..19)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest99()
        {
            Test(@"@""([\Pcat])""", @"<Tree>
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
                <TextToken>c</TextToken>
              </Text>
              <Text>
                <TextToken>a</TextToken>
              </Text>
              <Text>
                <TextToken>t</TextToken>
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
    <Diagnostic Message=""Malformed \p{X} character escape"" Start=""12"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..19)"" />
    <Capture Name=""1"" Span=""[10..19)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest100()
        {
            Test(@"@""(\p{""", @"<Tree>
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
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""11"" Length=""2"" />
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
    <Capture Name=""1"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest101()
        {
            Test(@"@""(\p{Ll""", @"<Tree>
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
          <Text>
            <TextToken>L</TextToken>
          </Text>
          <Text>
            <TextToken>l</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""11"" Length=""2"" />
    <Diagnostic Message=""Not enough )'s"" Start=""16"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..16)"" />
    <Capture Name=""1"" Span=""[10..16)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest102()
        {
            Test(@"@""(cat)([\o]*)(dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \o"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..27)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""2"" Span=""[15..22)"" />
    <Capture Name=""3"" Span=""[22..27)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest103()
        {
            Test(@"@""[\p]""", @"<Tree>
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
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""11"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..14)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest104()
        {
            Test(@"@""(?<cat>cat)\s+(?<dog>dog)\kcat""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""35"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..40)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..35)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..35)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest105()
        {
            Test(@"@""(?<cat>cat)\s+(?<dog>dog)\k<cat2>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat2"">cat2</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group name cat2"" Start=""38"" Length=""4"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..43)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..35)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..35)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest106()
        {
            Test(@"@""(?<cat>cat)\s+(?<dog>dog)\k<8>cat""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""8"">8</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 8"" Start=""38"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..43)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..35)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..35)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest107()
        {
            Test(@"@""^[abcd]{1}?*$""", @"<Tree>
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
                <TextToken>a</TextToken>
              </Text>
              <Text>
                <TextToken>b</TextToken>
              </Text>
              <Text>
                <TextToken>c</TextToken>
              </Text>
              <Text>
                <TextToken>d</TextToken>
              </Text>
            </Sequence>
            <CloseBracketToken>]</CloseBracketToken>
          </CharacterClass>
          <OpenBraceToken>{</OpenBraceToken>
          <NumberToken value=""1"">1</NumberToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""21"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..23)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest108()
        {
            Test(@"@""^[abcd]*+$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
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
    <Diagnostic Message=""Nested quantifier +"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..20)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest109()
        {
            Test(@"@""^[abcd]+*$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..20)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest110()
        {
            Test(@"@""^[abcd]?*$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..20)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest111()
        {
            Test(@"@""^[abcd]*?+$""", @"<Tree>
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
                <TextToken>a</TextToken>
              </Text>
              <Text>
                <TextToken>b</TextToken>
              </Text>
              <Text>
                <TextToken>c</TextToken>
              </Text>
              <Text>
                <TextToken>d</TextToken>
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
    <Diagnostic Message=""Nested quantifier +"" Start=""19"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..21)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest112()
        {
            Test(@"@""^[abcd]+?*$""", @"<Tree>
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
                <TextToken>a</TextToken>
              </Text>
              <Text>
                <TextToken>b</TextToken>
              </Text>
              <Text>
                <TextToken>c</TextToken>
              </Text>
              <Text>
                <TextToken>d</TextToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""19"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..21)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest113()
        {
            Test(@"@""^[abcd]{1,}?*$""", @"<Tree>
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
                <TextToken>a</TextToken>
              </Text>
              <Text>
                <TextToken>b</TextToken>
              </Text>
              <Text>
                <TextToken>c</TextToken>
              </Text>
              <Text>
                <TextToken>d</TextToken>
              </Text>
            </Sequence>
            <CloseBracketToken>]</CloseBracketToken>
          </CharacterClass>
          <OpenBraceToken>{</OpenBraceToken>
          <NumberToken value=""1"">1</NumberToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""22"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..24)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest114()
        {
            Test(@"@""^[abcd]??*$""", @"<Tree>
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
                <TextToken>a</TextToken>
              </Text>
              <Text>
                <TextToken>b</TextToken>
              </Text>
              <Text>
                <TextToken>c</TextToken>
              </Text>
              <Text>
                <TextToken>d</TextToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""19"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..21)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest115()
        {
            Test(@"@""^[abcd]+{0,5}$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
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
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <Text>
        <TextToken>5</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Nested quantifier {"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..24)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest116()
        {
            Test(@"@""^[abcd]?{0,5}$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
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
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <Text>
        <TextToken>5</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Nested quantifier {"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..24)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest117()
        {
            Test(@"@""\u""", @"<Tree>
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
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..12)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest118()
        {
            Test(@"@""\ua""", @"<Tree>
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
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""3"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest119()
        {
            Test(@"@""\u0""", @"<Tree>
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
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""3"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..13)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest120()
        {
            Test(@"@""\x""", @"<Tree>
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
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..12)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest121()
        {
            Test(@"@""^[abcd]*{0,5}$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
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
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <Text>
        <TextToken>5</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Nested quantifier {"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..24)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest122()
        {
            Test(@"@""[""", @"<Tree>
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
    <Diagnostic Message=""Unterminated [] set"" Start=""11"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..11)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest123()
        {
            Test(@"@""^[abcd]{0,16}?*$""", @"<Tree>
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
                <TextToken>a</TextToken>
              </Text>
              <Text>
                <TextToken>b</TextToken>
              </Text>
              <Text>
                <TextToken>c</TextToken>
              </Text>
              <Text>
                <TextToken>d</TextToken>
              </Text>
            </Sequence>
            <CloseBracketToken>]</CloseBracketToken>
          </CharacterClass>
          <OpenBraceToken>{</OpenBraceToken>
          <NumberToken value=""0"">0</NumberToken>
          <CommaToken>,</CommaToken>
          <NumberToken value=""16"">16</NumberToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""24"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..26)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest124()
        {
            Test(@"@""^[abcd]{1,}*$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
            </Text>
          </Sequence>
          <CloseBracketToken>]</CloseBracketToken>
        </CharacterClass>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""21"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..23)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest125()
        {
            Test(@"@""(?<cat>cat)\s+(?<dog>dog)\k<8>cat""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""8"">8</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 8"" Start=""38"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..43)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..35)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..35)"" />
  </Captures>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void NegativeTest126()
        {
            Test(@"@""(?<cat>cat)\s+(?<dog>dog)\k8""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
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
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""35"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..38)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..35)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..35)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest127()
        {
            Test(@"@""(?<cat>cat)\s+(?<dog>dog)\k8""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""cat"">cat</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <CaptureNameToken value=""dog"">dog</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
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
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""35"" Length=""2"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..38)"" />
    <Capture Name=""1"" Span=""[10..21)"" />
    <Capture Name=""2"" Span=""[24..35)"" />
    <Capture Name=""cat"" Span=""[10..21)"" />
    <Capture Name=""dog"" Span=""[24..35)"" />
  </Captures>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void NegativeTest128()
        {
            Test(@"@""(cat)(\7)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""7"">7</NumberToken>
          </BackreferenceEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 7"" Start=""17"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..19)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""2"" Span=""[15..19)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest129()
        {
            Test(@"@""(cat)\s+(?<2147483648>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <NumberToken value=""-2147483648"">2147483648</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture group numbers must be less than or equal to Int32.MaxValue"" Start=""21"" Length=""10"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""-2147483648"" Span=""[18..36)"" />
    <Capture Name=""0"" Span=""[10..36)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest130()
        {
            Test(@"@""(cat)\s+(?<21474836481097>dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
        <NumberToken value=""1097"">21474836481097</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture group numbers must be less than or equal to Int32.MaxValue"" Start=""21"" Length=""14"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..40)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""1097"" Span=""[18..40)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest131()
        {
            Test(@"@""^[abcd]{1}*$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
            </Text>
          </Sequence>
          <CloseBracketToken>]</CloseBracketToken>
        </CharacterClass>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""20"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..22)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest132()
        {
            Test(@"@""(cat)(\c*)(dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..25)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""2"" Span=""[15..20)"" />
    <Capture Name=""3"" Span=""[20..25)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest133()
        {
            Test(@"@""(cat)(\c *)(dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..26)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""2"" Span=""[15..21)"" />
    <Capture Name=""3"" Span=""[21..26)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest134()
        {
            Test(@"@""(cat)(\c?*)(dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""18"" Length=""1"" />
    <Diagnostic Message=""Nested quantifier *"" Start=""19"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..26)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""2"" Span=""[15..21)"" />
    <Capture Name=""3"" Span=""[21..26)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest135()
        {
            Test(@"@""(cat)(\c`*)(dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""18"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..26)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""2"" Span=""[15..21)"" />
    <Capture Name=""3"" Span=""[21..26)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest136()
        {
            Test(@"@""(cat)(\c\|*)(dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
            <TextToken>d</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>g</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""20"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..27)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""2"" Span=""[15..22)"" />
    <Capture Name=""3"" Span=""[22..27)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest137()
        {
            Test(@"@""(cat)(\c\[*)(dog)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
                <TextToken>*</TextToken>
              </Text>
              <Text>
                <TextToken>)</TextToken>
              </Text>
              <Text>
                <TextToken>(</TextToken>
              </Text>
              <Text>
                <TextToken>d</TextToken>
              </Text>
              <Text>
                <TextToken>o</TextToken>
              </Text>
              <Text>
                <TextToken>g</TextToken>
              </Text>
              <Text>
                <TextToken>)</TextToken>
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
    <Diagnostic Message=""Unterminated [] set"" Start=""27"" Length=""0"" />
    <Diagnostic Message=""Not enough )'s"" Start=""27"" Length=""0"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..27)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
    <Capture Name=""2"" Span=""[15..27)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest138()
        {
            Test(@"@""^[abcd]{0,16}*$""", @"<Tree>
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
              <TextToken>a</TextToken>
            </Text>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <Text>
              <TextToken>c</TextToken>
            </Text>
            <Text>
              <TextToken>d</TextToken>
            </Text>
          </Sequence>
          <CloseBracketToken>]</CloseBracketToken>
        </CharacterClass>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""0"">0</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""16"">16</NumberToken>
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
    <Diagnostic Message=""Nested quantifier *"" Start=""23"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..25)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void NegativeTest139()
        {
            Test(@"@""(cat)\c""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>c</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>t</TextToken>
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
    <Diagnostic Message=""Missing control character"" Start=""16"" Length=""1"" />
  </Diagnostics>
  <Captures>
    <Capture Name=""0"" Span=""[10..17)"" />
    <Capture Name=""1"" Span=""[10..15)"" />
  </Captures>
</Tree>", RegexOptions.None);
        }

        //        [Fact]
        //        public void NegativeTest140()
        //        {
        //            Test(@"@"" (?(?n))""", @"<Tree>
        //  <CompilationUnit>
        //    <Sequence>
        //      <Text>
        //        <TextToken> </TextToken>
        //      </Text>
        //      <ConditionalExpressionGrouping>
        //        <OpenParenToken>(</OpenParenToken>
        //        <QuestionToken>?</QuestionToken>
        //        <SimpleOptionsGrouping>
        //          <OpenParenToken>(</OpenParenToken>
        //          <QuestionToken>?</QuestionToken>
        //          <OptionsToken>n</OptionsToken>
        //          <CloseParenToken>)</CloseParenToken>
        //        </SimpleOptionsGrouping>
        //        <Sequence />
        //        <CloseParenToken>)</CloseParenToken>
        //      </ConditionalExpressionGrouping>
        //    </Sequence>
        //    <EndOfFile />
        //  </CompilationUnit>
        //</Tree>", RegexOptions.None);
        //        }
    }
}
