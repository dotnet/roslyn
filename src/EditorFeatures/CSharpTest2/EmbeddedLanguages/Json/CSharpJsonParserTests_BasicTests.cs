// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.Json;

public sealed partial class CSharpJsonParserBasicTests : CSharpJsonParserTests
{
    [Fact]
    public void TestEmpty()
    {
        Test("""
            ""
            """, expected: null,
            "",
            "", runLooseSubTreeCheck: false);
    }

    [Fact]
    public void TestOneSpace()
    {
        Test("""
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Syntax error" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Syntax error" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestTwoSpaces()
    {
        Test("""
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Syntax error" Start="9" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Syntax error" Start="9" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestTabSpace()
    {
        Test("""
            "\t"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia>	</WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Syntax error" Start="9" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Syntax error" Start="9" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestFormFeed()
    {
        Test("""
            "\f"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <WhitespaceTrivia>\f</WhitespaceTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Syntax error" Start="9" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Syntax error" Start="9" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestFormFeed2()
    {
        Test("""
            "[\f1,0]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[<Trivia><WhitespaceTrivia>\f</WhitespaceTrivia></Trivia></OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Illegal whitespace character" Start="10" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestFormFeed3()
    {
        // .net strict parsers don't report the problem with the trailing \f.  we do as it's
        // per the ecma spec.
        Test("""
            "[0\f,1]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>0<Trivia><WhitespaceTrivia>\f</WhitespaceTrivia></Trivia></NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Illegal whitespace character" Start="11" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSingleLineComment()
    {
        Test("""
            "//"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <SingleLineCommentTrivia>//</SingleLineCommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Unterminated comment" Start="9" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Unterminated comment" Start="9" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSingleLineCommentWithContent()
    {
        Test("""
            "// "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <SingleLineCommentTrivia>// </SingleLineCommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Comments not allowed" Start="9" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestEmptyMultiLineComment()
    {
        Test("""
            "/**/"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <MultiLineCommentTrivia>/**/</MultiLineCommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Comments not allowed" Start="9" Length="4" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiLineCommentWithStar()
    {
        Test("""
            "/***/"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence />
                <EndOfFile>
                  <Trivia>
                    <MultiLineCommentTrivia>/***/</MultiLineCommentTrivia>
                  </Trivia>
                </EndOfFile>
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Comments not allowed" Start="9" Length="5" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArray1()
    {
        Test("""
            "[]"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestArray2()
    {
        Test("""
            " [ ] "
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestArray3()
    {
        Test("""
            "["
            """, """
            <Tree>
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
            </Tree>
            """,
    """
    <Diagnostics>
      <Diagnostic Message="']' expected" Start="10" Length="0" />
    </Diagnostics>
    """,
    """
    <Diagnostics>
      <Diagnostic Message="']' expected" Start="10" Length="0" />
    </Diagnostics>
    """);
    }

    [Fact]
    public void TestArray4()
    {
        Test("""
            "]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>]</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="']' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="']' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArray5()
    {
        Test("""
            "[,]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="',' unexpected" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArray6()
    {
        Test("""
            "[true,]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <TrueLiteralToken>true</TrueLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Trailing comma not allowed" Start="14" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArray7()
    {
        Test("""
            "[true]"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestArray8()
    {
        Test("""
            "[,,]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="',' unexpected" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArray9()
    {
        Test("""
            "[true,,]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <TrueLiteralToken>true</TrueLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="',' unexpected" Start="15" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArray10()
    {
        Test("""
            "[,true,]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <TrueLiteralToken>true</TrueLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="',' unexpected" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArray11()
    {
        Test("""
            "[,,true]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <TrueLiteralToken>true</TrueLiteralToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="',' unexpected" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestTrueLiteral1()
    {
        Test("""
            "true"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <TrueLiteralToken>true</TrueLiteralToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestTrueLiteral2()
    {
        Test("""
            " true "
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestFalseLiteral1()
    {
        Test("""
            "false"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <FalseLiteralToken>false</FalseLiteralToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestFalseLiteral2()
    {
        Test("""
            " false "
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNullLiteral1()
    {
        Test("""
            "null"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NullLiteralToken>null</NullLiteralToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNullLiteral2()
    {
        Test("""
            " null "
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestUndefinedLiteral1()
    {
        Test("""
            "undefined"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <UndefinedLiteralToken>undefined</UndefinedLiteralToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="'undefined' literal not allowed" Start="9" Length="9" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNaNLiteral1()
    {
        Test("""
            "NaN"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NaNLiteralToken>NaN</NaNLiteralToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="'NaN' literal not allowed" Start="9" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNaNLiteral2()
    {
        Test("""
            " NaN "
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="'NaN' literal not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNaNLiteral3()
    {
        Test("""
            "nan"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>nan</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'n' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'n' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestInfinity1()
    {
        Test("""
            "Infinity"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <InfinityLiteralToken>Infinity</InfinityLiteralToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="'Infinity' literal not allowed" Start="9" Length="8" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNegativeInfinity1()
    {
        Test("""
            "-Infinity"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <NegativeLiteral>
                    <MinusToken>-</MinusToken>
                    <InfinityLiteralToken>Infinity</InfinityLiteralToken>
                  </NegativeLiteral>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="'-Infinity' literal not allowed" Start="9" Length="9" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNegativeInfinity2()
    {
        Test("""
            "- Infinity"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArrayWithMissingCommas()
    {
        Test("""
            "[0 1 2]"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="',' expected" Start="12" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="',' expected" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIncompleteNull1()
    {
        Test("""
            "n"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>n</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'n' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'n' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIncompleteNull2()
    {
        Test("""
            "nu"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>nu</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'n' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'n' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIncompleteUnicode1()
    {
        Test("""
            @"'h\u123'"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>'h\u123'</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="12" Length="6" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIncompleteEscape()
    {
        Test("""
            @"'h\u'"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>'h\u'</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="12" Length="3" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIncompleteUnicode2()
    {
        Test(""""
            @"""h\u123"""
            """", """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>"h\u123"</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="13" Length="7" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="13" Length="7" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIncompleteEscape2()
    {
        Test(""""
            @"""h\u"""
            """", """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>"h\u"</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="13" Length="4" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="13" Length="4" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestInvalidNonBase10()
    {
        Test("""
            "0aq2dun13.hod"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0aq2dun13.hod</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="9" Length="13" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="9" Length="13" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestUnterminatedString()
    {
        Test("""
            "'hi"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>'hi</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Unterminated string" Start="9" Length="3" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Unterminated string" Start="9" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestUnterminatedString2()
    {
        Test("""
            "\"hi"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>"hi</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Unterminated string" Start="9" Length="4" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Unterminated string" Start="9" Length="4" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestExtraEndToken()
    {
        Test("""
            "{}}"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="11" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiObject1()
    {
        Test("""
            "{'first':1,'second':2,'third':3}"
            """, """
            <Tree>
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
                      <CommaToken>,</CommaToken>
                      <Property>
                        <StringToken>'second'</StringToken>
                        <ColonToken>:</ColonToken>
                        <Literal>
                          <NumberToken>2</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,</CommaToken>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiObject2()
    {
        Test("""
            "{\"first\":1,\"second\":2,\"third\":3}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>"first"</StringToken>
                        <ColonToken>:</ColonToken>
                        <Literal>
                          <NumberToken>1</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,</CommaToken>
                      <Property>
                        <StringToken>"second"</StringToken>
                        <ColonToken>:</ColonToken>
                        <Literal>
                          <NumberToken>2</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,</CommaToken>
                      <Property>
                        <StringToken>"third"</StringToken>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestExtraChar()
    {
        Test("""
            "nullz"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>nullz</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'n' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'n' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMissingColon()
    {
        Test("""
            @"{ 'a': 0, 'b' 0 }"
            """, """
            <Tree>
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
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Literal>
                        <StringToken>'b'<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                      </Literal>
                      <CommaToken />
                      <Literal>
                        <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be followed by a ':'" Start="20" Length="3" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNestedPropertyMissingColon()
    {
        Test("""
            @"
            {
              ""description"": ""A person"",
              ""type"": ""object"",
              ""properties"":
              {
                ""name"" {""type"":""string""},
                ""hobbies"": {
                  ""type"": ""array"",
                  ""items"": {""type"":""string""}
                }
              }
            }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>
                      <Trivia>
                        <EndOfLineTrivia>
            </EndOfLineTrivia>
                      </Trivia>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>"description"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>"A person"</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>"type"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>"object"</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>"properties"</StringToken>
                        <ColonToken>:<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></ColonToken>
                        <Object>
                          <OpenBraceToken>
                            <Trivia>
                              <WhitespaceTrivia>  </WhitespaceTrivia>
                            </Trivia>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                          <Sequence>
                            <Literal>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>    </WhitespaceTrivia>
                                </Trivia>"name"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                            </Literal>
                            <CommaToken />
                            <Object>
                              <OpenBraceToken>{</OpenBraceToken>
                              <Sequence>
                                <Property>
                                  <StringToken>"type"</StringToken>
                                  <ColonToken>:</ColonToken>
                                  <Literal>
                                    <StringToken>"string"</StringToken>
                                  </Literal>
                                </Property>
                              </Sequence>
                              <CloseBraceToken>}</CloseBraceToken>
                            </Object>
                            <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                            <Property>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>    </WhitespaceTrivia>
                                </Trivia>"hobbies"</StringToken>
                              <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                              <Object>
                                <OpenBraceToken>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                                <Sequence>
                                  <Property>
                                    <StringToken>
                                      <Trivia>
                                        <WhitespaceTrivia>      </WhitespaceTrivia>
                                      </Trivia>"type"</StringToken>
                                    <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                                    <Literal>
                                      <StringToken>"array"</StringToken>
                                    </Literal>
                                  </Property>
                                  <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                                  <Property>
                                    <StringToken>
                                      <Trivia>
                                        <WhitespaceTrivia>      </WhitespaceTrivia>
                                      </Trivia>"items"</StringToken>
                                    <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                                    <Object>
                                      <OpenBraceToken>{</OpenBraceToken>
                                      <Sequence>
                                        <Property>
                                          <StringToken>"type"</StringToken>
                                          <ColonToken>:</ColonToken>
                                          <Literal>
                                            <StringToken>"string"</StringToken>
                                          </Literal>
                                        </Property>
                                      </Sequence>
                                      <CloseBraceToken>}<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                                    </Object>
                                  </Property>
                                </Sequence>
                                <CloseBraceToken>
                                  <Trivia>
                                    <WhitespaceTrivia>    </WhitespaceTrivia>
                                  </Trivia>}<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                              </Object>
                            </Property>
                          </Sequence>
                          <CloseBraceToken>
                            <Trivia>
                              <WhitespaceTrivia>  </WhitespaceTrivia>
                            </Trivia>}<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                        </Object>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be followed by a ':'" Start="102" Length="8" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be followed by a ':'" Start="102" Length="8" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMissingColon2()
    {
        Test("""
            @"{ ""a"": 0, ""b"" 0 }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>"a"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>0</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Literal>
                        <StringToken>"b"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                      </Literal>
                      <CommaToken />
                      <Literal>
                        <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be followed by a ':'" Start="22" Length="5" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be followed by a ':'" Start="22" Length="5" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestAdditionalContentComma()
    {
        Test("""
            @"[
            ""Small"",
            ""Medium"",
            ""Large""
            ],"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <StringToken>"Small"</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>"Medium"</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>"Large"<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></StringToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                  <CommaValue>
                    <CommaToken>,</CommaToken>
                  </CommaValue>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="',' unexpected" Start="50" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="',' unexpected" Start="50" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestAdditionalContentText()
    {
        Test("""
            @"[
            ""Small"",
            ""Medium"",
            ""Large""
            ]content"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <StringToken>"Small"</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>"Medium"</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>"Large"<Trivia><EndOfLineTrivia>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'c' unexpected" Start="50" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'c' unexpected" Start="50" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestAdditionalContentWhitespaceText()
    {
        Test("""
            @"'hi' a"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'a' unexpected" Start="15" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestAdditionalContentWhitespaceText2()
    {
        Test(""""
            @"""hi"" a"
            """", """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>"hi"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                  </Literal>
                  <Text>
                    <TextToken>a</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'a' unexpected" Start="17" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'a' unexpected" Start="17" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestTrailingCommentStart()
    {
        Test("""
            @"true/"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <TrueLiteralToken>true<Trivia><SingleLineCommentTrivia>/</SingleLineCommentTrivia></Trivia></TrueLiteralToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Error parsing comment" Start="14" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Error parsing comment" Start="14" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestBadCharInArray()
    {
        Test("""
            @"[}"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="11" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIncompleteObject()
    {
        Test("""
            @"{"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' expected" Start="11" Length="0" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' expected" Start="11" Length="0" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestEmptyObject()
    {
        Test("""
            @"{}"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestLargeInt()
    {
        Test("""
            @"3333333333333333333333333333333333333"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>3333333333333333333333333333333333333</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestIdentifierProperty()
    {
        Test("""
            @"{ a: 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumericProperty()
    {
        Test("""
            @"{ 1: 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNegativeNumericProperty()
    {
        Test("""
            @"{ -1: 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid property name" Start="12" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestArrayPropertyName()
    {
        Test("""
            @"{ []: 0 }"
            """, """
            <Tree>
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
                      <CommaToken />
                      <Text>
                        <TextToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
                      </Text>
                      <CommaToken />
                      <Literal>
                        <NumberToken>0<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="12" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNaNPropertyName()
    {
        Test("""
            @"{ NaN: 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestInfinityPropertyName()
    {
        Test("""
            @"{ Infinity: 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="8" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNullPropertyName()
    {
        Test("""
            @"{ null: 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="4" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestUndefinedPropertyName()
    {
        Test("""
            @"{ undefined: 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="9" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNameWithSpace()
    {
        Test("""
            @"{ a b : 0 }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Text>
                        <TextToken>a<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
                      </Text>
                      <CommaToken />
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'a' unexpected" Start="12" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'a' unexpected" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNameWithNumber()
    {
        Test("""
            @"{ a0 : 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumberWithHexName()
    {
        Test("""
            @"{ 0a : 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumberWithNonHexName()
    {
        Test("""
            @"{ 0z : 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestDollarPropName()
    {
        Test("""
            @"{ $ : 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestUnderscorePropName()
    {
        Test("""
            @"{ _ : 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestStrangeLegalPropName()
    {
        Test("""
            @"{ 0$0 : 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestStrangeIllegalPropName()
    {
        Test("""
            @"{ 0(0 : 0 }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>0</NumberToken>
                      </Literal>
                      <CommaToken />
                      <Text>
                        <TextToken>(</TextToken>
                      </Text>
                      <CommaToken />
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="12" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestStrangeIllegalPropName2()
    {
        Test("""
            @"{ 0%0 : 0 }"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid property name" Start="12" Length="3" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestObjectWithEmptyPropValue1()
    {
        Test("""
            "{'first': , }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>'first'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <CommaValue>
                          <CommaToken />
                        </CommaValue>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Value required" Start="18" Length="0" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestObjectWithEmptyPropValue2()
    {
        Test("""
            "{\"first\": , }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>"first"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <CommaValue>
                          <CommaToken />
                        </CommaValue>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Value required" Start="20" Length="0" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestObjectWithEmptyPropValue3()
    {
        Test("""
            "{'first': }"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="19" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestObjectWithEmptyPropValue4()
    {
        Test("""
            "{\"first\": }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>"first"</StringToken>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="21" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="21" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestObjectWithEmptyPropValue5()
    {
        Test("""
            "{'first': "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>'first'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <CommaValue>
                          <CommaToken />
                        </CommaValue>
                      </Property>
                    </Sequence>
                    <CloseBraceToken />
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Missing property value" Start="19" Length="0" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Value required" Start="18" Length="0" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestObjectWithEmptyPropValue6()
    {
        Test("""
            "{\"first\": "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>"first"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <CommaValue>
                          <CommaToken />
                        </CommaValue>
                      </Property>
                    </Sequence>
                    <CloseBraceToken />
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Missing property value" Start="21" Length="0" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Value required" Start="20" Length="0" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNestedProp1()
    {
        Test("""
            "{'first': 'second': 'third' }"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Nested properties not allowed" Start="27" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNestedProp2()
    {
        Test("""
            "{\"first\": \"second\": \"third\" }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>"first"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Property>
                          <StringToken>"second"</StringToken>
                          <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                          <Literal>
                            <StringToken>"third"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                          </Literal>
                        </Property>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Nested properties not allowed" Start="31" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Nested properties not allowed" Start="31" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiItemList()
    {
        Test("""
            "[{ 'name': 'Admin' },{ 'name': 'Publisher' },1,null,[],,'string']"
            """, """
            <Tree>
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
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
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
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NullLiteralToken>null</NullLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Array>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence />
                        <CloseBracketToken>]</CloseBracketToken>
                      </Array>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>'string'</StringToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiItemList2()
    {
        Test("""
            "[{ \"name\": \"Admin\" },{ \"name\": \"Publisher\" },1,null,[],,\"string\"]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Object>
                        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                        <Sequence>
                          <Property>
                            <StringToken>"name"</StringToken>
                            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                            <Literal>
                              <StringToken>"Admin"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                            </Literal>
                          </Property>
                        </Sequence>
                        <CloseBraceToken>}</CloseBraceToken>
                      </Object>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Object>
                        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                        <Sequence>
                          <Property>
                            <StringToken>"name"</StringToken>
                            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                            <Literal>
                              <StringToken>"Publisher"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                            </Literal>
                          </Property>
                        </Sequence>
                        <CloseBraceToken>}</CloseBraceToken>
                      </Object>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NullLiteralToken>null</NullLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Array>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence />
                        <CloseBracketToken>]</CloseBracketToken>
                      </Array>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>"string"</StringToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="',' unexpected" Start="72" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiLine1()
    {
        Test("""
            @"
            {'a':
            'bc','d':true
            }"
            """, """
            <Tree>
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
                      <CommaToken>,</CommaToken>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="13" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiLine2()
    {
        Test("""
            @"
            {""a"":
            ""bc"",""d"":true
            }"
            """, """
            <Tree>
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
                        <StringToken>"a"</StringToken>
                        <ColonToken>:<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>"bc"</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,</CommaToken>
                      <Property>
                        <StringToken>"d"</StringToken>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNestedObject()
    {
        Test("""
            @"
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
            }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>
                      <Trivia>
                        <EndOfLineTrivia>
            </EndOfLineTrivia>
                      </Trivia>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>'description'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>'A person'</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>'type'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>'object'</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>'properties'</StringToken>
                        <ColonToken>:<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></ColonToken>
                        <Object>
                          <OpenBraceToken>
                            <Trivia>
                              <WhitespaceTrivia>  </WhitespaceTrivia>
                            </Trivia>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                          <Sequence>
                            <Property>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>    </WhitespaceTrivia>
                                </Trivia>'name'</StringToken>
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
                            <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                            <Property>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>    </WhitespaceTrivia>
                                </Trivia>'hobbies'</StringToken>
                              <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                              <Object>
                                <OpenBraceToken>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                                <Sequence>
                                  <Property>
                                    <StringToken>
                                      <Trivia>
                                        <WhitespaceTrivia>      </WhitespaceTrivia>
                                      </Trivia>'type'</StringToken>
                                    <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                                    <Literal>
                                      <StringToken>'array'</StringToken>
                                    </Literal>
                                  </Property>
                                  <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                                  <Property>
                                    <StringToken>
                                      <Trivia>
                                        <WhitespaceTrivia>      </WhitespaceTrivia>
                                      </Trivia>'items'</StringToken>
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
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                                    </Object>
                                  </Property>
                                </Sequence>
                                <CloseBraceToken>
                                  <Trivia>
                                    <WhitespaceTrivia>    </WhitespaceTrivia>
                                  </Trivia>}<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                              </Object>
                            </Property>
                          </Sequence>
                          <CloseBraceToken>
                            <Trivia>
                              <WhitespaceTrivia>  </WhitespaceTrivia>
                            </Trivia>}<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                        </Object>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="17" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNestedObject1()
    {
        Test("""
            @"
            {
              ""description"": ""A person"",
              ""type"": ""object"",
              ""properties"":
              {
                ""name"": {""type"":""string""},
                ""hobbies"": {
                  ""type"": ""array"",
                  ""items"": {""type"":""string""}
                }
              }
            }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>
                      <Trivia>
                        <EndOfLineTrivia>
            </EndOfLineTrivia>
                      </Trivia>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>"description"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>"A person"</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>"type"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>"object"</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>"properties"</StringToken>
                        <ColonToken>:<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></ColonToken>
                        <Object>
                          <OpenBraceToken>
                            <Trivia>
                              <WhitespaceTrivia>  </WhitespaceTrivia>
                            </Trivia>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                          <Sequence>
                            <Property>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>    </WhitespaceTrivia>
                                </Trivia>"name"</StringToken>
                              <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                              <Object>
                                <OpenBraceToken>{</OpenBraceToken>
                                <Sequence>
                                  <Property>
                                    <StringToken>"type"</StringToken>
                                    <ColonToken>:</ColonToken>
                                    <Literal>
                                      <StringToken>"string"</StringToken>
                                    </Literal>
                                  </Property>
                                </Sequence>
                                <CloseBraceToken>}</CloseBraceToken>
                              </Object>
                            </Property>
                            <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                            <Property>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>    </WhitespaceTrivia>
                                </Trivia>"hobbies"</StringToken>
                              <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                              <Object>
                                <OpenBraceToken>{<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                                <Sequence>
                                  <Property>
                                    <StringToken>
                                      <Trivia>
                                        <WhitespaceTrivia>      </WhitespaceTrivia>
                                      </Trivia>"type"</StringToken>
                                    <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                                    <Literal>
                                      <StringToken>"array"</StringToken>
                                    </Literal>
                                  </Property>
                                  <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                                  <Property>
                                    <StringToken>
                                      <Trivia>
                                        <WhitespaceTrivia>      </WhitespaceTrivia>
                                      </Trivia>"items"</StringToken>
                                    <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                                    <Object>
                                      <OpenBraceToken>{</OpenBraceToken>
                                      <Sequence>
                                        <Property>
                                          <StringToken>"type"</StringToken>
                                          <ColonToken>:</ColonToken>
                                          <Literal>
                                            <StringToken>"string"</StringToken>
                                          </Literal>
                                        </Property>
                                      </Sequence>
                                      <CloseBraceToken>}<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                                    </Object>
                                  </Property>
                                </Sequence>
                                <CloseBraceToken>
                                  <Trivia>
                                    <WhitespaceTrivia>    </WhitespaceTrivia>
                                  </Trivia>}<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                              </Object>
                            </Property>
                          </Sequence>
                          <CloseBraceToken>
                            <Trivia>
                              <WhitespaceTrivia>  </WhitespaceTrivia>
                            </Trivia>}<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBraceToken>
                        </Object>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestLiterals1()
    {
        Test("""
            @"{ A: '', B: 1, C: , D: 1.23, E: 3.45, F: null }"
            """, """
            <Tree>
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
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Property>
                        <TextToken>B</TextToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>1</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Property>
                        <TextToken>C</TextToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <CommaValue>
                          <CommaToken />
                        </CommaValue>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Property>
                        <TextToken>D</TextToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>1.23</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Property>
                        <TextToken>E</TextToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>3.45</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="12" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestLiterals2()
    {
        Test("""""
            @"{ ""A"": """", ""B"": 1, ""D"": 1.23, ""E"": 3.45, ""F"": null }"
            """"", """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>"A"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>""</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>"B"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>1</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>"D"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>1.23</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>"E"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>3.45</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>"F"</StringToken>
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
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestLiterals3()
    {
        Test("""
            @"[
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
            ]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>0</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>1.1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>0.0</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>0.000000000001</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>9999999999</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>-9999999999</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>9999999999999999999999999999999999999999999999999999999999999999999999</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>-9999999999999999999999999999999999999999999999999999999999999999999999</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>'true'</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>'TRUE'</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>'false'</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>'FALSE'</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                            <SingleLineCommentTrivia>// comment!</SingleLineCommentTrivia>
                            <EndOfLineTrivia>
            </EndOfLineTrivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                            <MultiLineCommentTrivia>/* comment! */</MultiLineCommentTrivia>
                            <EndOfLineTrivia>
            </EndOfLineTrivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>''</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NullLiteralToken>
                          <Trivia>
                            <WhitespaceTrivia>  </WhitespaceTrivia>
                          </Trivia>null<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></NullLiteralToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="244" Length="1" />
            </Diagnostics>
            """, runLooseSubTreeCheck: false);
    }

    [Fact]
    public void TestCommentsInArray()
    {
        Test("""
            @"[/*hi*/1/*hi*/,2/*hi*/]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[<Trivia><MultiLineCommentTrivia>/*hi*/</MultiLineCommentTrivia></Trivia></OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1<Trivia><MultiLineCommentTrivia>/*hi*/</MultiLineCommentTrivia></Trivia></NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>2<Trivia><MultiLineCommentTrivia>/*hi*/</MultiLineCommentTrivia></Trivia></NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Comments not allowed" Start="11" Length="6" />
            </Diagnostics>
            """, runLooseSubTreeCheck: false);
    }

    [Fact]
    public void TestUnicode2()
    {
        Test("""
            @"{'text':0xabcdef12345}"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestUnicode3()
    {
        Test("""
            @"{""text"":0xabcdef12345}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>"text"</StringToken>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="20" Length="13" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestOctal1()
    {
        Test("""
            @"[0372, 0xFA, 0XFA]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>0372</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0xFA</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0XFA</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="4" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestOctal2()
    {
        Test("""
            @"[00]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>00</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestOctal3()
    {
        Test("""
            @"[0F]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>0F</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestOctal4()
    {
        Test("""
            @"[07777777]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>07777777</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="8" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestOctal5()
    {
        Test("""
            @"[0777777777777777]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>0777777777777777</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="16" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestOctal6()
    {
        Test("""
            @"[07777777777777777777777777777777]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>

            """ +
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="this is not a credential")]
            "            <NumberToken>07777777777777777777777777777777</NumberToken>" +
            """

                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="32" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="32" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestOctal7()
    {
        Test("""
            @"[07]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>07</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestOctal8()
    {
        Test("""
            @"[08]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>08</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="11" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestObjectLiteralComments()
    {
        Test("""
            @"/*comment*/ { /*comment*/
                    'Name': /*comment*/ 'Apple' /*comment*/, /*comment*/
                    'ExpiryDate': '1',
                    'Price': 3.99,
                    'Sizes': /*comment*/ [ /*comment*/
                      'Small', /*comment*/
                      'Medium' /*comment*/,
                      /*comment*/ 'Large'
                    /*comment*/ ] /*comment*/
                  } /*comment*/"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>
                      <Trivia>
                        <MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>        </WhitespaceTrivia>
                          </Trivia>'Name'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>'Apple'<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia></Trivia></StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>        </WhitespaceTrivia>
                          </Trivia>'ExpiryDate'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <StringToken>'1'</StringToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>        </WhitespaceTrivia>
                          </Trivia>'Price'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>3.99</NumberToken>
                        </Literal>
                      </Property>
                      <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                      <Property>
                        <StringToken>
                          <Trivia>
                            <WhitespaceTrivia>        </WhitespaceTrivia>
                          </Trivia>'Sizes'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Array>
                          <OpenBracketToken>[<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></OpenBracketToken>
                          <Sequence>
                            <Literal>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>          </WhitespaceTrivia>
                                </Trivia>'Small'</StringToken>
                            </Literal>
                            <CommaValue>
                              <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                            </CommaValue>
                            <Literal>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>          </WhitespaceTrivia>
                                </Trivia>'Medium'<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia></Trivia></StringToken>
                            </Literal>
                            <CommaValue>
                              <CommaToken>,<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CommaToken>
                            </CommaValue>
                            <Literal>
                              <StringToken>
                                <Trivia>
                                  <WhitespaceTrivia>          </WhitespaceTrivia>
                                  <MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia>
                                  <WhitespaceTrivia> </WhitespaceTrivia>
                                </Trivia>'Large'<Trivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></StringToken>
                            </Literal>
                          </Sequence>
                          <CloseBracketToken>
                            <Trivia>
                              <WhitespaceTrivia>        </WhitespaceTrivia>
                              <MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia>
                              <WhitespaceTrivia> </WhitespaceTrivia>
                            </Trivia>]<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia><EndOfLineTrivia>
            </EndOfLineTrivia></Trivia></CloseBracketToken>
                        </Array>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>
                      <Trivia>
                        <WhitespaceTrivia>      </WhitespaceTrivia>
                      </Trivia>}<Trivia><WhitespaceTrivia> </WhitespaceTrivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia></Trivia></CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Comments not allowed" Start="10" Length="11" />
            </Diagnostics>
            """, runLooseSubTreeCheck: false);
    }

    [Fact]
    public void TestEmptyStrings()
    {
        Test("""
            @"['','','','','','','']"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <StringToken>''</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>''</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>''</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>''</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>''</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>''</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>''</StringToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestEmptyStrings2()
    {
        Test("""""
            @"["""","""","""","""","""","""",""""]"
            """"", """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <StringToken>""</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>""</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>""</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>""</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>""</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>""</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>""</StringToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestInvalidNumber()
    {
        Test("""
            @"0-10"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0-10</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="4" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="4" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSimpleEscapes()
    {
        Test("""
            @"[false, true, true, false, 'test!', 1.11, 0e-10, 0E-10, 0.25e-5, 0.3e10, 6.0221418e23, 'Purple\r \n monkey\'s:\tdishwasher']"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <FalseLiteralToken>false</FalseLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <TrueLiteralToken>true</TrueLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <TrueLiteralToken>true</TrueLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <FalseLiteralToken>false</FalseLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>'test!'</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>1.11</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0e-10</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0E-10</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0.25e-5</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0.3e10</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>6.0221418e23</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>'Purple\r \n monkey\'s:\tdishwasher'</StringToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="37" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSimpleEscapes2()
    {
        Test("""
            @"[false, true, true, false, ""test!"", 1.11, 0e-10, 0E-10, 0.25e-5, 0.3e10, 6.0221418e23, ""Purple\r \n monkey\'s:\tdishwasher""]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <FalseLiteralToken>false</FalseLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <TrueLiteralToken>true</TrueLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <TrueLiteralToken>true</TrueLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <FalseLiteralToken>false</FalseLiteralToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>"test!"</StringToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>1.11</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0e-10</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0E-10</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0.25e-5</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>0.3e10</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>6.0221418e23</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <StringToken>"Purple\r \n monkey\'s:\tdishwasher"</StringToken>
                      </Literal>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="119" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestDoubleQuoteInSingleQuote()
    {
        Test("""
            @"'a""b'"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>'a"b'</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiLineString()
    {
        Test("""
            @"'a
            b'"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>'a
            b'</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultiLineString2()
    {
        Test(""""
            @"""a
            b"""
            """", """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>"a
            b"</StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Illegal string character" Start="13" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor1()
    {
        Test("""
            @"new"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Name expected" Start="13" Length="0" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor2()
    {
        Test("""
            @"new A"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'(' expected" Start="15" Length="0" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor3()
    {
        Test("""
            @"new A("
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="')' expected" Start="16" Length="0" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor4()
    {
        Test("""
            @"new A()"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor5()
    {
        Test("""
            @"new A(1)"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor6()
    {
        Test("""
            @"new A(1, 2)"
            """, """
            <Tree>
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
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>2</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </Constructor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor7()
    {
        Test("""
            @"new A([new B()])"
            """, """
            <Tree>
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
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor8()
    {
        Test("""
            @"new A(,)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Constructor>
                    <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
                    <TextToken>A</TextToken>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </Constructor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor9()
    {
        Test("""
            @"new A(1,)"
            """, """
            <Tree>
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
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </Constructor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor10()
    {
        Test("""
            @"new A(,1)"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Constructor>
                    <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
                    <TextToken>A</TextToken>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </Constructor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor11()
    {
        Test("""
            @"new A(1,1)"
            """, """
            <Tree>
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
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </Constructor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor12()
    {
        Test("""
            @"new A(1,,1)"
            """, """
            <Tree>
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
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                    </Sequence>
                    <CloseParenToken>)</CloseParenToken>
                  </Constructor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor13()
    {
        Test("""
            @"new %()"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid constructor name" Start="14" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestConstructor14()
    {
        Test("""
            @"new A(1 2)"
            """, """
            <Tree>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="',' expected" Start="18" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMultipleCommasInObject()
    {
        Test("""
            @"{0:0,,1:1}"
            """, """
            <Tree>
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
                      <CommaToken>,</CommaToken>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                      <CommaToken />
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="15" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSimpleEscapes3()
    {
        Test("""
            @" ""\r\n\f\t\b"" "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>"\r\n\f\t\b"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestSimpleEscapes4()
    {
        Test("""
            @" ""\m"" "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>"\m"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="13" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="13" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSimpleEscapes5()
    {
        Test("""""
            @" ""\\\/\"""" "
            """"", """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>"\\\/\""<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestSimpleEscapes6()
    {
        Test("""
            @" ""\'"" "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>"\'"<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid escape sequence" Start="13" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSimpleEscapes7()
    {
        Test("""
            @" '\'' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>'\''<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSimpleEscapes8()
    {
        Test("""
            @" '\""' "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <StringToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>'\"'<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestPropertyInArray1()
    {
        Test("""
            @" [""a"": 0] "
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>
                      <Trivia>
                        <WhitespaceTrivia> </WhitespaceTrivia>
                      </Trivia>[</OpenBracketToken>
                    <Sequence>
                      <Property>
                        <StringToken>"a"</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <NumberToken>0</NumberToken>
                        </Literal>
                      </Property>
                    </Sequence>
                    <CloseBracketToken>]<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Properties not allowed in an array" Start="17" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Properties not allowed in an array" Start="17" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSimpleNumber1()
    {
        Test("""
            @"0.0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0.0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestSimpleNumber2()
    {
        Test("""
            @"-0.0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-0.0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestSimpleNumber3()
    {
        Test("""
            @".0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>.0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestSimpleNumber4()
    {
        Test("""
            @"-.0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-.0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestStandaloneMinus()
    {
        Test("""
            @"-"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestMinusDot()
    {
        Test("""
            @"-."
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-.</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber1()
    {
        Test("""
            @"0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber2()
    {
        Test("""
            @"-0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber3()
    {
        Test("""
            @"00"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>00</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber4()
    {
        Test("""
            @"-00"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-00</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber5()
    {
        Test("""
            @"0."
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0.</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber6()
    {
        Test("""
            @"-0."
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-0.</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber7()
    {
        Test("""
            @"0e"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0e</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="2" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="2" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber8()
    {
        Test("""
            @"-0e"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-0e</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber9()
    {
        Test("""
            @"0e0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0e0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber10()
    {
        Test("""
            @"-0e0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-0e0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber11()
    {
        Test("""
            @"0e1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0e1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber12()
    {
        Test("""
            @"-0e1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-0e1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber13()
    {
        Test("""
            @"0e-1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0e-1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber14()
    {
        Test("""
            @"-0e-1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-0e-1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber15()
    {
        Test("""
            @"0e+1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0e+1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber16()
    {
        Test("""
            @"-0e+1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>-0e+1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNumber17()
    {
        Test("""
            @"--0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>--0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber18()
    {
        Test("""
            @"+0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>+0</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'+' unexpected" Start="10" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'+' unexpected" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber19()
    {
        Test("""
            @"0..0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0..0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="4" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="4" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber20()
    {
        Test("""
            @"0ee0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>0ee0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="4" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="4" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber21()
    {
        Test("""
            @"1e++1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>1e++1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber22()
    {
        Test("""
            @"1e--1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>1e--1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber23()
    {
        Test("""
            @"1e+-1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>1e+-1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber24()
    {
        Test("""
            @"1e-+1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>1e-+1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber25()
    {
        Test("""
            @"1e1.0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>1e1.0</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="5" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber26()
    {
        Test("""
            @"1e+1.1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>1e+1.1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="6" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="6" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber27()
    {
        Test("""
            @"1-1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>1-1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNumber28()
    {
        Test("""
            @"1+1"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Literal>
                    <NumberToken>1+1</NumberToken>
                  </Literal>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid number" Start="10" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIncompleteProperty()
    {
        Test("""
            "{ 'a': }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>'a'</StringToken>
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
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="16" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestPropertyWithCommaFollowedByComma()
    {
        Test("""
            "{ 'a': , , }"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <StringToken>'a'</StringToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <CommaValue>
                          <CommaToken />
                        </CommaValue>
                      </Property>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="18" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Value required" Start="15" Length="0" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestTopLevelProperty()
    {
        Test("""
            "'a': 0"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Property>
                    <StringToken>'a'</StringToken>
                    <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                    <Literal>
                      <NumberToken>0</NumberToken>
                    </Literal>
                  </Property>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="':' unexpected" Start="12" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Strings must start with &quot; not '" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestTopLevelConstructor()
    {
        Test("""
            "new Date()"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Constructor>
                    <NewKeyword>new<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NewKeyword>
                    <TextToken>Date</TextToken>
                    <OpenParenToken>(</OpenParenToken>
                    <Sequence />
                    <CloseParenToken>)</CloseParenToken>
                  </Constructor>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Constructors not allowed" Start="9" Length="3" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestTopLevelText()
    {
        Test("""
            "Date"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Text>
                    <TextToken>Date</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'D' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'D' unexpected" Start="9" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestNestedArrays1()
    {
        Test("""
            "[1, [2, [3, [4]]]]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Array>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Literal>
                            <NumberToken>2</NumberToken>
                          </Literal>
                          <CommaValue>
                            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                          </CommaValue>
                          <Array>
                            <OpenBracketToken>[</OpenBracketToken>
                            <Sequence>
                              <Literal>
                                <NumberToken>3</NumberToken>
                              </Literal>
                              <CommaValue>
                                <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                              </CommaValue>
                              <Array>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Literal>
                                    <NumberToken>4</NumberToken>
                                  </Literal>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </Array>
                            </Sequence>
                            <CloseBracketToken>]</CloseBracketToken>
                          </Array>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </Array>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            "");
    }

    [Fact]
    public void TestNestedArraysTrailingCommas1()
    {
        Test("""
            "[1, [2, [3, [4,],],],]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Array>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Literal>
                            <NumberToken>2</NumberToken>
                          </Literal>
                          <CommaValue>
                            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                          </CommaValue>
                          <Array>
                            <OpenBracketToken>[</OpenBracketToken>
                            <Sequence>
                              <Literal>
                                <NumberToken>3</NumberToken>
                              </Literal>
                              <CommaValue>
                                <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                              </CommaValue>
                              <Array>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Literal>
                                    <NumberToken>4</NumberToken>
                                  </Literal>
                                  <CommaValue>
                                    <CommaToken>,</CommaToken>
                                  </CommaValue>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </Array>
                              <CommaValue>
                                <CommaToken>,</CommaToken>
                              </CommaValue>
                            </Sequence>
                            <CloseBracketToken>]</CloseBracketToken>
                          </Array>
                          <CommaValue>
                            <CommaToken>,</CommaToken>
                          </CommaValue>
                        </Sequence>
                        <CloseBracketToken>]</CloseBracketToken>
                      </Array>
                      <CommaValue>
                        <CommaToken>,</CommaToken>
                      </CommaValue>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Trailing comma not allowed" Start="23" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestBogusNesting1()
    {
        Test("""
            "[1, [2, [3, [4}}}}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Array>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Literal>
                            <NumberToken>2</NumberToken>
                          </Literal>
                          <CommaValue>
                            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                          </CommaValue>
                          <Array>
                            <OpenBracketToken>[</OpenBracketToken>
                            <Sequence>
                              <Literal>
                                <NumberToken>3</NumberToken>
                              </Literal>
                              <CommaValue>
                                <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                              </CommaValue>
                              <Array>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Literal>
                                    <NumberToken>4</NumberToken>
                                  </Literal>
                                  <Text>
                                    <TextToken>}</TextToken>
                                  </Text>
                                  <Text>
                                    <TextToken>}</TextToken>
                                  </Text>
                                  <Text>
                                    <TextToken>}</TextToken>
                                  </Text>
                                  <Text>
                                    <TextToken>}</TextToken>
                                  </Text>
                                </Sequence>
                                <CloseBracketToken />
                              </Array>
                            </Sequence>
                            <CloseBracketToken />
                          </Array>
                        </Sequence>
                        <CloseBracketToken />
                      </Array>
                    </Sequence>
                    <CloseBracketToken />
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="23" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="23" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestBogusNesting2()
    {
        Test("""
            "[1, [2, [3, [4}]}]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Array>
                        <OpenBracketToken>[</OpenBracketToken>
                        <Sequence>
                          <Literal>
                            <NumberToken>2</NumberToken>
                          </Literal>
                          <CommaValue>
                            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                          </CommaValue>
                          <Array>
                            <OpenBracketToken>[</OpenBracketToken>
                            <Sequence>
                              <Literal>
                                <NumberToken>3</NumberToken>
                              </Literal>
                              <CommaValue>
                                <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                              </CommaValue>
                              <Array>
                                <OpenBracketToken>[</OpenBracketToken>
                                <Sequence>
                                  <Literal>
                                    <NumberToken>4</NumberToken>
                                  </Literal>
                                  <Text>
                                    <TextToken>}</TextToken>
                                  </Text>
                                </Sequence>
                                <CloseBracketToken>]</CloseBracketToken>
                              </Array>
                              <Text>
                                <TextToken>}</TextToken>
                              </Text>
                            </Sequence>
                            <CloseBracketToken>]</CloseBracketToken>
                          </Array>
                        </Sequence>
                        <CloseBracketToken />
                      </Array>
                    </Sequence>
                    <CloseBracketToken />
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="23" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="'}' unexpected" Start="23" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestBogusNesting3()
    {
        Test("""
            "{1, {2, {3, {4]]]]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      <Object>
                        <OpenBraceToken>{</OpenBraceToken>
                        <Sequence>
                          <Literal>
                            <NumberToken>2</NumberToken>
                          </Literal>
                          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                          <Object>
                            <OpenBraceToken>{</OpenBraceToken>
                            <Sequence>
                              <Literal>
                                <NumberToken>3</NumberToken>
                              </Literal>
                              <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                              <Object>
                                <OpenBraceToken>{</OpenBraceToken>
                                <Sequence>
                                  <Literal>
                                    <NumberToken>4</NumberToken>
                                  </Literal>
                                  <CommaToken />
                                  <Text>
                                    <TextToken>]</TextToken>
                                  </Text>
                                  <CommaToken />
                                  <Text>
                                    <TextToken>]</TextToken>
                                  </Text>
                                  <CommaToken />
                                  <Text>
                                    <TextToken>]</TextToken>
                                  </Text>
                                  <CommaToken />
                                  <Text>
                                    <TextToken>]</TextToken>
                                  </Text>
                                </Sequence>
                                <CloseBraceToken />
                              </Object>
                            </Sequence>
                            <CloseBraceToken />
                          </Object>
                        </Sequence>
                        <CloseBraceToken />
                      </Object>
                    </Sequence>
                    <CloseBraceToken />
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="10" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="10" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestBogusNesting4()
    {
        Test("""
            "[1, {2, [3, {4]]]]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Object>
                        <OpenBraceToken>{</OpenBraceToken>
                        <Sequence>
                          <Literal>
                            <NumberToken>2</NumberToken>
                          </Literal>
                          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                          <Array>
                            <OpenBracketToken>[</OpenBracketToken>
                            <Sequence>
                              <Literal>
                                <NumberToken>3</NumberToken>
                              </Literal>
                              <CommaValue>
                                <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                              </CommaValue>
                              <Object>
                                <OpenBraceToken>{</OpenBraceToken>
                                <Sequence>
                                  <Literal>
                                    <NumberToken>4</NumberToken>
                                  </Literal>
                                </Sequence>
                                <CloseBraceToken />
                              </Object>
                            </Sequence>
                            <CloseBracketToken>]</CloseBracketToken>
                          </Array>
                        </Sequence>
                        <CloseBraceToken />
                      </Object>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                  <Text>
                    <TextToken>]</TextToken>
                  </Text>
                  <Text>
                    <TextToken>]</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="14" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="14" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestBogusNesting5()
    {
        Test("""
            "[1, {2, [3, {4]}]}"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Object>
                        <OpenBraceToken>{</OpenBraceToken>
                        <Sequence>
                          <Literal>
                            <NumberToken>2</NumberToken>
                          </Literal>
                          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                          <Array>
                            <OpenBracketToken>[</OpenBracketToken>
                            <Sequence>
                              <Literal>
                                <NumberToken>3</NumberToken>
                              </Literal>
                              <CommaValue>
                                <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                              </CommaValue>
                              <Object>
                                <OpenBraceToken>{</OpenBraceToken>
                                <Sequence>
                                  <Literal>
                                    <NumberToken>4</NumberToken>
                                  </Literal>
                                </Sequence>
                                <CloseBraceToken />
                              </Object>
                            </Sequence>
                            <CloseBracketToken>]</CloseBracketToken>
                          </Array>
                        </Sequence>
                        <CloseBraceToken>}</CloseBraceToken>
                      </Object>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                  <Text>
                    <TextToken>}</TextToken>
                  </Text>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="14" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="14" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestBogusNesting6()
    {
        Test("""
            "[1, {2, [3, {4}]}]"
            """, """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Array>
                    <OpenBracketToken>[</OpenBracketToken>
                    <Sequence>
                      <Literal>
                        <NumberToken>1</NumberToken>
                      </Literal>
                      <CommaValue>
                        <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                      </CommaValue>
                      <Object>
                        <OpenBraceToken>{</OpenBraceToken>
                        <Sequence>
                          <Literal>
                            <NumberToken>2</NumberToken>
                          </Literal>
                          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                          <Array>
                            <OpenBracketToken>[</OpenBracketToken>
                            <Sequence>
                              <Literal>
                                <NumberToken>3</NumberToken>
                              </Literal>
                              <CommaValue>
                                <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
                              </CommaValue>
                              <Object>
                                <OpenBraceToken>{</OpenBraceToken>
                                <Sequence>
                                  <Literal>
                                    <NumberToken>4</NumberToken>
                                  </Literal>
                                </Sequence>
                                <CloseBraceToken>}</CloseBraceToken>
                              </Object>
                            </Sequence>
                            <CloseBracketToken>]</CloseBracketToken>
                          </Array>
                        </Sequence>
                        <CloseBraceToken>}</CloseBraceToken>
                      </Object>
                    </Sequence>
                    <CloseBracketToken>]</CloseBracketToken>
                  </Array>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="14" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Only properties allowed in an object" Start="14" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestIntegerPropertyName()
    {
        Test("""
            "{ 0: true }"
            """, expected: """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <TextToken>0</TextToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <TrueLiteralToken>true<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TrueLiteralToken>
                        </Literal>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            "",
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact]
    public void TestColonPropertyName()
    {
        Test("""
            "{ :: true }"
            """, expected: """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Object>
                    <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
                    <Sequence>
                      <Property>
                        <TextToken>:</TextToken>
                        <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                        <Literal>
                          <TrueLiteralToken>true<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TrueLiteralToken>
                        </Literal>
                      </Property>
                    </Sequence>
                    <CloseBraceToken>}</CloseBraceToken>
                  </Object>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid property name" Start="11" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="11" Length="1" />
            </Diagnostics>
            """);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1691963")]
    public void TestAllColons_BecomesNestedProperties()
    {
        Test("""
            "::::::::"
            """, expected: """
            <Tree>
              <CompilationUnit>
                <Sequence>
                  <Property>
                    <TextToken>:</TextToken>
                    <ColonToken>:</ColonToken>
                    <Property>
                      <TextToken>:</TextToken>
                      <ColonToken>:</ColonToken>
                      <Property>
                        <TextToken>:</TextToken>
                        <ColonToken>:</ColonToken>
                        <Property>
                          <TextToken>:</TextToken>
                          <ColonToken>:</ColonToken>
                          <CommaValue>
                            <CommaToken />
                          </CommaValue>
                        </Property>
                      </Property>
                    </Property>
                  </Property>
                </Sequence>
                <EndOfFile />
              </CompilationUnit>
            </Tree>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Invalid property name" Start="9" Length="1" />
            </Diagnostics>
            """,
            """
            <Diagnostics>
              <Diagnostic Message="Property name must be a string" Start="9" Length="1" />
            </Diagnostics>
            """);
    }
}
