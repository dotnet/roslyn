// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingEngineTriviaTests : CSharpFormattingTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void PreprocessorInEmptyFile()
        {
            var content = @"
                    
            #line 1000
        #error
                        ";

            var expected = @"

#line 1000
#error
";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment1()
        {
            var content = @"             // single line comment
            class C {           }";

            var expected = @"// single line comment
class C { }";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment2()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment3()
        {
            var content = @"class C 
{
                // single line comment
}";

            var expected = @"class C
{
    // single line comment
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment4()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment5()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment6()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment7()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment8()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment9()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment10()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment11()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment12()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment13()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment14()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment15()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment16()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment17()
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

            AssertFormat(expected, content, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment18()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment19()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment20()
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

            AssertFormat(expected, content);
        }

        // for now, formatting engine doesn't re-indent token if the indentation line contains noisy
        // chars
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment21()
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

            AssertFormat(expected, content);
        }

        // for now, formatting engine doesn't re-indent token if the indentation line contains noisy
        // chars
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment22()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment23()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment24()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment1()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment2()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment3()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment4()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment5()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment6()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment7()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment8()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment9()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment10()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment11()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocComment12()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void MixCommentAndDocComment1()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void MixCommentAndDocComment2()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void MixCommentAndDocComment3()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void MixCommentAndDocComment4()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor1()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor2()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor3()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor4()
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
            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor5()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor6()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor7()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor8()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void MixAll()
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
            AssertFormat(expected, content);
        }

        [WorkItem(537895, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor9()
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

            AssertFormat(expected, content);
        }

        [WorkItem(537895, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Preprocessor10()
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

            AssertFormat(expected, content);
        }

        [WorkItem(537765, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment25()
        {
            var content = @"class C 
{
            void Foo()//method
{
    int x;//variable
double y;
                    }
}
";

            var expected = @"class C
{
    void Foo()//method
    {
        int x;//variable
        double y;
    }
}
";

            AssertFormat(expected, content);
        }

        [WorkItem(537765, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment26()
        {
            var content = @"public class Class1
{
    void Foo()
    {
/**/int x;
    }
}";

            var expected = @"public class Class1
{
    void Foo()
    {
        /**/
        int x;
    }
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment27()
        {
            var content = @"public class Class1
{
    void Foo()
    {
        //      
        // 
    }
}";

            AssertFormat(content, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment28()
        {
            var content = @"public class Class1
{
    void Foo()
    {
        //      
            
        // 
            
    }
}";

            var expected = @"public class Class1
{
    void Foo()
    {
        //      

        // 

    }
}";
            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment29()
        {
            var content = @"public class Class1
{
    void Foo()
    {
        int			/**/ i = 10;
    }
}";

            var code = @"public class Class1
{
    void Foo()
    {
        int         /**/ i = 10;
    }
}";

            AssertFormat(code, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment30()
        {
            var content = @"
// Test";

            var code = @"
// Test";

            AssertFormat(code, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment31()
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

            AssertFormat(code, content);
        }

        [WorkItem(538703, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Comment32()
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

            AssertFormat(code, content);
        }

        [WorkItem(542316, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CommentInExpression()
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

            AssertFormat(code, content);
        }

        [WorkItem(542546, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatInvalidCode_1()
        {
            var content = @">	Roslyn.Utilities.dll! 	Basic";
            AssertFormat(content, content);
        }

        [WorkItem(542546, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatInvalidCode_2()
        {
            var content = @">	Roslyn.Utilities.dll! Line 43 + 0x5 bytes	Basic";
            var expectedContent = @">	Roslyn.Utilities.dll! Line 43 + 0x5 bytes Basic";
            AssertFormat(expectedContent, content);
        }

        [WorkItem(537895, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void EmbededStatement1()
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
            AssertFormat(expectedContent, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void RefKeywords()
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

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NewLineOptions_LineFeedOnly()
        {
            var tree = SyntaxFactory.ParseCompilationUnit("class C\r\n{\r\n}");

            // replace all EOL trivia with elastic markers to force the formatter to add EOL back
            tree = tree.ReplaceTrivia(tree.DescendantTrivia().Where(tr => tr.IsKind(SyntaxKind.EndOfLineTrivia)), (o, r) => SyntaxFactory.ElasticMarker);

            var formatted = Formatter.Format(tree, DefaultWorkspace, DefaultWorkspace.Options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\n"));

            var actual = formatted.ToFullString();
            var expected = "class C\n{\n}";

            Assert.Equal(expected, actual);
        }

        [WorkItem(4019, "https://github.com/dotnet/roslyn/issues/4019")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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

            var newLineText = SyntaxFactory.ElasticEndOfLine(DefaultWorkspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp));

            tree = tree.ReplaceTokens(tree.DescendantTokens(descendIntoTrivia: true)
                                          .Where(tr => tr.IsKind(SyntaxKind.EndOfDirectiveToken)), (o, r) => o.WithTrailingTrivia(o.LeadingTrivia.Add(newLineText))
                                                                                                              .WithLeadingTrivia(SyntaxFactory.TriviaList())
                                                                                                              .WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation));

            var formatted = Formatter.Format(tree, DefaultWorkspace, DefaultWorkspace.Options.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true));

            var actual = formatted.ToFullString();
            Assert.Equal(expected, actual);
        }
    }
}
