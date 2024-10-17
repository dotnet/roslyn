// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using System;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    [Trait(Traits.Feature, Traits.Features.Formatting)]
    public class FormattingEngineTriviaTests : CSharpFormattingTestBase
    {
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31130")]
        public async Task PreprocessorNullable()
        {
            var content = @"
    #nullable
class C
{
    #nullable     enable
    void Method()
    {
        #nullable    disable
    }
}";

            var expected = @"
#nullable
class C
{
#nullable enable
    void Method()
    {
#nullable disable
    }
}";
            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task PreprocessorInEmptyFile()
        {
            var content = @"
                    
            #line 1000
        #error
                        ";

            var expected = @"

#line 1000
#error
";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment1()
        {
            var content = @"             // single line comment
            class C {           }";

            var expected = @"// single line comment
class C { }";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment2()
        {
            var content = @"class C 
{
                // single line comment
    int i;
}";

            var expected = @"class C
{
    // single line comment
    int i;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment3()
        {
            var content = @"class C 
{
                // single line comment
}";

            var expected = @"class C
{
    // single line comment
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment4()
        {
            var content = @"class C 
{
                // single line comment
//  single line comment 2
    void Method() { }
}";

            var expected = @"class C
{
    // single line comment
    //  single line comment 2
    void Method() { }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment5()
        {
            var content = @"class C 
{
    void Method() { 
    // single line comment
    //  single line comment 2
}
}";

            var expected = @"class C
{
    void Method()
    {
        // single line comment
        //  single line comment 2
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment6()
        {
            var content = @"class C 
{
    void Method() { 
    // single line comment
    //  single line comment 2
        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        // single line comment
        //  single line comment 2
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment7()
        {
            var content = @"class C 
{
    void Method() { 
    // single line comment

        int i = 10;

    //  single line comment 2
}
}";

            var expected = @"class C
{
    void Method()
    {
        // single line comment

        int i = 10;

        //  single line comment 2
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment8()
        {
            var content = @"class C 
{
    void Method() { 
        /* multiline comment */

        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        /* multiline comment */

        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment9()
        {
            var content = @"class C 
{
    void Method() { 
        /* multiline comment */

        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        /* multiline comment */

        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment10()
        {
            var content = @"class C 
{
    void Method() { 
        /* multiline comment */

        int i = 10;
/* multiline comment */
}
}";

            var expected = @"class C
{
    void Method()
    {
        /* multiline comment */

        int i = 10;
        /* multiline comment */
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment11()
        {
            var content = @"class C 
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
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment12()
        {
            var content = @"class C 
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
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment13()
        {
            var content = @"class C 
{
    void Method() { // test
        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    { // test
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment14()
        {
            var content = @"class C 
{
    void Method() { // test
                    // test 2
                    // test 3
        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    { // test
      // test 2
      // test 3
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment15()
        {
            var content = @"class C 
{
    void Method() { /* test */
        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    { /* test */
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment16()
        {
            var content = @"class C 
{
    void Method() { /* test 
                     *      
                     */         
        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    { /* test 
                     *      
                     */
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment17()
        {
            var content = @"class C 
{
    void Method() { 
                    /* test 
                     *      
                     */         // test
        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        /* test 
         *      
         */         // test
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content, true);
        }

        [Fact]
        public async Task Comment18()
        {
            var content = @"class C 
{
    void Method() { 
                    /* test 
                     *      
                     */         // test     
                                // test 2       
        int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        /* test 
         *      
         */         // test     
                    // test 2       
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment19()
        {
            var content = @"class C 
{
    void Method() { 
                    /* test 
                     *      
                     */         /* test 2
                                 *
                                 */
        int i = 10;
}
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment20()
        {
            var content = @"class C 
{
    void Method() { 
        int i = 10;
                    /* test 
                     *      
                     */         /* test 2
                                 *
                                 */
}
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        // for now, formatting engine doesn't re-indent token if the indentation line contains noisy
        // chars
        [Fact]
        public async Task Comment21()
        {
            var content = @"class C 
{
    void Method() { 
                            /* */ int i = 10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        /* */
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        // for now, formatting engine doesn't re-indent token if the indentation line contains noisy
        // chars
        [Fact]
        public async Task Comment22()
        {
            var content = @"class C 
{
    void Method() { 
                            int i = 
                                /* */ 10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        int i =
            /* */ 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment23()
        {
            var content = @"class C 
{
    void Method() { 
                            int /* */ i             = /* */         10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        int /* */ i = /* */         10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment24()
        {
            var content = @"class C 
{
    void Method() {     
        /*
         */   int i             =          10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        /*
         */
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment1()
        {
            var content = @"class C 
{
    void Method() {     
                                /**
                                 */   
                int i             =          10;
}
}";

            var expected = @"class C
{
    void Method()
    {
        /**
         */
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment2()
        {
            var content = @"class C 
{
    void Method() {     /**
                         */   
                int i             =          10;
}
}";

            var expected = @"class C
{
    void Method()
    {     /**
                         */
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment3()
        {
            var content = @"class C 
{
    void Method() {     
                int i             =          10;
                        /**
                         */
}
}";

            var expected = @"class C
{
    void Method()
    {
        int i = 10;
        /**
         */
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment4()
        {
            var content = @"class C 
{
    void Method() {     
                int i             =          10; /**                
                         */
}
}";

            var expected = @"class C
{
    void Method()
    {
        int i = 10; /**                
                         */
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment5()
        {
            var content = @"class C 
{
    void Method() {     
                int i             =          10; /** */
}
}";

            var expected = @"class C
{
    void Method()
    {
        int i = 10; /** */
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment6()
        {
            var content = @"class C 
{
    void Method() {     
                int i /** */            =     
                    /** */ 10; 
}
}";

            var expected = @"class C
{
    void Method()
    {
        int i /** */            =
            /** */ 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment7()
        {
            var content = @"class C 
{
    void Method() {     ///
                        ///
                int i = 10; 
}
}";

            var expected = @"class C
{
    void Method()
    {     ///
          ///
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment8()
        {
            var content = @"class C 
{
    void Method() {     
                        ///
                        ///
                int i = 10; 
}
}";

            var expected = @"class C
{
    void Method()
    {
        ///
        ///
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment9()
        {
            var content = @"class C 
{
    void Method() {     
                int i = 10; 
///
                        ///
}
}";

            var expected = @"class C
{
    void Method()
    {
        int i = 10;
        ///
        ///
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment10()
        {
            var content = @"class C 
{
    void Method() {     
///
/**
 */ ///
    ///
                int i = 10; 
}
}";

            var expected = @"class C
{
    void Method()
    {
        ///
        /**
         */ ///
            ///
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment11()
        {
            var content = @"class C 
{
    void Method() {     
///
/**
 */ /**
      *
      */
                int i = 10; 
}
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task DocComment12()
        {
            var content = @"class C 
{
    void Method() {     
///
/**
 */ /** */
                int i = 10; 
}
}";

            var expected = @"class C
{
    void Method()
    {
        ///
        /**
         */ /** */
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task MixCommentAndDocComment1()
        {
            var content = @"class C 
{
    void Method() {     
//
/**
 */ /*          
     */ //
                int i = 10; 
}
}";

            var expected = @"class C
{
    void Method()
    {
        //
        /**
         */ /*          
             */ //
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task MixCommentAndDocComment2()
        {
            var content = @"class C 
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
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task MixCommentAndDocComment3()
        {
            var content = @"class C 
{
    void Method() {     
            // test
            // test 2

            /// <text></text>
            /// test 3
            ///

int i = 10; 
}
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task MixCommentAndDocComment4()
        {
            var content = @"class C 
{
            /// <text></text>
            /// test 3
            ///
void Method() {     
            // test
            // test 2

int i = 10; 
}
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Preprocessor1()
        {
            var content = @"class C 
{
                    #if true
                    #endif
void Method() {     
int i = 10; 
}
}";

            var expected = @"class C
{
#if true
#endif
    void Method()
    {
        int i = 10;
    }
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Preprocessor2()
        {
            var content = @"class C 
{
                    #if true
void Method() {     
int i = 10; 
}
}
    #endif
";

            var expected = @"class C
{
#if true
    void Method()
    {
        int i = 10;
    }
}
#endif
";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Preprocessor3()
        {
            var content = @"class C 
{
                    #if true
void Method() {     
                #elif false
int i = 10; 
}
}
    #endif
}
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Preprocessor4()
        {
            var content = @"class C 
{
                    #if true
void Method() {     
}
                #elif false
int i = 10; 
}
    #endif
}
";

            var expected = @"class C
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
";

            // turn off transformation check - conditional directive preprocessor
            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Preprocessor5()
        {
            var content = @"class C 
{
                    #region Test
        int i = 10;
                    #endregion

void Method() {     
}
}
";

            var expected = @"class C
{
    #region Test
    int i = 10;
    #endregion

    void Method()
    {
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Preprocessor6()
        {
            var content = @"class C 
{
                    #region Test
        int i = 10;
                    #endregion

void Method() {     
}
}
";

            var expected = @"class C
{
    #region Test
    int i = 10;
    #endregion

    void Method()
    {
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Preprocessor7()
        {
            var content = @"class C 
{
                    #region Test
        int i = 10;
                    
void Method() {     
}
#endregion
}
";

            var expected = @"class C
{
    #region Test
    int i = 10;

    void Method()
    {
    }
    #endregion
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Preprocessor8()
        {
            var content = @"class C 
{
                    #region Test
        int i = 10;
                    
void Method() {     
#endregion
int i = 10;
}
}
";

            var expected = @"class C
{
    #region Test
    int i = 10;

    void Method()
    {
        #endregion
        int i = 10;
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task MixAll()
        {
            var content = @"class C 
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
";

            var expected = @"class C
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
";

            // turn off transformation check since it doesn't work for conditional directive yet.
            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
        public async Task Preprocessor9()
        {
            var content = @"class C 
{
void Method() {     
#region Myregion
            int a;
            if (true)
                a++;
            #endregion 
}
}
";

            var expected = @"class C
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
";

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
        public async Task Preprocessor10()
        {
            var content = @"class C 
{
void Method() {     
            int a;
            if (true)
                a++;
#region Myregion
}
}
";

            var expected = @"class C
{
    void Method()
    {
        int a;
        if (true)
            a++;
        #region Myregion
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537765")]
        public async Task Comment25()
        {
            var content = @"class C 
{
            void Goo()//method
{
    int x;//variable
double y;
                    }
}
";

            var expected = @"class C
{
    void Goo()//method
    {
        int x;//variable
        double y;
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537765")]
        public async Task Comment26()
        {
            var content = @"public class Class1
{
    void Goo()
    {
/**/int x;
    }
}";

            var expected = @"public class Class1
{
    void Goo()
    {
        /**/
        int x;
    }
}";

            await AssertFormatAsync(expected, content);
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
            var content = @"public class Class1
{
    void Goo()
    {
        //      
            
        // 
            
    }
}";

            var expected = @"public class Class1
{
    void Goo()
    {
        //      

        // 

    }
}";
            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task Comment29()
        {
            var content = @"public class Class1
{
    void Goo()
    {
        int			/**/ i = 10;
    }
}";

            var code = @"public class Class1
{
    void Goo()
    {
        int         /**/ i = 10;
    }
}";

            await AssertFormatAsync(code, content);
        }

        [Fact]
        public async Task Comment30()
        {
            var content = @"
// Test";

            var code = @"
// Test";

            await AssertFormatAsync(code, content);
        }

        [Fact]
        public async Task Comment31()
        {
            var content = @"/// <summary>
///
        /// </summary>
class Program
{
    static void Main(string[] args)
    {
    }
}
";

            var code = @"/// <summary>
///
/// </summary>
class Program
{
    static void Main(string[] args)
    {
    }
}
";

            await AssertFormatAsync(code, content);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538703")]
        public async Task Comment32()
        {
            var content = @"class Program
{
    ///<summary>
        ///     TestMethod
///</summary>
    void Method() { }
}
";

            var code = @"class Program
{
    ///<summary>
    ///     TestMethod
    ///</summary>
    void Method() { }
}
";

            await AssertFormatAsync(code, content);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542316")]
        public async Task CommentInExpression()
        {
            var content = @"using System;
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
";

            var code = @"using System;
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
";

            await AssertFormatAsync(code, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542546")]
        public async Task FormatInvalidCode_1()
        {
            var expected = @"> Roslyn.Utilities.dll!   Basic";
            var content = @">	Roslyn.Utilities.dll! 	Basic";
            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542546")]
        public async Task FormatInvalidCode_2()
        {
            var content = @">	Roslyn.Utilities.dll! Line 43 + 0x5 bytes	Basic";
            var expectedContent = @"> Roslyn.Utilities.dll! Line 43 + 0x5 bytes	Basic";
            await AssertFormatAsync(expectedContent, content);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
        public async Task EmbededStatement1()
        {
            var content = @"using System;
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
}";
            var expectedContent = @"using System;
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
}";
            await AssertFormatAsync(expectedContent, content);
        }

        [Fact]
        public async Task RefKeywords()
        {
            var content = @"class C 
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
}";

            var expected = @"class C
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
}";

            await AssertFormatAsync(expected, content);
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
            var expected = "class C\n{\n}";

            Assert.Equal(expected, actual);
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
            var expected = @"#region Assembly mscorlib
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
            Assert.Equal(expected, actual);
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

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/72966")]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InlineComment(bool useTabs)
        {
            var content = @"enum E
{
    a, //a
    //b,
    c,

    d, //d
    //e,
    //f,

    g, /*g*/
    //h,
}

class C
{
    public void M()
    {
        int x = 1; //x
        //int y = 2;
    }
}";

            if (useTabs)
            {
                content = content.Replace("    ", "\t");
            }

            var optionSet = useTabs ? new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } } : null;

            await AssertNoFormattingChangesAsync(content, changedOptionSet: optionSet);
        }
    }
}
