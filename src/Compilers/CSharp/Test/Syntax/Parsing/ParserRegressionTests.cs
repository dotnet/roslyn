// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    public class ParserRegressionTests : ParsingTests
    {
        public ParserRegressionTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options ?? TestOptions.Regular);
        }

        [Fact]
        public void PartialLocationInModifierList()
        {
            var comp = CreateCompilation(@"
class Program
{
    partial abstract class A {}
    partial abstract class A {}

    partial partial class B {}
    partial partial class B {}

    partial abstract struct S {}
    partial abstract struct S {}
}");
            comp.VerifyDiagnostics(
                // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial abstract class A {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
                // (5,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial abstract class A {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 5),
                // (7,13): error CS1525: Invalid expression term 'partial'
                //     partial partial class B {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(7, 13),
                // (7,13): error CS1002: ; expected
                //     partial partial class B {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(7, 13),
                // (8,13): error CS1525: Invalid expression term 'partial'
                //     partial partial class B {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(8, 13),
                // (8,13): error CS1002: ; expected
                //     partial partial class B {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(8, 13),
                // (10,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial abstract struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(10, 5),
                // (11,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial abstract struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(11, 5),
                // (10,29): error CS0106: The modifier 'abstract' is not valid for this item
                //     partial abstract struct S {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("abstract").WithLocation(10, 29),
                // (8,13): error CS0102: The type 'Program' already contains a definition for ''
                //     partial partial class B {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "").WithArguments("Program", "").WithLocation(8, 13),
                // (8,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     partial partial class B {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(8, 5),
                // (7,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     partial partial class B {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(7, 5));
        }

        [WorkItem(540005, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540005")]
        [Fact]
        public void c01()
        {
            var test = @"///   \u2750\uDFC1  = </   @goto   </  ascending abstract  + (  descending __arglist  + descending   @if   <?   @switch  + global @long  + @orderby   \u23DC\u6D71\u5070\u9350   ++  into _\u6105\uE331\u27D0   #  join [  + break   @extern   [   @char   <<  partial |  + remove + do   @else  + @typeof   @private  + 
";
            var tree = SyntaxFactory.ParseSyntaxTree(test);
        }

        [WorkItem(540006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540006")]
        [Fact]
        public void c02()
        {
            var test = @"internal int TYPES()        {             break  retVal =  @while ; __reftype             CLASS c = dynamic   descending  CLASS( % ; on             IF xx = module   _4䓞  |=              \u14DB\u0849   <![CDATA[  c =>  @default  $             retVal @assembly  += c void .Member &  -= ; @typeof 
";
            var tree = SyntaxFactory.ParseSyntaxTree(test);
        }

        [WorkItem(540007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540007")]
        [Fact]
        public void c03()
        {
            var test = @"/// </summary>        /// <returns></returns>         else  int OPERATOR @uint  $ )        { -              static ? operator  :: ]  @readonly  = @on   async  int? , [ return ] { 1 ! ,  @property  &  3 !   @case  %   partial   += ;/*[] bug*/ // YES []            int % ] endregion  var  =   ]]>   @for  |=   @struct , 3, lock  4 @on  %  5 goto  } @stackalloc  } /*,;*/            int %=  i = @fixed   ?> int << a base  <= 1] default ; 

";
            var tree = SyntaxFactory.ParseSyntaxTree(test);
        }

        [WorkItem(540007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540007")]
        [Fact]
        public void c04()
        {
            var test = @"/// </summary>        /// <returns></returns>        internal  do  OPERATOR || )        {            int?[] a = new int? \u14DB\u0849 [5] { 1, 2, 3, 4,  @using  } /= /*[] bug*/ // YES []            int[] var = { 1, 2, 3, 4, 5 } $ /*,;*/            int i =  ; int)a[1];/*[]*/            i = i  <<=   @__arglist  - i @sbyte  *  @extern  / i % i ++   %  i  ||   @checked  ^ i; 
 /*+ - * / % & | ^*/
";
            var tree = SyntaxFactory.ParseSyntaxTree(test);
        }

        [WorkItem(540007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540007")]
        [Fact]
        public void c000138()
        {
            var test = @"int?[] ///  a   = new int? .  @string ] { 1,  typeof  $  3, 4, 5 } static ; @partial /*[] bug*/ // YES []            int[] var = { 1,  else , 3 </  4 |  5 };/*,;*/            int i = (int)a @in [ __refvalue ] [ /*[]*/             @readonly  = i + i - abstract   @typevar  * i /  @this  % i & ,  i | i ^ unchecked  i; in /*+ - * / % & | ^*/            bool b = true & false +  | true ^ false readonly ;/*& | ^*/             @unsafe  = !b;/*!*/            i = ~i;/*~i*/            b = i < (i - 
  1 @for ) && (i + 1) > i;/*< && >*/
";
            var tree = SyntaxFactory.ParseSyntaxTree(test);
        }

        [WorkItem(540007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540007")]
        [Fact]
        public void c000241()
        {
            var test = @"/// </summary>        /// <returns></returns>         const   by  TYPES ascending  / ) $         { @let             int @byte   @by   |  0 
 ; 
";
            var tree = SyntaxFactory.ParseSyntaxTree(test);
        }

        [WorkItem(540007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540007")]
        [Fact]
        public void c024928()
        {
            var test = @"/// </summary>        /// <returns></returns>        internal int OPERATOR()        { //             int?[ *   @method   !  new int explicit  , [  5 --  {  \uDD48\uEF5C , 2,  @ascending , @foreach   \uD17B\u21A8  .  5  ;  { /*[] bug*/ // YES []            int ::  (  <=  var  />  { @readonly  1 <!--  2 __makeref  ?  3 @descending , 4 @float , 5 } disable ;/*,;*/            int -=   _\uAEC4   -  ( operator int <<= a =>  += ] @abstract ; property /*[]*/            i = i double  + i -  @async   -  i '   &  i  )  i &  @using   #   @byte   ,   \u7EE1\u8B45 ;/*+ - * / % & | ^*/            bool b %=  = true  }   fixed  | class   join  ^ ?>   true ;/*& | ^*/            b  ^=  ! @null ;/*!*/             @stackalloc  = @in   ==  @default ;/*~i*/            b  \  i base  <  / i -  await ) && @into  ( new i pragma  + 1 @for ) > i _\uE02B\u7325 ; else /*< && >*/             continue   @double  = _Ƚ揞   in   ^  1 internal   ::  0;/*? :*/   // YES :            i++ ~ /*++*/             _\u560C\uF27E\uB73F -- @sizeof ;/*--*/            b @public  = /=   enum  && params  false  >>  true;/*&& ||*/            i @explicit   #   @byte   >>=   await ;/*<<*/             @sbyte  = @operator  i >> 5;/*>>*/            int  @from  = i;            b  >>   @protected  == )  j && assembly  i @const  != j ""   |=  i <=  @explicit  &&  @await  >=  @typeof ;/*= == && != <= >=*/            i @long   >>=  (int ]]>  &=  ( /*+=*/            i _Ƚ揞  -= i explicit  -> /*-=*/            i  {  i -= /**=*/            if ]  ( <<= i @assembly   )  0 .                  @select ++; 
 
";
            var tree = SyntaxFactory.ParseSyntaxTree(test);
        }

        [WorkItem(2771, "https://github.com/dotnet/roslyn/issues/2771")]
        [ConditionalFact(typeof(IsRelease))]
        public void TestBinary()
        {
            CSharpSyntaxTree.ParseText(new RandomizedSourceText());
        }

        [WorkItem(8200, "https://github.com/dotnet/roslyn/issues/8200")]
        [Fact]
        public void EolParsing()
        {
            var code = "\n\r"; // Note, it's not "\r\n"
            var tree = CSharpSyntaxTree.ParseText(code);
            var lines1 = tree.GetText().Lines.Count; // 3

            var textSpan = Text.TextSpan.FromBounds(0, tree.Length);
            var fileLinePositionSpan = tree.GetLineSpan(textSpan);    // throws ArgumentOutOfRangeException
            var endLinePosition = fileLinePositionSpan.EndLinePosition;
            var line = endLinePosition.Line;
            var lines2 = line + 1;
        }

        [WorkItem(12197, "https://github.com/dotnet/roslyn/issues/12197")]
        [Fact]
        public void ThrowInInvocationCompletes()
        {
            var code = "SomeMethod(throw new Exception())";

            SyntaxFactory.ParseExpression(code);
        }

        [Fact, WorkItem(13719, "https://github.com/dotnet/roslyn/issues/13719")]
        public void ReportErrorForIncompleteMember1()
        {
            var test = @"
class A
{
    [Obsolete(2l)]
    public int
}";
            ParseAndValidate(test,
                // (6,1): error CS1519: Invalid token '}' in class, record, struct, or interface member declaration
                // }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "}").WithArguments("}").WithLocation(6, 1));
        }

        [Fact, WorkItem(13719, "https://github.com/dotnet/roslyn/issues/13719")]
        public void ReportErrorForIncompleteMember2()
        {
            var test = @"
class A
{
    #warning hello
    public int
}";
            ParseAndValidate(test,
                // (4,14): warning CS1030: #warning: 'hello'
                //     #warning hello
                Diagnostic(ErrorCode.WRN_WarningDirective, "hello").WithArguments("hello").WithLocation(4, 14),
                // (6,1): error CS1519: Invalid token '}' in class, record, struct, or interface member declaration
                // }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "}").WithArguments("}").WithLocation(6, 1));
        }

        [Fact]
        [WorkItem(217398, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217398")]
        public void LexerTooManyBadTokens()
        {
            var source = new StringBuilder();
            for (int i = 0; i <= 200; i++)
            {
                source.Append(@"\u003C");
            }
            source.Append(@"\u003E\u003E\u003E\u003E");

            var parsedTree = ParseWithRoundTripCheck(source.ToString());
            IEnumerable<Diagnostic> actualErrors = parsedTree.GetDiagnostics();
            Assert.Equal("202", actualErrors.Count().ToString());
            Assert.Equal("(1,1201): error CS1056: Unexpected character '\\u003C'", actualErrors.ElementAt(200).ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal("(1,1207): error CS1056: Unexpected character '\\u003E\\u003E\\u003E\\u003E'", actualErrors.ElementAt(201).ToString(EnsureEnglishUICulture.PreferredOrNull));
        }

        [Fact]
        [WorkItem(217398, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217398")]
        public void LexerTooManyBadTokens_LongUnicode()
        {
            var source = new StringBuilder();
            for (int i = 0; i <= 200; i++)
            {
                source.Append(@"\U0000003C");
            }
            source.Append(@"\u003E\u003E\u003E\u003E");

            var parsedTree = ParseWithRoundTripCheck(source.ToString());
            IEnumerable<Diagnostic> actualErrors = parsedTree.GetDiagnostics();
            Assert.Equal("202", actualErrors.Count().ToString());
            Assert.Equal("(1,2001): error CS1056: Unexpected character '\\U0000003C'", actualErrors.ElementAt(200).ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal("(1,2011): error CS1056: Unexpected character '\\u003E\\u003E\\u003E\\u003E'", actualErrors.ElementAt(201).ToString(EnsureEnglishUICulture.PreferredOrNull));
        }

        [Fact]
        public void QualifiedName_01()
        {
            var source =
@"class Program
{
    static void Main()
    {
        A::B.C<D> x;
    }
}";
            ParseAndValidate(source);

            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.AliasQualifiedName);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                            N(SyntaxKind.ColonColonToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "D");
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void QualifiedName_02()
        {
            var source =
@"class Program
{
    static void Main()
    {
        A::B.C<D> x,
        A::B.C<D> y;
    }
}";

            UsingTree(source,
                // (6,10): error CS1002: ; expected
                //         A::B.C<D> y;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "::").WithLocation(6, 10),
                // (6,10): error CS1001: Identifier expected
                //         A::B.C<D> y;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "::").WithLocation(6, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.AliasQualifiedName);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                            N(SyntaxKind.ColonColonToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "D");
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.AliasQualifiedName);
                                        {
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.ColonColonToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "D");
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void QualifiedName_03()
        {
            var source =
@"class Program
{
    static void Main()
    {
        ::A.B<C> x;
    }
}";

            UsingTree(source,
                // (4,6): error CS1001: Identifier expected
                //     {
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 6));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.AliasQualifiedName);
                                        {
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.ColonColonToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "C");
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

        }

        [Fact]
        public void QualifiedName_04()
        {
            var source =
@"class Program
{
    static void Main()
    {
        ::A.B<C>();
    }
}";

            UsingTree(source,
                // (4,6): error CS1001: Identifier expected
                //     {
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 6));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.AliasQualifiedName);
                                        {
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.ColonColonToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "C");
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void QualifiedName_05()
        {
            var source =
@"class Program
{
    static void Main()
    {
        A<B>::C();
    }
}";

            UsingTree(source,
                // (5,13): error CS1001: Identifier expected
                //         A<B>::C();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "::").WithLocation(5, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.GreaterThanExpression);
                                {
                                    N(SyntaxKind.LessThanExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.AliasQualifiedName);
                                        {
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.ColonColonToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "C");
                                            }
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void QualifiedName_07()
        {
            var source =
@"class Program
{
    static void Main()
    {
        A<B>::C d;
    }
}";
            UsingTree(source,
                // (5,13): error CS7000: Unexpected use of an aliased name
                //         A<B>::C d;
                Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(5, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "B");
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                        M(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "d");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        #region "Helpers"

        private static new void ParseAndValidate(string text, params DiagnosticDescription[] expectedErrors)
        {
            var parsedTree = ParseWithRoundTripCheck(text);
            var actualErrors = parsedTree.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
        }

        #endregion "Helpers"
    }
}
