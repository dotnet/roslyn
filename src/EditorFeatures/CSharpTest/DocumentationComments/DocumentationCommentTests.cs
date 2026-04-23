// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments;

[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class DocumentationCommentTests : AbstractDocumentationCommentTests
{
    [WpfFact]
    public void TypingCharacter_Class()
        => VerifyTypingCharacter("""
            //$$
            class C
            {
            }
            """, """
            /// <summary>
            /// $$
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/78770")]
    public void TypingCharacter_Extension()
        => VerifyTypingCharacter("""
            static class C
            {
                //$$
                extension<T>(int i) { }
            }
            """, """
            static class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="i"></param>
                extension<T>(int i) { }
            }
            """);

    [WpfFact]
    public void TypingCharacter_Record()
        => VerifyTypingCharacter("""
            //$$
            record R;
            """, """
            /// <summary>
            /// $$
            /// </summary>
            record R;
            """);

    [WpfFact]
    public void TypingCharacter_RecordStruct()
        => VerifyTypingCharacter("""
            //$$
            record struct R;
            """, """
            /// <summary>
            /// $$
            /// </summary>
            record struct R;
            """);

    [WpfFact]
    public void TypingCharacter_RecordWithPositionalParameters()
        => VerifyTypingCharacter("""
            //$$
            record R(string S, int I);
            """, """
            /// <summary>
            /// $$
            /// </summary>
            /// <param name="S"></param>
            /// <param name="I"></param>
            record R(string S, int I);
            """);

    [WpfFact]
    public void TypingCharacter_ClassParameters()
        => VerifyTypingCharacter("""
            //$$
            class R(string S, int I);
            """, """
            /// <summary>
            /// $$
            /// </summary>
            /// <param name="S"></param>
            /// <param name="I"></param>
            class R(string S, int I);
            """);

    [WpfFact]
    public void TypingCharacter_RecordStructWithPositionalParameters()
        => VerifyTypingCharacter("""
            //$$
            record struct R(string S, int I);
            """, """
            /// <summary>
            /// $$
            /// </summary>
            /// <param name="S"></param>
            /// <param name="I"></param>
            record struct R(string S, int I);
            """);

    [WpfFact]
    public void TypingCharacter_StructParameters()
        => VerifyTypingCharacter("""
            //$$
            struct R(string S, int I);
            """, """
            /// <summary>
            /// $$
            /// </summary>
            /// <param name="S"></param>
            /// <param name="I"></param>
            struct R(string S, int I);
            """);

    [WpfFact]
    public void TypingCharacter_Class_NewLine()
    {
        var code = """
            //$$
            class C
            {
            }
            """;

        var expected = """
            /// <summary>
            /// $$
            /// </summary>
            class C
            {
            }
            """;

        VerifyTypingCharacter(code, expected, newLine: """


            """);

        code = """
            //$$
            class C
            {
            }
            """;

        expected = """
            /// <summary>
            /// $$
            /// </summary>
            class C
            {
            }
            """;

        VerifyTypingCharacter(code, expected, newLine: """


            """);
    }

    [WpfFact]
    public void TypingCharacter_Class_AutoGenerateXmlDocCommentsOff()
        => VerifyTypingCharacter("""
            //$$
            class C
            {
            }
            """, """
            ///$$
            class C
            {
            }
            """, globalOptions: new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, false }
        });

    [WpfFact]
    public void TypingCharacter_Method()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                int M<T>(int goo) { return 0; }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="goo"></param>
                /// <returns></returns>
                int M<T>(int goo) { return 0; }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54245")]
    public void TypingCharacter_Method_WithExceptions()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                int M<T>(int goo)
                {
                    if (goo < 0) throw new /*leading trivia*/Exception/*trailing trivia*/();
                    return 0;
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="goo"></param>
                /// <returns></returns>
                /// <exception cref="Exception"></exception>
                int M<T>(int goo)
                {
                    if (goo < 0) throw new /*leading trivia*/Exception/*trailing trivia*/();
                    return 0;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54245")]
    public void TypingCharacter_Constructor_WithExceptions()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                public C(int goo)
                {
                    if (goo < 0) throw new /*leading trivia*/Exception/*trailing trivia*/();
                    throw null;
                    throw null;
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <param name="goo"></param>
                /// <exception cref="Exception"></exception>
                /// <exception cref="System.NullReferenceException"></exception>
                public C(int goo)
                {
                    if (goo < 0) throw new /*leading trivia*/Exception/*trailing trivia*/();
                    throw null;
                    throw null;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54245")]
    public void TypingCharacter_Constructor_WithExceptions_Caught()
        => VerifyTypingCharacter("""

            using System;

            class C
            {
                //$$
                public C(int goo)
                {
                    try
                    {
                        if (goo == 10)
                            throw new Exception();
                        if (goo == 9)
                            throw new ArgumentOutOfRangeException();
                    }
                    catch (ArgumentException)
                    {
                    }

                    throw null;
                    throw null;
                }
            }
            """, """

            using System;

            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <param name="goo"></param>
                /// <exception cref="Exception"></exception>
                /// <exception cref="ArgumentOutOfRangeException"></exception>
                /// <exception cref="NullReferenceException"></exception>
                public C(int goo)
                {
                    try
                    {
                        if (goo == 10)
                            throw new Exception();
                        if (goo == 9)
                            throw new ArgumentOutOfRangeException();
                    }
                    catch (ArgumentException)
                    {
                    }

                    throw null;
                    throw null;
                }
            }
            """);

    [WpfFact]
    public void TypingCharacter_Method_WithVerbatimParams()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                int M<@int>(int @goo) { return 0; }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="int"></typeparam>
                /// <param name="goo"></param>
                /// <returns></returns>
                int M<@int>(int @goo) { return 0; }
            }
            """);

    [WpfFact]
    public void TypingCharacter_AutoProperty()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                int P { get; set; }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                int P { get; set; }
            }
            """);

    [WpfFact]
    public void TypingCharacter_Property()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                int P
                {
                    get { return 0; }
                    set { }
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                int P
                {
                    get { return 0; }
                    set { }
                }
            }
            """);

    [WpfFact]
    public void TypingCharacter_Indexer()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                int this[int index]
                {
                    get { return 0; }
                    set { }
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <param name="index"></param>
                /// <returns></returns>
                int this[int index]
                {
                    get { return 0; }
                    set { }
                }
            }
            """);

    [WpfFact]
    public void TypingCharacter_VoidMethod1()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                void M<T>(int goo) {  }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="goo"></param>
                void M<T>(int goo) {  }
            }
            """);

    [WpfFact]
    public void TypingCharacter_VoidMethod_WithVerbatimParams()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                void M<@T>(int @int) {  }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="int"></param>
                void M<@T>(int @int) {  }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538699")]
    public void TypingCharacter_VoidMethod2()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                void Method() { }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                void Method() { }
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotWhenDocCommentExists1()
        => VerifyTypingCharacter("""

            ///
            //$$
            class C
            {
            }
            """, """

            ///
            ///$$
            class C
            {
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotWhenDocCommentExists2()
        => VerifyTypingCharacter("""

            ///

            //$$
            class C
            {
            }
            """, """

            ///

            ///$$
            class C
            {
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotWhenDocCommentExists3()
        => VerifyTypingCharacter("""

            class B { } ///

            //$$
            class C
            {
            }
            """, """

            class B { } ///

            ///$$
            class C
            {
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotWhenDocCommentExists4()
        => VerifyTypingCharacter("""
            //$$
            /// <summary></summary>
            class C
            {
            }
            """, """
            ///$$
            /// <summary></summary>
            class C
            {
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotWhenDocCommentExists5()
        => VerifyTypingCharacter("""
            class C
            {
                //$$
                /// <summary></summary>
                int M<T>(int goo) { return 0; }
            }
            """, """
            class C
            {
                ///$$
                /// <summary></summary>
                int M<T>(int goo) { return 0; }
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotInsideMethodBody1()
        => VerifyTypingCharacter("""
            class C
            {
                void M(int goo)
                {
                  //$$
                }
            }
            """, """
            class C
            {
                void M(int goo)
                {
                  ///$$
                }
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotInsideMethodBody2()
        => VerifyTypingCharacter("""
            class C
            {
                /// <summary></summary>
                void M(int goo)
                {
                  //$$
                }
            }
            """, """
            class C
            {
                /// <summary></summary>
                void M(int goo)
                {
                  ///$$
                }
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotAfterClassName()
        => VerifyTypingCharacter("""
            class C//$$
            {
            }
            """, """
            class C///$$
            {
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotAfterOpenBrace()
        => VerifyTypingCharacter("""
            class C
            {//$$
            }
            """, """
            class C
            {///$$
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotAfterCtorName()
        => VerifyTypingCharacter("""
            class C
            {
            C() //$$
            }
            """, """
            class C
            {
            C() ///$$
            }
            """);

    [WpfFact]
    public void TypingCharacter_NotInsideCtor()
        => VerifyTypingCharacter("""
            class C
            {
            C()
            {
            //$$
            }
            }
            """, """
            class C
            {
            C()
            {
            ///$$
            }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59081")]
    public void TypingCharacter_NotInTopLevel()
        => VerifyTypingCharacter("""

            using System;

            //$$
            Console.WriteLine();

            """, """

            using System;

            ///$$
            Console.WriteLine();

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59081")]
    public void TypingCharacter_NotInNamespace()
        => VerifyTypingCharacter("""

            using System;

            //$$
            namespace NS { }

            """, """

            using System;

            ///$$
            namespace NS { }

            """);

    [WpfFact]
    public void PressingEnter_InsertComment_Class1()
        => VerifyPressingEnter("""
            ///$$
            class C
            {
            }
            """, """
            /// <summary>
            /// $$
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4817")]
    public void PressingEnter_InsertComment_Class1_AutoGenerateXmlDocCommentsOff()
        => VerifyPressingEnter("""
            ///$$
            class C
            {
            }
            """, """
            ///
            $$
            class C
            {
            }
            """, globalOptions: new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, false }
        });

    [WpfFact]
    public void PressingEnter_InsertComment_Class2()
        => VerifyPressingEnter("""
            ///$$class C
            {
            }
            """, """
            /// <summary>
            /// $$
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertComment_Class3()
        => VerifyPressingEnter("""
            ///$$[Goo] class C
            {
            }
            """, """
            /// <summary>
            /// $$
            /// </summary>
            [Goo] class C
            {
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertComment_NotAfterWhitespace()
        => VerifyPressingEnter("""
        ///    $$class C
        {
        }
        """, """
            ///    
            /// $$class C
            {
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertComment_Method1()
        => VerifyPressingEnter("""
            class C
            {
                ///$$
                int M<T>(int goo) { return 0; }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="goo"></param>
                /// <returns></returns>
                int M<T>(int goo) { return 0; }
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertComment_Method2()
        => VerifyPressingEnter("""
            class C
            {
                ///$$int M<T>(int goo) { return 0; }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="goo"></param>
                /// <returns></returns>
                int M<T>(int goo) { return 0; }
            }
            """);

    [WpfFact]
    public void PressingEnter_NotInMethodBody1()
        => VerifyPressingEnter("""
            class C
            {
            void Goo()
            {
            ///$$
            }
            }
            """, """
            class C
            {
            void Goo()
            {
            ///
            $$
            }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537513")]
    public void PressingEnter_NotInterleavedInClassName1()
        => VerifyPressingEnter("""
            class///$$ C
            {
            }
            """, """
            class///
            $$ C
            {
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537513")]
    public void PressingEnter_NotInterleavedInClassName2()
        => VerifyPressingEnter("""
            class ///$$C
            {
            }
            """, """
            class ///
            $$C
            {
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537513")]
    public void PressingEnter_NotInterleavedInClassName3()
        => VerifyPressingEnter("""
            class /// $$C
            {
            }
            """, """
            class /// 
            $$C
            {
            }
            """);

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537514")]
    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537532")]
    public void PressingEnter_NotAfterClassName1()
        => VerifyPressingEnter("""
            class C ///$$
            {
            }
            """, """
            class C ///
            $$
            {
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537552")]
    public void PressingEnter_NotAfterClassName2()
        => VerifyPressingEnter("""
            class C /** $$
            {
            }
            """, """
            class C /** 
            $$
            {
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537535")]
    public void PressingEnter_NotAfterCtorName()
        => VerifyPressingEnter("""
            class C
            {
            C() ///$$
            }
            """, """
            class C
            {
            C() ///
            $$
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537511")]
    public void PressingEnter_NotInsideCtor()
        => VerifyPressingEnter("""
            class C
            {
            C()
            {
            ///$$
            }
            }
            """, """
            class C
            {
            C()
            {
            ///
            $$
            }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537550")]
    public void PressingEnter_NotBeforeDocComment()
        => VerifyPressingEnter("""
                class c1
                {
            $$/// <summary>
                    /// 
                    /// </summary>
                    /// <returns></returns>
                    public async Task goo()
                    {
                        var x = 1;
                    }
                }
            """, """
                class c1
                {

            $$/// <summary>
                    /// 
                    /// </summary>
                    /// <returns></returns>
                    public async Task goo()
                    {
                        var x = 1;
                    }
                }
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes1()
        => VerifyPressingEnter("""
            ///$$
            /// <summary></summary>
            class C
            {
            }
            """, """
            ///
            /// $$
            /// <summary></summary>
            class C
            {
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes2()
        => VerifyPressingEnter("""
            /// <summary>
            /// $$
            /// </summary>
            class C
            {
            }
            """, """
            /// <summary>
            /// 
            /// $$
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes3()
        => VerifyPressingEnter("""
                /// <summary>
                /// $$
                /// </summary>
                class C
                {
                }
            """, """
                /// <summary>
                /// 
                /// $$
                /// </summary>
                class C
                {
                }
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes4()
        => VerifyPressingEnter("""
            /// <summary>$$</summary>
            class C
            {
            }
            """, """
            /// <summary>
            /// $$</summary>
            class C
            {
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes5()
        => VerifyPressingEnter("""
                /// <summary>
                /// $$
                /// </summary>
                class C
                {
                }
            """, """
                /// <summary>
                /// 
                /// $$
                /// </summary>
                class C
                {
                }
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes6()
        => VerifyPressingEnter("""
            /// <summary></summary>$$
            class C
            {
            }
            """, """
            /// <summary></summary>
            /// $$
            class C
            {
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes7()
        => VerifyPressingEnter("""
                /// <summary>$$</summary>
                class C
                {
                }
            """, """
                /// <summary>
                /// $$</summary>
                class C
                {
                }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538702")]
    public void PressingEnter_InsertSlashes8()
        => VerifyPressingEnter("""
            /// <summary>
            /// 
            /// </summary>
            ///$$class C {}
            """, """
            /// <summary>
            /// 
            /// </summary>
            ///
            /// $$class C {}
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes9()
        => VerifyPressingEnter("""
            class C
            {
                ///$$
                /// <summary></summary>
                int M<T>(int goo) { return 0; }
            }
            """, """
            class C
            {
                ///
                /// $$
                /// <summary></summary>
                int M<T>(int goo) { return 0; }
            }
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes10()
        => VerifyPressingEnter("""
            /// <summary>
            /// 
            /// </summary>
            ///$$Go ahead and add some slashes
            """, """
            /// <summary>
            /// 
            /// </summary>
            ///
            /// $$Go ahead and add some slashes
            """);

    [WpfFact]
    public void PressingEnter_InsertSlashes11()
        => VerifyPressingEnter("""
            class C
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i">$$</param>
                void Goo(int i)
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i">
                /// $$</param>
                void Goo(int i)
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4817")]
    public void PressingEnter_InsertSlashes12_AutoGenerateXmlDocCommentsOff()
        => VerifyPressingEnter("""
            ///$$
            /// <summary></summary>
            class C
            {
            }
            """, """
            ///
            /// $$
            /// <summary></summary>
            class C
            {
            }
            """, globalOptions: new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, false }
        });

    [WpfFact]
    public void PressingEnter_DoNotInsertSlashes1()
        => VerifyPressingEnter("""
            /// <summary></summary>
            /// $$
            class C
            {
            }
            """, """
            /// <summary></summary>
            /// 
            $$
            class C
            {
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538701")]
    public void PressingEnter_DoNotInsertSlashes2()
        => VerifyPressingEnter("""
            ///<summary></summary>

            ///$$
            class C{}
            """, """
            ///<summary></summary>

            ///
            $$
            class C{}
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/25746")]
    public void PressingEnter_ExtraSlashesAfterExteriorTrivia()
        => VerifyPressingEnter("""
            class C
            {
            C()
            {
            //////$$
            }
            }
            """, """
            class C
            {
            C()
            {
            //////
            ///$$
            }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542426")]
    public void PressingEnter_PreserveParams()
        => VerifyPressingEnter("""
            /// <summary>
            /// 
            /// </summary>
            /// <param name="args">$$</param>
            static void Main(string[] args)
            { }
            """, """
            /// <summary>
            /// 
            /// </summary>
            /// <param name="args">
            /// $$</param>
            static void Main(string[] args)
            { }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2091")]
    public void PressingEnter_InTextBeforeSpace()
        => VerifyPressingEnter("""
            class C
            {
                /// <summary>
                /// hello$$ world
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// hello
                /// $$world
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")]
    public void PressingEnter_Indentation1()
        => VerifyPressingEnter("""
            class C
            {
                /// <summary>
                ///     hello world$$
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                ///     hello world
                ///     $$
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")]
    public void PressingEnter_Indentation2()
        => VerifyPressingEnter("""
            class C
            {
                /// <summary>
                ///     hello $$world
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                ///     hello 
                ///     $$world
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")]
    public void PressingEnter_Indentation3()
        => VerifyPressingEnter("""
            class C
            {
                /// <summary>
                ///     hello$$ world
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                ///     hello
                ///     $$world
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")]
    public void PressingEnter_Indentation4()
        => VerifyPressingEnter("""
            class C
            {
                /// <summary>
                ///     $$hello world
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                ///     
                /// $$hello world
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")]
    public void PressingEnter_Indentation5_UseTabs()
        => VerifyPressingEnter("""
            class C
            {
            	/// <summary>
            	///     hello world$$
            	/// </summary>
            	void M()
            	{
            	}
            }
            """, """
            class C
            {
            	/// <summary>
            	///     hello world
            	///     $$
            	/// </summary>
            	void M()
            	{
            	}
            }
            """, useTabs: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5486")]
    public void PressingEnter_Selection1()
        => VerifyPressingEnter("""
            /// <summary>
            /// Hello [|World|]$$!
            /// </summary>
            class C
            {
            }
            """, """
            /// <summary>
            /// Hello 
            /// $$!
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5486")]
    public void PressingEnter_Selection2()
        => VerifyPressingEnter("""
            /// <summary>
            /// Hello $$[|World|]!
            /// </summary>
            class C
            {
            }
            """, """
            /// <summary>
            /// Hello 
            /// $$!
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/27223")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/49564")]
    public void PressingEnter_XmlDocCommentInStringLiteral()
        => VerifyPressingEnter("""
            class C
            {
                C()
                {
                    string s = @"
                        /// <summary>$$</summary>
                        void M() {}"
                }
            }
            """, """
            class C
            {
                C()
                {
                    string s = @"
                        /// <summary>
            $$</summary>
                        void M() {}"
                }
            }
            """);

    [WpfFact]
    public void Command_Class()
        => VerifyInsertCommentCommand("""
            class C
            {$$
            }
            """, """
            /// <summary>
            /// $$
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact]
    public void Command_Record()
        => VerifyInsertCommentCommand("record R$$;", """
            /// <summary>
            /// $$
            /// </summary>
            record R;
            """);

    [WpfFact]
    public void Command_RecordStruct()
        => VerifyInsertCommentCommand("record struct R$$;", """
            /// <summary>
            /// $$
            /// </summary>
            record struct R;
            """);

    [WpfFact]
    public void Command_RecordWithPositionalParameters()
        => VerifyInsertCommentCommand("record R$$(string S, int I);", """
            /// <summary>
            /// $$
            /// </summary>
            /// <param name="S"></param>
            /// <param name="I"></param>
            record R(string S, int I);
            """);

    [WpfFact]
    public void Command_ClassParameters()
        => VerifyInsertCommentCommand("class R$$(string S, int I);", """
            /// <summary>
            /// $$
            /// </summary>
            /// <param name="S"></param>
            /// <param name="I"></param>
            class R(string S, int I);
            """);

    [WpfFact]
    public void Command_RecordStructWithPositionalParameters()
        => VerifyInsertCommentCommand("record struct R$$(string S, int I);", """
            /// <summary>
            /// $$
            /// </summary>
            /// <param name="S"></param>
            /// <param name="I"></param>
            record struct R(string S, int I);
            """);

    [WpfFact]
    public void Command_StructParameters()
        => VerifyInsertCommentCommand("struct R$$(string S, int I);", """
            /// <summary>
            /// $$
            /// </summary>
            /// <param name="S"></param>
            /// <param name="I"></param>
            struct R(string S, int I);
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4817")]
    public void Command_Class_AutoGenerateXmlDocCommentsOff()
        => VerifyInsertCommentCommand("""
            class C
            {$$
            }
            """, """
            /// <summary>
            /// $$
            /// </summary>
            class C
            {
            }
            """, globalOptions: new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, false }
        });

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538714")]
    public void Command_BeforeClass1()
        => VerifyInsertCommentCommand("""
            $$
            class C { }
            """, """

            /// <summary>
            /// $$
            /// </summary>
            class C { }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538714")]
    public void Command_BeforeClass2()
        => VerifyInsertCommentCommand("""
            class B { }
            $$
            class C { }
            """, """
            class B { }

            /// <summary>
            /// $$
            /// </summary>
            class C { }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538714")]
    public void Command_BeforeClass3()
        => VerifyInsertCommentCommand("""
            class B
            {
                $$
                class C { }
            }
            """, """
            class B
            {
                
                /// <summary>
                /// $$
                /// </summary>
                class C { }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527604")]
    public void Command_Class_NotIfMultilineDocCommentExists()
        => VerifyInsertCommentCommand("""
            /**
            */
            class C { $$ }
            """, """
            /**
            */
            class C { $$ }
            """);

    [WpfFact]
    public void Command_Method()
        => VerifyInsertCommentCommand("""
            class C
            {
                int M<T>(int goo) { $$return 0; }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="goo"></param>
                /// <returns></returns>
                int M<T>(int goo) { return 0; }
            }
            """);

    [WpfFact]
    public void Command_Class_NotIfCommentExists()
        => VerifyInsertCommentCommand("""
            /// <summary></summary>
            class C
            {$$
            }
            """, """
            /// <summary></summary>
            class C
            {$$
            }
            """);

    [WpfFact]
    public void Command_Method_NotIfCommentExists()
        => VerifyInsertCommentCommand("""
            class C
            {
                /// <summary></summary>
                int M<T>(int goo) { $$return 0; }
            }
            """, """
            class C
            {
                /// <summary></summary>
                int M<T>(int goo) { $$return 0; }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")]
    public void Command_FirstClassOnLine()
        => VerifyInsertCommentCommand(@"$$class C { } class D { }", """
            /// <summary>
            /// $$
            /// </summary>
            class C { } class D { }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")]
    public void Command_NotOnSecondClassOnLine()
        => VerifyInsertCommentCommand(@"class C { } $$class D { }", @"class C { } $$class D { }");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")]
    public void Command_FirstMethodOnLine()
        => VerifyInsertCommentCommand("""
            class C
            {
                protected abstract void $$Goo(); protected abstract void Bar();
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                protected abstract void Goo(); protected abstract void Bar();
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")]
    public void Command_NotOnSecondMethodOnLine()
        => VerifyInsertCommentCommand("""
            class C
            {
                protected abstract void Goo(); protected abstract void $$Bar();
            }
            """, """
            class C
            {
                protected abstract void Goo(); protected abstract void $$Bar();
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/917904")]
    public void TestUseTab()
        => VerifyTypingCharacter("""
            using System;

            public class Class1
            {
            	//$$
            	public Class1()
            	{
            	}
            }
            """, """
            using System;

            public class Class1
            {
            	/// <summary>
            	/// $$
            	/// </summary>
            	public Class1()
            	{
            	}
            }
            """, useTabs: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")]
    public void TestOpenLineAbove1()
        => VerifyOpenLineAbove("""
            class C
            {
                /// <summary>
                /// stuff$$
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// stuff
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")]
    public void TestOpenLineAbove2()
        => VerifyOpenLineAbove("""
            class C
            {
                /// <summary>
                /// $$stuff
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// stuff
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")]
    public void TestOpenLineAbove3()
    {

        // Note that the caret position specified below does not look correct because
        // it is in virtual space in this case.

        VerifyOpenLineAbove("""
            class C
            {
                /// $$<summary>
                /// stuff
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
            $$
                /// <summary>
                /// stuff
                /// </summary>
                void M()
                {
                }
            }
            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")]
    public void TestOpenLineAbove4_Tabs()
        => VerifyOpenLineAbove("""
            class C
            {
            		  /// <summary>
            	/// $$stuff
            	/// </summary>
            	void M()
            	{
            	}
            }
            """, """
            class C
            {
            		  /// <summary>
            		  /// $$
            	/// stuff
            	/// </summary>
            	void M()
            	{
            	}
            }
            """, useTabs: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")]
    public void TestOpenLineBelow1()
        => VerifyOpenLineBelow("""
            class C
            {
                /// <summary>
                /// stuff$$
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// stuff
                /// $$
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")]
    public void TestOpenLineBelow2()
        => VerifyOpenLineBelow("""
            class C
            {
                /// <summary>
                /// $$stuff
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class C
            {
                /// <summary>
                /// stuff
                /// $$
                /// </summary>
                void M()
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")]
    public void TestOpenLineBelow3()
        => VerifyOpenLineBelow("""
            /// <summary>
            /// stuff
            /// $$</summary>

            """, """
            /// <summary>
            /// stuff
            /// </summary>
            /// $$

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")]
    public void TestOpenLineBelow4_Tabs()
        => VerifyOpenLineBelow("""
            class C
            {
            	/// <summary>
            		  /// $$stuff
            	/// </summary>
            	void M()
            	{
            	}
            }
            """, """
            class C
            {
            	/// <summary>
            		  /// stuff
            		  /// $$
            	/// </summary>
            	void M()
            	{
            	}
            }
            """, useTabs: true);

    [WpfFact, WorkItem(468638, @"https://devdiv.visualstudio.com/DevDiv/NET%20Developer%20Experience%20IDE/_workitems/edit/468638")]
    public void VerifyEnterWithTrimNewLineEditorConfigOption()
        => VerifyPressingEnter("""
            /// <summary>
            /// $$
            /// </summary>
            class C { }
            """, """
            /// <summary>
            ///
            /// $$
            /// </summary>
            class C { }
            """, useTabs: true, trimTrailingWhiteSpace: true);

    [WpfFact]
    public void TypingCharacter_Class_WithComment()
        => VerifyTypingCharacter("""
            //$$ This is my class and it does great things.
            class C
            {
            }
            """, """
            /// <summary>
            /// $$This is my class and it does great things.
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact]
    public void TypingCharacter_Class_WithComment_NoSpace()
        => VerifyTypingCharacter("""
            //$$This is my class and it does great things.
            class C
            {
            }
            """, """
            /// <summary>
            /// $$This is my class and it does great things.
            /// </summary>
            class C
            {
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/75838")]
    public void TypingCharacter_ExistingText1()
        => VerifyTypingCharacter("""
            /// foo$$
            class C
            {
            }
            """, """
            /// foo/$$
            class C
            {
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/75838")]
    public void TypingCharacter_ExistingText2()
        => VerifyTypingCharacter("""
            namespace N
            {
                /// foo$$
                class C
                {
                }
            }
            """, """
            namespace N
            {
                /// foo/$$
                class C
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/75838")]
    public void PressingEnter_ExistingText1()
        => VerifyPressingEnter("""
            /// foo$$
            class C
            {
            }
            """, """
            /// foo
            /// $$
            class C
            {
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/75838")]
    public void PressingEnter_ExistingText2()
        => VerifyPressingEnter("""
            namespace N
            {
                /// foo$$
                class C
                {
                }
            }
            """, """
            namespace N
            {
                /// foo
                /// $$
                class C
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")]
    public void TypingCharacter_Class_Collapsed()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, true }
        };

        VerifyTypingCharacter("""
            //$$
            class C
            {
            }
            """, """
            /// <summary>$$</summary>
            class C
            {
            }
            """, globalOptions: globalOptions);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")]
    public void TypingCharacter_Method_Collapsed()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, true }
        };

        VerifyTypingCharacter("""
            class C
            {
                //$$
                void M() { }
            }
            """, """
            class C
            {
                /// <summary>$$</summary>
                void M() { }
            }
            """, globalOptions: globalOptions);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")]
    public void TypingCharacter_MethodWithParameters_Collapsed()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, true }
        };

        VerifyTypingCharacter("""
            class C
            {
                //$$
                void M(int x, string y) { }
            }
            """, """
            class C
            {
                /// <summary>$$</summary>
                /// <param name="x"></param>
                /// <param name="y"></param>
                void M(int x, string y) { }
            }
            """, globalOptions: globalOptions);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")]
    public void TypingCharacter_Property_Collapsed()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, true }
        };

        VerifyTypingCharacter("""
            class C
            {
                //$$
                public int P { get; set; }
            }
            """, """
            class C
            {
                /// <summary>$$</summary>
                public int P { get; set; }
            }
            """, globalOptions: globalOptions);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")]
    public void TypingCharacter_MethodWithParameters_OnlySummary()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.GenerateOnlySummaryTag, true }
        };

        VerifyTypingCharacter("""
            class C
            {
                //$$
                void M(int x, string y) { }
            }
            """, """
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                void M(int x, string y) { }
            }
            """, globalOptions: globalOptions);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")]
    public void TypingCharacter_MethodWithParameters_OnlySummaryAndSingleLine()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, true },
            { DocumentationCommentOptionsStorage.GenerateOnlySummaryTag, true }
        };

        VerifyTypingCharacter("""
            class C
            {
                //$$
                void M(int x, string y) { }
            }
            """, """
            class C
            {
                /// <summary>$$</summary>
                void M(int x, string y) { }
            }
            """, globalOptions: globalOptions);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")]
    public void PressingEnter_InsideSingleLineSummary_Expands()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, true }
        };

        VerifyPressingEnter("""
            /// <summary>$$</summary>
            class C
            {
            }
            """, """
            /// <summary>
            /// $$</summary>
            class C
            {
            }
            """, globalOptions: globalOptions);
    }

    protected override char DocumentationCommentCharacter
    {
        get { return '/'; }
    }

    internal override ICommandHandler CreateCommandHandler(EditorTestWorkspace workspace)
    {
        return workspace.ExportProvider.GetCommandHandler<DocumentationCommentCommandHandler>(PredefinedCommandHandlerNames.DocumentationComments, ContentTypeNames.CSharpContentType);
    }

    protected override EditorTestWorkspace CreateTestWorkspace(string code)
        => EditorTestWorkspace.CreateCSharp(code);
}
