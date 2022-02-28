// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// tests from: https://github.com/nst/JSONTestSuite
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.Json
{
    public partial class CSharpJsonParserNstTests : CSharpJsonParserTests
    {
        private void TestNST(
            string stringText, string expected, string _, string strictDiagnostics, [CallerMemberName] string caller = "")
        {
            var (_, tree, allChars) = JustParseTree(stringText, JsonOptions.Strict, conversionFailureOk: false);
            Assert.NotNull(tree);
            Roslyn.Utilities.Contract.ThrowIfNull(tree);
            var actualTree = TreeToText(tree!).Replace("\"", "\"\"");
            Assert.Equal(expected.Replace("\"", "\"\""), actualTree);

            var actualDiagnostics = DiagnosticsToText(tree.Diagnostics).Replace("\"", "\"\"");
            Assert.Equal(strictDiagnostics.Replace("\"", "\"\""), actualDiagnostics);

            CheckInvariants(tree, allChars);

            if (caller.StartsWith("y_"))
            {
                // y_ tests must produce no diagnostics.
                Assert.Empty(strictDiagnostics);
            }
            else if (caller.StartsWith("i_"))
            {
                // We don't want to have diagnostics for i_ tests even though we're allowed to.
                // That's because we want our parser to be permissive when possible so we don't
                // error on json that is legal under some other parser.
                Assert.Empty(strictDiagnostics);
            }
            else if (caller.StartsWith("n_"))
            {
                // n_ tests must always produce diagnostics.
                Assert.NotEmpty(strictDiagnostics);
            }
            else
            {
                Assert.False(true, "Unexpected test name.");
            }
        }

        [Fact]
        public void i_number_double_huge_neg_exp_json()
        {
            TestNST(@"@""[123.456e-789]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123.456e-789</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_number_huge_exp_json()
        {
            TestNST(@"@""[0.4e00669999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999969999999006]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0.4e00669999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999969999999006</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""135"" />
</Diagnostics>",
        @"");
        }

        [Fact]
        public void i_number_neg_int_huge_exp_json()
        {
            TestNST(@"@""[-1e+9999]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-1e+9999</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""8"" />
</Diagnostics>",
        @"");
        }

        [Fact]
        public void i_number_pos_double_huge_exp_json()
        {
            TestNST(@"@""[1.5e+9999]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1.5e+9999</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""9"" />
</Diagnostics>",
        @"");
        }

        [Fact]
        public void i_number_real_neg_overflow_json()
        {
            TestNST(@"@""[-123123e100000]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-123123e100000</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""14"" />
</Diagnostics>",
        @"");
        }

        [Fact]
        public void i_number_real_pos_overflow_json()
        {
            TestNST(@"@""[123123e100000]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123123e100000</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""13"" />
</Diagnostics>",
        @"");
        }

        [Fact]
        public void i_number_real_underflow_json()
        {
            TestNST(@"@""[123e-10000000]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123e-10000000</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_number_too_big_neg_int_json()
        {
            TestNST(@"@""[-123123123123123123123123123123]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-123123123123123123123123123123</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_number_too_big_pos_int_json()
        {
            TestNST(@"@""[100000000000000000000]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>100000000000000000000</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_number_very_big_negative_int_json()
        {
            TestNST(@"@""[-237462374673276894279832749832423479823246327846]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-237462374673276894279832749832423479823246327846</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_object_key_lone_2nd_surrogate_json()
        {
            TestNST(@"@""{""""\uDFAA"""":0}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""\uDFAA""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>0</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_1st_surrogate_but_2nd_missing_json()
        {
            TestNST(@"@""[""""\uDADA""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uDADA""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_1st_valid_surrogate_2nd_invalid_json()
        {
            TestNST(@"@""[""""\uD888\u1234""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD888\u1234""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_incomplete_surrogates_escape_valid_json()
        {
            TestNST(@"@""[""""\uD800\uD800\n""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD800\uD800\n""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_incomplete_surrogate_and_escape_valid_json()
        {
            TestNST(@"@""[""""\uD800\n""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD800\n""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_incomplete_surrogate_pair_json()
        {
            TestNST(@"@""[""""\uDd1ea""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uDd1ea""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_invalid_lonely_surrogate_json()
        {
            TestNST(@"@""[""""\ud800""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\ud800""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_invalid_surrogate_json()
        {
            TestNST(@"@""[""""\ud800abc""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\ud800abc""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_invalid_utf_8_json()
        {
            TestNST(@"@""[""""�""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""�""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_inverted_surrogates_U_1D11E_json()
        {
            TestNST(@"@""[""""\uDd1e\uD834""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uDd1e\uD834""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_iso_latin_1_json()
        {
            TestNST(@"@""[""""�""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""�""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_lone_second_surrogate_json()
        {
            TestNST(@"@""[""""\uDFAA""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uDFAA""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_lone_utf8_continuation_byte_json()
        {
            TestNST(@"@""[""""�""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""�""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_not_in_unicode_range_json()
        {
            TestNST(@"@""[""""���""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""���""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_overlong_sequence_2_bytes_json()
        {
            TestNST(@"@""[""""��""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""��""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_overlong_sequence_6_bytes_json()
        {
            TestNST(@"@""[""""������""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""������""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_overlong_sequence_6_bytes_null_json()
        {
            TestNST(@"@""[""""������""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""������""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_truncated_utf_8_json()
        {
            TestNST(@"@""[""""��""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""��""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_UTF_16LE_with_BOM_json()
        {
            TestNST(@"@""[""""é""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""é""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_UTF_8_invalid_sequence_json()
        {
            TestNST(@"@""[""""日ш�""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""日ш�""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_string_UTF8_surrogate_U_D800_json()
        {
            TestNST(@"@""[""""��""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""��""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void i_structure_UTF_8_BOM_empty_object_json()
        {
            TestNST(@"@""{}""", @"<Tree>
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
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void n_array_1_true_without_comma_json()
        {
            TestNST(@"@""[1 true]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
          </Literal>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""13"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""13"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_a_invalid_utf8_json()
        {
            TestNST(@"@""[a�]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a�</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'a' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'a' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_colon_instead_of_comma_json()
        {
            TestNST(@"@""["""""""": 1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Property>
            <StringToken>""""</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>1</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Properties not allowed in an array"" Start=""15"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Properties not allowed in an array"" Start=""15"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_comma_after_close_json()
        {
            TestNST(@"@""[""""""""],""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""""</StringToken>
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
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""16"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""16"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_comma_and_number_json()
        {
            TestNST(@"@""[,1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
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
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_double_comma_json()
        {
            TestNST(@"@""[1,,2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
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
            <NumberToken>2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""13"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_double_extra_comma_json()
        {
            TestNST(@"@""[""""x"""",,]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""x""</StringToken>
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
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""17"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_extra_close_json()
        {
            TestNST(@"@""[""""x""""]]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""x""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
      <Text>
        <TextToken>]</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""17"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""17"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_extra_comma_json()
        {
            TestNST(@"@""["""""""",]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""""</StringToken>
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
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""15"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_incomplete_json()
        {
            TestNST(@"@""[""""x""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""x""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""16"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""16"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_incomplete_invalid_value_json()
        {
            TestNST(@"@""[x""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>x</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'x' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'x' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_inner_array_no_comma_json()
        {
            TestNST(@"@""[3[4]]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>3</NumberToken>
          </Literal>
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
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""12"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""12"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_invalid_utf8_json()
        {
            TestNST(@"@""[�]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>�</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'�' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'�' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_items_separated_by_semicolon_json()
        {
            TestNST(@"@""[1:2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Property>
            <TextToken>1</TextToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>2</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be a string"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be a string"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_just_comma_json()
        {
            TestNST(@"@""[,]""", @"<Tree>
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
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_just_minus_json()
        {
            TestNST(@"@""[-]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_missing_value_json()
        {
            TestNST(@"@""[   , """"""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><WhitespaceTrivia>   </WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <CommaValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </CommaValue>
          <Literal>
            <StringToken>""""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""14"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_newlines_unclosed_json()
        {
            TestNST(@"@""[""""a"""",
4
,1,""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""a""</StringToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CommaToken>
          </CommaValue>
          <Literal>
            <NumberToken>4<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></NumberToken>
          </Literal>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""24"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""24"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_number_and_comma_json()
        {
            TestNST(@"@""[1,]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
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
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""12"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_number_and_several_commas_json()
        {
            TestNST(@"@""[1,,]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
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
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""13"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_star_inside_json()
        {
            TestNST(@"@""[*]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>*</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'*' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'*' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_unclosed_json()
        {
            TestNST(@"@""[""""""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""15"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""15"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_unclosed_trailing_comma_json()
        {
            TestNST(@"@""[1,""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""12"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""12"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_unclosed_with_new_lines_json()
        {
            TestNST(@"@""[1,
1
,1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CommaToken>
          </CommaValue>
          <Literal>
            <NumberToken>1<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></NumberToken>
          </Literal>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""20"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""20"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_array_unclosed_with_object_inside_json()
        {
            TestNST(@"@""[{}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Object>
            <OpenBraceToken>{</OpenBraceToken>
            <Sequence />
            <CloseBraceToken>}</CloseBraceToken>
          </Object>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""13"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""13"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_incomplete_false_json()
        {
            TestNST(@"@""[fals]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>fals</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'f' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'f' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_incomplete_null_json()
        {
            TestNST(@"@""[nul]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>nul</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'n' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'n' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_incomplete_true_json()
        {
            TestNST(@"@""[tru]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>tru</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'t' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'t' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number____json()
        {
            TestNST(@"@""[++1234]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>++1234</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'+' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'+' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number__1_json()
        {
            TestNST(@"@""[+1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>+1</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'+' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'+' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number__Inf_json()
        {
            TestNST(@"@""[+Inf]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>+Inf</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'+' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'+' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number__01_json()
        {
            TestNST(@"@""[-01]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-01</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number__1_0__json()
        {
            TestNST(@"@""[-1.0.]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-1.0.</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number__2__json()
        {
            TestNST(@"@""[-2.]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-2.</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number__NaN_json()
        {
            TestNST(@"@""[-NaN]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-NaN</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number___1_json()
        {
            TestNST(@"@""[.-1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>.-1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number__2e_3_json()
        {
            TestNST(@"@""[.2e-3]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>.2e-3</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_0_1_2_json()
        {
            TestNST(@"@""[0.1.2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0.1.2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_0_3e__json()
        {
            TestNST(@"@""[0.3e+]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0.3e+</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_0_3e_json()
        {
            TestNST(@"@""[0.3e]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0.3e</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_0_e1_json()
        {
            TestNST(@"@""[0.e1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0.e1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_0e__json()
        {
            TestNST(@"@""[0e+]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0e+</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_0e_json()
        {
            TestNST(@"@""[0e]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0e</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_0_capital_E__json()
        {
            TestNST(@"@""[0E+]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0E+</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_0_capital_E_json()
        {
            TestNST(@"@""[0E]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0E</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_1_0e__json()
        {
            TestNST(@"@""[1.0e+]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1.0e+</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_1_0e_json()
        {
            TestNST(@"@""[1.0e]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1.0e</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_1eE2_json()
        {
            TestNST(@"@""[1eE2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1eE2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_1_000_json()
        {
            TestNST(@"@""[1 000.0]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
          </Literal>
          <Literal>
            <NumberToken>000.0</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""13"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""13"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_2_e_3_json()
        {
            TestNST(@"@""[2.e+3]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>2.e+3</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_2_e3_json()
        {
            TestNST(@"@""[2.e3]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>2.e3</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_9_e__json()
        {
            TestNST(@"@""[9.e+]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>9.e+</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_expression_json()
        {
            TestNST(@"@""[1+2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1+2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_hex_1_digit_json()
        {
            TestNST(@"@""[0x1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0x1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_hex_2_digits_json()
        {
            TestNST(@"@""[0x42]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0x42</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_Inf_json()
        {
            TestNST(@"@""[Inf]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>Inf</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'I' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'I' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_infinity_json()
        {
            TestNST(@"@""[Infinity]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <InfinityLiteralToken>Infinity</InfinityLiteralToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""'Infinity' literal not allowed"" Start=""11"" Length=""8"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_invalid___json()
        {
            TestNST(@"@""[0e+-1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0e+-1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_invalid_negative_real_json()
        {
            TestNST(@"@""[-123.123foo]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-123.123foo</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""11"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""11"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_invalid_utf_8_in_bigger_int_json()
        {
            TestNST(@"@""[123�]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123�</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_invalid_utf_8_in_exponent_json()
        {
            TestNST(@"@""[1e1�]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1e1�</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_invalid_utf_8_in_int_json()
        {
            TestNST(@"@""[0�]
""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0�</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_minus_infinity_json()
        {
            TestNST(@"@""[-Infinity]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <NegativeLiteral>
            <MinusToken>-</MinusToken>
            <InfinityLiteralToken>Infinity</InfinityLiteralToken>
          </NegativeLiteral>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""'-Infinity' literal not allowed"" Start=""11"" Length=""9"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_minus_sign_with_trailing_garbage_json()
        {
            TestNST(@"@""[-foo]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-foo</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_minus_space_1_json()
        {
            TestNST(@"@""[- 1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
          </Literal>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""13"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_NaN_json()
        {
            TestNST(@"@""[NaN]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NaNLiteralToken>NaN</NaNLiteralToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""'NaN' literal not allowed"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_neg_int_starting_with_zero_json()
        {
            TestNST(@"@""[-012]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-012</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_neg_real_without_int_part_json()
        {
            TestNST(@"@""[-.123]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-.123</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_neg_with_garbage_at_end_json()
        {
            TestNST(@"@""[-1x]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-1x</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_real_garbage_after_e_json()
        {
            TestNST(@"@""[1ea]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1ea</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_real_without_fractional_part_json()
        {
            TestNST(@"@""[1.]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1.</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_real_with_invalid_utf8_after_e_json()
        {
            TestNST(@"@""[1e�]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1e�</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_starting_with_dot_json()
        {
            TestNST(@"@""[.123]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>.123</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_U_FF11_fullwidth_digit_one_json()
        {
            TestNST(@"@""[１]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>１</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'１' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'１' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_with_alpha_json()
        {
            TestNST(@"@""[1.2a-3]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1.2a-3</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""6"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""6"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_with_alpha_char_json()
        {
            TestNST(@"@""[1.8011670033376514H-308]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1.8011670033376514H-308</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""23"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""23"" />
</Diagnostics>");
        }

        [Fact]
        public void n_number_with_leading_zero_json()
        {
            TestNST(@"@""[012]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>012</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_bad_value_json()
        {
            TestNST(@"@""[""""x"""", truth]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""x""</StringToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </CommaValue>
          <Text>
            <TextToken>truth</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'t' unexpected"" Start=""18"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'t' unexpected"" Start=""18"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_bracket_key_json()
        {
            TestNST(@"@""{[: """"x""""}
""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Array>
            <OpenBracketToken>[</OpenBracketToken>
            <Sequence>
              <Text>
                <TextToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></TextToken>
              </Text>
              <Literal>
                <StringToken>""x""</StringToken>
              </Literal>
            </Sequence>
            <CloseBracketToken />
          </Array>
        </Sequence>
        <CloseBraceToken>}<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_comma_instead_of_colon_json()
        {
            TestNST(@"@""{""""x"""", null}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Literal>
            <StringToken>""x""</StringToken>
          </Literal>
          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          <Literal>
            <NullLiteralToken>null</NullLiteralToken>
          </Literal>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_double_colon_json()
        {
            TestNST(@"@""{""""x""""::""""b""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""x""</StringToken>
            <ColonToken>:</ColonToken>
            <Text>
              <TextToken>:</TextToken>
            </Text>
          </Property>
          <CommaToken />
          <Literal>
            <StringToken>""b""</StringToken>
          </Literal>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""':' unexpected"" Start=""17"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""':' unexpected"" Start=""17"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_garbage_at_end_json()
        {
            TestNST(@"@""{""""a"""":""""a"""" 123}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""a""<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
            </Literal>
          </Property>
          <CommaToken />
          <Literal>
            <NumberToken>123</NumberToken>
          </Literal>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""23"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""',' expected"" Start=""23"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_key_with_single_quotes_json()
        {
            TestNST(@"@""{key: 'value'}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>key</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>'value'</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be a string"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_lone_continuation_byte_in_key_and_trailing_comma_json()
        {
            TestNST(@"@""{""""�"""":""""0"""",}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""�""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""0""</StringToken>
            </Literal>
          </Property>
          <CommaToken>,</CommaToken>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""22"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_missing_colon_json()
        {
            TestNST(@"@""{""""a"""" b}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Literal>
            <StringToken>""a""<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
          </Literal>
          <CommaToken />
          <Text>
            <TextToken>b</TextToken>
          </Text>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_missing_key_json()
        {
            TestNST(@"@""{:""""b""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Text>
            <TextToken>:</TextToken>
          </Text>
          <CommaToken />
          <Literal>
            <StringToken>""b""</StringToken>
          </Literal>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""':' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""':' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_missing_semicolon_json()
        {
            TestNST(@"@""{""""a"""" """"b""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Literal>
            <StringToken>""a""<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
          </Literal>
          <CommaToken />
          <Literal>
            <StringToken>""b""</StringToken>
          </Literal>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_missing_value_json()
        {
            TestNST(@"@""{""""a"""":""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
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
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Missing property value"" Start=""17"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Missing property value"" Start=""17"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_no_colon_json()
        {
            TestNST(@"@""{""""a""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Literal>
            <StringToken>""a""</StringToken>
          </Literal>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_non_string_key_json()
        {
            TestNST(@"@""{1:1}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
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
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be a string"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_non_string_key_but_huge_number_instead_json()
        {
            TestNST(@"@""{9999E9999:1}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>9999E9999</TextToken>
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
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be a string"" Start=""11"" Length=""9"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_repeated_null_null_json()
        {
            TestNST(@"@""{null:null,null:null}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>null</TextToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NullLiteralToken>null</NullLiteralToken>
            </Literal>
          </Property>
          <CommaToken>,</CommaToken>
          <Property>
            <TextToken>null</TextToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NullLiteralToken>null</NullLiteralToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be a string"" Start=""11"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_several_trailing_commas_json()
        {
            TestNST(@"@""{""""id"""":0,,,,,}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""id""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>0</NumberToken>
            </Literal>
          </Property>
          <CommaToken>,</CommaToken>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
          <CommaToken>,</CommaToken>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
          <CommaToken>,</CommaToken>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""20"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""20"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_single_quote_json()
        {
            TestNST(@"@""{'a':0}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>'a'</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>0</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Strings must start with &quot; not '"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_trailing_comma_json()
        {
            TestNST(@"@""{""""id"""":0,}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""id""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>0</NumberToken>
            </Literal>
          </Property>
          <CommaToken>,</CommaToken>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""19"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_trailing_comment_json()
        {
            TestNST(@"@""{""""a"""":""""b""""}/**/""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}<Trivia><MultiLineCommentTrivia>/**/</MultiLineCommentTrivia></Trivia></CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Comments not allowed"" Start=""23"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_trailing_comment_open_json()
        {
            TestNST(@"@""{""""a"""":""""b""""}/**//""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}<Trivia><MultiLineCommentTrivia>/**/</MultiLineCommentTrivia><SingleLineCommentTrivia>/</SingleLineCommentTrivia></Trivia></CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Error parsing comment"" Start=""27"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Comments not allowed"" Start=""23"" Length=""4"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_trailing_comment_slash_open_json()
        {
            TestNST(@"@""{""""a"""":""""b""""}//""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}<Trivia><SingleLineCommentTrivia>//</SingleLineCommentTrivia></Trivia></CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated comment"" Start=""23"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated comment"" Start=""23"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_trailing_comment_slash_open_incomplete_json()
        {
            TestNST(@"@""{""""a"""":""""b""""}/""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}<Trivia><SingleLineCommentTrivia>/</SingleLineCommentTrivia></Trivia></CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Error parsing comment"" Start=""23"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Error parsing comment"" Start=""23"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_two_commas_in_a_row_json()
        {
            TestNST(@"@""{""""a"""":""""b"""",,""""c"""":""""d""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
          <CommaToken>,</CommaToken>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
          <CommaToken />
          <Property>
            <StringToken>""c""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""d""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""23"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""23"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_unquoted_key_json()
        {
            TestNST(@"@""{a: """"b""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <TextToken>a</TextToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be a string"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_unterminated_value_json()
        {
            TestNST(@"@""{""""a"""":""""a""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""a</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""17"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""17"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_with_single_string_json()
        {
            TestNST(@"@""{ """"foo"""" : """"bar"""", """"a"""" }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""foo""<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>""bar""</StringToken>
            </Literal>
          </Property>
          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          <Literal>
            <StringToken>""a""<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
          </Literal>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""31"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""31"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_object_with_trailing_garbage_json()
        {
            TestNST(@"@""{""""a"""":""""b""""}#""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
      <Text>
        <TextToken>#</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'#' unexpected"" Start=""23"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'#' unexpected"" Start=""23"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_single_space_json()
        {
            TestNST(@"@"" """, @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Syntax error"" Start=""10"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Syntax error"" Start=""10"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_1_surrogate_then_escape_json()
        {
            TestNST(@"@""[""""\uD800\""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD800\""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""12"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""12"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_1_surrogate_then_escape_u_json()
        {
            TestNST(@"@""[""""\uD800\u""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD800\u""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""19"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""19"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_1_surrogate_then_escape_u1_json()
        {
            TestNST(@"@""[""""\uD800\u1""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD800\u1""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""19"" Length=""6"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""19"" Length=""6"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_1_surrogate_then_escape_u1x_json()
        {
            TestNST(@"@""[""""\uD800\u1x""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD800\u1x""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""19"" Length=""7"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""19"" Length=""7"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_accentuated_char_no_quotes_json()
        {
            TestNST(@"@""[é]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>é</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'é' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'é' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_escaped_backslash_bad_json()
        {
            TestNST(@"@""[""""\\\""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\\\""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""8"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""8"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_escaped_ctrl_char_tab_json()
        {
            TestNST(@"@""[""""\	""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\	""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_escaped_emoji_json()
        {
            TestNST(@"@""[""""\🌀""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\🌀""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_escape_x_json()
        {
            TestNST(@"@""[""""\x00""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\x00""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_incomplete_escape_json()
        {
            TestNST(@"@""[""""\""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""6"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""6"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_incomplete_escaped_character_json()
        {
            TestNST(@"@""[""""\u00A""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u00A""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""7"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""7"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_incomplete_surrogate_json()
        {
            TestNST(@"@""[""""\uD834\uDd""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD834\uDd""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""19"" Length=""7"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""19"" Length=""7"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_incomplete_surrogate_escape_invalid_json()
        {
            TestNST(@"@""[""""\uD800\uD800\x""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD800\uD800\x""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""25"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""25"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_invalid_utf_8_in_escape_json()
        {
            TestNST(@"@""[""""\u�""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u�""]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""6"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""6"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_invalid_backslash_esc_json()
        {
            TestNST(@"@""[""""\a""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\a""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_invalid_unicode_escape_json()
        {
            TestNST(@"@""[""""\uqqqq""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uqqqq""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""6"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""6"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_invalid_utf8_after_escape_json()
        {
            TestNST(@"@""[""""\�""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\�""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_leading_uescaped_thinspace_json()
        {
            TestNST(@"@""[\u0020""""asd""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>\u0020</TextToken>
          </Text>
          <Literal>
            <StringToken>""asd""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'\' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'\' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_no_quotes_with_bad_escape_json()
        {
            TestNST(@"@""[\n]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>\n</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'\' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'\' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_single_doublequote_json()
        {
            TestNST(@"@""""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>""</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""10"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""10"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_single_quote_json()
        {
            TestNST(@"@""['single quote']""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>'single quote'</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Strings must start with &quot; not '"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_single_string_no_double_quotes_json()
        {
            TestNST(@"@""abc""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>abc</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'a' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'a' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_start_escape_unclosed_json()
        {
            TestNST(@"@""[""""\""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_unescaped_newline_json()
        {
            TestNST(@"@""[""""new
line""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""new
line""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Illegal string character"" Start=""16"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_unescaped_tab_json()
        {
            TestNST(@"@""[""""	""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""	""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Illegal string character"" Start=""13"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_unicode_CapitalU_json()
        {
            TestNST(@"@""""""\UA66D""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>""\UA66D""</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""12"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""12"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_string_with_trailing_garbage_json()
        {
            TestNST(@"@""""""""""x""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>""""</StringToken>
      </Literal>
      <Text>
        <TextToken>x</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'x' unexpected"" Start=""14"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'x' unexpected"" Start=""14"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_angle_bracket___json()
        {
            TestNST(@"@""<.>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>&lt;.&gt;</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'&lt;' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'&lt;' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_angle_bracket_null_json()
        {
            TestNST(@"@""[<null>]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>&lt;null&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'&lt;' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'&lt;' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_array_trailing_garbage_json()
        {
            TestNST(@"@""[1]x""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
      <Text>
        <TextToken>x</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'x' unexpected"" Start=""13"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'x' unexpected"" Start=""13"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_array_with_extra_array_close_json()
        {
            TestNST(@"@""[1]]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
      <Text>
        <TextToken>]</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""13"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""13"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_array_with_unclosed_string_json()
        {
            TestNST(@"@""[""""asd]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""asd]</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""6"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""6"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_ascii_unicode_identifier_json()
        {
            TestNST(@"@""aå""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>aå</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'a' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'a' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_capitalized_True_json()
        {
            TestNST(@"@""[True]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>True</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'T' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'T' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_close_unopened_array_json()
        {
            TestNST(@"@""1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NumberToken>1</NumberToken>
      </Literal>
      <Text>
        <TextToken>]</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_comma_instead_of_closing_brace_json()
        {
            TestNST(@"@""{""""x"""": true,""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""x""</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <TrueLiteralToken>true</TrueLiteralToken>
            </Literal>
          </Property>
          <CommaToken>,</CommaToken>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""22"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Trailing comma not allowed"" Start=""22"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_double_array_json()
        {
            TestNST(@"@""[][]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence />
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence />
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'[' unexpected"" Start=""12"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'[' unexpected"" Start=""12"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_end_array_json()
        {
            TestNST(@"@""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>]</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_incomplete_UTF8_BOM_json()
        {
            TestNST(@"@""�{}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>�</TextToken>
      </Text>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence />
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'�' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'�' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_lone_open_bracket_json()
        {
            TestNST(@"@""[""", @"<Tree>
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
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""11"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""11"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_number_with_trailing_garbage_json()
        {
            TestNST(@"@""2@""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NumberToken>2@</NumberToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""10"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid number"" Start=""10"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_object_followed_by_closing_object_json()
        {
            TestNST(@"@""{}}""", @"<Tree>
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
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'}' unexpected"" Start=""12"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'}' unexpected"" Start=""12"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_object_unclosed_no_value_json()
        {
            TestNST(@"@""{"""""""":""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""""</StringToken>
            <ColonToken>:</ColonToken>
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
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Missing property value"" Start=""16"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Missing property value"" Start=""16"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_object_with_comment_json()
        {
            TestNST(@"@""{""""a"""":/*comment*/""""b""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:<Trivia><MultiLineCommentTrivia>/*comment*/</MultiLineCommentTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Comments not allowed"" Start=""17"" Length=""11"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_object_with_trailing_garbage_json()
        {
            TestNST(@"@""{""""a"""": true} """"x""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <TrueLiteralToken>true</TrueLiteralToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CloseBraceToken>
      </Object>
      <Literal>
        <StringToken>""x""</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'&quot;' unexpected"" Start=""24"" Length=""5"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'&quot;' unexpected"" Start=""24"" Length=""5"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_array_apostrophe_json()
        {
            TestNST(@"@""['""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>'</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_array_comma_json()
        {
            TestNST(@"@""[,""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""',' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_array_open_object_json()
        {
            TestNST(@"@""[{""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Object>
            <OpenBraceToken>{</OpenBraceToken>
            <Sequence />
            <CloseBraceToken />
          </Object>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'}' expected"" Start=""12"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'}' expected"" Start=""12"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_array_open_string_json()
        {
            TestNST(@"@""[""""a""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""a</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_array_string_json()
        {
            TestNST(@"@""[""""a""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""a""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""16"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""16"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_object_json()
        {
            TestNST(@"@""{""", @"<Tree>
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
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'}' expected"" Start=""11"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'}' expected"" Start=""11"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_object_close_array_json()
        {
            TestNST(@"@""{]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Text>
            <TextToken>]</TextToken>
          </Text>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_object_comma_json()
        {
            TestNST(@"@""{,""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <CommaValue>
            <CommaToken>,</CommaToken>
          </CommaValue>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_object_open_array_json()
        {
            TestNST(@"@""{[""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Array>
            <OpenBracketToken>[</OpenBracketToken>
            <Sequence />
            <CloseBracketToken />
          </Array>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Only properties allowed in an object"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_object_open_string_json()
        {
            TestNST(@"@""{""""a""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Literal>
            <StringToken>""a</StringToken>
          </Literal>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Unterminated string"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_object_string_with_apostrophes_json()
        {
            TestNST(@"@""{'a'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Literal>
            <StringToken>'a'</StringToken>
          </Literal>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""3"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Property name must be followed by a ':'"" Start=""11"" Length=""3"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_open_open_json()
        {
            TestNST(@"@""[""""\{[""""\{[""""\{[""""\{""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\{[""</StringToken>
          </Literal>
          <Text>
            <TextToken>\</TextToken>
          </Text>
          <Object>
            <OpenBraceToken>{</OpenBraceToken>
            <Sequence>
              <Array>
                <OpenBracketToken>[</OpenBracketToken>
                <Sequence>
                  <Literal>
                    <StringToken>""\{[""</StringToken>
                  </Literal>
                  <Text>
                    <TextToken>\</TextToken>
                  </Text>
                  <Object>
                    <OpenBraceToken>{</OpenBraceToken>
                    <Sequence />
                    <CloseBraceToken />
                  </Object>
                </Sequence>
                <CloseBracketToken />
              </Array>
            </Sequence>
            <CloseBraceToken />
          </Object>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""Invalid escape sequence"" Start=""13"" Length=""2"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_single_star_json()
        {
            TestNST(@"@""*""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>*</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'*' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'*' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_trailing___json()
        {
            TestNST(@"@""{""""a"""":""""b""""}#{}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
      <Text>
        <TextToken>#</TextToken>
      </Text>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence />
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'#' unexpected"" Start=""23"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'#' unexpected"" Start=""23"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_U_2060_word_joined_json()
        {
            TestNST(@"@""[⁠]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>⁠</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'⁠' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'⁠' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_uescaped_LF_before_string_json()
        {
            TestNST(@"@""[\u000A""""""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>\u000A</TextToken>
          </Text>
          <Literal>
            <StringToken>""""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'\' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'\' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_unclosed_array_json()
        {
            TestNST(@"@""[1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""12"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""']' expected"" Start=""12"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_unclosed_array_partial_null_json()
        {
            TestNST(@"@""[ false, nul""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <FalseLiteralToken>false</FalseLiteralToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </CommaValue>
          <Text>
            <TextToken>nul</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'n' unexpected"" Start=""19"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'n' unexpected"" Start=""19"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_unclosed_array_unfinished_false_json()
        {
            TestNST(@"@""[ true, fals""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <TrueLiteralToken>true</TrueLiteralToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </CommaValue>
          <Text>
            <TextToken>fals</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'f' unexpected"" Start=""18"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'f' unexpected"" Start=""18"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_unclosed_array_unfinished_true_json()
        {
            TestNST(@"@""[ false, tru""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <FalseLiteralToken>false</FalseLiteralToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </CommaValue>
          <Text>
            <TextToken>tru</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'t' unexpected"" Start=""19"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'t' unexpected"" Start=""19"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_unclosed_object_json()
        {
            TestNST(@"@""{""""asd"""":""""asd""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""asd""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""asd""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken />
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'}' expected"" Start=""26"" Length=""0"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'}' expected"" Start=""26"" Length=""0"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_unicode_identifier_json()
        {
            TestNST(@"@""å""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>å</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'å' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'å' unexpected"" Start=""10"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_whitespace_formfeed_json()
        {
            TestNST(@"@""[]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><WhitespaceTrivia>\f</WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence />
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"<Diagnostics>
  <Diagnostic Message=""Illegal whitespace character"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void n_structure_whitespace_U_2060_word_joiner_json()
        {
            TestNST(@"@""[⁠]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>⁠</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"<Diagnostics>
  <Diagnostic Message=""'⁠' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>",
        @"<Diagnostics>
  <Diagnostic Message=""'⁠' unexpected"" Start=""11"" Length=""1"" />
</Diagnostics>");
        }

        [Fact]
        public void y_array_arraysWithSpaces_json()
        {
            TestNST(@"@""[[]   ]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Array>
            <OpenBracketToken>[</OpenBracketToken>
            <Sequence />
            <CloseBracketToken>]<Trivia><WhitespaceTrivia>   </WhitespaceTrivia></Trivia></CloseBracketToken>
          </Array>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_empty_string_json()
        {
            TestNST(@"@""[""""""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_empty_json()
        {
            TestNST(@"@""[]""", @"<Tree>
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
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_ending_with_newline_json()
        {
            TestNST(@"@""[""""a""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""a""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_false_json()
        {
            TestNST(@"@""[false]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <FalseLiteralToken>false</FalseLiteralToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_heterogeneous_json()
        {
            TestNST(@"@""[null, 1, """"1"""", {}]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NullLiteralToken>null</NullLiteralToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </CommaValue>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </CommaValue>
          <Literal>
            <StringToken>""1""</StringToken>
          </Literal>
          <CommaValue>
            <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          </CommaValue>
          <Object>
            <OpenBraceToken>{</OpenBraceToken>
            <Sequence />
            <CloseBraceToken>}</CloseBraceToken>
          </Object>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_null_json()
        {
            TestNST(@"@""[null]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NullLiteralToken>null</NullLiteralToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_with_1_and_newline_json()
        {
            TestNST(@"@""[1
]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_with_leading_space_json()
        {
            TestNST(@"@"" [1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_with_several_null_json()
        {
            TestNST(@"@""[1,null,null,null,2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
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
          <Literal>
            <NullLiteralToken>null</NullLiteralToken>
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
          <Literal>
            <NumberToken>2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_array_with_trailing_space_json()
        {
            TestNST(@"@""[2] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_json()
        {
            TestNST(@"@""[123e65]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123e65</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_0e_1_json()
        {
            TestNST(@"@""[0e+1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0e+1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_0e1_json()
        {
            TestNST(@"@""[0e1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>0e1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_after_space_json()
        {
            TestNST(@"@""[ 4]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>4</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_double_close_to_zero_json()
        {
            TestNST(@"@""[-0.000000000000000000000000000000000000000000000000000000000000000000000000000001]
""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-0.000000000000000000000000000000000000000000000000000000000000000000000000000001</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_int_with_exp_json()
        {
            TestNST(@"@""[20e1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>20e1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_minus_zero_json()
        {
            TestNST(@"@""[-0]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-0</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_negative_int_json()
        {
            TestNST(@"@""[-123]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-123</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_negative_one_json()
        {
            TestNST(@"@""[-1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-1</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_negative_zero_json()
        {
            TestNST(@"@""[-0]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>-0</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_real_capital_e_json()
        {
            TestNST(@"@""[1E22]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1E22</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_real_capital_e_neg_exp_json()
        {
            TestNST(@"@""[1E-2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1E-2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_real_capital_e_pos_exp_json()
        {
            TestNST(@"@""[1E+2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1E+2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_real_exponent_json()
        {
            TestNST(@"@""[123e45]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123e45</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_real_fraction_exponent_json()
        {
            TestNST(@"@""[123.456e78]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123.456e78</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_real_neg_exp_json()
        {
            TestNST(@"@""[1e-2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1e-2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_real_pos_exponent_json()
        {
            TestNST(@"@""[1e+2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>1e+2</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_simple_int_json()
        {
            TestNST(@"@""[123]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_number_simple_real_json()
        {
            TestNST(@"@""[123.456789]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <NumberToken>123.456789</NumberToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_json()
        {
            TestNST(@"@""{""""asd"""":""""sdf"""", """"dfg"""":""""fgh""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""asd""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""sdf""</StringToken>
            </Literal>
          </Property>
          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          <Property>
            <StringToken>""dfg""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""fgh""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_basic_json()
        {
            TestNST(@"@""{""""asd"""":""""sdf""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""asd""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""sdf""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_duplicated_key_json()
        {
            TestNST(@"@""{""""a"""":""""b"""",""""a"""":""""c""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
          <CommaToken>,</CommaToken>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""c""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_duplicated_key_and_value_json()
        {
            TestNST(@"@""{""""a"""":""""b"""",""""a"""":""""b""""}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
          <CommaToken>,</CommaToken>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""b""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_empty_json()
        {
            TestNST(@"@""{}""", @"<Tree>
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
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_empty_key_json()
        {
            TestNST(@"@""{"""""""":0}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <NumberToken>0</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_escaped_null_in_key_json()
        {
            TestNST(@"@""{""""foo\u0000bar"""": 42}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""foo\u0000bar""</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>42</NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_extreme_numbers_json()
        {
            TestNST(@"@""{ """"min"""": -1.0e+28, """"max"""": 1.0e+28 }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""min""</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>-1.0e+28</NumberToken>
            </Literal>
          </Property>
          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          <Property>
            <StringToken>""max""</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <NumberToken>1.0e+28<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></NumberToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_long_strings_json()
        {
            TestNST(@"@""{""""x"""":[{""""id"""": """"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx""""}], """"id"""": """"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx""""}""",
                @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""x""</StringToken>
            <ColonToken>:</ColonToken>
            <Array>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Object>
                  <OpenBraceToken>{</OpenBraceToken>
                  <Sequence>
                    <Property>
                      <StringToken>""id""</StringToken>
                      <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
                      <Literal>
                        <StringToken>""xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx""</StringToken>
                      </Literal>
                    </Property>
                  </Sequence>
                  <CloseBraceToken>}</CloseBraceToken>
                </Object>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </Array>
          </Property>
          <CommaToken>,<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CommaToken>
          <Property>
            <StringToken>""id""</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>""xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx""</StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_simple_json()
        {
            TestNST(@"@""{""""a"""":[]}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:</ColonToken>
            <Array>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence />
              <CloseBracketToken>]</CloseBracketToken>
            </Array>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_string_unicode_json()
        {
            TestNST(@"@""{""""title"""":""""\u041f\u043e\u043b\u0442\u043e\u0440\u0430 \u0417\u0435\u043c\u043b\u0435\u043a\u043e\u043f\u0430"""" }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{</OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""title""</StringToken>
            <ColonToken>:</ColonToken>
            <Literal>
              <StringToken>""\u041f\u043e\u043b\u0442\u043e\u0440\u0430 \u0417\u0435\u043c\u043b\u0435\u043a\u043e\u043f\u0430""<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_object_with_newlines_json()
        {
            TestNST(@"@""{
""""a"""": """"b""""
}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Object>
        <OpenBraceToken>{<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></OpenBraceToken>
        <Sequence>
          <Property>
            <StringToken>""a""</StringToken>
            <ColonToken>:<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></ColonToken>
            <Literal>
              <StringToken>""b""<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></StringToken>
            </Literal>
          </Property>
        </Sequence>
        <CloseBraceToken>}</CloseBraceToken>
      </Object>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_1_2_3_bytes_UTF_8_sequences_json()
        {
            TestNST(@"@""[""""\u0060\u012a\u12AB""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u0060\u012a\u12AB""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_accepted_surrogate_pair_json()
        {
            TestNST(@"@""[""""\uD801\udc37""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD801\udc37""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_accepted_surrogate_pairs_json()
        {
            TestNST(@"@""[""""\ud83d\ude39\ud83d\udc8d""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\ud83d\ude39\ud83d\udc8d""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_allowed_escapes_json()
        {
            TestNST(@"@""[""""\""""\\\/\b\f\n\r\t""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\""\\\/\b\f\n\r\t""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_backslash_and_u_escaped_zero_json()
        {
            TestNST(@"@""[""""\\u0000""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\\u0000""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_backslash_doublequotes_json()
        {
            TestNST(@"@""[""""\""""""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\""""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_comments_json()
        {
            TestNST(@"@""[""""a/*b*/c/*d//e""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""a/*b*/c/*d//e""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_double_escape_a_json()
        {
            TestNST(@"@""[""""\\a""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\\a""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_double_escape_n_json()
        {
            TestNST(@"@""[""""\\n""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\\n""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_escaped_control_character_json()
        {
            TestNST(@"@""[""""\u0012""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u0012""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_escaped_noncharacter_json()
        {
            TestNST(@"@""[""""\uFFFF""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uFFFF""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_in_array_json()
        {
            TestNST(@"@""[""""asd""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""asd""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_in_array_with_leading_space_json()
        {
            TestNST(@"@""[ """"asd""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""asd""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_last_surrogates_1_and_2_json()
        {
            TestNST(@"@""[""""\uDBFF\uDFFF""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uDBFF\uDFFF""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_nbsp_uescaped_json()
        {
            TestNST(@"@""[""""new\u00A0line""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""new\u00A0line""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_nonCharacterInUTF_8_U_10FFFF_json()
        {
            TestNST(@"@""[""""􏿿""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""􏿿""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_null_escape_json()
        {
            TestNST(@"@""[""""\u0000""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u0000""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_one_byte_utf_8_json()
        {
            TestNST(@"@""[""""\u002c""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u002c""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_pi_json()
        {
            TestNST(@"@""[""""π""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""π""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_reservedCharacterInUTF_8_U_1BFFF_json()
        {
            TestNST(@"@""[""""𛿿""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""𛿿""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_simple_ascii_json()
        {
            TestNST(@"@""[""""asd """"]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""asd ""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_space_json()
        {
            TestNST(@"@"""""" """"""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>"" ""</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_surrogates_U_1D11E_MUSICAL_SYMBOL_G_CLEF_json()
        {
            TestNST(@"@""[""""\uD834\uDd1e""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD834\uDd1e""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_three_byte_utf_8_json()
        {
            TestNST(@"@""[""""\u0821""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u0821""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_two_byte_utf_8_json()
        {
            TestNST(@"@""[""""\u0123""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u0123""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_u_2028_line_sep_json()
        {
            TestNST(@"@""["""" """"]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>"" ""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_u_2029_par_sep_json()
        {
            TestNST(@"@""["""" """"]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>"" ""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_uEscape_json()
        {
            TestNST(@"@""[""""\u0061\u30af\u30EA\u30b9""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u0061\u30af\u30EA\u30b9""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_uescaped_newline_json()
        {
            TestNST(@"@""[""""new\u000Aline""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""new\u000Aline""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unescaped_char_delete_json()
        {
            TestNST(@"@""[""""""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_json()
        {
            TestNST(@"@""[""""\uA66D""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uA66D""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicodeEscapedBackslash_json()
        {
            TestNST(@"@""[""""\u005C""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u005C""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_2_json()
        {
            TestNST(@"@""[""""⍂㈴⍂""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""⍂㈴⍂""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_escaped_double_quote_json()
        {
            TestNST(@"@""[""""\u0022""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u0022""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_U_10FFFE_nonchar_json()
        {
            TestNST(@"@""[""""\uDBFF\uDFFE""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uDBFF\uDFFE""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_U_1FFFE_nonchar_json()
        {
            TestNST(@"@""[""""\uD83F\uDFFE""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uD83F\uDFFE""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_U_200B_ZERO_WIDTH_SPACE_json()
        {
            TestNST(@"@""[""""\u200B""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u200B""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_U_2064_invisible_plus_json()
        {
            TestNST(@"@""[""""\u2064""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\u2064""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_U_FDD0_nonchar_json()
        {
            TestNST(@"@""[""""\uFDD0""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uFDD0""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_unicode_U_FFFE_nonchar_json()
        {
            TestNST(@"@""[""""\uFFFE""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""\uFFFE""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_utf8_json()
        {
            TestNST(@"@""[""""€𝄞""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""€𝄞""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_string_with_del_character_json()
        {
            TestNST(@"@""[""""aa""""]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""aa""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_lonely_false_json()
        {
            TestNST(@"@""false""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <FalseLiteralToken>false</FalseLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_lonely_int_json()
        {
            TestNST(@"@""42""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NumberToken>42</NumberToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_lonely_negative_real_json()
        {
            TestNST(@"@""-0.1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NumberToken>-0.1</NumberToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_lonely_null_json()
        {
            TestNST(@"@""null""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <NullLiteralToken>null</NullLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_lonely_string_json()
        {
            TestNST(@"@""""""asd""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>""asd""</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_lonely_true_json()
        {
            TestNST(@"@""true""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <TrueLiteralToken>true</TrueLiteralToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_string_empty_json()
        {
            TestNST(@"@""""""""""""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Literal>
        <StringToken>""""</StringToken>
      </Literal>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_trailing_newline_json()
        {
            TestNST(@"@""[""""a""""]
""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Literal>
            <StringToken>""a""</StringToken>
          </Literal>
        </Sequence>
        <CloseBracketToken>]<Trivia><EndOfLineTrivia>
</EndOfLineTrivia></Trivia></CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_true_in_array_json()
        {
            TestNST(@"@""[true]""", @"<Tree>
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
</Tree>",
        @"",
        @"");
        }

        [Fact]
        public void y_structure_whitespace_array_json()
        {
            TestNST(@"@"" [] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Array>
        <OpenBracketToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>[</OpenBracketToken>
        <Sequence />
        <CloseBracketToken>]<Trivia><WhitespaceTrivia> </WhitespaceTrivia></Trivia></CloseBracketToken>
      </Array>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>",
        @"",
        @"");
        }
    }
}
