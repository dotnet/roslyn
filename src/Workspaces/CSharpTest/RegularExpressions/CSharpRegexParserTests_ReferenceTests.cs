// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.RegularExpressions
{
    public partial class CSharpRegexParserTests
    {
        [Fact]
        public void ReferenceTest0()
        {
            Test(@"@""[aeiou]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>e</TextToken>
          </Text>
          <Text>
            <TextToken>i</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>u</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest1()
        {
            Test(@"@""(?<duplicateWord>\w+)\s\k<duplicateWord>\W(?<nextWord>\w+)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""duplicateWord"">duplicateWord</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""duplicateWord"">duplicateWord</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>W</TextToken>
      </SimpleEscape>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""nextWord"">nextWord</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest2()
        {
            Test(@"@""((?<One>abc)\d+)?(?<Two>xyz)(.*)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrOneQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <CaptureGrouping>
              <OpenParenToken>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <LessThanToken>&lt;</LessThanToken>
              <CaptureNameToken value=""One"">One</CaptureNameToken>
              <GreaterThanToken>&gt;</GreaterThanToken>
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
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </CaptureGrouping>
            <OneOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""Two"">Two</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>x</TextToken>
          </Text>
          <Text>
            <TextToken>y</TextToken>
          </Text>
          <Text>
            <TextToken>z</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
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
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest3()
        {
            Test(@"@""(\w+)\s(\1)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""1"">1</NumberToken>
          </BackreferenceEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest4()
        {
            Test(@"@""\Bqu\w+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>B</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>q</TextToken>
      </Text>
      <Text>
        <TextToken>u</TextToken>
      </Text>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest5()
        {
            Test(@"@""\bare\w*\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>r</TextToken>
      </Text>
      <Text>
        <TextToken>e</TextToken>
      </Text>
      <ZeroOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest6()
        {
            Test(@"@""\G(\w+\s?\w*),?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>G</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
          <ZeroOrOneQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
            <QuestionToken>?</QuestionToken>
          </ZeroOrOneQuantifier>
          <ZeroOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <AsteriskToken>*</AsteriskToken>
          </ZeroOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <ZeroOrOneQuantifier>
        <Text>
          <TextToken>,</TextToken>
        </Text>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest7()
        {
            Test(@"@""\D+(?<digit>\d+)\D+(?<digit>\d+)?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>D</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""digit"">digit</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>D</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <ZeroOrOneQuantifier>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <CaptureNameToken value=""digit"">digit</CaptureNameToken>
          <GreaterThanToken>&gt;</GreaterThanToken>
          <Sequence>
            <OneOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </CaptureGrouping>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest8()
        {
            Test(@"@""(\s\d{4}(-(\d{4}&#124;present))?,?)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""4"">4</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <Text>
                    <TextToken>-</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <ExactNumericQuantifier>
                        <SimpleEscape>
                          <BackslashToken>\</BackslashToken>
                          <TextToken>d</TextToken>
                        </SimpleEscape>
                        <OpenBraceToken>{</OpenBraceToken>
                        <NumberToken value=""4"">4</NumberToken>
                        <CloseBraceToken>}</CloseBraceToken>
                      </ExactNumericQuantifier>
                      <Text>
                        <TextToken>&amp;</TextToken>
                      </Text>
                      <Text>
                        <TextToken>#</TextToken>
                      </Text>
                      <Text>
                        <TextToken>1</TextToken>
                      </Text>
                      <Text>
                        <TextToken>2</TextToken>
                      </Text>
                      <Text>
                        <TextToken>4</TextToken>
                      </Text>
                      <Text>
                        <TextToken>;</TextToken>
                      </Text>
                      <Text>
                        <TextToken>p</TextToken>
                      </Text>
                      <Text>
                        <TextToken>r</TextToken>
                      </Text>
                      <Text>
                        <TextToken>e</TextToken>
                      </Text>
                      <Text>
                        <TextToken>s</TextToken>
                      </Text>
                      <Text>
                        <TextToken>e</TextToken>
                      </Text>
                      <Text>
                        <TextToken>n</TextToken>
                      </Text>
                      <Text>
                        <TextToken>t</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>,</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest9()
        {
            Test(@"@""^((\w+(\s?)){2,}),\s(\w+\s\w+),(\s\d{4}(-(\d{4}|present))?,?)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OpenRangeNumericQuantifier>
            <SimpleGrouping>
              <OpenParenToken>(</OpenParenToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
                <SimpleGrouping>
                  <OpenParenToken>(</OpenParenToken>
                  <Sequence>
                    <ZeroOrOneQuantifier>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>s</TextToken>
                      </SimpleEscape>
                      <QuestionToken>?</QuestionToken>
                    </ZeroOrOneQuantifier>
                  </Sequence>
                  <CloseParenToken>)</CloseParenToken>
                </SimpleGrouping>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </SimpleGrouping>
            <OpenBraceToken>{</OpenBraceToken>
            <NumberToken value=""2"">2</NumberToken>
            <CommaToken>,</CommaToken>
            <CloseBraceToken>}</CloseBraceToken>
          </OpenRangeNumericQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>s</TextToken>
          </SimpleEscape>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""4"">4</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <Text>
                    <TextToken>-</TextToken>
                  </Text>
                  <SimpleGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <Alternation>
                      <Sequence>
                        <ExactNumericQuantifier>
                          <SimpleEscape>
                            <BackslashToken>\</BackslashToken>
                            <TextToken>d</TextToken>
                          </SimpleEscape>
                          <OpenBraceToken>{</OpenBraceToken>
                          <NumberToken value=""4"">4</NumberToken>
                          <CloseBraceToken>}</CloseBraceToken>
                        </ExactNumericQuantifier>
                      </Sequence>
                      <BarToken>|</BarToken>
                      <Sequence>
                        <Text>
                          <TextToken>p</TextToken>
                        </Text>
                        <Text>
                          <TextToken>r</TextToken>
                        </Text>
                        <Text>
                          <TextToken>e</TextToken>
                        </Text>
                        <Text>
                          <TextToken>s</TextToken>
                        </Text>
                        <Text>
                          <TextToken>e</TextToken>
                        </Text>
                        <Text>
                          <TextToken>n</TextToken>
                        </Text>
                        <Text>
                          <TextToken>t</TextToken>
                        </Text>
                      </Sequence>
                    </Alternation>
                    <CloseParenToken>)</CloseParenToken>
                  </SimpleGrouping>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>,</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest10()
        {
            Test(@"@""^[0-9-[2468]]+$""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <OneOrMoreQuantifier>
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
            <CharacterClassSubtraction>
              <MinusToken>-</MinusToken>
              <CharacterClass>
                <OpenBracketToken>[</OpenBracketToken>
                <Sequence>
                  <Text>
                    <TextToken>2</TextToken>
                  </Text>
                  <Text>
                    <TextToken>4</TextToken>
                  </Text>
                  <Text>
                    <TextToken>6</TextToken>
                  </Text>
                  <Text>
                    <TextToken>8</TextToken>
                  </Text>
                </Sequence>
                <CloseBracketToken>]</CloseBracketToken>
              </CharacterClass>
            </CharacterClassSubtraction>
          </Sequence>
          <CloseBracketToken>]</CloseBracketToken>
        </CharacterClass>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest11()
        {
            Test(@"@""[a-z-[0-9]]""", @"<Tree>
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
              <TextToken>z</TextToken>
            </Text>
          </CharacterClassRange>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
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
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest12()
        {
            Test(@"@""[\p{IsBasicLatin}-[\x00-\x7F]]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>IsBasicLatin</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <CharacterClassRange>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>00</TextToken>
                  </HexEscape>
                  <MinusToken>-</MinusToken>
                  <HexEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>x</TextToken>
                    <TextToken>7F</TextToken>
                  </HexEscape>
                </CharacterClassRange>
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
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest13()
        {
            Test(@"@""[\u0000-\uFFFF-[\s\p{P}\p{IsGreek}\x85]]""", @"<Tree>
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
              <TextToken>FFFF</TextToken>
            </UnicodeEscape>
          </CharacterClassRange>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <SimpleEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>s</TextToken>
                </SimpleEscape>
                <CategoryEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>p</TextToken>
                  <OpenBraceToken>{</OpenBraceToken>
                  <EscapeCategoryToken>P</EscapeCategoryToken>
                  <CloseBraceToken>}</CloseBraceToken>
                </CategoryEscape>
                <CategoryEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>p</TextToken>
                  <OpenBraceToken>{</OpenBraceToken>
                  <EscapeCategoryToken>IsGreek</EscapeCategoryToken>
                  <CloseBraceToken>}</CloseBraceToken>
                </CategoryEscape>
                <HexEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>x</TextToken>
                  <TextToken>85</TextToken>
                </HexEscape>
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
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest14()
        {
            Test(@"@""[a-z-[d-w-[m-o]]]""", @"<Tree>
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
              <TextToken>z</TextToken>
            </Text>
          </CharacterClassRange>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <CharacterClassRange>
                  <Text>
                    <TextToken>d</TextToken>
                  </Text>
                  <MinusToken>-</MinusToken>
                  <Text>
                    <TextToken>w</TextToken>
                  </Text>
                </CharacterClassRange>
                <CharacterClassSubtraction>
                  <MinusToken>-</MinusToken>
                  <CharacterClass>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CharacterClassRange>
                        <Text>
                          <TextToken>m</TextToken>
                        </Text>
                        <MinusToken>-</MinusToken>
                        <Text>
                          <TextToken>o</TextToken>
                        </Text>
                      </CharacterClassRange>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </CharacterClass>
                </CharacterClassSubtraction>
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
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest15()
        {
            Test(@"@""((\w+(\s?)){2,}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OpenRangeNumericQuantifier>
            <SimpleGrouping>
              <OpenParenToken>(</OpenParenToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
                <SimpleGrouping>
                  <OpenParenToken>(</OpenParenToken>
                  <Sequence>
                    <ZeroOrOneQuantifier>
                      <SimpleEscape>
                        <BackslashToken>\</BackslashToken>
                        <TextToken>s</TextToken>
                      </SimpleEscape>
                      <QuestionToken>?</QuestionToken>
                    </ZeroOrOneQuantifier>
                  </Sequence>
                  <CloseParenToken>)</CloseParenToken>
                </SimpleGrouping>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </SimpleGrouping>
            <OpenBraceToken>{</OpenBraceToken>
            <NumberToken value=""2"">2</NumberToken>
            <CommaToken>,</CommaToken>
            <CloseBraceToken>}</CloseBraceToken>
          </OpenRangeNumericQuantifier>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""25"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest16()
        {
            Test(@"@""[a-z-[djp]]""", @"<Tree>
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
              <TextToken>z</TextToken>
            </Text>
          </CharacterClassRange>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>d</TextToken>
                </Text>
                <Text>
                  <TextToken>j</TextToken>
                </Text>
                <Text>
                  <TextToken>p</TextToken>
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
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest17()
        {
            Test(@"@""^[^<>]*(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*(?(Open)(?!))$""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <ZeroOrMoreQuantifier>
        <NegatedCharacterClass>
          <OpenBracketToken>[</OpenBracketToken>
          <CaretToken>^</CaretToken>
          <Sequence>
            <Text>
              <TextToken>&lt;</TextToken>
            </Text>
            <Text>
              <TextToken>&gt;</TextToken>
            </Text>
          </Sequence>
          <CloseBracketToken>]</CloseBracketToken>
        </NegatedCharacterClass>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <ZeroOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <OneOrMoreQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <QuoteToken>'</QuoteToken>
                    <CaptureNameToken value=""Open"">Open</CaptureNameToken>
                    <QuoteToken>'</QuoteToken>
                    <Sequence>
                      <Text>
                        <TextToken>&lt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ZeroOrMoreQuantifier>
                    <NegatedCharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <CaretToken>^</CaretToken>
                      <Sequence>
                        <Text>
                          <TextToken>&lt;</TextToken>
                        </Text>
                        <Text>
                          <TextToken>&gt;</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </NegatedCharacterClass>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
            <OneOrMoreQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <QuoteToken>'</QuoteToken>
                    <CaptureNameToken value=""Close"">Close</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken value=""Open"">Open</CaptureNameToken>
                    <QuoteToken>'</QuoteToken>
                    <Sequence>
                      <Text>
                        <TextToken>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <ZeroOrMoreQuantifier>
                    <NegatedCharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <CaretToken>^</CaretToken>
                      <Sequence>
                        <Text>
                          <TextToken>&lt;</TextToken>
                        </Text>
                        <Text>
                          <TextToken>&gt;</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </NegatedCharacterClass>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <CaptureNameToken value=""Open"">Open</CaptureNameToken>
        <CloseParenToken>)</CloseParenToken>
        <Sequence>
          <NegativeLookaheadGrouping>
            <OpenParenToken>(</OpenParenToken>
            <QuestionToken>?</QuestionToken>
            <ExclamationToken>!</ExclamationToken>
            <Sequence />
            <CloseParenToken>)</CloseParenToken>
          </NegativeLookaheadGrouping>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest18()
        {
            Test(@"@""((?'Close-Open'>)[^<>]*)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <BalancingGrouping>
              <OpenParenToken>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <QuoteToken>'</QuoteToken>
              <CaptureNameToken value=""Close"">Close</CaptureNameToken>
              <MinusToken>-</MinusToken>
              <CaptureNameToken value=""Open"">Open</CaptureNameToken>
              <QuoteToken>'</QuoteToken>
              <Sequence>
                <Text>
                  <TextToken>&gt;</TextToken>
                </Text>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </BalancingGrouping>
            <ZeroOrMoreQuantifier>
              <NegatedCharacterClass>
                <OpenBracketToken>[</OpenBracketToken>
                <CaretToken>^</CaretToken>
                <Sequence>
                  <Text>
                    <TextToken>&lt;</TextToken>
                  </Text>
                  <Text>
                    <TextToken>&gt;</TextToken>
                  </Text>
                </Sequence>
                <CloseBracketToken>]</CloseBracketToken>
              </NegatedCharacterClass>
              <AsteriskToken>*</AsteriskToken>
            </ZeroOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group name Open"" Start=""20"" Length=""4"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest19()
        {
            Test(@"@""(\w)\1+.\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <OneOrMoreQuantifier>
        <BackreferenceEscape>
          <BackslashToken>\</BackslashToken>
          <NumberToken value=""1"">1</NumberToken>
        </BackreferenceEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <Wildcard>
        <DotToken>.</DotToken>
      </Wildcard>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest20()
        {
            Test(@"@""\d{4}\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ExactNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""4"">4</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest21()
        {
            Test(@"@""\d{1,2},""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ClosedRangeNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""2"">2</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
      <Text>
        <TextToken>,</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest22()
        {
            Test(@"@""(?<!(Saturday|Sunday) )\b\w+ \d{1,2}, \d{4}\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence>
          <SimpleGrouping>
            <OpenParenToken>(</OpenParenToken>
            <Alternation>
              <Sequence>
                <Text>
                  <TextToken>S</TextToken>
                </Text>
                <Text>
                  <TextToken>a</TextToken>
                </Text>
                <Text>
                  <TextToken>t</TextToken>
                </Text>
                <Text>
                  <TextToken>u</TextToken>
                </Text>
                <Text>
                  <TextToken>r</TextToken>
                </Text>
                <Text>
                  <TextToken>d</TextToken>
                </Text>
                <Text>
                  <TextToken>a</TextToken>
                </Text>
                <Text>
                  <TextToken>y</TextToken>
                </Text>
              </Sequence>
              <BarToken>|</BarToken>
              <Sequence>
                <Text>
                  <TextToken>S</TextToken>
                </Text>
                <Text>
                  <TextToken>u</TextToken>
                </Text>
                <Text>
                  <TextToken>n</TextToken>
                </Text>
                <Text>
                  <TextToken>d</TextToken>
                </Text>
                <Text>
                  <TextToken>a</TextToken>
                </Text>
                <Text>
                  <TextToken>y</TextToken>
                </Text>
              </Sequence>
            </Alternation>
            <CloseParenToken>)</CloseParenToken>
          </SimpleGrouping>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NegativeLookbehindGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <ClosedRangeNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""2"">2</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <ExactNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""4"">4</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest23()
        {
            Test(@"@""(?<=\b20)\d{2}\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <EqualsToken>=</EqualsToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>b</TextToken>
          </SimpleEscape>
          <Text>
            <TextToken>2</TextToken>
          </Text>
          <Text>
            <TextToken>0</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </PositiveLookbehindGrouping>
      <ExactNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""2"">2</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest24()
        {
            Test(@"@""\b\w+\b(?!\p{P})""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <NegativeLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>P</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NegativeLookaheadGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest25()
        {
            Test(@"@""(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <OneOrMoreQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <CaptureGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <QuoteToken>'</QuoteToken>
                    <CaptureNameToken value=""Open"">Open</CaptureNameToken>
                    <QuoteToken>'</QuoteToken>
                    <Sequence>
                      <Text>
                        <TextToken>&lt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </CaptureGrouping>
                  <ZeroOrMoreQuantifier>
                    <NegatedCharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <CaretToken>^</CaretToken>
                      <Sequence>
                        <Text>
                          <TextToken>&lt;</TextToken>
                        </Text>
                        <Text>
                          <TextToken>&gt;</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </NegatedCharacterClass>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
            <OneOrMoreQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <BalancingGrouping>
                    <OpenParenToken>(</OpenParenToken>
                    <QuestionToken>?</QuestionToken>
                    <QuoteToken>'</QuoteToken>
                    <CaptureNameToken value=""Close"">Close</CaptureNameToken>
                    <MinusToken>-</MinusToken>
                    <CaptureNameToken value=""Open"">Open</CaptureNameToken>
                    <QuoteToken>'</QuoteToken>
                    <Sequence>
                      <Text>
                        <TextToken>&gt;</TextToken>
                      </Text>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </BalancingGrouping>
                  <ZeroOrMoreQuantifier>
                    <NegatedCharacterClass>
                      <OpenBracketToken>[</OpenBracketToken>
                      <CaretToken>^</CaretToken>
                      <Sequence>
                        <Text>
                          <TextToken>&lt;</TextToken>
                        </Text>
                        <Text>
                          <TextToken>&gt;</TextToken>
                        </Text>
                      </Sequence>
                      <CloseBracketToken>]</CloseBracketToken>
                    </NegatedCharacterClass>
                    <AsteriskToken>*</AsteriskToken>
                  </ZeroOrMoreQuantifier>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest26()
        {
            Test(@"@""\b(?!un)\w+\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <NegativeLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence>
          <Text>
            <TextToken>u</TextToken>
          </Text>
          <Text>
            <TextToken>n</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NegativeLookaheadGrouping>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest27()
        {
            Test(@"@""\b(?ix: d \w+)\s""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>ix</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>d</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>
                <Trivia>
                  <WhitespaceTrivia> </WhitespaceTrivia>
                </Trivia>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest28()
        {
            Test(@"@""(?:\w+)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonCapturingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NonCapturingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest29()
        {
            Test(@"@""(?:\b(?:\w+)\W*)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <NonCapturingGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <ColonToken>:</ColonToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>b</TextToken>
            </SimpleEscape>
            <NonCapturingGrouping>
              <OpenParenToken>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <ColonToken>:</ColonToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </NonCapturingGrouping>
            <ZeroOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>W</TextToken>
              </SimpleEscape>
              <AsteriskToken>*</AsteriskToken>
            </ZeroOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </NonCapturingGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest30()
        {
            Test(@"@""(?:\b(?:\w+)\W*)+\.""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <NonCapturingGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <ColonToken>:</ColonToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>b</TextToken>
            </SimpleEscape>
            <NonCapturingGrouping>
              <OpenParenToken>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <ColonToken>:</ColonToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </NonCapturingGrouping>
            <ZeroOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>W</TextToken>
              </SimpleEscape>
              <AsteriskToken>*</AsteriskToken>
            </ZeroOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </NonCapturingGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <DotToken>.</DotToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest31()
        {
            Test(@"@""(?'Close-Open'>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""Close"">Close</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <CaptureNameToken value=""Open"">Open</CaptureNameToken>
        <QuoteToken>'</QuoteToken>
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
    <Diagnostic Message=""Reference to undefined group name Open"" Start=""19"" Length=""4"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest32()
        {
            Test(@"@""[^<>]*""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <NegatedCharacterClass>
          <OpenBracketToken>[</OpenBracketToken>
          <CaretToken>^</CaretToken>
          <Sequence>
            <Text>
              <TextToken>&lt;</TextToken>
            </Text>
            <Text>
              <TextToken>&gt;</TextToken>
            </Text>
          </Sequence>
          <CloseBracketToken>]</CloseBracketToken>
        </NegatedCharacterClass>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest33()
        {
            Test(@"@""\b\w+(?=\sis\b)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <PositiveLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <EqualsToken>=</EqualsToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>s</TextToken>
          </SimpleEscape>
          <Text>
            <TextToken>i</TextToken>
          </Text>
          <Text>
            <TextToken>s</TextToken>
          </Text>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>b</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </PositiveLookaheadGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest34()
        {
            Test(@"@""[a-z-[m]]""", @"<Tree>
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
              <TextToken>z</TextToken>
            </Text>
          </CharacterClassRange>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>m</TextToken>
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
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest35()
        {
            Test(@"@""^\D\d{1,5}\D*$""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>D</TextToken>
      </SimpleEscape>
      <ClosedRangeNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""5"">5</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
      <ZeroOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>D</TextToken>
        </SimpleEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest36()
        {
            Test(@"@""[^0-9]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
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
      </NegatedCharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest37()
        {
            Test(@"@""(\p{IsGreek}+(\s)?)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <OneOrMoreQuantifier>
              <CategoryEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>p</TextToken>
                <OpenBraceToken>{</OpenBraceToken>
                <EscapeCategoryToken>IsGreek</EscapeCategoryToken>
                <CloseBraceToken>}</CloseBraceToken>
              </CategoryEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>s</TextToken>
                  </SimpleEscape>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest38()
        {
            Test(@"@""\b(\p{IsGreek}+(\s)?)+\p{Pd}\s(\p{IsBasicLatin}+(\s)?)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <OneOrMoreQuantifier>
              <CategoryEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>p</TextToken>
                <OpenBraceToken>{</OpenBraceToken>
                <EscapeCategoryToken>IsGreek</EscapeCategoryToken>
                <CloseBraceToken>}</CloseBraceToken>
              </CategoryEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>s</TextToken>
                  </SimpleEscape>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <CategoryEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
        <OpenBraceToken>{</OpenBraceToken>
        <EscapeCategoryToken>Pd</EscapeCategoryToken>
        <CloseBraceToken>}</CloseBraceToken>
      </CategoryEscape>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <OneOrMoreQuantifier>
              <CategoryEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>p</TextToken>
                <OpenBraceToken>{</OpenBraceToken>
                <EscapeCategoryToken>IsBasicLatin</EscapeCategoryToken>
                <CloseBraceToken>}</CloseBraceToken>
              </CategoryEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleGrouping>
                <OpenParenToken>(</OpenParenToken>
                <Sequence>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>s</TextToken>
                  </SimpleEscape>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </SimpleGrouping>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest39()
        {
            Test(@"@""\b.*[.?!;:](\s|\z)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrMoreQuantifier>
        <Wildcard>
          <DotToken>.</DotToken>
        </Wildcard>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>.</TextToken>
          </Text>
          <Text>
            <TextToken>?</TextToken>
          </Text>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>;</TextToken>
          </Text>
          <Text>
            <TextToken>:</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Alternation>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
          </Sequence>
          <BarToken>|</BarToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>z</TextToken>
            </SimpleEscape>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest40()
        {
            Test(@"@""^.+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <OneOrMoreQuantifier>
        <Wildcard>
          <DotToken>.</DotToken>
        </Wildcard>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest41()
        {
            Test(@"@""[^o]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
        <Sequence>
          <Text>
            <TextToken>o</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </NegatedCharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest42()
        {
            Test(@"@""\bth[^o]\w+\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <Text>
        <TextToken>h</TextToken>
      </Text>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
        <Sequence>
          <Text>
            <TextToken>o</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </NegatedCharacterClass>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest43()
        {
            Test(@"@""(\P{Sc})+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <CategoryEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>P</TextToken>
              <OpenBraceToken>{</OpenBraceToken>
              <EscapeCategoryToken>Sc</EscapeCategoryToken>
              <CloseBraceToken>}</CloseBraceToken>
            </CategoryEscape>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest44()
        {
            Test(@"@""[^\p{P}\d]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
        <Sequence>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>P</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>d</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </NegatedCharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest45()
        {
            Test(@"@""\b[A-Z]\w*\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
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
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest46()
        {
            Test(@"@""\S+?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <OneOrMoreQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>S</TextToken>
          </SimpleEscape>
          <PlusToken>+</PlusToken>
        </OneOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest47()
        {
            Test(@"@""y\s""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>y</TextToken>
      </Text>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest48()
        {
            Test(@"@""gr[ae]y\s\S+?[\s\p{P}]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>r</TextToken>
      </Text>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>e</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken>y</TextToken>
      </Text>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <LazyQuantifier>
        <OneOrMoreQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>S</TextToken>
          </SimpleEscape>
          <PlusToken>+</PlusToken>
        </OneOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>s</TextToken>
          </SimpleEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>P</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest49()
        {
            Test(@"@""[\s\p{P}]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>s</TextToken>
          </SimpleEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>P</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest50()
        {
            Test(@"@""[\p{P}\d]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>P</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>d</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest51()
        {
            Test(@"@""[^aeiou]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>e</TextToken>
          </Text>
          <Text>
            <TextToken>i</TextToken>
          </Text>
          <Text>
            <TextToken>o</TextToken>
          </Text>
          <Text>
            <TextToken>u</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </NegatedCharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest52()
        {
            Test(@"@""(\w)\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest53()
        {
            Test(@"@""[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}\p{Lm}] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
        <Sequence>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Ll</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Lu</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Lt</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Lo</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Nd</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Pc</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Lm</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </NegatedCharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest54()
        {
            Test(@"@""[^a-zA-Z_0-9]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
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
          <CharacterClassRange>
            <Text>
              <TextToken>A</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>Z</TextToken>
            </Text>
          </CharacterClassRange>
          <Text>
            <TextToken>_</TextToken>
          </Text>
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
      </NegatedCharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest55()
        {
            Test(@"@""\P{Nd}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CategoryEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>P</TextToken>
        <OpenBraceToken>{</OpenBraceToken>
        <EscapeCategoryToken>Nd</EscapeCategoryToken>
        <CloseBraceToken>}</CloseBraceToken>
      </CategoryEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest56()
        {
            Test(@"@""(\(?\d{3}\)?[\s-])?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrOneQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <OpenParenToken>(</OpenParenToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""3"">3</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <CloseParenToken>)</CloseParenToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <SimpleEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>s</TextToken>
                </SimpleEscape>
                <Text>
                  <TextToken>-</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest57()
        {
            Test(@"@""^(\(?\d{3}\)?[\s-])?\d{3}-\d{4}$""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <ZeroOrOneQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <OpenParenToken>(</OpenParenToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""3"">3</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <CloseParenToken>)</CloseParenToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <SimpleEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>s</TextToken>
                </SimpleEscape>
                <Text>
                  <TextToken>-</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <ExactNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""3"">3</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
      <Text>
        <TextToken>-</TextToken>
      </Text>
      <ExactNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""4"">4</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest58()
        {
            Test(@"@""[0-9]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
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
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest59()
        {
            Test(@"@""\p{Nd}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CategoryEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
        <OpenBraceToken>{</OpenBraceToken>
        <EscapeCategoryToken>Nd</EscapeCategoryToken>
        <CloseBraceToken>}</CloseBraceToken>
      </CategoryEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest60()
        {
            Test(@"@""\b(\S+)\s?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>S</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>s</TextToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest61()
        {
            Test(@"@""[^ \f\n\r\t\v]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>f</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>n</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>r</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>t</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>v</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </NegatedCharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest62()
        {
            Test(@"@""[^\f\n\r\t\v\x85\p{Z}]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegatedCharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <CaretToken>^</CaretToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>f</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>n</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>r</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>t</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>v</TextToken>
          </SimpleEscape>
          <HexEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>x</TextToken>
            <TextToken>85</TextToken>
          </HexEscape>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Z</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </NegatedCharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest63()
        {
            Test(@"@""(\s|$)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Alternation>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
          </Sequence>
          <BarToken>|</BarToken>
          <Sequence>
            <EndAnchor>
              <DollarToken>$</DollarToken>
            </EndAnchor>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest64()
        {
            Test(@"@""\b\w+(e)?s(\s|$)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <ZeroOrOneQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>e</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <Text>
        <TextToken>s</TextToken>
      </Text>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Alternation>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
          </Sequence>
          <BarToken>|</BarToken>
          <Sequence>
            <EndAnchor>
              <DollarToken>$</DollarToken>
            </EndAnchor>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest65()
        {
            Test(@"@""[ \f\n\r\t\v]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>f</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>n</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>r</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>t</TextToken>
          </SimpleEscape>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>v</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest66()
        {
            Test(@"@""(\W){1,2}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ClosedRangeNumericQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>W</TextToken>
            </SimpleEscape>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""2"">2</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest67()
        {
            Test(@"@""(\w+)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest68()
        {
            Test(@"@""\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest69()
        {
            Test(@"@""\b(\w+)(\W){1,2}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <ClosedRangeNumericQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>W</TextToken>
            </SimpleEscape>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""2"">2</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest70()
        {
            Test(@"@""(?>(\w)\1+).\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonBacktrackingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <SimpleGrouping>
            <OpenParenToken>(</OpenParenToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>w</TextToken>
              </SimpleEscape>
            </Sequence>
            <CloseParenToken>)</CloseParenToken>
          </SimpleGrouping>
          <OneOrMoreQuantifier>
            <BackreferenceEscape>
              <BackslashToken>\</BackslashToken>
              <NumberToken value=""1"">1</NumberToken>
            </BackreferenceEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NonBacktrackingGrouping>
      <Wildcard>
        <DotToken>.</DotToken>
      </Wildcard>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest71()
        {
            Test(@"@""(\b(\w+)\W+)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>b</TextToken>
            </SimpleEscape>
            <SimpleGrouping>
              <OpenParenToken>(</OpenParenToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </SimpleGrouping>
            <OneOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>W</TextToken>
              </SimpleEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest72()
        {
            Test(@"@""(\w)\1+.\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <OneOrMoreQuantifier>
        <BackreferenceEscape>
          <BackslashToken>\</BackslashToken>
          <NumberToken value=""1"">1</NumberToken>
        </BackreferenceEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <Wildcard>
        <DotToken>.</DotToken>
      </Wildcard>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest73()
        {
            Test(@"@""\p{Sc}*(\s?\d+[.,]?\d*)\p{Sc}*""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <CategoryEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>p</TextToken>
          <OpenBraceToken>{</OpenBraceToken>
          <EscapeCategoryToken>Sc</EscapeCategoryToken>
          <CloseBraceToken>}</CloseBraceToken>
        </CategoryEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <ZeroOrOneQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
            <QuestionToken>?</QuestionToken>
          </ZeroOrOneQuantifier>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
          <ZeroOrOneQuantifier>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>.</TextToken>
                </Text>
                <Text>
                  <TextToken>,</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
            <QuestionToken>?</QuestionToken>
          </ZeroOrOneQuantifier>
          <ZeroOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <AsteriskToken>*</AsteriskToken>
          </ZeroOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <ZeroOrMoreQuantifier>
        <CategoryEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>p</TextToken>
          <OpenBraceToken>{</OpenBraceToken>
          <EscapeCategoryToken>Sc</EscapeCategoryToken>
          <CloseBraceToken>}</CloseBraceToken>
        </CategoryEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest74()
        {
            Test(@"@""p{Sc}*(?<amount>\s?\d+[.,]?\d*)\p{Sc}*""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>p</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>S</TextToken>
      </Text>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>}</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""amount"">amount</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <ZeroOrOneQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
            <QuestionToken>?</QuestionToken>
          </ZeroOrOneQuantifier>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
          <ZeroOrOneQuantifier>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>.</TextToken>
                </Text>
                <Text>
                  <TextToken>,</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
            <QuestionToken>?</QuestionToken>
          </ZeroOrOneQuantifier>
          <ZeroOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <AsteriskToken>*</AsteriskToken>
          </ZeroOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <ZeroOrMoreQuantifier>
        <CategoryEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>p</TextToken>
          <OpenBraceToken>{</OpenBraceToken>
          <EscapeCategoryToken>Sc</EscapeCategoryToken>
          <CloseBraceToken>}</CloseBraceToken>
        </CategoryEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest75()
        {
            Test(@"@""^(\w+\s?)+$""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <OneOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>w</TextToken>
              </SimpleEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>s</TextToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest76()
        {
            Test(@"@""(?ix) d \w+ \s""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>ix</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>d</TextToken>
      </Text>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>
            <Trivia>
              <WhitespaceTrivia> </WhitespaceTrivia>
            </Trivia>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest77()
        {
            Test(@"@""\b(?ix: d \w+)\s""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>ix</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>d</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>
                <Trivia>
                  <WhitespaceTrivia> </WhitespaceTrivia>
                </Trivia>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest78()
        {
            Test(@"@""\bthe\w*\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <Text>
        <TextToken>h</TextToken>
      </Text>
      <Text>
        <TextToken>e</TextToken>
      </Text>
      <ZeroOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest79()
        {
            Test(@"@""\b(?i:t)he\w*\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>i</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <Text>
            <TextToken>t</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
      <Text>
        <TextToken>h</TextToken>
      </Text>
      <Text>
        <TextToken>e</TextToken>
      </Text>
      <ZeroOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest80()
        {
            Test(@"@""^(\w+)\s(\d+)$""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest81()
        {
            Test(@"@""^(\w+)\s(\d+)\r*$""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <ZeroOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>r</TextToken>
        </SimpleEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.Multiline);
        }

        [Fact]
        public void ReferenceTest82()
        {
            Test(@"@""(?m)^(\w+)\s(\d+)\r*$""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>m</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <ZeroOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>r</TextToken>
        </SimpleEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <EndAnchor>
        <DollarToken>$</DollarToken>
      </EndAnchor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.Multiline);
        }

        [Fact]
        public void ReferenceTest83()
        {
            Test(@"@""(?s)^.+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>s</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <OneOrMoreQuantifier>
        <Wildcard>
          <DotToken>.</DotToken>
        </Wildcard>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest84()
        {
            Test(@"@""\b(\d{2}-)*(?(1)\d{7}|\d{3}-\d{2}-\d{4})\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""2"">2</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""1"">1</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Alternation>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""7"">7</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
          </Sequence>
          <BarToken>|</BarToken>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""3"">3</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""2"">2</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""4"">4</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest85()
        {
            Test(@"@""\b\(?((\w+),?\s?)+[\.!?]\)?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <OpenParenToken>(</OpenParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleGrouping>
              <OpenParenToken>(</OpenParenToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </SimpleGrouping>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>,</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>s</TextToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <DotToken>.</DotToken>
          </SimpleEscape>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest86()
        {
            Test(@"@""(?n)\b\(?((?>\w+),?\s?)+[\.!?]\)?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <OpenParenToken>(</OpenParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <NonBacktrackingGrouping>
              <OpenParenToken>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <GreaterThanToken>&gt;</GreaterThanToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </NonBacktrackingGrouping>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>,</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>s</TextToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <DotToken>.</DotToken>
          </SimpleEscape>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest87()
        {
            Test(@"@""\b\(?(?n:(?>\w+),?\s?)+[\.!?]\)?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <OpenParenToken>(</OpenParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <OneOrMoreQuantifier>
        <NestedOptionsGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <OptionsToken>n</OptionsToken>
          <ColonToken>:</ColonToken>
          <Sequence>
            <NonBacktrackingGrouping>
              <OpenParenToken>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <GreaterThanToken>&gt;</GreaterThanToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </NonBacktrackingGrouping>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>,</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>s</TextToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </NestedOptionsGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <DotToken>.</DotToken>
          </SimpleEscape>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest88()
        {
            Test(@"@""\b\(?((?>\w+),?\s?)+[\.!?]\)?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <OpenParenToken>(</OpenParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <NonBacktrackingGrouping>
              <OpenParenToken>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <GreaterThanToken>&gt;</GreaterThanToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </NonBacktrackingGrouping>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>,</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>s</TextToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <DotToken>.</DotToken>
          </SimpleEscape>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void ReferenceTest89()
        {
            Test(@"@""(?x)\b \(? ( (?>\w+) ,?\s? )+  [\.!?] \)? # Matches an entire sentence.""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>x</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>
            <Trivia>
              <WhitespaceTrivia> </WhitespaceTrivia>
            </Trivia>\</BackslashToken>
          <OpenParenToken>(</OpenParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>
            <Trivia>
              <WhitespaceTrivia> </WhitespaceTrivia>
            </Trivia>(</OpenParenToken>
          <Sequence>
            <NonBacktrackingGrouping>
              <OpenParenToken>
                <Trivia>
                  <WhitespaceTrivia> </WhitespaceTrivia>
                </Trivia>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <GreaterThanToken>&gt;</GreaterThanToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </NonBacktrackingGrouping>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>
                  <Trivia>
                    <WhitespaceTrivia> </WhitespaceTrivia>
                  </Trivia>,</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>s</TextToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>
            <Trivia>
              <WhitespaceTrivia> </WhitespaceTrivia>
            </Trivia>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <CharacterClass>
        <OpenBracketToken>
          <Trivia>
            <WhitespaceTrivia>  </WhitespaceTrivia>
          </Trivia>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <DotToken>.</DotToken>
          </SimpleEscape>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>
            <Trivia>
              <WhitespaceTrivia> </WhitespaceTrivia>
            </Trivia>\</BackslashToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
        <CommentTrivia># Matches an entire sentence.</CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest90()
        {
            Test(@"@""\bb\w+\s""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>b</TextToken>
      </Text>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.RightToLeft);
        }

        [Fact]
        public void ReferenceTest91()
        {
            Test(@"@""(?<=\d{1,2}\s)\w+,?\s\d{4}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <EqualsToken>=</EqualsToken>
        <Sequence>
          <ClosedRangeNumericQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>d</TextToken>
            </SimpleEscape>
            <OpenBraceToken>{</OpenBraceToken>
            <NumberToken value=""1"">1</NumberToken>
            <CommaToken>,</CommaToken>
            <NumberToken value=""2"">2</NumberToken>
            <CloseBraceToken>}</CloseBraceToken>
          </ClosedRangeNumericQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>s</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </PositiveLookbehindGrouping>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>w</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <ZeroOrOneQuantifier>
        <Text>
          <TextToken>,</TextToken>
        </Text>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <ExactNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""4"">4</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.RightToLeft);
        }

        [Fact]
        public void ReferenceTest92()
        {
            Test(@"@""\b(\w+\s*)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <OneOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>w</TextToken>
              </SimpleEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
            <ZeroOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>s</TextToken>
              </SimpleEscape>
              <AsteriskToken>*</AsteriskToken>
            </ZeroOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void ReferenceTest93()
        {
            Test(@"@""((a+)(\1) ?)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleGrouping>
              <OpenParenToken>(</OpenParenToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <Text>
                    <TextToken>a</TextToken>
                  </Text>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </SimpleGrouping>
            <SimpleGrouping>
              <OpenParenToken>(</OpenParenToken>
              <Sequence>
                <BackreferenceEscape>
                  <BackslashToken>\</BackslashToken>
                  <NumberToken>1</NumberToken>
                </BackreferenceEscape>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </SimpleGrouping>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken> </TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void ReferenceTest94()
        {
            Test(@"@""\b(D\w+)\s(d\w+)\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>D</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest95()
        {
            Test(@"@""\b(D\w+)(?ixn) \s (d\w+) \b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>D</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>ixn</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleEscape>
        <BackslashToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>d</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest96()
        {
            Test(@"@""\b((?# case-sensitive comparison)D\w+)\s((?#case-insensitive comparison)d\w+)\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <CommentTrivia>(?# case-sensitive comparison)</CommentTrivia>
              </Trivia>D</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <CommentTrivia>(?#case-insensitive comparison)</CommentTrivia>
              </Trivia>d</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest97()
        {
            Test(@"@""\b\(?((?>\w+),?\s?)+[\.!?]\)?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <OpenParenToken>(</OpenParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <NonBacktrackingGrouping>
              <OpenParenToken>(</OpenParenToken>
              <QuestionToken>?</QuestionToken>
              <GreaterThanToken>&gt;</GreaterThanToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </NonBacktrackingGrouping>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>,</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
            <ZeroOrOneQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>s</TextToken>
              </SimpleEscape>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <DotToken>.</DotToken>
          </SimpleEscape>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <ZeroOrOneQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <CloseParenToken>)</CloseParenToken>
        </SimpleEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest98()
        {
            Test(@"@""\b(?<n2>\d{2}-)*(?(n2)\d{7}|\d{3}-\d{2}-\d{4})\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <ZeroOrMoreQuantifier>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <CaptureNameToken value=""n2"">n2</CaptureNameToken>
          <GreaterThanToken>&gt;</GreaterThanToken>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""2"">2</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </CaptureGrouping>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <CaptureNameToken value=""n2"">n2</CaptureNameToken>
        <CloseParenToken>)</CloseParenToken>
        <Alternation>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""7"">7</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
          </Sequence>
          <BarToken>|</BarToken>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""3"">3</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""2"">2</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""4"">4</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest99()
        {
            Test(@"@""\b(\d{2}-\d{7}|\d{3}-\d{2}-\d{4})\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Alternation>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""2"">2</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""7"">7</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
          </Sequence>
          <BarToken>|</BarToken>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""3"">3</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""2"">2</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""4"">4</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest100()
        {
            Test(@"@""\bgr(a|e)y\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>r</TextToken>
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
              <TextToken>e</TextToken>
            </Text>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <Text>
        <TextToken>y</TextToken>
      </Text>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest101()
        {
            Test(@"@""(?>(\w)\1+).\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonBacktrackingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <SimpleGrouping>
            <OpenParenToken>(</OpenParenToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>w</TextToken>
              </SimpleEscape>
            </Sequence>
            <CloseParenToken>)</CloseParenToken>
          </SimpleGrouping>
          <OneOrMoreQuantifier>
            <BackreferenceEscape>
              <BackslashToken>\</BackslashToken>
              <NumberToken value=""1"">1</NumberToken>
            </BackreferenceEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NonBacktrackingGrouping>
      <Wildcard>
        <DotToken>.</DotToken>
      </Wildcard>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest102()
        {
            Test(@"@""(\b(\w+)\W+)+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>b</TextToken>
            </SimpleEscape>
            <SimpleGrouping>
              <OpenParenToken>(</OpenParenToken>
              <Sequence>
                <OneOrMoreQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <PlusToken>+</PlusToken>
                </OneOrMoreQuantifier>
              </Sequence>
              <CloseParenToken>)</CloseParenToken>
            </SimpleGrouping>
            <OneOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>W</TextToken>
              </SimpleEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest103()
        {
            Test(@"@""\b91*9*\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>9</TextToken>
      </Text>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>1</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>9</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest104()
        {
            Test(@"@""\ban+\w*?\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <OneOrMoreQuantifier>
        <Text>
          <TextToken>n</TextToken>
        </Text>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <LazyQuantifier>
        <ZeroOrMoreQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
          <AsteriskToken>*</AsteriskToken>
        </ZeroOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest105()
        {
            Test(@"@""\ban?\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <ZeroOrOneQuantifier>
        <Text>
          <TextToken>n</TextToken>
        </Text>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest106()
        {
            Test(@"@""\b\d+\,\d{3}\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>,</TextToken>
      </SimpleEscape>
      <ExactNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""3"">3</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest107()
        {
            Test(@"@""\b\d{2,}\b\D+""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OpenRangeNumericQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""2"">2</NumberToken>
        <CommaToken>,</CommaToken>
        <CloseBraceToken>}</CloseBraceToken>
      </OpenRangeNumericQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>D</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest108()
        {
            Test(@"@""(00\s){2,4}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ClosedRangeNumericQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>0</TextToken>
            </Text>
            <Text>
              <TextToken>0</TextToken>
            </Text>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>s</TextToken>
            </SimpleEscape>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""2"">2</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""4"">4</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest109()
        {
            Test(@"@""\b\w*?oo\w*?\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <LazyQuantifier>
        <ZeroOrMoreQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
          <AsteriskToken>*</AsteriskToken>
        </ZeroOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <LazyQuantifier>
        <ZeroOrMoreQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
          <AsteriskToken>*</AsteriskToken>
        </ZeroOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest110()
        {
            Test(@"@""\b\w+?\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <LazyQuantifier>
        <OneOrMoreQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
          <PlusToken>+</PlusToken>
        </OneOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest111()
        {
            Test(@"@""^\s*(System.)??Console.Write(Line)??\(??""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <StartAnchor>
        <CaretToken>^</CaretToken>
      </StartAnchor>
      <ZeroOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>s</TextToken>
        </SimpleEscape>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <LazyQuantifier>
        <ZeroOrOneQuantifier>
          <SimpleGrouping>
            <OpenParenToken>(</OpenParenToken>
            <Sequence>
              <Text>
                <TextToken>S</TextToken>
              </Text>
              <Text>
                <TextToken>y</TextToken>
              </Text>
              <Text>
                <TextToken>s</TextToken>
              </Text>
              <Text>
                <TextToken>t</TextToken>
              </Text>
              <Text>
                <TextToken>e</TextToken>
              </Text>
              <Text>
                <TextToken>m</TextToken>
              </Text>
              <Wildcard>
                <DotToken>.</DotToken>
              </Wildcard>
            </Sequence>
            <CloseParenToken>)</CloseParenToken>
          </SimpleGrouping>
          <QuestionToken>?</QuestionToken>
        </ZeroOrOneQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <Text>
        <TextToken>C</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>n</TextToken>
      </Text>
      <Text>
        <TextToken>s</TextToken>
      </Text>
      <Text>
        <TextToken>o</TextToken>
      </Text>
      <Text>
        <TextToken>l</TextToken>
      </Text>
      <Text>
        <TextToken>e</TextToken>
      </Text>
      <Wildcard>
        <DotToken>.</DotToken>
      </Wildcard>
      <Text>
        <TextToken>W</TextToken>
      </Text>
      <Text>
        <TextToken>r</TextToken>
      </Text>
      <Text>
        <TextToken>i</TextToken>
      </Text>
      <Text>
        <TextToken>t</TextToken>
      </Text>
      <Text>
        <TextToken>e</TextToken>
      </Text>
      <LazyQuantifier>
        <ZeroOrOneQuantifier>
          <SimpleGrouping>
            <OpenParenToken>(</OpenParenToken>
            <Sequence>
              <Text>
                <TextToken>L</TextToken>
              </Text>
              <Text>
                <TextToken>i</TextToken>
              </Text>
              <Text>
                <TextToken>n</TextToken>
              </Text>
              <Text>
                <TextToken>e</TextToken>
              </Text>
            </Sequence>
            <CloseParenToken>)</CloseParenToken>
          </SimpleGrouping>
          <QuestionToken>?</QuestionToken>
        </ZeroOrOneQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <LazyQuantifier>
        <ZeroOrOneQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <OpenParenToken>(</OpenParenToken>
          </SimpleEscape>
          <QuestionToken>?</QuestionToken>
        </ZeroOrOneQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest112()
        {
            Test(@"@""(System.)??""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <ZeroOrOneQuantifier>
          <SimpleGrouping>
            <OpenParenToken>(</OpenParenToken>
            <Sequence>
              <Text>
                <TextToken>S</TextToken>
              </Text>
              <Text>
                <TextToken>y</TextToken>
              </Text>
              <Text>
                <TextToken>s</TextToken>
              </Text>
              <Text>
                <TextToken>t</TextToken>
              </Text>
              <Text>
                <TextToken>e</TextToken>
              </Text>
              <Text>
                <TextToken>m</TextToken>
              </Text>
              <Wildcard>
                <DotToken>.</DotToken>
              </Wildcard>
            </Sequence>
            <CloseParenToken>)</CloseParenToken>
          </SimpleGrouping>
          <QuestionToken>?</QuestionToken>
        </ZeroOrOneQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest113()
        {
            Test(@"@""\b(\w{3,}?\.){2}?\w{3,}?\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <LazyQuantifier>
        <ExactNumericQuantifier>
          <SimpleGrouping>
            <OpenParenToken>(</OpenParenToken>
            <Sequence>
              <LazyQuantifier>
                <OpenRangeNumericQuantifier>
                  <SimpleEscape>
                    <BackslashToken>\</BackslashToken>
                    <TextToken>w</TextToken>
                  </SimpleEscape>
                  <OpenBraceToken>{</OpenBraceToken>
                  <NumberToken value=""3"">3</NumberToken>
                  <CommaToken>,</CommaToken>
                  <CloseBraceToken>}</CloseBraceToken>
                </OpenRangeNumericQuantifier>
                <QuestionToken>?</QuestionToken>
              </LazyQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <DotToken>.</DotToken>
              </SimpleEscape>
            </Sequence>
            <CloseParenToken>)</CloseParenToken>
          </SimpleGrouping>
          <OpenBraceToken>{</OpenBraceToken>
          <NumberToken value=""2"">2</NumberToken>
          <CloseBraceToken>}</CloseBraceToken>
        </ExactNumericQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <LazyQuantifier>
        <OpenRangeNumericQuantifier>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
          <OpenBraceToken>{</OpenBraceToken>
          <NumberToken value=""3"">3</NumberToken>
          <CommaToken>,</CommaToken>
          <CloseBraceToken>}</CloseBraceToken>
        </OpenRangeNumericQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest114()
        {
            Test(@"@""\b[A-Z](\w*?\s*?){1,10}[.!?]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
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
      <ClosedRangeNumericQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <LazyQuantifier>
              <ZeroOrMoreQuantifier>
                <SimpleEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>w</TextToken>
                </SimpleEscape>
                <AsteriskToken>*</AsteriskToken>
              </ZeroOrMoreQuantifier>
              <QuestionToken>?</QuestionToken>
            </LazyQuantifier>
            <LazyQuantifier>
              <ZeroOrMoreQuantifier>
                <SimpleEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>s</TextToken>
                </SimpleEscape>
                <AsteriskToken>*</AsteriskToken>
              </ZeroOrMoreQuantifier>
              <QuestionToken>?</QuestionToken>
            </LazyQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""10"">10</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>.</TextToken>
          </Text>
          <Text>
            <TextToken>!</TextToken>
          </Text>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest115()
        {
            Test(@"@""b.*([0-9]{4})\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>b</TextToken>
      </Text>
      <ZeroOrMoreQuantifier>
        <Wildcard>
          <DotToken>.</DotToken>
        </Wildcard>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <ExactNumericQuantifier>
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
            <OpenBraceToken>{</OpenBraceToken>
            <NumberToken value=""4"">4</NumberToken>
            <CloseBraceToken>}</CloseBraceToken>
          </ExactNumericQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest116()
        {
            Test(@"@""\b.*?([0-9]{4})\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <LazyQuantifier>
        <ZeroOrMoreQuantifier>
          <Wildcard>
            <DotToken>.</DotToken>
          </Wildcard>
          <AsteriskToken>*</AsteriskToken>
        </ZeroOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <ExactNumericQuantifier>
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
            <OpenBraceToken>{</OpenBraceToken>
            <NumberToken value=""4"">4</NumberToken>
            <CloseBraceToken>}</CloseBraceToken>
          </ExactNumericQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest117()
        {
            Test(@"@""(a?)*""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <ZeroOrOneQuantifier>
              <Text>
                <TextToken>a</TextToken>
              </Text>
              <QuestionToken>?</QuestionToken>
            </ZeroOrOneQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest118()
        {
            Test(@"@""(a\1|(?(1)\1)){0,2}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ClosedRangeNumericQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Alternation>
            <Sequence>
              <Text>
                <TextToken>a</TextToken>
              </Text>
              <BackreferenceEscape>
                <BackslashToken>\</BackslashToken>
                <NumberToken value=""1"">1</NumberToken>
              </BackreferenceEscape>
            </Sequence>
            <BarToken>|</BarToken>
            <Sequence>
              <ConditionalCaptureGrouping>
                <OpenParenToken>(</OpenParenToken>
                <QuestionToken>?</QuestionToken>
                <OpenParenToken>(</OpenParenToken>
                <NumberToken value=""1"">1</NumberToken>
                <CloseParenToken>)</CloseParenToken>
                <Sequence>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value=""1"">1</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </ConditionalCaptureGrouping>
            </Sequence>
          </Alternation>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""0"">0</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""2"">2</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest119()
        {
            Test(@"@""(a\1|(?(1)\1)){2}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ExactNumericQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Alternation>
            <Sequence>
              <Text>
                <TextToken>a</TextToken>
              </Text>
              <BackreferenceEscape>
                <BackslashToken>\</BackslashToken>
                <NumberToken value=""1"">1</NumberToken>
              </BackreferenceEscape>
            </Sequence>
            <BarToken>|</BarToken>
            <Sequence>
              <ConditionalCaptureGrouping>
                <OpenParenToken>(</OpenParenToken>
                <QuestionToken>?</QuestionToken>
                <OpenParenToken>(</OpenParenToken>
                <NumberToken value=""1"">1</NumberToken>
                <CloseParenToken>)</CloseParenToken>
                <Sequence>
                  <BackreferenceEscape>
                    <BackslashToken>\</BackslashToken>
                    <NumberToken value=""1"">1</NumberToken>
                  </BackreferenceEscape>
                </Sequence>
                <CloseParenToken>)</CloseParenToken>
              </ConditionalCaptureGrouping>
            </Sequence>
          </Alternation>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""2"">2</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest120()
        {
            Test(@"@""(\w)\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest121()
        {
            Test(@"@""(?<char>\w)\k<char>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""char"">char</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""char"">char</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest122()
        {
            Test(@"@""(?<2>\w)\k<2>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest123()
        {
            Test(@"@""(?<1>a)(?<1>\1b)*""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <ZeroOrMoreQuantifier>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <NumberToken value=""1"">1</NumberToken>
          <GreaterThanToken>&gt;</GreaterThanToken>
          <Sequence>
            <BackreferenceEscape>
              <BackslashToken>\</BackslashToken>
              <NumberToken value=""1"">1</NumberToken>
            </BackreferenceEscape>
            <Text>
              <TextToken>b</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </CaptureGrouping>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest124()
        {
            Test(@"@""\b(\p{Lu}{2})(\d{2})?(\p{Lu}{2})\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <ExactNumericQuantifier>
            <CategoryEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>p</TextToken>
              <OpenBraceToken>{</OpenBraceToken>
              <EscapeCategoryToken>Lu</EscapeCategoryToken>
              <CloseBraceToken>}</CloseBraceToken>
            </CategoryEscape>
            <OpenBraceToken>{</OpenBraceToken>
            <NumberToken value=""2"">2</NumberToken>
            <CloseBraceToken>}</CloseBraceToken>
          </ExactNumericQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <ZeroOrOneQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <ExactNumericQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <OpenBraceToken>{</OpenBraceToken>
              <NumberToken value=""2"">2</NumberToken>
              <CloseBraceToken>}</CloseBraceToken>
            </ExactNumericQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <ExactNumericQuantifier>
            <CategoryEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>p</TextToken>
              <OpenBraceToken>{</OpenBraceToken>
              <EscapeCategoryToken>Lu</EscapeCategoryToken>
              <CloseBraceToken>}</CloseBraceToken>
            </CategoryEscape>
            <OpenBraceToken>{</OpenBraceToken>
            <NumberToken value=""2"">2</NumberToken>
            <CloseBraceToken>}</CloseBraceToken>
          </ExactNumericQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest125()
        {
            Test(@"@""\bgr[ae]y\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>r</TextToken>
      </Text>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>e</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken>y</TextToken>
      </Text>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest126()
        {
            Test(@"@""\b((?# case sensitive comparison)D\w+)\s(?ixn)((?#case insensitive comparison)d\w+)\b""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <CommentTrivia>(?# case sensitive comparison)</CommentTrivia>
              </Trivia>D</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </SimpleEscape>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>ixn</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <CommentTrivia>(?#case insensitive comparison)</CommentTrivia>
              </Trivia>d</TextToken>
          </Text>
          <OneOrMoreQuantifier>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </SimpleEscape>
            <PlusToken>+</PlusToken>
          </OneOrMoreQuantifier>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void ReferenceTest127()
        {
            Test(@"@""\{\d+(,-*\d+)*(\:\w{1,4}?)*\}(?x) # Looks for a composite format item.""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <OpenBraceToken>{</OpenBraceToken>
      </SimpleEscape>
      <OneOrMoreQuantifier>
        <SimpleEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>d</TextToken>
        </SimpleEscape>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
      <ZeroOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>,</TextToken>
            </Text>
            <ZeroOrMoreQuantifier>
              <Text>
                <TextToken>-</TextToken>
              </Text>
              <AsteriskToken>*</AsteriskToken>
            </ZeroOrMoreQuantifier>
            <OneOrMoreQuantifier>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>d</TextToken>
              </SimpleEscape>
              <PlusToken>+</PlusToken>
            </OneOrMoreQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <ZeroOrMoreQuantifier>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <SimpleEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>:</TextToken>
            </SimpleEscape>
            <LazyQuantifier>
              <ClosedRangeNumericQuantifier>
                <SimpleEscape>
                  <BackslashToken>\</BackslashToken>
                  <TextToken>w</TextToken>
                </SimpleEscape>
                <OpenBraceToken>{</OpenBraceToken>
                <NumberToken value=""1"">1</NumberToken>
                <CommaToken>,</CommaToken>
                <NumberToken value=""4"">4</NumberToken>
                <CloseBraceToken>}</CloseBraceToken>
              </ClosedRangeNumericQuantifier>
              <QuestionToken>?</QuestionToken>
            </LazyQuantifier>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>}</TextToken>
      </SimpleEscape>
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
        <CommentTrivia># Looks for a composite format item.</CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }
    }
}
