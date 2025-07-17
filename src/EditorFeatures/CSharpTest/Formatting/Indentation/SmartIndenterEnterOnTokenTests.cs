// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using IndentStyle = Microsoft.CodeAnalysis.Formatting.FormattingOptions2.IndentStyle;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation;

[Trait(Traits.Feature, Traits.Features.SmartIndent)]
public sealed class SmartIndenterEnterOnTokenTests : CSharpFormatterTestsBase
{
    public SmartIndenterEnterOnTokenTests(ITestOutputHelper output) : base(output) { }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537808")]
    public Task MethodBody1()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class Class1
            {
                void method()
                            { }
            }

            """,
            '{',
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact]
    public Task Preprocessor1()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class A
            {
                #region T
            #endregion
            }

            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact]
    public Task Preprocessor2()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class A
            {
            #line 1
            #lien 2
            }

            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact]
    public Task Preprocessor3()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            #region stuff
            #endregion

            """,
            indentationLine: 2,
            expectedIndentation: 0);

    [WpfFact]
    public Task Comments()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            using System;

            class Class
            {
                // Comments
            // Comments

            """,
            indentationLine: 5,
            expectedIndentation: 4);

    [WpfFact]
    public Task UsingDirective()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;
            using System.Linq;

            """,
            'u',
            indentationLine: 1,
            expectedIndentation: 0);

    [WpfFact]
    public Task AfterTopOfFileComment()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            // comment

            class

            """,
            indentationLine: 2,
            expectedIndentation: 0);

    [WpfFact]
    public Task DottedName()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            using System.
            Collection;

            """,
            indentationLine: 1,
            expectedIndentation: 4);

    [WpfFact]
    public Task Namespace()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {

            """,
            '{',
            indentationLine: 3,
            expectedIndentation: 0);

    [WpfFact]
    public Task NamespaceDottedName()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            using System;

            namespace NS.
            NS2

            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact]
    public Task NamespaceBody()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
            class

            """,
            'c',
            indentationLine: 4,
            expectedIndentation: 4);

    [WpfFact]
    public Task NamespaceCloseBrace()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
            }

            """,
            '}',
            indentationLine: 4,
            expectedIndentation: 0);

    [WpfFact]
    public Task Class()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
            {

            """,
            '{',
            indentationLine: 5,
            expectedIndentation: 4);

    [WpfFact]
    public Task ClassBody()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
            int

            """,
            'i',
            indentationLine: 6,
            expectedIndentation: 8);

    [WpfFact]
    public Task ClassCloseBrace()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
            }

            """,
            '}',
            indentationLine: 6,
            expectedIndentation: 4);

    [WpfFact]
    public Task Method()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
            {

            """,
            '{',
            indentationLine: 7,
            expectedIndentation: 8);

    [WpfFact]
    public Task MethodBody()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
            int

            """,
            'i',
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfFact]
    public Task MethodCloseBrace()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
            }

            """,
            '}',
            indentationLine: 8,
            expectedIndentation: 8);

    [WpfFact]
    public Task Statement()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        int i = 10;
            int

            """,
            'i',
            indentationLine: 9,
            expectedIndentation: 12);

    [WpfFact]
    public Task MethodCall()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class c
            {
                void Method()
                {
                    M(
            a: 1, 
                        b: 1);
                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public Task Switch()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        switch (10)
            {

            """,
            '{',
            indentationLine: 9,
            expectedIndentation: 12);

    [WpfFact]
    public Task SwitchBody()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        switch (10)
                        {
            case

            """,
            'c',
            indentationLine: 10,
            expectedIndentation: 16);

    [WpfFact]
    public Task SwitchCase()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        switch (10)
                        {
                            case 10 :
            int

            """,
            'i',
            indentationLine: 11,
            expectedIndentation: 20);

    [WpfFact]
    public Task SwitchCaseBlock()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        switch (10)
                        {
                            case 10 :
            {

            """,
            '{',
            indentationLine: 11,
            expectedIndentation: 20);

    [WpfFact]
    public Task Block()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        switch (10)
                        {
                            case 10 :
                            {
            int

            """,
            'i',
            indentationLine: 12,
            expectedIndentation: 24);

    [WpfFact]
    public Task MultilineStatement1()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        int i = 10 +
            1

            """,
            indentationLine: 9,
            expectedIndentation: 16);

    [WpfFact]
    public Task MultilineStatement2()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        int i = 10 +
                                20 +
            30

            """,
            indentationLine: 10,
            expectedIndentation: 20);

    // Bug number 902477
    [WpfFact]
    public Task Comments2()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class Class
            {
                void Method()
                {
                    if (true) // Test
            int
                }
            }

            """,
            'i',
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public Task AfterCompletedBlock()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    foreach(var a in x) {}
            int
                }
            }


            """,
            'i',
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact]
    public Task AfterTopLevelAttribute()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class Program
            {
                [Attr]
            [
            }


            """,
            '[',
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537802")]
    public Task EmbededStatement()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        Console.WriteLine(1);
            int
                }
            }


            """,
            'i',
            indentationLine: 6,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537808")]
    public Task MethodBraces1()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class Class1
            {
                void method()
            { }
            }

            """,
            '{',
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537808")]
    public Task MethodBraces2()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class Class1
            {
                void method()
                {
            }
            }

            """,
            '}',
            indentationLine: 4,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537795")]
    public Task Property1()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
                string Name
                { 
                    get; 
                    set;
            }
            }

            """,
            '}',
            indentationLine: 6,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537563")]
    public Task Class1()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
            }

            """,
            '}',
            indentationLine: 2,
            expectedIndentation: 0);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public Task ArrayInitializer1()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class C
            {
                var a = new [] 
            { 1, 2, 3 }
            }

            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact]
    public Task ArrayInitializer2()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
                var a = new [] 
                {
                    1, 2, 3 
            }
            }

            """,
            '}',
            indentationLine: 5,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public Task ArrayInitializer3()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            namespace NS
            {
                class Class
                {
                    void Method(int i)
                    {
                        var a = new []
            {
                    }
            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfFact]
    public Task QueryExpression2()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
                void Method()
                {
                    var a = from c in b
                where
                }
            }

            """,
            'w',
            indentationLine: 5,
            expectedIndentation: 16);

    [WpfFact]
    public Task QueryExpression3()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
                void Method()
                {
                    var a = from c in b
                where select
                }
            }

            """,
            'w',
            indentationLine: 5,
            expectedIndentation: 16);

    [WpfFact]
    public Task QueryExpression4()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
                void Method()
                {
                    var a = from c in b where c > 10
                    select
                }
            }

            """,
            's',
            indentationLine: 5,
            expectedIndentation: 16);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853748")]
    public Task ArrayInitializer()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
                void Method()
                {
                    var l = new int[] {
                    }
                }
            }

            """,
            '}',
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939305")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public Task ArrayExpression()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class C
            {
                void M(object[] q)
                {
                    M(
                          q: new object[] 
            { });
                }
            }

            """,
            indentationLine: 6,
            expectedIndentation: 14);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public Task CollectionExpression()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
                void M(List<int> e)
                {
                    M(
                        new List<int> 
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                }
            }

            """,
            '{',
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public Task ObjectInitializer()
        => AssertIndentUsingSmartTokenFormatterAsync(
            """
            class C
            {
                void M(What dd)
                {
                    M(
                        new What 
            { d = 3, dd = " });
                }
            }

            class What
            {
                public int d;
                public string dd;
            }
            """,
            '{',
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact]
    public Task Preprocessor()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """

            #line 1 ""Bar""class Goo : [|IComparable|]#line default#line hidden
            """,
            indentationLine: 1,
            expectedIndentation: 0);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public Task InsideInitializerWithTypeBody_Implicit()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class X {
                int[] a = {
                    1,

                };
            }
            """,
            indentationLine: 3,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public Task InsideInitializerWithTypeBody_ImplicitNew()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class X {
                int[] a = new[] {
                    1,

                };
            }
            """,
            indentationLine: 3,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public Task InsideInitializerWithTypeBody_Explicit()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class X {
                int[] a = new int[] {
                    1,

                };
            }
            """,
            indentationLine: 3,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public Task InsideInitializerWithTypeBody_Collection()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            using System.Collections.Generic;
            class X {
                private List<int> a = new List<int>() {
                    1,

                };
            }
            """,
            indentationLine: 4,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public Task InsideInitializerWithTypeBody_ObjectInitializers()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class C
            {
                private What sdfsd = new What
                {
                    d = 3,

                }
            }

            class What
            {
                public int d;
                public string dd;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationString_1()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = $@"
            {Program.number}";
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 0);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationString_2()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = $@"Comment
            {Program.number}";
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 0);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationString_3()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = $@"Comment{Program.number}
            ";
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 0);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationString_4()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = $@"Comment{Program.number}Comment here
            ";
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 0);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task OutsideInterpolationString()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = $@"Comment{Program.number}Comment here"
            ;
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationSyntax_1()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = $@"{
            Program.number}";
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationSyntax_2()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = $@"{
                        Program
            .number}";
                }

                static int number;
            }
            """,
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationSyntax_3()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = $@"{
            }";
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationSyntax_4()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine($@"PPP{ 
            ((Func<int, int>)((int s) => { return number; })).Invoke(3):(408) ###-####}");
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationSyntax_5()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine($@"PPP{ ((Func<int, int>)((int s) 
            => { return number; })).Invoke(3):(408) ###-####}");
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationSyntax_6()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine($@"PPP{ ((Func<int, int>)((int s) => { return number; }))
            .Invoke(3):(408) ###-####}");
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task InsideInterpolationSyntax_7()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine($@"PPP{ ((Func<int, int>)((int s) => 
            { return number; })).Invoke(3):(408) ###-####}");
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public Task IndentLambdaBodyOneIndentationToFirstTokenOfTheStatement()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(((Func<int, int>)((int s) => 
            { return number; })).Invoke(3));
                }

                static int number;
            }
            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/1339")]
    public Task IndentAutoPropertyInitializerAsPartOfTheDeclaration()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """
            class Program
            {
                public int d { get; } 
            = 3;
                static void Main(string[] args)
                {
                }
            }
            """,
            indentationLine: 3,
            expectedIndentation: 8);

    [WpfFact]
    public Task IndentPatternPropertyFirst()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """

            class C
            {
                void Main(object o)
                {
                    var y = o is Point
                    {

                    }
                }
            }
            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfFact]
    public Task IndentPatternPropertySecond()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """

            class C
            {
                void Main(object o)
                {
                    var y = o is Point
                    {
                        X is 13,

                    }
                }
            }
            """,
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfFact]
    public Task IndentListPattern()
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            """

            class C
            {
                void Main(object o)
                {
                    var y = o is
                    [

                    ]
                }
            }
            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfTheory]
    [InlineData("x", "is < 7 and (>= 3 or > 50) or not <= 0;", 12)]
    [InlineData("x is", "< 7 and (>= 3 or > 50) or not <= 0;", 12)]
    [InlineData("x is <", "7 and (>= 3 or > 50) or not <= 0;", 12)]
    [InlineData("x is < 7", "and (>= 3 or > 50) or not <= 0;", 12)]
    [InlineData("x is < 7 and", "(>= 3 or > 50) or not <= 0;", 12)]
    [InlineData("x is < 7 and (", ">= 3 or > 50) or not <= 0;", 12)]
    [InlineData("x is < 7 and (>=", "3 or > 50) or not <= 0;", 12)]
    [InlineData("x is < 7 and (>= 3", "or > 50) or not <= 0;", 12)]
    [InlineData("x is < 7 and (>= 3 or", "> 50) or not <= 0;", 12)]
    [InlineData("x is < 7 and (>= 3 or >", "50) or not <= 0;", 12)]
    [InlineData("x is < 7 and (>= 3 or > 50", ") or not <= 0;", 12)]
    [InlineData("x is < 7 and (>= 3 or > 50)", "or not <= 0;", 12)]
    [InlineData("x is < 7 and (>= 3 or > 50) or", "not <= 0;", 12)]
    [InlineData("x is < 7 and (>= 3 or > 50) or not", "<= 0;", 12)]
    [InlineData("x is < 7 and (>= 3 or > 50) or not <=", "0;", 12)]
    [InlineData("x is < 7 and (>= 3 or > 50) or not <= 0", ";", 12)]
    public Task IndentPatternsInLocalDeclarationCSharp9(string line1, string line2, int expectedIndentation)
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            $$"""

            class C
            {
                void M()
                {
                    var x = 7;
                    var y = {{line1}}
            {{line2}}
                }
            }
            """,
            indentationLine: 7,
            expectedIndentation);

    [WpfTheory]
    [InlineData("x", "is < 7 and (>= 3 or > 50) or not <= 0;", 8)]
    [InlineData("x is", "< 7 and (>= 3 or > 50) or not <= 0;", 8)]
    [InlineData("x is <", "7 and (>= 3 or > 50) or not <= 0;", 8)]
    [InlineData("x is < 7", "and (>= 3 or > 50) or not <= 0;", 8)]
    [InlineData("x is < 7 and", "(>= 3 or > 50) or not <= 0;", 8)]
    [InlineData("x is < 7 and (", ">= 3 or > 50) or not <= 0;", 8)]
    [InlineData("x is < 7 and (>=", "3 or > 50) or not <= 0;", 8)]
    [InlineData("x is < 7 and (>= 3", "or > 50) or not <= 0;", 8)]
    [InlineData("x is < 7 and (>= 3 or", "> 50) or not <= 0;", 8)]
    [InlineData("x is < 7 and (>= 3 or >", "50) or not <= 0;", 8)]
    [InlineData("x is < 7 and (>= 3 or > 50", ") or not <= 0;", 8)]
    [InlineData("x is < 7 and (>= 3 or > 50)", "or not <= 0;", 8)]
    [InlineData("x is < 7 and (>= 3 or > 50) or", "not <= 0;", 8)]
    [InlineData("x is < 7 and (>= 3 or > 50) or not", "<= 0;", 8)]
    [InlineData("x is < 7 and (>= 3 or > 50) or not <=", "0;", 8)]
    [InlineData("x is < 7 and (>= 3 or > 50) or not <= 0", ";", 8)]
    public Task IndentPatternsInFieldDeclarationCSharp9(string line1, string line2, int expectedIndentation)
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            $$"""

            class C
            {
                static int x = 7;
                bool y = {{line1}}
            {{line2}}
            }
            """,
            indentationLine: 5,
            expectedIndentation);

    [WpfTheory]
    [InlineData("<", "7 and (>= 3 or > 50) or not <= 0", 12)]
    [InlineData("< 7", "and (>= 3 or > 50) or not <= 0", 12)]
    [InlineData("< 7 and", "(>= 3 or > 50) or not <= 0", 12)]
    [InlineData("< 7 and (", ">= 3 or > 50) or not <= 0", 12)]
    [InlineData("< 7 and (>=", "3 or > 50) or not <= 0", 12)]
    [InlineData("< 7 and (>= 3", "or > 50) or not <= 0", 12)]
    [InlineData("< 7 and (>= 3 or", "> 50) or not <= 0", 12)]
    [InlineData("< 7 and (>= 3 or >", "50) or not <= 0", 12)]
    [InlineData("< 7 and (>= 3 or > 50", ") or not <= 0", 12)]
    [InlineData("< 7 and (>= 3 or > 50)", "or not <= 0", 12)]
    [InlineData("< 7 and (>= 3 or > 50) or", "not <= 0", 12)]
    [InlineData("< 7 and (>= 3 or > 50) or not", "<= 0", 12)]
    [InlineData("< 7 and (>= 3 or > 50) or not <=", "0", 12)]
    public Task IndentPatternsInSwitchCSharp9(string line1, string line2, int expectedIndentation)
        => AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            $$"""

            class C
            {
                void M()
                {
                    var x = 7;
                    var y = x switch
                    {
                        {{line1}}
            {{line2}} => true,
                        _ => false
                    };
                }
            }
            """,
            indentationLine: 9,
            expectedIndentation);

    private static async Task AssertIndentUsingSmartTokenFormatterAsync(
        string code,
        char ch,
        int indentationLine,
        int? expectedIndentation)
    {
        await AssertIndentUsingSmartTokenFormatterAsync(code, ch, indentationLine, expectedIndentation, useTabs: false).ConfigureAwait(false);
        await AssertIndentUsingSmartTokenFormatterAsync(code.Replace("    ", "\t"), ch, indentationLine, expectedIndentation, useTabs: true).ConfigureAwait(false);
    }

    private static async Task AssertIndentUsingSmartTokenFormatterAsync(
        string code,
        char ch,
        int indentationLine,
        int? expectedIndentation,
        bool useTabs)
    {
        // create tree service
        using var workspace = EditorTestWorkspace.CreateCSharp(code);

        var hostdoc = workspace.Documents.First();
        var buffer = hostdoc.GetTextBuffer();
        var snapshot = buffer.CurrentSnapshot;
        var line = snapshot.GetLineFromLineNumber(indentationLine);
        var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

        var root = (await document.GetSyntaxRootAsync()) as CompilationUnitSyntax;

        var options = new IndentationOptions(
            new CSharpSyntaxFormattingOptions() { LineFormatting = new() { UseTabs = useTabs } });

        Assert.True(
            CSharpIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(
                Formatter.GetDefaultFormattingRules(document),
                root, line.AsTextLine(), options, out _));

        var actualIndentation = await GetSmartTokenFormatterIndentationWorkerAsync(workspace, buffer, indentationLine, ch, useTabs);
        Assert.Equal(expectedIndentation.Value, actualIndentation);
    }

    private async Task AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
        string code,
        int indentationLine,
        int? expectedIndentation,
        IndentStyle indentStyle = IndentStyle.Smart)
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(code, indentationLine, expectedIndentation, useTabs: false, indentStyle).ConfigureAwait(false);
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(code.Replace("    ", "\t"), indentationLine, expectedIndentation, useTabs: true, indentStyle).ConfigureAwait(false);
    }

    private async Task AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
        string code,
        int indentationLine,
        int? expectedIndentation,
        bool useTabs,
        IndentStyle indentStyle)
    {
        // create tree service
        using var workspace = EditorTestWorkspace.CreateCSharp(code);
        var hostdoc = workspace.Documents.First();
        var buffer = hostdoc.GetTextBuffer();
        var snapshot = buffer.CurrentSnapshot;

        var line = snapshot.GetLineFromLineNumber(indentationLine);

        var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

        var root = (await document.GetSyntaxRootAsync()) as CompilationUnitSyntax;

        var options = new IndentationOptions(new CSharpSyntaxFormattingOptions() { LineFormatting = new() { UseTabs = useTabs } })
        {
            IndentStyle = indentStyle
        };

        Assert.False(
            CSharpIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(
                Formatter.GetDefaultFormattingRules(document),
                root, line.AsTextLine(), options, out _));

        TestIndentation(workspace, indentationLine, expectedIndentation, indentStyle, useTabs);
    }
}
