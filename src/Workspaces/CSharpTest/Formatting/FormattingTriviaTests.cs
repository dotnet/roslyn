// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class FormattingEngineTriviaTests : CSharpFormattingTestBase
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31130")]
    public async Task PreprocessorNullable()
    {
        await AssertFormatAsync(@"
#nullable
class C
{
#nullable enable
    void Method()
    {
#nullable disable
    }
}", @"
    #nullable
class C
{
    #nullable     enable
    void Method()
    {
        #nullable    disable
    }
}");
    }

    [Fact]
    public async Task PreprocessorInEmptyFile()
    {
        await AssertFormatAsync(@"

#line 1000
#error
", @"
                    
            #line 1000
        #error
                        ");
    }

    [Fact]
    public async Task Comment1()
    {
        await AssertFormatAsync(@"// single line comment
class C { }", @"             // single line comment
            class C {           }");
    }

    [Fact]
    public async Task Comment2()
    {
        await AssertFormatAsync(@"class C
{
    // single line comment
    int i;
}", @"class C 
{
                // single line comment
    int i;
}");
    }

    [Fact]
    public async Task Comment3()
    {
        await AssertFormatAsync(@"class C
{
    // single line comment
}", @"class C 
{
                // single line comment
}");
    }

    [Fact]
    public async Task Comment4()
    {
        await AssertFormatAsync(@"class C
{
    // single line comment
    //  single line comment 2
    void Method() { }
}", @"class C 
{
                // single line comment
//  single line comment 2
    void Method() { }
}");
    }

    [Fact]
    public async Task Comment5()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        // single line comment
        //  single line comment 2
    }
}", @"class C 
{
    void Method() { 
    // single line comment
    //  single line comment 2
}
}");
    }

    [Fact]
    public async Task Comment6()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        // single line comment
        //  single line comment 2
        int i = 10;
    }
}", @"class C 
{
    void Method() { 
    // single line comment
    //  single line comment 2
        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment7()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        // single line comment

        int i = 10;

        //  single line comment 2
    }
}", @"class C 
{
    void Method() { 
    // single line comment

        int i = 10;

    //  single line comment 2
}
}");
    }

    [Fact]
    public async Task Comment8()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* multiline comment */

        int i = 10;
    }
}", @"class C 
{
    void Method() { 
        /* multiline comment */

        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment9()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* multiline comment */

        int i = 10;
    }
}", @"class C 
{
    void Method() { 
        /* multiline comment */

        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment10()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* multiline comment */

        int i = 10;
        /* multiline comment */
    }
}", @"class C 
{
    void Method() { 
        /* multiline comment */

        int i = 10;
/* multiline comment */
}
}");
    }

    [Fact]
    public async Task Comment11()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* 
         * multiline comment 
         */

        int i = 10;

        /* 
         * multiline comment 
         */
    }
}", @"class C 
{
    void Method() { 
                    /* 
                     * multiline comment 
                     */

        int i = 10;

/* 
 * multiline comment 
 */
}
}");
    }

    [Fact]
    public async Task Comment12()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* 
* multiline comment 
*/

        int i = 10;

        /* 
* multiline comment 
*/
    }
}", @"class C 
{
    void Method() { 
                                                        /* 
                     * multiline comment 
                     */

        int i = 10;

                            /* 
             * multiline comment 
             */
}
}");
    }

    [Fact]
    public async Task Comment13()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    { // test
        int i = 10;
    }
}", @"class C 
{
    void Method() { // test
        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment14()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    { // test
      // test 2
      // test 3
        int i = 10;
    }
}", @"class C 
{
    void Method() { // test
                    // test 2
                    // test 3
        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment15()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    { /* test */
        int i = 10;
    }
}", @"class C 
{
    void Method() { /* test */
        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment16()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    { /* test 
                     *      
                     */
        int i = 10;
    }
}", @"class C 
{
    void Method() { /* test 
                     *      
                     */         
        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment17()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* test 
         *      
         */         // test
        int i = 10;
    }
}", @"class C 
{
    void Method() { 
                    /* test 
                     *      
                     */         // test
        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment18()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* test 
         *      
         */         // test     
                    // test 2       
        int i = 10;
    }
}", @"class C 
{
    void Method() { 
                    /* test 
                     *      
                     */         // test     
                                // test 2       
        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment19()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* test 
         *      
         */         /* test 2
                     *
                     */
        int i = 10;
    }
}", @"class C 
{
    void Method() { 
                    /* test 
                     *      
                     */         /* test 2
                                 *
                                 */
        int i = 10;
}
}");
    }

    [Fact]
    public async Task Comment20()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int i = 10;
        /* test 
         *      
         */         /* test 2
                     *
                     */
    }
}", @"class C 
{
    void Method() { 
        int i = 10;
                    /* test 
                     *      
                     */         /* test 2
                                 *
                                 */
}
}");
    }

    // for now, formatting engine doesn't re-indent token if the indentation line contains noisy
    // chars
    [Fact]
    public async Task Comment21()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /* */
        int i = 10;
    }
}", @"class C 
{
    void Method() { 
                            /* */ int i = 10;
}
}");
    }

    // for now, formatting engine doesn't re-indent token if the indentation line contains noisy
    // chars
    [Fact]
    public async Task Comment22()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int i =
            /* */ 10;
    }
}", @"class C 
{
    void Method() { 
                            int i = 
                                /* */ 10;
}
}");
    }

    [Fact]
    public async Task Comment23()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int /* */ i = /* */         10;
    }
}", @"class C 
{
    void Method() { 
                            int /* */ i             = /* */         10;
}
}");
    }

    [Fact]
    public async Task Comment24()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /*
         */
        int i = 10;
    }
}", @"class C 
{
    void Method() {     
        /*
         */   int i             =          10;
}
}");
    }

    [Fact]
    public async Task DocComment1()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /**
         */
        int i = 10;
    }
}", @"class C 
{
    void Method() {     
                                /**
                                 */   
                int i             =          10;
}
}");
    }

    [Fact]
    public async Task DocComment2()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {     /**
                         */
        int i = 10;
    }
}", @"class C 
{
    void Method() {     /**
                         */   
                int i             =          10;
}
}");
    }

    [Fact]
    public async Task DocComment3()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int i = 10;
        /**
         */
    }
}", @"class C 
{
    void Method() {     
                int i             =          10;
                        /**
                         */
}
}");
    }

    [Fact]
    public async Task DocComment4()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int i = 10; /**                
                         */
    }
}", @"class C 
{
    void Method() {     
                int i             =          10; /**                
                         */
}
}");
    }

    [Fact]
    public async Task DocComment5()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int i = 10; /** */
    }
}", @"class C 
{
    void Method() {     
                int i             =          10; /** */
}
}");
    }

    [Fact]
    public async Task DocComment6()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int i /** */            =
            /** */ 10;
    }
}", @"class C 
{
    void Method() {     
                int i /** */            =     
                    /** */ 10; 
}
}");
    }

    [Fact]
    public async Task DocComment7()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {     ///
          ///
        int i = 10;
    }
}", @"class C 
{
    void Method() {     ///
                        ///
                int i = 10; 
}
}");
    }

    [Fact]
    public async Task DocComment8()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        ///
        ///
        int i = 10;
    }
}", @"class C 
{
    void Method() {     
                        ///
                        ///
                int i = 10; 
}
}");
    }

    [Fact]
    public async Task DocComment9()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int i = 10;
        ///
        ///
    }
}", @"class C 
{
    void Method() {     
                int i = 10; 
///
                        ///
}
}");
    }

    [Fact]
    public async Task DocComment10()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        ///
        /**
         */ ///
            ///
        int i = 10;
    }
}", @"class C 
{
    void Method() {     
///
/**
 */ ///
    ///
                int i = 10; 
}
}");
    }

    [Fact]
    public async Task DocComment11()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        ///
        /**
         */ /**
              *
              */
        int i = 10;
    }
}", @"class C 
{
    void Method() {     
///
/**
 */ /**
      *
      */
                int i = 10; 
}
}");
    }

    [Fact]
    public async Task DocComment12()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        ///
        /**
         */ /** */
        int i = 10;
    }
}", @"class C 
{
    void Method() {     
///
/**
 */ /** */
                int i = 10; 
}
}");
    }

    [Fact]
    public async Task MixCommentAndDocComment1()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        //
        /**
         */ /*          
             */ //
        int i = 10;
    }
}", @"class C 
{
    void Method() {     
//
/**
 */ /*          
     */ //
                int i = 10; 
}
}");
    }

    [Fact]
    public async Task MixCommentAndDocComment2()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        /*
         *
         */ /**          
             *
             */ /*
         *
         */ ///
            ///
        int i = 10;
    }
}", @"class C 
{
    void Method() {     
/*
 *
 */ /**          
     *
     */ /*
 *
 */ ///
    ///
                int i = 10; 
}
}");
    }

    [Fact]
    public async Task MixCommentAndDocComment3()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        // test
        // test 2

        /// <text></text>
        /// test 3
        ///

        int i = 10;
    }
}", @"class C 
{
    void Method() {     
            // test
            // test 2

            /// <text></text>
            /// test 3
            ///

int i = 10; 
}
}");
    }

    [Fact]
    public async Task MixCommentAndDocComment4()
    {
        await AssertFormatAsync(@"class C
{
    /// <text></text>
    /// test 3
    ///
    void Method()
    {
        // test
        // test 2

        int i = 10;
    }
}", @"class C 
{
            /// <text></text>
            /// test 3
            ///
void Method() {     
            // test
            // test 2

int i = 10; 
}
}");
    }

    [Fact]
    public async Task Preprocessor1()
    {
        await AssertFormatAsync(@"class C
{
#if true
#endif
    void Method()
    {
        int i = 10;
    }
}", @"class C 
{
                    #if true
                    #endif
void Method() {     
int i = 10; 
}
}");
    }

    [Fact]
    public async Task Preprocessor2()
    {
        await AssertFormatAsync(@"class C
{
#if true
    void Method()
    {
        int i = 10;
    }
}
#endif
", @"class C 
{
                    #if true
void Method() {     
int i = 10; 
}
}
    #endif
");
    }

    [Fact]
    public async Task Preprocessor3()
    {
        await AssertFormatAsync(@"class C
{
#if true
    void Method()
    {
#elif false
int i = 10; 
}
}
#endif
    }
}", @"class C 
{
                    #if true
void Method() {     
                #elif false
int i = 10; 
}
}
    #endif
}
}");
    }

    [Fact]
    public async Task Preprocessor4()
    {

        // turn off transformation check - conditional directive preprocessor
        await AssertFormatAsync(@"class C
{
#if true
    void Method()
    {
    }
#elif false
int i = 10; 
}
#endif
}
", @"class C 
{
                    #if true
void Method() {     
}
                #elif false
int i = 10; 
}
    #endif
}
");
    }

    [Fact]
    public async Task Preprocessor5()
    {
        await AssertFormatAsync(@"class C
{
    #region Test
    int i = 10;
    #endregion

    void Method()
    {
    }
}
", @"class C 
{
                    #region Test
        int i = 10;
                    #endregion

void Method() {     
}
}
");
    }

    [Fact]
    public async Task Preprocessor6()
    {
        await AssertFormatAsync(@"class C
{
    #region Test
    int i = 10;
    #endregion

    void Method()
    {
    }
}
", @"class C 
{
                    #region Test
        int i = 10;
                    #endregion

void Method() {     
}
}
");
    }

    [Fact]
    public async Task Preprocessor7()
    {
        await AssertFormatAsync(@"class C
{
    #region Test
    int i = 10;

    void Method()
    {
    }
    #endregion
}
", @"class C 
{
                    #region Test
        int i = 10;
                    
void Method() {     
}
#endregion
}
");
    }

    [Fact]
    public async Task Preprocessor8()
    {
        await AssertFormatAsync(@"class C
{
    #region Test
    int i = 10;

    void Method()
    {
        #endregion
        int i = 10;
    }
}
", @"class C 
{
                    #region Test
        int i = 10;
                    
void Method() {     
#endregion
int i = 10;
}
}
");
    }

    [Fact]
    public async Task MixAll()
    {

        // turn off transformation check since it doesn't work for conditional directive yet.
        await AssertFormatAsync(@"class C
{
    #region Test

#if true

    // test
    ///
    ///
    int i = 10;

#else
void Method() {     
}
#endif
    #endregion
}
", @"class C 
{
                    #region Test

            #if true

                        // test
                ///
                ///
        int i = 10;

            #else                    
void Method() {     
}
    #endif
#endregion
}
");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
    public async Task Preprocessor9()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        #region Myregion
        int a;
        if (true)
            a++;
        #endregion
    }
}
", @"class C 
{
void Method() {     
#region Myregion
            int a;
            if (true)
                a++;
            #endregion 
}
}
");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
    public async Task Preprocessor10()
    {
        await AssertFormatAsync(@"class C
{
    void Method()
    {
        int a;
        if (true)
            a++;
        #region Myregion
    }
}
", @"class C 
{
void Method() {     
            int a;
            if (true)
                a++;
#region Myregion
}
}
");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537765")]
    public async Task Comment25()
    {
        await AssertFormatAsync(@"class C
{
    void Goo()//method
    {
        int x;//variable
        double y;
    }
}
", @"class C 
{
            void Goo()//method
{
    int x;//variable
double y;
                    }
}
");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537765")]
    public async Task Comment26()
    {
        await AssertFormatAsync(@"public class Class1
{
    void Goo()
    {
        /**/
        int x;
    }
}", @"public class Class1
{
    void Goo()
    {
/**/int x;
    }
}");
    }

    [Fact]
    public async Task Comment27()
    {
        var content = @"public class Class1
{
    void Goo()
    {
        //      
        // 
    }
}";

        await AssertFormatAsync(content, content);
    }

    [Fact]
    public async Task Comment28()
    {
        await AssertFormatAsync(@"public class Class1
{
    void Goo()
    {
        //      

        // 

    }
}", @"public class Class1
{
    void Goo()
    {
        //      
            
        // 
            
    }
}");
    }

    [Fact]
    public async Task Comment29()
    {
        await AssertFormatAsync(@"public class Class1
{
    void Goo()
    {
        int         /**/ i = 10;
    }
}", @"public class Class1
{
    void Goo()
    {
        int			/**/ i = 10;
    }
}");
    }

    [Fact]
    public async Task Comment30()
    {
        await AssertFormatAsync(@"
// Test", @"
// Test");
    }

    [Fact]
    public async Task Comment31()
    {
        await AssertFormatAsync(@"/// <summary>
///
/// </summary>
class Program
{
    static void Main(string[] args)
    {
    }
}
", @"/// <summary>
///
        /// </summary>
class Program
{
    static void Main(string[] args)
    {
    }
}
");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538703")]
    public async Task Comment32()
    {
        await AssertFormatAsync(@"class Program
{
    ///<summary>
    ///     TestMethod
    ///</summary>
    void Method() { }
}
", @"class Program
{
    ///<summary>
        ///     TestMethod
///</summary>
    void Method() { }
}
");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542316")]
    public async Task CommentInExpression()
    {
        await AssertFormatAsync(@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        CreateCompilationWithMscorlib(source).VerifyDiagnostics(
            // (10,30): error CS0455: Type parameter 'X' inherits conflicting constraints 'B' and 'A'
            Diagnostic(ErrorCode.ERR_BaseConstraintConflict, ""X"").WithArguments(""X"", ""B"", ""A"").WithLocation(10, 30));
    }
}
", @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                            // (10,30): error CS0455: Type parameter 'X' inherits conflicting constraints 'B' and 'A'
            Diagnostic(ErrorCode.ERR_BaseConstraintConflict, ""X"").WithArguments(""X"", ""B"", ""A"").WithLocation(10, 30));
    }
}
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542546")]
    public async Task FormatInvalidCode_1()
    {
        await AssertFormatAsync(@"> Roslyn.Utilities.dll!   Basic", @">	Roslyn.Utilities.dll! 	Basic");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542546")]
    public async Task FormatInvalidCode_2()
    {
        await AssertFormatAsync(@"> Roslyn.Utilities.dll! Line 43 + 0x5 bytes	Basic", @">	Roslyn.Utilities.dll! Line 43 + 0x5 bytes	Basic");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
    public async Task EmbededStatement1()
    {
        await AssertFormatAsync(@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        #region Myregion
        int a;
        if (true)
            a++;
        #endregion
    }
}", @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        #region Myregion
        int a;
        if (true)
            a++;
            #endregion
    }
}");
    }

    [Fact]
    public async Task RefKeywords()
    {
        await AssertFormatAsync(@"class C
{
    static void Main(string[] args)
    {
        int i = 1;
        TypedReference tr = __makeref(i);
        Type t = __reftype(tr
            );
        int j = __refvalue(tr,
            int
            );
    }
}", @"class C 
{
    static void Main(string[] args)
    {
        int i = 1;
        TypedReference tr = __makeref(   i );
        Type t = __reftype( tr 
            );
        int j = __refvalue(            tr  ,
            int
            );
    }
}");
    }

    [Fact]
    public void NewLineOptions_LineFeedOnly()
    {
        using var workspace = new AdhocWorkspace();
        var tree = SyntaxFactory.ParseCompilationUnit("class C\r\n{\r\n}");

        // replace all EOL trivia with elastic markers to force the formatter to add EOL back
        tree = tree.ReplaceTrivia(tree.DescendantTrivia().Where(tr => tr.IsKind(SyntaxKind.EndOfLineTrivia)), (o, r) => SyntaxFactory.ElasticMarker);

        var options = new CSharpSyntaxFormattingOptions()
        {
            LineFormatting = new() { NewLine = "\n" }
        };

        var formatted = Formatter.Format(tree, workspace.Services.SolutionServices, options, CancellationToken.None);

        var actual = formatted.ToFullString();
        Assert.Equal("class C\n{\n}", actual);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4019")]
    public void FormatWithTabs()
    {
        var code = @"#region Assembly mscorlib
// C:\
#endregion

using System.Collections;

class F
{
    string s;
}";
        var tree = SyntaxFactory.ParseCompilationUnit(code);
        var newLine = Environment.NewLine;

        tree = tree.ReplaceTokens(tree.DescendantTokens(descendIntoTrivia: true)
                                      .Where(tr => tr.IsKind(SyntaxKind.EndOfDirectiveToken)), (o, r) => o.WithTrailingTrivia(o.LeadingTrivia.Add(SyntaxFactory.ElasticEndOfLine(newLine)))
                                                                                                          .WithLeadingTrivia(SyntaxFactory.TriviaList())
                                                                                                          .WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation));

        using var workspace = new AdhocWorkspace();

        var options = new CSharpSyntaxFormattingOptions()
        {
            LineFormatting = new() { UseTabs = true, NewLine = newLine }
        };

        var formatted = Formatter.Format(tree, workspace.Services.SolutionServices, options, CancellationToken.None);

        var actual = formatted.ToFullString();
        Assert.Equal(@"#region Assembly mscorlib
// C:\
#endregion

using System.Collections;

class F
{
	string s;
}", actual);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39351")]
    public async Task SingleLineComment_AtEndOfFile_DoesNotAddNewLine()
    {
        await AssertNoFormattingChangesAsync(@"class Program { }

// Test");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39351")]
    public async Task MultiLineComment_AtEndOfFile_DoesNotAddNewLine()
    {
        await AssertNoFormattingChangesAsync(@"class Program { }

/* Test */");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39351")]
    public async Task DocComment_AtEndOfFile_DoesNotAddNewLine()
    {
        await AssertNoFormattingChangesAsync(@"class Program { }

/// Test");
    }
}
