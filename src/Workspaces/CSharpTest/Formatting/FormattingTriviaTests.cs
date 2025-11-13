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
    public Task PreprocessorNullable()
        => AssertFormatAsync("""
            #nullable
            class C
            {
            #nullable enable
                void Method()
                {
            #nullable disable
                }
            }
            """, """
                #nullable
            class C
            {
                #nullable     enable
                void Method()
                {
                    #nullable    disable
                }
            }
            """);

    [Fact]
    public Task PreprocessorInEmptyFile()
        => AssertFormatAsync("""
            #line 1000
            #error
            """, """
                #line 1000
            #error
            """);

    [Fact]
    public Task Comment1()
        => AssertFormatAsync("""
            // single line comment
            class C { }
            """, """
             // single line comment
            class C {           }
            """);

    [Fact]
    public Task Comment2()
        => AssertFormatAsync("""
            class C
            {
                // single line comment
                int i;
            }
            """, """
            class C 
            {
                            // single line comment
                int i;
            }
            """);

    [Fact]
    public Task Comment3()
        => AssertFormatAsync("""
            class C
            {
                // single line comment
            }
            """, """
            class C 
            {
                            // single line comment
            }
            """);

    [Fact]
    public Task Comment4()
        => AssertFormatAsync("""
            class C
            {
                // single line comment
                //  single line comment 2
                void Method() { }
            }
            """, """
            class C 
            {
                            // single line comment
            //  single line comment 2
                void Method() { }
            }
            """);

    [Fact]
    public Task Comment5()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    // single line comment
                    //  single line comment 2
                }
            }
            """, """
            class C 
            {
                void Method() { 
                // single line comment
                //  single line comment 2
            }
            }
            """);

    [Fact]
    public Task Comment6()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    // single line comment
                    //  single line comment 2
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { 
                // single line comment
                //  single line comment 2
                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment7()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    // single line comment

                    int i = 10;

                    //  single line comment 2
                }
            }
            """, """
            class C 
            {
                void Method() { 
                // single line comment

                    int i = 10;

                //  single line comment 2
            }
            }
            """);

    [Fact]
    public Task Comment8()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    /* multiline comment */

                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { 
                    /* multiline comment */

                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment9()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    /* multiline comment */

                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { 
                    /* multiline comment */

                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment10()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    /* multiline comment */

                    int i = 10;
                    /* multiline comment */
                }
            }
            """, """
            class C 
            {
                void Method() { 
                    /* multiline comment */

                    int i = 10;
            /* multiline comment */
            }
            }
            """);

    [Fact]
    public Task Comment11()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
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
            }
            """);

    [Fact]
    public Task Comment12()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
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
            }
            """);

    [Fact]
    public Task Comment13()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                { // test
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { // test
                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment14()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                { // test
                  // test 2
                  // test 3
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { // test
                                // test 2
                                // test 3
                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment15()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                { /* test */
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { /* test */
                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment16()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                { /* test 
                                 *      
                                 */
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { /* test 
                                 *      
                                 */         
                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment17()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    /* test 
                     *      
                     */         // test
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { 
                                /* test 
                                 *      
                                 */         // test
                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment18()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    /* test 
                     *      
                     */         // test     
                                // test 2       
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { 
                                /* test 
                                 *      
                                 */         // test     
                                            // test 2       
                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment19()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
            {
                void Method() { 
                                /* test 
                                 *      
                                 */         /* test 2
                                             *
                                             */
                    int i = 10;
            }
            }
            """);

    [Fact]
    public Task Comment20()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
            {
                void Method() { 
                    int i = 10;
                                /* test 
                                 *      
                                 */         /* test 2
                                             *
                                             */
            }
            }
            """);

    // for now, formatting engine doesn't re-indent token if the indentation line contains noisy
    // chars
    [Fact]
    public Task Comment21()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    /* */
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() { 
                                        /* */ int i = 10;
            }
            }
            """);

    // for now, formatting engine doesn't re-indent token if the indentation line contains noisy
    // chars
    [Fact]
    public Task Comment22()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    int i =
                        /* */ 10;
                }
            }
            """, """
            class C 
            {
                void Method() { 
                                        int i = 
                                            /* */ 10;
            }
            }
            """);

    [Fact]
    public Task Comment23()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    int /* */ i = /* */         10;
                }
            }
            """, """
            class C 
            {
                void Method() { 
                                        int /* */ i             = /* */         10;
            }
            }
            """);

    [Fact]
    public Task Comment24()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    /*
                     */
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     
                    /*
                     */   int i             =          10;
            }
            }
            """);

    [Fact]
    public Task DocComment1()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    /**
                     */
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     
                                            /**
                                             */   
                            int i             =          10;
            }
            }
            """);

    [Fact]
    public Task DocComment2()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {     /**
                                     */
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     /**
                                     */   
                            int i             =          10;
            }
            }
            """);

    [Fact]
    public Task DocComment3()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    int i = 10;
                    /**
                     */
                }
            }
            """, """
            class C 
            {
                void Method() {     
                            int i             =          10;
                                    /**
                                     */
            }
            }
            """);

    [Fact]
    public Task DocComment4()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    int i = 10; /**                
                                     */
                }
            }
            """, """
            class C 
            {
                void Method() {     
                            int i             =          10; /**                
                                     */
            }
            }
            """);

    [Fact]
    public Task DocComment5()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    int i = 10; /** */
                }
            }
            """, """
            class C 
            {
                void Method() {     
                            int i             =          10; /** */
            }
            }
            """);

    [Fact]
    public Task DocComment6()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    int i /** */            =
                        /** */ 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     
                            int i /** */            =     
                                /** */ 10; 
            }
            }
            """);

    [Fact]
    public Task DocComment7()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {     ///
                      ///
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     ///
                                    ///
                            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task DocComment8()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    ///
                    ///
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     
                                    ///
                                    ///
                            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task DocComment9()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    int i = 10;
                    ///
                    ///
                }
            }
            """, """
            class C 
            {
                void Method() {     
                            int i = 10; 
            ///
                                    ///
            }
            }
            """);

    [Fact]
    public Task DocComment10()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    ///
                    /**
                     */ ///
                        ///
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     
            ///
            /**
             */ ///
                ///
                            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task DocComment11()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
            {
                void Method() {     
            ///
            /**
             */ /**
                  *
                  */
                            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task DocComment12()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    ///
                    /**
                     */ /** */
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     
            ///
            /**
             */ /** */
                            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task MixCommentAndDocComment1()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    //
                    /**
                     */ /*          
                         */ //
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                void Method() {     
            //
            /**
             */ /*          
                 */ //
                            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task MixCommentAndDocComment2()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
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
            }
            """);

    [Fact]
    public Task MixCommentAndDocComment3()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
            {
                void Method() {     
                        // test
                        // test 2

                        /// <text></text>
                        /// test 3
                        ///

            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task MixCommentAndDocComment4()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
            {
                        /// <text></text>
                        /// test 3
                        ///
            void Method() {     
                        // test
                        // test 2

            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task Preprocessor1()
        => AssertFormatAsync("""
            class C
            {
            #if true
            #endif
                void Method()
                {
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                                #if true
                                #endif
            void Method() {     
            int i = 10; 
            }
            }
            """);

    [Fact]
    public Task Preprocessor2()
        => AssertFormatAsync("""
            class C
            {
            #if true
                void Method()
                {
                    int i = 10;
                }
            }
            #endif
            """, """
            class C 
            {
                                #if true
            void Method() {     
            int i = 10; 
            }
            }
                #endif
            """);

    [Fact]
    public Task Preprocessor3()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
            {
                                #if true
            void Method() {     
                            #elif false
            int i = 10; 
            }
            }
                #endif
            }
            }
            """);

    [Fact]
    public Task Preprocessor4()
        => AssertFormatAsync("""
            class C
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
            """, """
            class C 
            {
                                #if true
            void Method() {     
            }
                            #elif false
            int i = 10; 
            }
                #endif
            }
            """);

    [Fact]
    public Task Preprocessor5()
        => AssertFormatAsync("""
            class C
            {
                #region Test
                int i = 10;
                #endregion

                void Method()
                {
                }
            }
            """, """
            class C 
            {
                                #region Test
                    int i = 10;
                                #endregion

            void Method() {     
            }
            }
            """);

    [Fact]
    public Task Preprocessor6()
        => AssertFormatAsync("""
            class C
            {
                #region Test
                int i = 10;
                #endregion

                void Method()
                {
                }
            }
            """, """
            class C 
            {
                                #region Test
                    int i = 10;
                                #endregion

            void Method() {     
            }
            }
            """);

    [Fact]
    public Task Preprocessor7()
        => AssertFormatAsync("""
            class C
            {
                #region Test
                int i = 10;

                void Method()
                {
                }
                #endregion
            }
            """, """
            class C 
            {
                                #region Test
                    int i = 10;

            void Method() {     
            }
            #endregion
            }
            """);

    [Fact]
    public Task Preprocessor8()
        => AssertFormatAsync("""
            class C
            {
                #region Test
                int i = 10;

                void Method()
                {
                    #endregion
                    int i = 10;
                }
            }
            """, """
            class C 
            {
                                #region Test
                    int i = 10;

            void Method() {     
            #endregion
            int i = 10;
            }
            }
            """);

    [Fact]
    public Task MixAll()
        => AssertFormatAsync("""
            class C
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
            """, """
            class C 
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
    public Task Preprocessor9()
        => AssertFormatAsync("""
            class C
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
            """, """
            class C 
            {
            void Method() {     
            #region Myregion
                        int a;
                        if (true)
                            a++;
                        #endregion 
            }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
    public Task Preprocessor10()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    int a;
                    if (true)
                        a++;
                    #region Myregion
                }
            }
            """, """
            class C 
            {
            void Method() {     
                        int a;
                        if (true)
                            a++;
            #region Myregion
            }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537765")]
    public Task Comment25()
        => AssertFormatAsync("""
            class C
            {
                void Goo()//method
                {
                    int x;//variable
                    double y;
                }
            }
            """, """
            class C 
            {
                        void Goo()//method
            {
                int x;//variable
            double y;
                                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537765")]
    public Task Comment26()
        => AssertFormatAsync("""
            public class Class1
            {
                void Goo()
                {
                    /**/
                    int x;
                }
            }
            """, """
            public class Class1
            {
                void Goo()
                {
            /**/int x;
                }
            }
            """);

    [Fact]
    public async Task Comment27()
    {
        var content = """
            public class Class1
            {
                void Goo()
                {
                    //      
                    // 
                }
            }
            """;

        await AssertFormatAsync(content, content);
    }

    [Fact]
    public Task Comment28()
        => AssertFormatAsync("""
            public class Class1
            {
                void Goo()
                {
                    //      

                    // 

                }
            }
            """, """
            public class Class1
            {
                void Goo()
                {
                    //      

                    // 

                }
            }
            """);

    [Fact]
    public Task Comment29()
        => AssertFormatAsync("""
            public class Class1
            {
                void Goo()
                {
                    int         /**/ i = 10;
                }
            }
            """, """
            public class Class1
            {
                void Goo()
                {
                    int			/**/ i = 10;
                }
            }
            """);

    [Fact]
    public Task Comment30()
        => AssertFormatAsync("""
            // Test
            """, """
            // Test
            """);

    [Fact]
    public Task Comment31()
        => AssertFormatAsync("""
            /// <summary>
            ///
            /// </summary>
            class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """, """
            /// <summary>
            ///
                    /// </summary>
            class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538703")]
    public Task Comment32()
        => AssertFormatAsync("""
            class Program
            {
                ///<summary>
                ///     TestMethod
                ///</summary>
                void Method() { }
            }
            """, """
            class Program
            {
                ///<summary>
                    ///     TestMethod
            ///</summary>
                void Method() { }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542316")]
    public Task CommentInExpression()
        => AssertFormatAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                        // (10,30): error CS0455: Type parameter 'X' inherits conflicting constraints 'B' and 'A'
                        Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "X").WithArguments("X", "B", "A").WithLocation(10, 30));
                }
            }
            """, """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                                        // (10,30): error CS0455: Type parameter 'X' inherits conflicting constraints 'B' and 'A'
                        Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "X").WithArguments("X", "B", "A").WithLocation(10, 30));
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542546")]
    public Task FormatInvalidCode_1()
        => AssertFormatAsync(@"> Roslyn.Utilities.dll!   Basic", @">	Roslyn.Utilities.dll! 	Basic");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542546")]
    public Task FormatInvalidCode_2()
        => AssertFormatAsync(@"> Roslyn.Utilities.dll! Line 43 + 0x5 bytes	Basic", @">	Roslyn.Utilities.dll! Line 43 + 0x5 bytes	Basic");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537895")]
    public Task EmbededStatement1()
        => AssertFormatAsync("""
            using System;
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
            }
            """, """
            using System;
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75919")]
    public Task EndRegionFollowedByLabel()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    int i = 0;

                    #region region one
                    if (i == 0)
                    {
                        goto label0;
                    }
                    else if (i == 1)
                    {
                        goto label1;
                    }
                    #endregion

                label0:
                    return;
                label1:
                    return;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int i = 0;

                    #region region one
                    if (i == 0)
                    {
                        goto label0;
                    }
                    else if (i == 1)
                    {
                        goto label1;
                    }
            #endregion

            label0:
                    return;
            label1:
                    return;
                }
            }
            """);

    [Fact]
    public Task RefKeywords()
        => AssertFormatAsync("""
            class C
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
            }
            """, """
            class C 
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
            }
            """);

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
        var code = """
            #region Assembly mscorlib
            // C:\
            #endregion

            using System.Collections;

            class F
            {
                string s;
            }
            """;
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
        Assert.Equal("""
            #region Assembly mscorlib
            // C:\
            #endregion

            using System.Collections;

            class F
            {
            	string s;
            }
            """, actual);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39351")]
    public Task SingleLineComment_AtEndOfFile_DoesNotAddNewLine()
        => AssertNoFormattingChangesAsync("""
            class Program { }

            // Test
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39351")]
    public Task MultiLineComment_AtEndOfFile_DoesNotAddNewLine()
        => AssertNoFormattingChangesAsync("""
            class Program { }

            /* Test */
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39351")]
    public Task DocComment_AtEndOfFile_DoesNotAddNewLine()
        => AssertNoFormattingChangesAsync("""
            class Program { }

            /// Test
            """);
}
