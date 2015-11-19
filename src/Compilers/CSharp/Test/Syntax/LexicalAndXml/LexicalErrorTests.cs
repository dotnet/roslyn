// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// this place is dedicated to lexical related error tests
    /// </summary>
    public class LexicalErrorTests : CSharpTestBase
    {
        #region "Targeted Error Tests - please arrange tests in the order of error code"

        [WorkItem(535880, "DevDiv")]
        [WorkItem(553293, "DevDiv")]
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
                // (3,23): error CS0594: Floating-point constant is outside the range of type 'double'
                //     const double d1 = 0endOfDirective.Span;
                Diagnostic(ErrorCode.ERR_FloatOverflow, "").WithArguments("double").WithLocation(3, 23),
                // (3,25): error CS1002: ; expected
                //     const double d1 = 0endOfDirective.Span;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ndOfDirective").WithLocation(3, 25),
                // (3,43): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     const double d1 = 0endOfDirective.Span;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 43),
                // (3,43): error CS1519: Invalid token ';' in class, struct, or interface member declaration
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

        [WorkItem(553293, "DevDiv")]
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
# r ""foo""
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

        [Fact, WorkItem(526993, "DevDiv")]
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

        [Fact, WorkItem(536688, "DevDiv")]
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

        [Fact, WorkItem(526994, "DevDiv")]
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
    Diagnostic(ErrorCode.ERR_SemicolonExpected, @"\u005C"),
    // (1,14): error CS0116: A namespace does not directly contain members such as fields or methods
    // using S\u005Cu0065 = System;
    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "u0065"),
    // (1,22): error CS0116: A namespace does not directly contain members such as fields or methods
    // using S\u005Cu0065 = System;
    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "System"),
    // (1,8): error CS1056: Unexpected character '\u005C'
    // using S\u005Cu0065 = System;
    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u005C"),
    // (1,20): error CS1022: Type or namespace definition, or end-of-file expected
    // using S\u005Cu0065 = System;
    Diagnostic(ErrorCode.ERR_EOFExpected, "="),
    // (1,28): error CS1022: Type or namespace definition, or end-of-file expected
    // using S\u005Cu0065 = System;
    Diagnostic(ErrorCode.ERR_EOFExpected, ";"));
        }

        [Fact, WorkItem(536882, "DevDiv")]
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

        [Fact, WorkItem(535937, "DevDiv")]
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

        #endregion

        #region "Targeted Warning Tests - please arrange tests in the order of error code"

        [Fact, WorkItem(535871, "DevDiv"), WorkItem(527942, "DevDiv")]
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

            ParserErrorMessageTests.ParseAndValidate(test,
Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l"),
Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l"),
Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l"));
        }

        [Fact, WorkItem(530118, "DevDiv")]
        public void TestEndIfExpectedOnEOF()
        {
            var test = @"
#if false
int 1 = 0;";

            ParserErrorMessageTests.ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(3, 11));
        }

        [Fact, WorkItem(530118, "DevDiv")]
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
