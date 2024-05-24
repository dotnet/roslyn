// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// this place is dedicated to lexical related error tests
    /// </summary>
    public class LexicalErrorTests : CSharpTestBase
    {
        #region "Targeted Error Tests - please arrange tests in the order of error code"

        [WorkItem(535880, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535880")]
        [WorkItem(553293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553293")]
        [Fact]
        public void CS0594ERR_FloatOverflow()
        {
            var test =
@"class C
{
    const double d1 = -1e1000d;
    const double d2 = 1e-1000d;
    const float f1 = -2e100f;
    const float f2 = 2e-100f;
    const decimal m1 = -3e100m;
    const decimal m2 = 3e-100m;
}";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (3,24): error CS0594: Floating-point constant is outside the range of type 'double'
                //     const double d1 = -1e1000d;
                Diagnostic(ErrorCode.ERR_FloatOverflow, "").WithArguments("double"),
                // (5,23): error CS0594: Floating-point constant is outside the range of type 'float'
                //     const float f1 = -2e100f;
                Diagnostic(ErrorCode.ERR_FloatOverflow, "").WithArguments("float"),
                // (7,25): error CS0594: Floating-point constant is outside the range of type 'decimal'
                //     const decimal m1 = -3e100m;
                Diagnostic(ErrorCode.ERR_FloatOverflow, "3e100m").WithArguments("decimal"));
        }

        [Fact]
        public void CS0595ERR_InvalidReal()
        {
            var test =
@"public class C
{
    double d1 = 0e;
    double d2 = .0e;
    double d3 = 0.0e;
    double d4 = 0e+;
    double d5 = 0e-;
}";

            ParserErrorMessageTests.ParseAndValidate(test,
                  // (3,17): error CS0595: Invalid real literal
                  //     double d1 = 0e;
                  Diagnostic(ErrorCode.ERR_InvalidReal, "").WithLocation(3, 17),
                  // (4,17): error CS0595: Invalid real literal
                  //     double d2 = .0e;
                  Diagnostic(ErrorCode.ERR_InvalidReal, "").WithLocation(4, 17),
                  // (5,17): error CS0595: Invalid real literal
                  //     double d3 = 0.0e;
                  Diagnostic(ErrorCode.ERR_InvalidReal, "").WithLocation(5, 17),
                  // (6,17): error CS0595: Invalid real literal
                  //     double d4 = 0e+;
                  Diagnostic(ErrorCode.ERR_InvalidReal, "").WithLocation(6, 17),
                  // (7,17): error CS0595: Invalid real literal
                  //     double d5 = 0e-;
                  Diagnostic(ErrorCode.ERR_InvalidReal, "").WithLocation(7, 17)
                );
        }

        [WorkItem(6079, "https://github.com/dotnet/roslyn/issues/6079")]
        [Fact]
        public void FloatLexicalError()
        {
            var test =
@"class C
{
    const double d1 = 0endOfDirective.Span;
}";
            // The precise errors don't matter so much as the fact that the compiler should not crash.
            ParserErrorMessageTests.ParseAndValidate(test,
                  // (3,23): error CS0595: Invalid real literal
                  //     const double d1 = 0endOfDirective.Span;
                  Diagnostic(ErrorCode.ERR_InvalidReal, "").WithLocation(3, 23),
                  // (3,25): error CS1002: ; expected
                  //     const double d1 = 0endOfDirective.Span;
                  Diagnostic(ErrorCode.ERR_SemicolonExpected, "ndOfDirective").WithLocation(3, 25),
                  // (3,43): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                  //     const double d1 = 0endOfDirective.Span;
                  Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 43),
                  // (3,43): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                  //     const double d1 = 0endOfDirective.Span;
                  Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 43)
                );
        }

        [Fact]
        public void CS1009ERR_IllegalEscape()
        {
            var test = @"
namespace x
{
    public class a
    {
        public static void f(int i, char j)
        {
            string a = ""\m"";    // CS1009
        }
        public static void Main()
        {
        }
    }
}
";

            ParserErrorMessageTests.ParseAndValidate(test, Diagnostic(ErrorCode.ERR_IllegalEscape, @"\m"));
        }

        [Fact]
        public void EscapeCharStandalone()
        {
            var test = """
                \e
                """;

            ParserErrorMessageTests.ParseAndValidate(test,
                // (1,1): error CS1056: Unexpected character '\'
                // \e
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("\\").WithLocation(1, 1),
                // (1,3): error CS1001: Identifier expected
                // \e
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 3),
                // (1,3): error CS1002: ; expected
                // \e
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 3));
        }

        [Fact]
        public void EscapeCharInTypeName_A()
        {
            var test = """
                namespace x
                {
                    public class \e
                    {
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test,
                // (3,18): error CS1001: Identifier expected
                //     public class \e
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\").WithLocation(3, 18),
                // (3,18): error CS1514: { expected
                //     public class \e
                Diagnostic(ErrorCode.ERR_LbraceExpected, @"\").WithLocation(3, 18),
                // (3,18): error CS1513: } expected
                //     public class \e
                Diagnostic(ErrorCode.ERR_RbraceExpected, @"\").WithLocation(3, 18),
                // (3,18): error CS1056: Unexpected character '\'
                //     public class \e
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("\\").WithLocation(3, 18),
                // (3,19): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                //     public class \e
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "e").WithLocation(3, 19),
                // (4,5): error CS1022: Type or namespace definition, or end-of-file expected
                //     {
                Diagnostic(ErrorCode.ERR_EOFExpected, "{").WithLocation(4, 5),
                // (6,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 1));
        }

        [Fact]
        public void EscapeCharInTypeName_B()
        {
            var test = """
                namespace x
                {
                    public class X\e
                    {
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test,
                // (3,19): error CS1514: { expected
                //     public class X\e
                Diagnostic(ErrorCode.ERR_LbraceExpected, @"\").WithLocation(3, 19),
                // (3,19): error CS1513: } expected
                //     public class X\e
                Diagnostic(ErrorCode.ERR_RbraceExpected, @"\").WithLocation(3, 19),
                // (3,19): error CS1056: Unexpected character '\'
                //     public class X\e
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("\\").WithLocation(3, 19),
                // (3,20): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                //     public class X\e
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "e").WithLocation(3, 20),
                // (4,5): error CS1022: Type or namespace definition, or end-of-file expected
                //     {
                Diagnostic(ErrorCode.ERR_EOFExpected, "{").WithLocation(4, 5),
                // (6,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 1));
        }

        [Fact]
        public void EscapeCharInTypeName_C()
        {
            var test = """
                namespace x
                {
                    public class \eX
                    {
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test,
                // (3,18): error CS1001: Identifier expected
                //     public class \eX
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\").WithLocation(3, 18),
                // (3,18): error CS1514: { expected
                //     public class \eX
                Diagnostic(ErrorCode.ERR_LbraceExpected, @"\").WithLocation(3, 18),
                // (3,18): error CS1513: } expected
                //     public class \eX
                Diagnostic(ErrorCode.ERR_RbraceExpected, @"\").WithLocation(3, 18),
                // (3,18): error CS1056: Unexpected character '\'
                //     public class \eX
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("\\").WithLocation(3, 18),
                // (3,19): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                //     public class \eX
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "eX").WithLocation(3, 19),
                // (4,5): error CS1022: Type or namespace definition, or end-of-file expected
                //     {
                Diagnostic(ErrorCode.ERR_EOFExpected, "{").WithLocation(4, 5),
                // (6,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 1));
        }

        [Fact]
        public void EscapeCharInStringCSharp12_A()
        {
            var test = """
                namespace x
                {
                    public class a
                    {
                        string a = "\e";
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test, TestOptions.Regular12,
                // (5,21): error CS8652: The feature 'string escape character' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         string a = "\e";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"\e").WithArguments("string escape character").WithLocation(5, 21));
        }

        [Fact]
        public void EscapeCharInStringCSharp12_B()
        {
            var test = """
                namespace x
                {
                    public class a
                    {
                        string a = "e\ee";
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test, TestOptions.Regular12,
                // (5,22): error CS8652: The feature 'string escape character' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         string a = "e\ee";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"\e").WithArguments("string escape character").WithLocation(5, 22));
        }

        [Fact]
        public void EscapeCharInCharCSharp12_A()
        {
            var test = """
                namespace x
                {
                    public class a
                    {
                        char a = '\e';
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test, TestOptions.Regular12,
                // (5,19): error CS8652: The feature 'string escape character' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         char a = '\e';
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"\e").WithArguments("string escape character").WithLocation(5, 19));
        }

        [Fact]
        public void EscapeCharInStringCSharpPreview_A()
        {
            var test = """
                namespace x
                {
                    public class a
                    {
                        string a = "\e";
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test, TestOptions.RegularNext);
        }

        [Fact]
        public void EscapeCharInStringCSharpPreview_B()
        {
            var test = """
                namespace x
                {
                    public class a
                    {
                        string a = "e\ee";
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test, TestOptions.RegularNext);
        }

        [Fact]
        public void EscapeCharInCharCSharpPreview_A()
        {
            var test = """
                namespace x
                {
                    public class a
                    {
                        string a = '\e';
                    }
                }
                """;

            ParserErrorMessageTests.ParseAndValidate(test, TestOptions.RegularNext);
        }

        [Fact]
        public void CS1010ERR_NewlineInConst()
        {
            var test = @"
namespace x {
    abstract public class clx 
    {
        string a = ""Hello World    // CS1010
        char b = 'a';
    }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_NewlineInConst, ""),
Diagnostic(ErrorCode.ERR_SemicolonExpected, ""));
        }

        [Fact]
        public void CS1011ERR_EmptyCharConst()
        {
            var test = @"
namespace x {
    abstract public class clx 
    {
        char b = '';
    }
}
";

            ParserErrorMessageTests.ParseAndValidate(test, Diagnostic(ErrorCode.ERR_EmptyCharConst, ""));
        }

        [Fact]
        public void CS1012ERR_TooManyCharsInConst()
        {
            var test = @"
namespace x
{
    public class b : c
    {
        char a = 'xx';    
        public static void Main()
        {
        }    
    }
}
";

            ParserErrorMessageTests.ParseAndValidate(test, Diagnostic(ErrorCode.ERR_TooManyCharsInConst, ""));
        }

        [Fact]
        public void CS1015ERR_TypeExpected()
        {
            var test = @"
public class C
{
    public static void Main()
    {
        const int i = 0;
        const const double d = 0;
        const const const long l = 0;
        const readonly readonly readonly const double r = 0;
    }    
}
";
            ParserErrorMessageTests.ParseAndValidate(test,
                // (7,15): error CS1031: Type expected
                //         const const double d = 0;
                Diagnostic(ErrorCode.ERR_TypeExpected, "const").WithLocation(7, 15),
                // (8,15): error CS1031: Type expected
                //         const const const long l = 0;
                Diagnostic(ErrorCode.ERR_TypeExpected, "const").WithLocation(8, 15),
                // (8,21): error CS1031: Type expected
                //         const const const long l = 0;
                Diagnostic(ErrorCode.ERR_TypeExpected, "const").WithLocation(8, 21),
                // (9,15): error CS0106: The modifier 'readonly' is not valid for this item
                //         const readonly readonly readonly const double r = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(9, 15),
                // (9,24): error CS0106: The modifier 'readonly' is not valid for this item
                //         const readonly readonly readonly const double r = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(9, 24),
                // (9,33): error CS0106: The modifier 'readonly' is not valid for this item
                //         const readonly readonly readonly const double r = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(9, 33),
                // (9,42): error CS1031: Type expected
                //         const readonly readonly readonly const double r = 0;
                Diagnostic(ErrorCode.ERR_TypeExpected, "const").WithLocation(9, 42)
            );
        }

        [WorkItem(553293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553293")]
        [Fact]
        public void CS1021ERR_IntOverflow()
        {
            var test =
@"#line 12345678901234567890
class C
{
    const int x = -123456789012345678901234567890;
}";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (1,7): error CS1021: Integral constant is too large
                // #line 12345678901234567890
                Diagnostic(ErrorCode.ERR_IntOverflow, ""),
                // (1,7): error CS1576: The line number specified for #line directive is missing or invalid
                // #line 12345678901234567890
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "12345678901234567890"),
                // (4,20): error CS1021: Integral constant is too large
                //     const int x = -123456789012345678901234567890;
                Diagnostic(ErrorCode.ERR_IntOverflow, ""));
        }

        // Preprocessor:
        [Fact]
        public void CS1032ERR_PPDefFollowsTokenpp()
        {
            var test = @"
public class Test
{
 # define ABC
}
";

            ParserErrorMessageTests.ParseAndValidate(test, Diagnostic(ErrorCode.ERR_PPDefFollowsToken, "define"));
        }

        // Preprocessor:
        [Fact]
        public void ERR_PPReferenceFollowsToken()
        {
            var test = @"
using System;
# r ""goo""
";

            ParserErrorMessageTests.ParseAndValidate(test, TestOptions.Script, Diagnostic(ErrorCode.ERR_PPReferenceFollowsToken, "r"));
        }

        [Fact]
        public void CS1035ERR_OpenEndedComment()
        {
            var test = @"
public class MainClass
    {
    public static int Main ()
        {
        return 1;
        }
    }
//Comment lacks closing */
/*    
";

            ParserErrorMessageTests.ParseAndValidate(test, Diagnostic(ErrorCode.ERR_OpenEndedComment, ""));
        }

        [Fact]
        public void CS1035ERR_OpenEndedComment_Razor()
        {
            var test = @"
public class MainClass
    {
    public static int Main ()
        {
        return 1;
        }
    }
//Comment lacks closing */
@*    
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (10,1): error CS1056: Unexpected character '@'
                // @*    
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "@").WithArguments("@").WithLocation(10, 1),
                // (10,1): error CS1035: End-of-file found, '*/' expected
                // @*    
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(10, 1));
        }

        [Fact]
        public void CS1035ERR_OpenEndedComment_Razor_InterpolatedString()
        {
            var test = """
                public class MainClass
                    {
                    public static int Main ()
                        {
                        return $"{1@*   
                """;

            ParserErrorMessageTests.ParseAndValidate(test,
                // (5,17): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //         return $"{1@*   
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, @"""{").WithLocation(5, 17),
                // (5,20): error CS1056: Unexpected character '@'
                //         return $"{1@*   
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "@").WithArguments("@").WithLocation(5, 20),
                // (5,20): error CS1035: End-of-file found, '*/' expected
                //         return $"{1@*   
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(5, 20),
                // (5,25): error CS1002: ; expected
                //         return $"{1@*   
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 25),
                // (5,25): error CS1513: } expected
                //         return $"{1@*   
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 25),
                // (5,25): error CS1513: } expected
                //         return $"{1@*   
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 25));
        }

        [Fact]
        public void CS1035AtColonParsedAsBadRazorContent_01()
        {
            var test = """
                var x = @:;
                """;

            ParsingTests.ParseAndValidate(test,
                // (1,9): error CS1525: Invalid expression term ''
                // var x = @:;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "@:;").WithArguments("").WithLocation(1, 9),
                // (1,10): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                // var x = @:;
                Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ":").WithLocation(1, 10),
                // (1,12): error CS1002: ; expected
                // var x = @:;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 12));
        }

        [Fact]
        public void CS1035AtColonParsedAsBadRazorContent_02()
        {
            var test = """
                @:<div>test</div>
                """;

            ParsingTests.ParseAndValidate(test,
                // (1,2): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                // @:<div>test</div>
                Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ":").WithLocation(1, 2));
        }

        [Fact]
        public void CS1035AtColonParsedAsBadRazorContent_03()
        {
            var test = """
                @: M() {}
                """;

            ParsingTests.ParseAndValidate(test,
                // (1,2): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                // @: M() {}
                Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ":").WithLocation(1, 2));
        }

        [Fact, WorkItem(526993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/526993")]
        public void CS1039ERR_UnterminatedStringLit()
        {
            // TODO: extra errors
            var test = @"
public class Test
{
   public static int Main()
   {
      string s =@""string;
      return 1;
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
    // (6,17): error CS1039: Unterminated string literal
    //       string s =@"string;
    Diagnostic(ErrorCode.ERR_UnterminatedStringLit, ""),
    // (10,1): error CS1002: ; expected
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ""),
    // (10,1): error CS1513: } expected
    Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
    // (10,1): error CS1513: } expected
    Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [Fact, WorkItem(536688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536688")]
        public void CS1040ERR_BadDirectivePlacementpp()
        {
            var test = @"
/* comment */ #define TEST
class Test
{
}
";

            ParserErrorMessageTests.ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#"));
        }

        [Fact, WorkItem(526994, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/526994")]
        public void CS1056ERR_UnexpectedCharacter()
        {
            // TODO: Extra errors
            var test = @"
using System;
class Test
{
	public static void Main()
	{
		int \\u070Fidentifier1 = 1;
		Console.WriteLine(identifier1);
	}
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
    // (7,7): error CS1001: Identifier expected
    // 		int \\u070Fidentifier1 = 1;
    Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\"),
    // (7,7): error CS1056: Unexpected character '\'
    // 		int \\u070Fidentifier1 = 1;
    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\"),
    // (7,8): error CS1056: Unexpected character '\u070F'
    // 		int \\u070Fidentifier1 = 1;
    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u070F"),
    // (7,14): error CS1002: ; expected
    // 		int \\u070Fidentifier1 = 1;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "identifier1"));
        }

        [Fact]
        public void CS1056ERR_UnexpectedCharacter_EscapedBackslash()
        {
            var test = @"using S\u005Cu0065 = System;
class A
{
int x = 0;
}
";

            ParserErrorMessageTests.ParseAndValidate(test,// (1,8): error CS1002: ; expected
                                                          // using S\u005Cu0065 = System;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, @"\u005C").WithLocation(1, 8),
    // (1,8): error CS1056: Unexpected character '\u005C'
    // using S\u005Cu0065 = System;
    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u005C").WithLocation(1, 8));
        }

        [Fact, WorkItem(536882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536882")]
        public void CS1056RegressDisallowedUnicodeChars()
        {
            var test = @"using S\u0600 = System;
class A
{
    int x\u0060 = 0;
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (4,10): error CS1056: Unexpected character '\u0060'
                //     int x\u0060 = 0;
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u0060"));
        }

        [Fact]
        public void CS1056ERR_UnexpectedCharacter_UnpairedSurrogate1()
        {
            var test = $$"""
                using System;
                class Test
                {
                    public static void Main()
                    {
                        int {{'\ud86d'}} = 1;
                    }
                }
                """;

            ParsingTests.ParseAndValidate(test,
                // (6,13): error CS1001: Identifier expected
                //         int � = 1;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "\ud86d").WithLocation(6, 13),
                // (6,13): error CS1056: Unexpected character '\ud86d'
                //         int � = 1;
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\ud86d").WithLocation(6, 13),
                // (6,15): error CS1002: ; expected
                //         int � = 1;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=").WithLocation(6, 15),
                // (6,15): error CS1525: Invalid expression term '='
                //         int � = 1;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(6, 15));
        }

        [Fact]
        public void CS1056ERR_UnexpectedCharacter_UnpairedSurrogate2()
        {
            var test = $$"""
                using System;
                class Test
                {
                    public static void Main()
                    {
                        int {{'\udce7'}} = 1;
                    }
                }
                """;

            ParsingTests.ParseAndValidate(test,
                // (6,13): error CS1001: Identifier expected
                //         int � = 1;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "\udce7").WithLocation(6, 13),
                // (6,13): error CS1056: Unexpected character '\udce7'
                //         int � = 1;
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\udce7").WithLocation(6, 13),
                // (6,15): error CS1002: ; expected
                //         int � = 1;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=").WithLocation(6, 15),
                // (6,15): error CS1525: Invalid expression term '='
                //         int � = 1;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(6, 15));
        }

        [Fact]
        public void CS1056ERR_UnexpectedCharacter_Surrogate()
        {
            var test = """
                using System;
                class Test
                {
                    public static void Main()
                    {
                        int 𫓧龦 = 1;
                    }
                }
                """;

            ParsingTests.ParseAndValidate(test,
                // (6,13): error CS1001: Identifier expected
                //         int 𫓧龦 = 1;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "𫓧").WithLocation(6, 13),
                // (6,13): error CS1056: Unexpected character '𫓧'
                //         int 𫓧龦 = 1;
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("𫓧").WithLocation(6, 13),
                // (6,15): error CS1002: ; expected
                //         int 𫓧龦 = 1;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "龦").WithLocation(6, 15));
        }

        [Fact]
        public void CS1056ERR_UnexpectedCharacter_Surrogate_Long()
        {
            // Create a file with 200 slashes in a row.  This will cause 200 'expected character' errors, after which
            // the compiler will give up and make a single error (with a multi-char message) for the remainder of the doc.

            var test = $$"""
                using System;
                class Test
                {
                    public static void Main()
                    {
                        int {{new string('\\', 200)}}𫓧𫓧 = 1;
                    }
                }
                """;

            var descriptions = new List<DiagnosticDescription>
            { 
                // (6,13): error CS1001: Identifier expected
                //         int \..200 more slashes..\𫓧𫓧 = 1;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\").WithLocation(6, 13),
            };

            for (int i = 0; i < 200; i++)
            {
                descriptions.Add(
                    // (6,13 + i): error CS1056: Unexpected character '\'
                    //         int \..200 more slashes..\𫓧𫓧 = 1;
                    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("\\").WithLocation(6, 13 + i));
            }

            descriptions.Add(
                // (6,213): error CS1056: Unexpected character '𫓧'
                //         int \..200 more slashes..\𫓧𫓧 = 1;
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"𫓧").WithLocation(6, 213));

            // (6,214): error CS1056: Unexpected character '龦 = 1;\r\n    }\r\n}'
            //         int \..200 more slashes..\𫓧𫓧 = 1;
            if (PathUtilities.IsUnixLikePlatform)
            {
                descriptions.Add(Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"𫓧 = 1;\n    }\n}").WithLocation(6, 215));
            }
            else
            {
                descriptions.Add(Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"𫓧 = 1;\r\n    }\r\n}").WithLocation(6, 215));
            }

            descriptions.AddRange(new[]
            { 
                // (8,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 2),
                // (8,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 2),
                // (8,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 2),
            });

            ParsingTests.ParseAndValidate(test, descriptions.ToArray());
        }

        [Fact]
        public void CS1056ERR_RazorComment()
        {
            var test = """
                @* comment *@
                class C {}
                """;

            ParsingTests.ParseAndValidate(test,
                // (1,1): error CS1056: Unexpected character '@'
                // @* comment *@
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "@").WithArguments("@").WithLocation(1, 1));
        }

        [Fact]
        public void CS1056ERR_RazorComment_InterpolatedString()
        {
            var test = """
                $"{@* comment *@}"
                """;

            ParsingTests.ParseAndValidate(test,
                // (1,4): error CS1056: Unexpected character '@'
                // $"{@* comment *@}"
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "@").WithArguments("@").WithLocation(1, 4),
                // (1,17): error CS1733: Expected expression
                // $"{@* comment *@}"
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 17),
                // (1,19): error CS1002: ; expected
                // $"{@* comment *@}"
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 19));
        }

        [Fact, WorkItem(535937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535937")]
        public void CS1646ERR_ExpectedVerbatimLiteral()
        {
            var test = @"
class Test
{
    public static int Main()
    {
        int i = @5;  // CS1646
        return 1;
    }
}
";

            // Roslyn more errors
            ParserErrorMessageTests.ParseAndValidate(test,
    // (7,17): error CS1525: Invalid expression term ''
    //         int i = @\u0303;  // CS1646
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "@").WithArguments(""),
    // (7,17): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
    //         int i = @\u0303;  // CS1646
    Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ""),
                // (7,18): error CS1056: Unexpected character '\u0303'
                //         int i = @\u0303;  // CS1646
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "5"));
            // Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u0303"));
        }

        [Fact]
        public void CS1646ERR_ExpectedVerbatimLiteral_WithEscapeAndIdentifierPartChar()
        {
            var test = @"
delegate int MyDelegate();
class Test
{
    public static int Main()
    {
        int i = @\u0303;  // CS1646
        return 1;
    }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
    // (7,17): error CS1525: Invalid expression term ''
    //         int i = @\u0303;  // CS1646
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "@").WithArguments(""),
    // (7,17): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
    //         int i = @\u0303;  // CS1646
    Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ""),
    // (7,18): error CS1056: Unexpected character '\u0303'
    //         int i = @\u0303;  // CS1646
    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u0303"));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString1()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { @"" "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics();
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString2()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { @"" ""
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                       } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(7, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString3()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { @""
                         "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,28): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                          " } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(7, 28));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString4()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { @""
                         ""
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (8,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                       } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(8, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString5()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        @"" "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,30): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                         @" " } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(7, 30));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString6()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        @""
                         "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (8,28): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                          " } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(8, 28));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString7()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        @""
                         ""
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (9,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                       } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(9, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString8()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { @""

                         "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (8,28): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                          " } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(8, 28));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString9()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { @""

                         "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (8,28): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                          " } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(8, 28));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString10()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { $@"" { @""

                                "" } "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (8,39): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                                 " } " } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(8, 39));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString11()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment */ } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (6,38): error CS1733: Expected expression
                //       string s = $"x { /* comment */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(6, 38));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (6,38): error CS1733: Expected expression
                //       string s = $"x { /* comment */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(6, 38));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (6,38): error CS1733: Expected expression
                //       string s = $"x { /* comment */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(6, 38));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString12()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (6,21): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //       string s = $"x { /* comment } y";
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, " {").WithLocation(6, 21),
                // (6,24): error CS1035: End-of-file found, '*/' expected
                //       string s = $"x { /* comment } y";
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(6, 24),
                // (9,1): error CS1733: Expected expression
                // 
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 1),
                // (9,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(9, 1),
                // (9,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 1),
                // (9,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 1));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (6,21): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //       string s = $"x { /* comment } y";
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, " {").WithLocation(6, 21),
                // (6,24): error CS1035: End-of-file found, '*/' expected
                //       string s = $"x { /* comment } y";
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(6, 24),
                // (9,1): error CS1733: Expected expression
                // 
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 1),
                // (9,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(9, 1),
                // (9,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 1),
                // (9,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 1));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (6,21): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //       string s = $"x { /* comment } y";
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, " {").WithLocation(6, 21),
                // (6,24): error CS1035: End-of-file found, '*/' expected
                //       string s = $"x { /* comment } y";
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(6, 24),
                // (9,1): error CS1733: Expected expression
                // 
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 1),
                // (9,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(9, 1),
                // (9,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 1),
                // (9,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 1));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString13()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment
         } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (6,21): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //       string s = $"x { /* comment
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, " {").WithLocation(6, 21),
                // (6,24): error CS1035: End-of-file found, '*/' expected
                //       string s = $"x { /* comment
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(6, 24),
                // (10,1): error CS1733: Expected expression
                // 
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(10, 1),
                // (10,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(10, 1),
                // (10,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(10, 1),
                // (10,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(10, 1));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (6,21): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //       string s = $"x { /* comment
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, " {").WithLocation(6, 21),
                // (6,24): error CS1035: End-of-file found, '*/' expected
                //       string s = $"x { /* comment
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(6, 24),
                // (10,1): error CS1733: Expected expression
                // 
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(10, 1),
                // (10,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(10, 1),
                // (10,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(10, 1),
                // (10,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(10, 1));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (6,21): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //       string s = $"x { /* comment
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, " {").WithLocation(6, 21),
                // (6,24): error CS1035: End-of-file found, '*/' expected
                //       string s = $"x { /* comment
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(6, 24),
                // (10,1): error CS1733: Expected expression
                // 
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(10, 1),
                // (10,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(10, 1),
                // (10,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(10, 1),
                // (10,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(10, 1));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString14()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment */ 0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics();
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString15()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment */
                        0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,27): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                         0 } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(7, 27));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString16()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /*
                         * comment
                         */ } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (8,29): error CS1733: Expected expression
                //                          */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(8, 29));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (8,29): error CS1733: Expected expression
                //                          */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(8, 29));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (8,29): error CS1733: Expected expression
                //                          */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(8, 29));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString17()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment */ 0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics();
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString18()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment */
                        0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,27): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                         0 } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(7, 27));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString19()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment */
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (7,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 23));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString20()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment */ 0
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                       } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(7, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString21()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /* comment */
                        0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,27): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                         0 } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(7, 27));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString22()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /*
                         * comment
                         */
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (9,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (9,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (9,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 23));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString23()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /*
                         * comment
                         */ 0
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (9,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                       } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(9, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString24()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /*
                         * comment
                         */
                        0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (9,27): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                         0 } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(9, 27));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString25()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { /*
                         * comment
                         */
                        0
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (10,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                       } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(10, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString26()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        /* comment */ } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (7,39): error CS1733: Expected expression
                //                         /* comment */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 39));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,39): error CS1733: Expected expression
                //                         /* comment */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 39));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,39): error CS1733: Expected expression
                //                         /* comment */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 39));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString27()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        /* comment */ 0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (7,41): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                         /* comment */ 0 } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(7, 41));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString28()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        /*
                         * comment
                         */ } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (9,29): error CS1733: Expected expression
                //                          */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 29));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (9,29): error CS1733: Expected expression
                //                          */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 29));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (9,29): error CS1733: Expected expression
                //                          */ } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 29));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString29()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        /*
                         * comment
                         */ 0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (9,31): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                          */ 0 } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(9, 31));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString30()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        /*
                         * comment
                         */
                        0 } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (10,27): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                         0 } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(10, 27));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString31()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        /*
                         * comment
                         */
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                // (10,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(10, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (10,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(10, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (10,23): error CS1733: Expected expression
                //                       } y";
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(10, 23));
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString32()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        /*
                         * comment
                         */ 0
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (10,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                       } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(10, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8967ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString33()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x {
                        /*
                         *comment
                         */
                        0
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (11,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                       } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(11, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8077ERR_SingleLineCommentInExpressionHole1()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { // comment
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                    // (7,23): error CS1733: Expected expression
                    //                       } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                    // (7,23): error CS1733: Expected expression
                    //                       } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                    // (7,23): error CS1733: Expected expression
                    //                       } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 23));
        }

        [Fact]
        public void CS8077ERR_SingleLineCommentInExpressionHole2()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $@""x { // comment
                       } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                    // (7,24): error CS1733: Expected expression
                    //                        } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 24));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                    // (7,24): error CS1733: Expected expression
                    //                        } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 24));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                    // (7,24): error CS1733: Expected expression
                    //                        } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 24));
        }

        [Fact]
        public void CS8077ERR_SingleLineCommentInExpressionHole3()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { $@"" { // comment
                             } "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test,
                    // (7,30): error CS1733: Expected expression
                    //                              } " } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 30));
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                    // (7,30): error CS1733: Expected expression
                    //                              } " } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 30));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                    // (7,30): error CS1733: Expected expression
                    //                              } " } y";
                    Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(7, 30));
        }

        [Fact]
        public void CS8077ERR_SingleLineCommentInExpressionHole4()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { // comment
                        0
                      } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                    // (8,23): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                    //                       } y";
                    Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(8, 23));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8077ERR_SingleLineCommentInExpressionHole5()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $@""x { // comment
                         0
                       } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics();
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact]
        public void CS8077ERR_SingleLineCommentInExpressionHole6()
        {
            var test = @"
public class Test
{
   public static void Main()
   {
      string s = $""x { $@"" { // comment
                               0
                             } "" } y"";
   }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);
            CreateCompilation(test, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (8,34): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 10.0. Please use language version 11.0 or greater.
                //                              } " } y";
                Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("10.0", "11.0").WithLocation(8, 34));
            CreateCompilation(test, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        #endregion

        #region "Targeted Warning Tests - please arrange tests in the order of error code"

        [Fact, WorkItem(535871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535871"), WorkItem(527942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527942")]
        public void CS0078WRN_LowercaseEllSuffix()
        {
            var test = @"
class Test
{
    public static int Main()
    {
        long l = 25l;   // CS0078
        ulong n1 = 1lu;   // CS0078
        ulong n2 = 10lU;   // CS0078
        System.Console.WriteLine(""{0}+{1}+{2}"", l, n1, n2);
        return 0;
    }
}
";

            ParserErrorMessageTests.ParseAndValidate(test);

            CreateCompilation(test).VerifyDiagnostics(
                // (6,20): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                //         long l = 25l;   // CS0078
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(6, 20),
                // (7,21): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                //         ulong n1 = 1lu;   // CS0078
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(7, 21),
                // (8,22): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                //         ulong n2 = 10lU;   // CS0078
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(8, 22));
        }

        [Fact, WorkItem(530118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530118")]
        public void TestEndIfExpectedOnEOF()
        {
            var test = @"
#if false
int 1 = 0;";

            ParserErrorMessageTests.ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(3, 11));
        }

        [Fact, WorkItem(530118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530118")]
        public void TestEndIfExpectedOnEndRegion()
        {
            var test = @"
#region xyz
#if false
int 1 = 0;
#endregion
";

            ParserErrorMessageTests.ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "#endregion").WithLocation(5, 1),
Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(6, 1));
        }

        #endregion
    }
}
