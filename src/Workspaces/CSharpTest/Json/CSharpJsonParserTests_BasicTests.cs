// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Json
{ 
    public partial class CSharpJsonParserTests
    {
        [Fact]
        public void TestOneSpace()
        {
            Test(@""" """, @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Syntax error"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestTwoSpaces()
        {
            Test(@"""  """, @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia>  </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Syntax error"" Start=""9"" Length=""2"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestSingleLineComment()
        {
            Test(@"""//""", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <SingleLineCommentTrivia>//</SingleLineCommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated comment"" Start=""9"" Length=""2"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestSingleLineCommentWithContent()
        {
            Test(@"""// """, @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <SingleLineCommentTrivia>// </SingleLineCommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestEmptyMultiLineComment()
        {
            Test(@"""/**/""", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <MultiLineCommentTrivia>/**/</MultiLineCommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestMultiLineCommentWithStar()
        {
            Test(@"""/***/""", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <MultiLineCommentTrivia>/***/</MultiLineCommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray1()
        {
            Test(@"""[]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence />
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray2()
        {
            Test(@""" [ ] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>[<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence />
        <CloseBracketToken>]<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray3()
        {
            Test(@"""[""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence />
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""']' expected"" Start=""10"" Length=""0"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestArray4()
        {
            Test(@"""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>]</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""']' unexpected"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestArray5()
        {
            Test(@"""[,]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray6()
        {
            Test(@"""[true,]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray7()
        {
            Test(@"""[true]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray8()
        {
            Test(@"""[,,]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray9()
        {
            Test(@"""[true,,]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray10()
        {
            Test(@"""[,true,]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestArray11()
        {
            Test(@"""[,,true]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestTrueLiteral1()
        {
            Test(@"""true""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <TrueLiteralToken>true</TrueLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestTrueLiteral2()
        {
            Test(@""" true """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <TrueLiteralToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>true<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TrueLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestFalseLiteral1()
        {
            Test(@"""false""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <FalseLiteralToken>false</FalseLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestFalseLiteral2()
        {
            Test(@""" false """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <FalseLiteralToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>false<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></FalseLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNullLiteral1()
        {
            Test(@"""null""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NullLiteralToken>null</NullLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNullLiteral2()
        {
            Test(@""" null """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NullLiteralToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>null<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NullLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestUndefinedLiteral1()
        {
            Test(@"""undefined""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <UndefinedLiteralToken>undefined</UndefinedLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNaNLiteral1()
        {
            Test(@"""NaN""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NaNLiteralToken>NaN</NaNLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNaNLiteral2()
        {
            Test(@""" NaN """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NaNLiteralToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>NaN<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NaNLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNaNLiteral3()
        {
            Test(@"""nan""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>nan</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'n' unexpected"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestInfinity1()
        {
            Test(@"""Infinity""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <InfinityLiteralToken>Infinity</InfinityLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNegativeInfinity1()
        {
            Test(@"""-Infinity""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLiteral>
        <MinusToken>-</MinusToken>
        <InfinityLiteralToken>Infinity</InfinityLiteralToken>
      </NegativeLiteral>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNegativeInfinity2()
        {
            Test(@"""- Infinity""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NumberToken>-<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
      </Literal>
      <Literal>
        <InfinityLiteralToken>Infinity</InfinityLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid number"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestArrayWithMissingCommas()
        {
            Test(@"""[0 1 2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
          </Literal>
          <Literal>
            <NumberToken>1<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
          </Literal>
          <Literal>
            <NumberToken>2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""',' expected"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestIncompleteNull1()
        {
            Test(@"""n""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>n</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'n' unexpected"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestIncompleteNull2()
        {
            Test(@"""nu""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>nu</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'n' unexpected"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestIncompleteUnicode1()
        {
            Test(@"@""'h\u123'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>'h\u123'</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid escape sequence"" Start=""12"" Length=""6"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestIncompleteEscape()
        {
            Test(@"@""'h\u'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>'h\u'</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid escape sequence"" Start=""12"" Length=""3"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestInvalidNonBase10()
        {
            Test(@"""0aq2dun13.hod""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NumberToken>0aq2dun13.hod</NumberToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid number"" Start=""9"" Length=""13"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestUnterminatedString()
        {
            Test(@"""'hi""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>'hi</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated string"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestExtraEndToken()
        {
            Test(@"""{}}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence />
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'}' unexpected"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestMultiObject1()
        {
            Test(@"""{'first':1,'second':2,'third':3}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'first'</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>1</NumberToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Property>
            <StringToken>'second'</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>2</NumberToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Property>
            <StringToken>'third'</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>3</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestExtraChar()
        {
            Test(@"""nullz""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>nullz</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'n' unexpected"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestMissingColon()
        {
            Test(@"@""{ 'a': 0, 'b' 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'a'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0</NumberToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>'b'<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
          </Literal>
          <Literal>
            <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
          </Literal>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Only properties allowed in a json object"" Start=""20"" Length=""3"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestAdditionalContentComma()
        {
            Test(@"@""[
""""Small"""",
""""Medium"""",
""""Large""""
],""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""Small""</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>""Medium""</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>""Large""<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
      <EmptyValue>
        <CommaToken>,</CommaToken>
      </EmptyValue>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""',' unexpected"" Start=""50"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestAdditionalContentText()
        {
            Test(@"@""[
""""Small"""",
""""Medium"""",
""""Large""""
]content""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""Small""</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>""Medium""</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>""Large""<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
      <Text>
        <TextToken>content</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'c' unexpected"" Start=""50"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestAdditionalContentWhitespaceText()
        {
            Test(@"@""'hi' a""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>'hi'<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
      </Literal>
      <Text>
        <TextToken>a</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'a' unexpected"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestTrailingCommentStart()
        {
            Test(@"@""true/""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <TrueLiteralToken>true<Trivia><SingleLineCommentTrivia>/</SingleLineCommentTrivia></Trivia></TrueLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Error parsing comment"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestBadCharInArray()
        {
            Test(@"@""[}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>}</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'}' unexpected"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestIncompleteObject()
        {
            Test(@"@""{""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence />
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'}' expected"" Start=""11"" Length=""0"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestEmptyObject()
        {
            Test(@"@""{}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence />
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestLargeInt()
        {
            Test(@"@""3333333333333333333333333333333333333""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NumberToken>3333333333333333333333333333333333333</NumberToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestIdentifierProperty()
        {
            Test(@"@""{ a: 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>a</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNumericProperty()
        {
            Test(@"@""{ 1: 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>1</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNegativeNumericProperty()
        {
            Test(@"@""{ -1: 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>-1</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid property name"" Start=""12"" Length=""2"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestArrayPropertyName()
        {
            Test(@"@""{ []: 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Array>
            <OpenBracketToken>[</OpenBracketToken>
            <Sequence />
            <CloseBracketToken>]</CloseBracketToken>
          </Array>
          <Text>
            <TextToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
          </Text>
          <Literal>
            <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
          </Literal>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""':' unexpected"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestNaNPropertyName()
        {
            Test(@"@""{ NaN: 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>NaN</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestInfinityPropertyName()
        {
            Test(@"@""{ Infinity: 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>Infinity</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNullPropertyName()
        {
            Test(@"@""{ null: 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>null</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestUndefinedPropertyName()
        {
            Test(@"@""{ undefined: 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>undefined</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNameWithSpace()
        {
            Test(@"@""{ a b : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Text>
            <TextToken>a<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
          </Text>
          <Property>
            <TextToken>b<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'a' unexpected"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestNameWithNumber()
        {
            Test(@"@""{ a0 : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>a0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNumberWithHexName()
        {
            Test(@"@""{ 0a : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>0a<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNumberWithNonHexName()
        {
            Test(@"@""{ 0z : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>0z<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestDollarPropName()
        {
            Test(@"@""{ $ : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>$<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestUnderscorePropName()
        {
            Test(@"@""{ _ : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>_<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestStrangeLegalPropName()
        {
            Test(@"@""{ 0$0 : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>0$0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestStrangeIllegalPropName()
        {
            Test(@"@""{ 0(0 : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Literal>
            <NumberToken>0</NumberToken>
          </Literal>
          <Text>
            <TextToken>(</TextToken>
          </Text>
          <Property>
            <TextToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'(' unexpected"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestStrangeIllegalPropName2()
        {
            Test(@"@""{ 0%0 : 0 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>0%0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid property name"" Start=""12"" Length=""3"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestObjectWithEmptyPropValue1()
        {
            Test(@"""{'first': , }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'first'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <EmptyValue>
              <CommaToken />
            </EmptyValue>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestObjectWithEmptyPropValue2()
        {
            Test(@"""{'first': }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'first'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Text>
              <TextToken>}</TextToken>
            </Text>
          </Property>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'}' unexpected"" Start=""19"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestObjectWithEmptyPropValue3()
        {
            Test(@"""{'first': """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'first'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <EmptyValue>
              <CommaToken />
            </EmptyValue>
          </Property>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Missing property value"" Start=""19"" Length=""0"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestNestedProp1()
        {
            Test(@"""{'first': 'second': 'third' }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'first'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Property>
              <StringToken>'second'</StringToken>
              <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
              <Literal>
                <StringToken>'third'<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
              </Literal>
            </Property>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Nested properties not allowed"" Start=""27"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestMultiItemList()
        {
            Test(@"""[{ 'name': 'Admin' },{ 'name': 'Publisher' },1,null,[],,'string']""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Object>
            <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
            <Sequence>
              <Property>
                <StringToken>'name'</StringToken>
                <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                <Literal>
                  <StringToken>'Admin'<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                </Literal>
              </Property>
            </Sequence>
            <CloseBraceToken>}</CloseBraceToken>
          </Object>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Object>
            <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
            <Sequence>
              <Property>
                <StringToken>'name'</StringToken>
                <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                <Literal>
                  <StringToken>'Publisher'<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                </Literal>
              </Property>
            </Sequence>
            <CloseBraceToken>}</CloseBraceToken>
          </Object>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <NullLiteralToken>null</NullLiteralToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Array>
            <OpenBracketToken>[</OpenBracketToken>
            <Sequence />
            <CloseBracketToken>]</CloseBracketToken>
          </Array>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>'string'</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestMultiLine1()
        {
            Test(@"@""
{'a':
'bc','d':true
}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>
          <Trivia>
            <EndOfLineTrivia>
</EndOfLineTrivia>
          </Trivia>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'a'</StringToken>
            <ColonToken>:<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>'bc'</StringToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Property>
            <StringToken>'d'</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <TrueLiteralToken>true<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></TrueLiteralToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestNestedObject()
        {
            Test(@"@""
{
  'description': 'A person',
  'type': 'object',
  'properties':
  {
    'name': {'type':'string'},
    'hobbies': {
      'type': 'array',
      'items': {'type':'string'}
    }
  }
}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>
          <Trivia>
            <EndOfLineTrivia>
</EndOfLineTrivia>
          </Trivia>{<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'description'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>'A person'</StringToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <StringToken>'type'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>'object'</StringToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <StringToken>'properties'</StringToken>
            <ColonToken>:<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></ColonToken>
            <Object>
              <OpenBraceToken>{<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>    </WhitespaceTrivia></Trivia></OpenBraceToken>
              <Sequence>
                <Property>
                  <StringToken>'name'</StringToken>
                  <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>'type'</StringToken>
                        <ColonToken>:</ColonToken>
                        <Literal>
                          <StringToken>'string'</StringToken>
                        </Literal>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Property>
                <EmptyValue>
                  <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>    </WhitespaceTrivia></Trivia></CommaToken>
                </EmptyValue>
                <Property>
                  <StringToken>'hobbies'</StringToken>
                  <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                  <Object>
                    <OpenBraceToken>{<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>      </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>'type'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>'array'</StringToken>
                        </Literal>
                      </Property>
                      <EmptyValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>      </WhitespaceTrivia></Trivia></CommaToken>
                      </EmptyValue>
                      <Property>
                        <StringToken>'items'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Object>
                          <OpenBraceToken>{</OpenBraceToken>
                          <Sequence>
                            <Property>
                              <StringToken>'type'</StringToken>
                              <ColonToken>:</ColonToken>
                              <Literal>
                                <StringToken>'string'</StringToken>
                              </Literal>
                            </Property>
                          </Sequence>
                          <CloseBraceToken>}<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>    </WhitespaceTrivia></Trivia></CloseBraceToken>
                        </Object>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>}<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CloseBraceToken>
                  </Object>
                </Property>
              </Sequence>
              <CloseBraceToken>}<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CloseBraceToken>
            </Object>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestLiterals1()
        {
            Test(@"@""{ A: '', B: 1, C: , D: 1.23, E: 3.45, F: null }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>A</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>''</StringToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <TextToken>B</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>1</NumberToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <TextToken>C</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <EmptyValue>
              <CommaToken />
            </EmptyValue>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <TextToken>D</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>1.23</NumberToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <TextToken>E</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>3.45</NumberToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <TextToken>F</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NullLiteralToken>null<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NullLiteralToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestLiterals2()
        {
            // Note: we don't run subtree tests on json.net v9 because it has a bug where
            // it won't fail when we expect it to.  This is due to:
            //
            // https://github.com/JamesNK/Newtonsoft.Json/releases
            // Fix - Fixed JObject/JArray Parse not throwing an error when there is a comment followed by additional content
            Test(@"@""[
  1,
  0,
  1.1,
  0.0,
  0.000000000001,
  9999999999,
  -9999999999,
  9999999999999999999999999999999999999999999999999999999999999999999999,
  -9999999999999999999999999999999999999999999999999999999999999999999999,
  'true',
  'TRUE',
  'false',
  'FALSE',
  // comment!
  /* comment! */
  '',
  null
]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>1.1</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0.0</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0.000000000001</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>9999999999</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>-9999999999</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>9999999999999999999999999999999999999999999999999999999999999999999999</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>-9999999999999999999999999999999999999999999999999999999999999999999999</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>'true'</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>'TRUE'</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>'false'</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>'FALSE'</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia><SingleLineCommentTrivia>// comment!</SingleLineCommentTrivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia><MultiLineCommentTrivia>/* comment! */</MultiLineCommentTrivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>''</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>  </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NullLiteralToken>null<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></NullLiteralToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", runJsonNetSubTreeTests: false);
        }

        [Fact]
        public void TestCommentsInArray()
        {
            // Note: we don't run subtree tests on json.net v9 because it has a bug where
            // it won't fail when we expect it to.  This is due to:
            //
            // https://github.com/JamesNK/Newtonsoft.Json/releases
            // Fix - Fixed JObject/JArray Parse not throwing an error when there is a comment followed by additional content
            Test(@"@""[/*hi*/1/*hi*/,2/*hi*/]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><MultiLineCommentTrivia>/*hi*/</MultiLineCommentTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1<Trivia><MultiLineCommentTrivia>/*hi*/</MultiLineCommentTrivia></Trivia></NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>2<Trivia><MultiLineCommentTrivia>/*hi*/</MultiLineCommentTrivia></Trivia></NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", runJsonNetSubTreeTests: false);
        }

        [Fact]
        public void TestUnicode2()
        {
            Test(@"@""{'text':0xabcdef12345}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'text'</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>0xabcdef12345</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestOctal1()
        {
            Test(@"@""[0372, 0xFA, 0XFA]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0372</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0xFA</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0XFA</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestObjectLiteralComments()
        {
            // Note: we don't run the json.net v9 because it has a bug where it will fail when we 
            // expect it not to.  When we move to json.net 10 we can enable the test.

            Test(@"@""/*comment*/ { /*comment*/
        'Name': /*comment*/ 'Apple' /*comment*/, /*comment*/
        'ExpiryDate': '1',
        'Price': 3.99,
        'Sizes': /*comment*/ [ /*comment*/
          'Small', /*comment*/
          'Medium' /*comment*/,
          /*comment*/ 'Large'
        /*comment*/ ] /*comment*/
      } /*comment*/""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>
          <Trivia>
            <MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>        </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'Name'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>'Apple'<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia></Trivia></StringToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>        </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <StringToken>'ExpiryDate'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>'1'</StringToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>        </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <StringToken>'Price'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>3.99</NumberToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>        </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Property>
            <StringToken>'Sizes'</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Array>
              <OpenBracketToken>[<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>          </WhitespaceTrivia></Trivia></OpenBracketToken>
              <Sequence>
                <Literal>
                  <StringToken>'Small'</StringToken>
                </Literal>
                <EmptyValue>
                  <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>          </WhitespaceTrivia></Trivia></CommaToken>
                </EmptyValue>
                <Literal>
                  <StringToken>'Medium'<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia></Trivia></StringToken>
                </Literal>
                <EmptyValue>
                  <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>          </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                </EmptyValue>
                <Literal>
                  <StringToken>'Large'<Trivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>        </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                </Literal>
              </Sequence>
              <CloseBracketToken>]<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
</EndOfLineTrivia><WhitespaceTrivia>      </WhitespaceTrivia></Trivia></CloseBracketToken>
            </Array>
          </Property>
        </Sequence>
        <CloseBraceToken>}<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia></Trivia></CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", runJsonNetCheck: false);
        }

        [Fact]
        public void TestEmptyStrings()
        {
            Test(@"@""['','','','','','','']""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>''</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>''</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>''</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>''</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>''</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>''</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>''</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestInvalidNumber()
        {
            Test(@"@""0-10""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NumberToken>0-10</NumberToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid number"" Start=""10"" Length=""4"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestSimpleEscapes()
        {
            Test(@"@""[false, true, true, false, 'test!', 1.11, 0e-10, 0E-10, 0.25e-5, 0.3e10, 6.0221418e23, 'Purple\r \n monkey\'s:\tdishwasher']""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <FalseLiteralToken>false</FalseLiteralToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <FalseLiteralToken>false</FalseLiteralToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>'test!'</StringToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>1.11</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0e-10</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0E-10</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0.25e-5</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>0.3e10</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>6.0221418e23</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <StringToken>'Purple\r \n monkey\'s:\tdishwasher'</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestDoubleQuoteInSingleQuote()
        {
            Test(@"@""'a""""b'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>'a""b'</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestMultiLineString()
        {
            Test(@"@""'a
b'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>'a
b'</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor1()
        {
            Test(@"@""new""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new</NewKeyword>
        <TextToken />
        <OpenParenToken />
        <Sequence />
        <CloseParenToken />
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Name expected"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestConstructor2()
        {
            Test(@"@""new A""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken />
        <Sequence />
        <CloseParenToken />
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""'(' expected"" Start=""15"" Length=""0"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestConstructor3()
        {
            Test(@"@""new A(""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken />
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""')' expected"" Start=""16"" Length=""0"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestConstructor4()
        {
            Test(@"@""new A()""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor5()
        {
            Test(@"@""new A(1)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor6()
        {
            Test(@"@""new A(1, 2)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>2</NumberToken>
          </Literal>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor7()
        {
            Test(@"@""new A([new B()])""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Array>
            <OpenBracketToken>[</OpenBracketToken>
            <Sequence>
              <Constructor>
                <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
                <TextToken>B</TextToken>
                <OpenParenToken>(</OpenParenToken>
                <Sequence />
                <CloseParenToken>)</CloseParenToken>
              </Constructor>
            </Sequence>
            <CloseBracketToken>]</CloseBracketToken>
          </Array>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor8()
        {
            Test(@"@""new A(,)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor9()
        {
            Test(@"@""new A(1,)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor10()
        {
            Test(@"@""new A(,1)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor11()
        {
            Test(@"@""new A(1,1)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor12()
        {
            Test(@"@""new A(1,,1)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>");
        }

        [Fact]
        public void TestConstructor13()
        {
            Test(@"@""new %()""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>%</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid constructor name"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestConstructor14()
        {
            Test(@"@""new A(1 2)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Constructor>
        <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
        <TextToken>A</TextToken>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Literal>
            <NumberToken>1<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
          </Literal>
          <Literal>
            <NumberToken>2</NumberToken>
          </Literal>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </Constructor>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""',' expected"" Start=""18"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }

        [Fact]
        public void TestMultipleCommasInObject()
        {
            Test(@"@""{0:0,,1:1}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>0</TextToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>0</NumberToken>
            </Literal>
          </Property>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <EmptyValue>
            <CommaToken>,</CommaToken>
          </EmptyValue>
          <Property>
            <TextToken>1</TextToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>1</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Only properties allowed in a json object"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>");
        }
    }
}
