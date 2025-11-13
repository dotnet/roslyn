// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using IndentStyle = Microsoft.CodeAnalysis.Formatting.FormattingOptions2.IndentStyle;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation;

[Trait(Traits.Feature, Traits.Features.SmartIndent)]
public sealed partial class SmartIndenterTests : CSharpFormatterTestsBase
{
    private static readonly TestComposition s_compositionWithTestFormattingRules = EditorTestCompositions.EditorFeatures
        .AddParts(typeof(TestFormattingRuleFactoryServiceFactory));

    public SmartIndenterTests(ITestOutputHelper output) : base(output) { }

    [WpfFact]
    public void EmptyFile()
        => AssertSmartIndent(
            code: string.Empty,
            indentationLine: 0,
            expectedIndentation: 0);

    [WpfFact]
    public void NoPreviousLine()
        => AssertSmartIndent(
            """
            #region Test

            #warning 0
            #undef SYMBOL
            #define SYMBOL
            #if false
            #elif true
            #else
            #endif
            #pragma warning disable 99999
            #goo

            #endregion


            """,
            indentationLine: 13,
            expectedIndentation: 0);

    [WpfFact]
    public void EndOfFileInactive()
        => AssertSmartIndent(
            """

                // Line 1
            #if false
            #endif


            """,
            indentationLine: 4,
            expectedIndentation: 0);

    [WpfFact]
    public void EndOfFileInactive2()
        => AssertSmartIndent(
            """

                // Line 1
            #if false
            #endif
            // Line 2


            """,
            indentationLine: 5,
            expectedIndentation: 0);

    [WpfFact]
    public void Comments()
        => AssertSmartIndent(
            """
            using System;

            class Class
            {
                // Comments
                /// Xml Comments


            """,
            indentationLine: 6,
            expectedIndentation: 4);

    [WpfFact]
    public void TestExplicitNoneIndentStyle()
        => AssertSmartIndent(
            """
            using System;

            class Class
            {
                // Comments
                /// Xml Comments


            """,
            indentationLine: 6,
            expectedIndentation: 0,
            indentStyle: IndentStyle.None);

    [WpfFact]
    public void UsingDirective()
        => AssertSmartIndent(
            """
            using System;


            """,
            indentationLine: 1,
            expectedIndentation: 0);

    [WpfFact]
    public void DottedName()
        => AssertSmartIndent(
            """
            using System.


            """,
            indentationLine: 1,
            expectedIndentation: 4);

    [WpfFact]
    public void Namespace()
    {
        var code = """
            using System;

            namespace NS


            """;
        AssertSmartIndent(
            code,
            indentationLine: 3,
            expectedIndentation: 4);

        AssertSmartIndent(
            code,
            indentationLine: 4,
            expectedIndentation: 4);
    }

    [WpfFact]
    public void NamespaceDottedName()
        => AssertSmartIndent(
            """
            using System;

            namespace NS.


            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact]
    public void NamespaceBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {


            """,
            indentationLine: 4,
            expectedIndentation: 4);

    [WpfFact]
    public void FileScopedNamespace()
    {
        var code = """
            using System;

            namespace NS;



            """;
        AssertSmartIndent(
            code,
            indentationLine: 1,
            expectedIndentation: 0);

        AssertSmartIndent(
            code,
            indentationLine: 2,
            expectedIndentation: 0);

        AssertSmartIndent(
            code,
            indentationLine: 4,
            expectedIndentation: 0);
    }

    [WpfFact]
    public void Class()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class


            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact]
    public void ClassBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {


            """,
            indentationLine: 6,
            expectedIndentation: 8);

    [WpfFact]
    public void Method()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()


            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfFact]
    public void MethodBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {


            """,
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfFact]
    public void Property()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static string Name


            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfFact]
    public void PropertyGetBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    private string name;
                    public string Names
                    {
                        get


            """,
            indentationLine: 10,
            expectedIndentation: 16);

    [WpfFact]
    public void PropertySetBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    private static string name;
                    public static string Names
                    {
                        set


            """,
            indentationLine: 10,
            expectedIndentation: 16);

    [WpfFact]
    public void Statement()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        int i = 10;


            """,
            indentationLine: 9,
            expectedIndentation: 12);

    [WpfFact]
    public void FieldInitializer()
        => AssertSmartIndent(
            """
            class C
            {
                int i = 2;

            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact]
    public void FieldInitializerWithNamespace()
        => AssertSmartIndent(
            """
            namespace NS
            {
                class C
                {
                    C c = new C();


            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact]
    public void MethodCall()
    {
        var code = """
            class c
            {
                void Method()
                {
                    M(
                        a: 1, 
                        b: 1);
                }
            }
            """;

        AssertSmartIndent(
            code,
            indentationLine: 5,
            expectedIndentation: 12);
        AssertSmartIndent(
            code,
            indentationLine: 6,
            expectedIndentation: 12);
        AssertSmartIndent(
            code,
            indentationLine: 7,
            expectedIndentation: 4);
    }

    [WpfFact]
    public void Switch()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        switch (10)


            """,
            indentationLine: 9,
            expectedIndentation: 16);

    [WpfFact]
    public void SwitchBody()
        => AssertSmartIndent(
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
            indentationLine: 10,
            expectedIndentation: 16);

    [WpfFact]
    public void SwitchCase()
        => AssertSmartIndent(
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


            """,
            indentationLine: 11,
            expectedIndentation: 20);

    [WpfFact]
    public void ExtendedPropertyPattern()
        => AssertSmartIndent(
            """

            class C
            {
                void M()
                {
                    _ = this is
                    {


            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfFact]
    public void ExtendedPropertyPattern_WithPattern()
    {
        var code = """

            class C
            {
                void M()
                {
                    _ = this is
                    {

                        A.B: 1,


            """;
        AssertSmartIndent(
            code,
            indentationLine: 7,
            expectedIndentation: 12);
        AssertSmartIndent(
            code,
            indentationLine: 9,
            expectedIndentation: 12);
    }

    [WpfFact]
    public void Block()
        => AssertSmartIndent(
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
            indentationLine: 12,
            expectedIndentation: 24);

    [WpfFact]
    public void MultilineStatement1()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    void Method()
                    {
                        int i = 10 +


            """,
            indentationLine: 9,
            expectedIndentation: 16);

    [WpfFact]
    public void MultilineStatement2()
        => AssertSmartIndent(
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


            """,
            indentationLine: 10,
            expectedIndentation: 20);

    // Bug number 902477
    [WpfFact]
    public void Comments2()
        => AssertSmartIndent(
            """
            class Class
            {
                void Method()
                {
                    if (true) // Test

                }
            }

            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void AfterCompletedBlock()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    foreach(var a in x) {}

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact]
    public void AfterCompletedBlockNestedInOtherBlock()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    foreach(var a in x) {{}

                    }
                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void AfterTopLevelAttribute()
        => AssertSmartIndent(
            """
            class Program
            {
                [Attr]

            }


            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537802")]
    public void EmbeddedStatement()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        Console.WriteLine(1);

                }
            }


            """,
            indentationLine: 6,
            expectedIndentation: 8);

    [WpfTheory(Skip = "https://github.com/dotnet/roslyn/issues/50063")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/50063")]
    [InlineData("do")]
    [InlineData("for (;;)")]
    [InlineData("if (true)")]
    [InlineData("void localFunction()")]
    [InlineData("static void localFunction()")]
    public void EmbeddedStatement2(string statement)
        => AssertSmartIndent(
            $$"""
            class Program
            {
                static void Main(string[] args)
                {
            {{statement}}

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537883")]
    public void EnterAfterComment()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    int a; // enter

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538121")]
    public void NestedBlock1()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    {

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void NestedEmbeddedStatement1()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        if (true)
                            if (true)
                                args = null;

                }
            }

            """,
            indentationLine: 8,
            expectedIndentation: 8);

    [WpfFact]
    public void NestedEmbeddedStatement2()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        if (true)
                            if (true)
                                { }

                }
            }

            """,
            indentationLine: 8,
            expectedIndentation: 8);

    [WpfFact]
    public void NestedEmbeddedStatement3()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        if (true)
                            if (true)
                                { return; }

                }
            }

            """,
            indentationLine: 8,
            expectedIndentation: 8);

    [WpfFact]
    public void NestedEmbeddedStatement4()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        if (true)
                            if (true)
                                args = null;


            """,
            indentationLine: 8,
            expectedIndentation: 8);

    [WpfFact]
    public void NestedEmbeddedStatement5()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        if (true)
                            if (true)
                                { }


            """,
            indentationLine: 8,
            expectedIndentation: 8);

    [WpfFact]
    public void NestedEmbeddedStatement6()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        if (true)
                            if (true)
                                { return; }


            """,
            indentationLine: 8,
            expectedIndentation: 8);

    [WpfFact]
    public void NestedEmbeddedStatement7()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        if (true)
                            if (true)
                                return;
                            else
                                return;


            """,
            indentationLine: 10,
            expectedIndentation: 8);

    [WpfFact]
    public void NestedEmbeddedStatement8()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        if (true)
                            if (true)
                                return;
                            else
                                return;

                }
            }
            """,
            indentationLine: 10,
            expectedIndentation: 8);

    [WpfFact]
    public void Label1()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                Label:
                    Console.WriteLine(1);

                }
            }


            """,
            indentationLine: 6,
            expectedIndentation: 8);

    [WpfFact]
    public void Label2()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                Label: Console.WriteLine(1);

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact]
    public void Label3()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    switch(args.GetType())
                    {
                        case 1:
                            Console.WriteLine(1);

                }
            }


            """,
            indentationLine: 8,
            expectedIndentation: 16);

    [WpfFact]
    public void Label4()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    switch(args.GetType())
                    {
                        case 1: Console.WriteLine(1);

                }
            }


            """,
            indentationLine: 7,
            expectedIndentation: 16);

    [WpfFact]
    public void Label5()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    switch(args.GetType())
                    {
                        case 1:

                }
            }


            """,
            indentationLine: 7,
            expectedIndentation: 16);

    [WpfFact]
    public void Label6()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                Label:

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/15866")]
    public void LabelAtColumn0WithIfStatement()
        => AssertSmartIndent(
            """
            namespace ConsoleApp
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        int i = 0;
            loop:
                        if (i > 10)
                            return;

                    }
                }
            }


            """,
            indentationLine: 10,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/15866")]
    public void LabelAtColumn0WithWhileStatement()
        => AssertSmartIndent(
            """
            namespace ConsoleApp
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        int i = 0;
            loop:
                        while (i > 10)
                            return;

                    }
                }
            }


            """,
            indentationLine: 10,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/15866")]
    public void IndentedLabelWithIfStatement()
        => AssertSmartIndent(
            """
            namespace ConsoleApp
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        int i = 0;
                    loop:
                        if (i > 10)
                            return;

                    }
                }
            }


            """,
            indentationLine: 10,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/35105")]
    public void LabelAfterIfStatementWithoutBraces()
        => AssertSmartIndent(
            """
            class Test
            {
                static void Test()
                {
                    Test();

                label1:
                    Test();
                    if (true)
                        Test();

            label2:
                    
                }
            }


            """,
            indentationLine: 11,
            expectedIndentation: 8);

    [WpfFact]
    public void QueryExpression1()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var a = from c in

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 20);

    [WpfFact]
    public void QueryExpression2()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var a = from c in b

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 16);

    [WpfFact]
    public void QueryExpression3()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var a = from c in b.

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 16);

    [WpfFact]
    public void QueryExpression4()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var a = from c in b where c > 10

                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 16);

    [WpfFact]
    public void QueryExpression5()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var a = from c in
                                from b in G

                }
            }


            """,
            indentationLine: 6,
            expectedIndentation: 20);

    [WpfFact]
    public void QueryExpression6()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var a = from c in
                                from b in G
                                select b

                }
            }


            """,
            indentationLine: 7,
            expectedIndentation: 20);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538779")]
    public void QueryExpression7()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var q = from string s in args

                            where s == null
                            select s;
                }
            }


            """,
            indentationLine: 5,
            expectedIndentation: 16);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538779")]
    public void QueryExpression8()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var q = from string s in args.
                                          b.c.

                            where s == null
                            select s;
                }
            }


            """,
            indentationLine: 6,
            expectedIndentation: 30);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538780")]
    public void QueryExpression9()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var q = from string s in args
                            where s == null

                            select s;
                }
            }


            """,
            indentationLine: 6,
            expectedIndentation: 16);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538780")]
    public void QueryExpression10()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var q = from string s in args
                            where s == null
                                    == 1

                            select s;
                }
            }


            """,
            indentationLine: 7,
            expectedIndentation: 24);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538333")]
    public void Statement1()
        => AssertSmartIndent(
            """
            class Program
            {
                void Test() { }

            }
            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538933")]
    public void EndOfFile1()
        => AssertSmartIndent(
            """
            class Program
            {
                void Test() 
                {
                    int i;



            """,
            indentationLine: 6,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539059")]
    public void VerbatimString()
        => AssertSmartIndent(
            """
            class Program
            {
                void Test() 
                {
                    var goo = @"Goo


            """,
            indentationLine: 5,
            expectedIndentation: 0);

    [WpfFact]
    public void RawString1()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = """

                        """;
                }
            }

            """",
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void RawString2()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = """
                        Goo

                        """


            """",
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact]
    public void RawString3()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = """
                    Goo

                        """


            """",
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/60946")]
    public void RawString4()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = """
                            Goo

                        """


            """",
            indentationLine: 6,
            expectedIndentation: 16);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/60946")]
    public void RawString5()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = """
                            Goo


                        """


            """",
            indentationLine: 7,
            expectedIndentation: 16);

    [WpfFact]
    public void RawString6()
    {
        var code = """"
            var goo = """

                """;

            """";

        AssertSmartIndent(
            code,
            indentationLine: 1,
            expectedIndentation: 4);
        AssertSmartIndent(
            code,
            indentationLine: 0,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66368")]
    public void UnterminatedRawString1()
        => AssertSmartIndent(
            """""""
            var x = """"""
                1
                2
                3
                4
                5
                """;

            """"""",
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66368")]
    public void UnterminatedInterpolatedRawString1()
        => AssertSmartIndent(
            """""""
            var x = $""""""
                1
                2
                3
                4
                5
                """;

            """"""",
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact]
    public void InterpolatedRawString1()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = $"""

                        """;
                }
            }

            """",
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void InterpolatedRawString2()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = $"""
                        Goo

                        """


            """",
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact]
    public void InterpolatedRawString3()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = $"""
                    Goo

                        """


            """",
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/60946")]
    public void InterpolatedRawString4()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = $"""
                            Goo

                        """


            """",
            indentationLine: 6,
            expectedIndentation: 16);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/60946")]
    public void InterpolatedRawString5()
        => AssertSmartIndent(
            """"
            class Program
            {
                void Test() 
                {
                    var goo = $"""
                            Goo


                        """


            """",
            indentationLine: 7,
            expectedIndentation: 16);

    [WpfFact]
    public void InterpolatedRawString6()
    {
        var code = """"
            var goo = $"""

                """;

            """";

        AssertSmartIndent(
            code,
            indentationLine: 1,
            expectedIndentation: 4);
        AssertSmartIndent(
            code,
            indentationLine: 0,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/60946")]
    public void InterpolatedRawString7()
    {
        var code = """"
            class Program
            {
                void Test() 
                {
                    var goo = $"""
                            Goo{nameof(goo)}


                        """


            """";

        AssertSmartIndent(
            code,
            indentationLine: 6,
            expectedIndentation: 16);
        AssertSmartIndent(
            code,
            indentationLine: 7,
            expectedIndentation: 16);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/60946")]
    public void InterpolatedRawString8()
    {
        var code = """"
            class Program
            {
                void Test() 
                {
                    var goo = $"""
                            Goo{
            nameof(goo)}


                        """


            """";

        AssertSmartIndent(
            code,
            indentationLine: 7,
            expectedIndentation: 16);
        AssertSmartIndent(
            code,
            indentationLine: 8,
            expectedIndentation: 16);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539892")]
    public void Bug5994()
        => AssertSmartIndent(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    var studentQuery =
                        from student in students
                           group student by (avg == 0 ? 0 : avg / 10) into g

                        ;
                }
            }

            """,
            indentationLine: 11,
            expectedIndentation: 15);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539990")]
    public void Bug6124()
        => AssertSmartIndent(
            """
            class Program
            {
                void Main()
                {
                    var commandLine = string.Format(
                        ",
                        0,
                        42,
                        string.Format(",
                            0,
                            0),

                        0);
                }
            }
            """,
            indentationLine: 11,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539990")]
    public void Bug6124_1()
        => AssertSmartIndent(
            """
            class Program
            {
                void Main()
                {
                    var commandLine = string.Format(
                        ",
                        0,
                        42,
                        string.Format(",
                            0,
                            0

            ),
                        0);
                }
            }
            """,
            indentationLine: 11,
            expectedIndentation: 16);

    [WpfFact]
    public void AfterIfWithSingleStatementInTopLevelMethod_Bug7291_1()
        => AssertSmartIndent(
            """
            int fact(int x)
            {
                if (x < 1)
                    return 1;


            """,
            indentationLine: 4,
            expectedIndentation: 4,
            options: TestOptions.Script);

    [WpfFact]
    public void AfterIfWithSingleStatementInTopLevelMethod_Bug7291_2()
        => AssertSmartIndent(
            """
            int fact(int x)
            {
                if (x < 1)
                    return 1;

            }

            """,
            indentationLine: 4,
            expectedIndentation: 4,
            options: TestOptions.Script);

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540634")]
    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544268")]
    public void FirstArgumentInArgumentList()
        => AssertSmartIndent(
            """
            class Program
            {
                public Program(
                    string a,
                    int b,
                    bool c)
                    : this(
                        a,
                        new Program(

                            ",
                            3,
                            true),
                        b,
                        c)
                {
                }
            }
            """,
            indentationLine: 9,
            expectedIndentation: 16);

    [WpfFact]
    public void ForLoop()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    for (;      
                    ;) { }
                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void CallBaseCtor()
        => AssertSmartIndent(
            """
            class Program
            {
                public Program() :           
                base() { }
            }
            """,
            indentationLine: 3,
            expectedIndentation: 8);

    [WpfFact]
    public void MultipleDeclarations()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    int i,
                    j = 42;
                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void CloseBracket()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var i = new int[1]
                    ;
                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void SwitchLabel()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    switch (args[0])
                    {
                        case "goo":

                        case "bar":
                            break;
                    }
                }
            }
            """,
            indentationLine: 7,
            expectedIndentation: 16);

    [WpfFact]
    public void TypeParameters()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Goo<T1,                 
            T2>() { }
            }
            """,
            indentationLine: 3,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542428")]
    public void TypeArguments()
        => AssertSmartIndent(
            """
            class Program
            {
                    static void Goo<T1, T2>(T1 t1, T2 t2) { }

                    static void Main(string[] args)
                    {
                        Goo<int, 
                        int>(4, 2);
                    }
            }
            """,
            indentationLine: 7,
            expectedIndentation: 16);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542983")]
    public void ConstructorInitializer1()
        => AssertSmartIndent(
            """
            public class Asset
            {
                public Asset() : this(


            """,
            indentationLine: 3,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542983")]
    public void ConstructorInitializer2()
        => AssertSmartIndent(
            """
            public class Asset
            {
                public Asset()
                    : this(


            """,
            indentationLine: 4,
            expectedIndentation: 14);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542983")]
    public void ConstructorInitializer3()
        => AssertSmartIndent(
            """
            public class Asset
            {
                public Asset() :
                    this(


            """,
            indentationLine: 4,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543131")]
    public void LockStatement1()
        => AssertSmartIndent(
            """
            using System;
            class Program
            {
                static object lockObj = new object();
                static int Main()
                {
                    int sum = 0;
                    lock (lockObj)
                        try
                        { sum = 0; }

                    return sum;
                }
            }
            """,
            indentationLine: 10,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543533")]
    public void ConstructorInitializer()
        => AssertSmartIndent(
            """
            public class Asset
            {
                public Asset() :


            """,
            indentationLine: 3,
            expectedIndentation: 8);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/952803")]
    public void ArrayInitializer()
        => AssertSmartIndent(
            """
            using System.Collections.ObjectModel;

            class Program
            {
                static void Main(string[] args)
                {
                    new ReadOnlyCollection<int>(new int[]
                    {

                    });
                }
            }

            """,
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
    public void LambdaEmbededInExpression()
        => AssertSmartIndent(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
             
            class Program
            {
                static void Main(string[] args)
                {
                    using (var var = new GooClass(() =>
                    {

                    }))
                    {
                        var var2 = var;
                    }
                }
            }
             
            class GooClass : IDisposable
            {
                public GooClass(Action a)
                {
                }
             
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            indentationLine: 10,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
    public void LambdaEmbededInExpression_1()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    using (var var = new GooClass(() =>

                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 16);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
    public void LambdaEmbededInExpression_3()
        => AssertSmartIndent(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    using (var var = new GooClass(() =>
                    {

                    }))
                    {
                        var var2 = var;
                    }
                }
            }

            class GooClass : IDisposable
            {
                public GooClass(Action a)
                {
                }

                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
    public void LambdaEmbededInExpression_2()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    using (var var = new GooClass(
                        () =>

                }
            }
            """,
            indentationLine: 6,
            expectedIndentation: 16);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
    public void LambdaEmbededInExpression_4()
        => AssertSmartIndent(
            """
            using System;
            class Class
            {
                public void Method()
                {
                    OtherMethod(() =>
                    {
                        var aaa = new object(); if (aaa != null)
                        {
                            var bbb = new object();

                        }
                    });
                }
                private void OtherMethod(Action action) { }
            }
            """,
            indentationLine: 10,
            expectedIndentation: 16);

    [WpfFact]
    public void LambdaDefaultParameter_EnterAfterParamList()
        => AssertSmartIndent(
            """
            class Program
            {
                public void Main()
                {
                    var lam = (int x = 7) =>
                    
                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact]
    public void LambdaDefaultParameter_EnterAfterEquals()
        => AssertSmartIndent(
            """
            class Program
            {
                public void Main()
                {
                    var lam = (int x =
                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact]
    public void LambdaDefaultParameter_EnterBeforeEquals()
    {
        var code = """
            class Program
            {
                public void Main()
                {
                    var lam = (int x
                                = 10,
                                int y
                                = 20) => x + y;
                }
            }
            """;
        AssertSmartIndent(
            code,
            indentationLine: 5,
            expectedIndentation: 12);

        AssertSmartIndent(
            code,
            indentationLine: 6,
            expectedIndentation: 20);

        AssertSmartIndent(
            code,
            indentationLine: 7,
            expectedIndentation: 20);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530074")]
    public void EnterInArgumentList1()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Main(args,

                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530074")]
    public void EnterInArgumentList2()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Main(args,
            )
                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/806266")]
    public void EnterInArgumentList3()
        => AssertSmartIndent(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = string.Format(1,

                }
            }
            """,
            indentationLine: 5,
            expectedIndentation: 12);

    [WpfFact, WorkItem(9216, "DevDiv_Projects/Roslyn")]
    public void FollowPreviousLineInMultilineStatements()
        => AssertSmartIndent("""
            class Program
            {
                static void Main(string[] args)
                {
                    var accessibleConstructors = normalType.InstanceConstructors
                                                   .Where(c => c.IsAccessibleWithin(within))
                                                   .Where(s => s.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
            .Sort(symbolDisplayService, invocationExpression.GetLocation(), semanticModel);
                }
            }
            """, indentationLine: 7, expectedIndentation: 39);

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/648068")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674611")]
    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void AtBeginningOfSpanInNugget()
        => AssertSmartIndentInProjection(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|
            $$Console.WriteLine();|]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 4);

    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void AtEndOfSpanInNugget()
        => AssertSmartIndentInProjection(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|Console.WriteLine();
            $$|]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 4);

    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void InMiddleOfSpanAtStartOfNugget()
    {

        // Again, it doesn't matter where Console _is_ in this case -we format based on
        // where we think it _should_ be.  So the position is one indent level past the base
        // for the nugget (where we think the statement should be), plus one more since it is
        // a continuation
        AssertSmartIndentInProjection(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|Console.Wri
            $$teLine();|]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 8);
    }

    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void InMiddleOfSpanInsideOfNugget()
    {

        // Again, it doesn't matter where Console _is_ in this case -we format based on
        // where we think it _should_ be.  So the position is one indent level past the base
        // for the nugget (where we think the statement should be), plus one more since it is
        // a continuation
        AssertSmartIndentInProjection(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|
                          Console.Wri
            $$teLine();|]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 8);
    }

    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void AfterStatementInNugget()
        => AssertSmartIndentInProjection(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|
                          Console.WriteLine();
            $$
                        |]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 4);

    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void AfterStatementOnFirstLineOfNugget()
    {

        // TODO: Fix this to indent relative to the previous statement,
        // instead of relative to the containing scope.  I.e. Format like:
        //     <%Console.WriteLine();
        //       Console.WriteLine(); %>
        // instead of
        //     <%Console.WriteLine();
        //         Console.WriteLine(); %>
        // C# had the desired behavior in Dev12, where VB had the same behavior
        // as Roslyn has.  The Roslyn formatting engine currently always formats
        // each statement independently, so let's not change that just for Venus
        AssertSmartIndentInProjection(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|Console.WriteLine();
            $$
            |]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 4);
    }

    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void InQueryOnFistLineOfNugget()
        => AssertSmartIndentInProjection(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|var q = from
            $$
            |]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 8);

    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void InQueryInNugget()
        => AssertSmartIndentInProjection(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|
                          var q = from
            $$
            |]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 8);

    [WorkItem(9216, "DevDiv_Projects/Roslyn")]
    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void InsideBracesInNugget()
        => AssertSmartIndentInProjection("""
            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                                {|S1:[|if (true)
                    {
            $$
                    }|]|}
            #line default
            #line hidden
                }
            }
            """, BaseIndentationOfNugget + 8);

    [WorkItem(9216, "DevDiv_Projects/Roslyn")]
    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void AfterEmbeddedStatementOnFirstLineOfNugget()
    {

        // In this case, we align the next statement with the "if" (though we _don't_
        // align the braces with it :S)
        AssertSmartIndentInProjection("""
            class Program
                    {
                        static void Main(string[] args)
                        {
                    #line "Goo.aspx", 27
                                        {|S1:[|if (true)
                            {
                            }
                            $$
            |]|}
                    #line default
                    #line hidden
                        }
                    }
            """,
            expectedIndentation: BaseIndentationOfNugget + 2);
    }

    [WorkItem(9216, "DevDiv_Projects/Roslyn")]
    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void AfterEmbeddedStatementInNugget()
    {

        // In this case we align with the "if", - the base indentation we pass in doesn't matter.
        AssertSmartIndentInProjection("""
            class Program
                    {
                        static void Main(string[] args)
                        {
                    #line "Goo.aspx", 27
                                        {|S1:[|
                        if (true)
                        {
                        }
            $$
            |]|}
                    #line default
                    #line hidden
                        }
                    }
            """,
            expectedIndentation: BaseIndentationOfNugget + 4);
    }

    // this is the special case where the smart indenter 
    // aligns with the base or base + 4th position.
    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void AfterSwitchStatementAtEndOfNugget()
    {

        // It's yuck that I saw differences depending on where the end of the nugget is
        // but I did, so lets add a test.
        AssertSmartIndentInProjection("""

            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|switch (10)
                        {
                            case 10:
            $$
                        }|]|}
            #line default
            #line hidden
                }
            }
            """,
            expectedIndentation: BaseIndentationOfNugget + 12);
    }

    // this is the special case where the smart indenter 
    // aligns with the base or base + 4th position.
    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void AfterSwitchStatementInNugget()
        => AssertSmartIndentInProjection("""

            class Program
            {
                static void Main(string[] args)
                {
            #line "Goo.aspx", 27
                        {|S1:[|switch (10)
                        {
                            case 10:
            $$
                        }
            |]|}
            #line default
            #line hidden
                }
            }
            """,
            expectedIndentation: BaseIndentationOfNugget + 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529876"), Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
    public void InEmptyNugget()
        => AssertSmartIndentInProjection("""
            class Program
                    {
                        static void Main(string[] args)
                        {
                    #line "Goo.aspx", 27
                        {|S1:[|
            $$|]|}
                    #line default
                    #line hidden
                        }
                    }
            """,
            expectedIndentation: BaseIndentationOfNugget + 4);

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1190278")]
    [WpfFact, Trait(Traits.Feature, Traits.Features.Venus)]
    public void GetNextTokenForFormattingSpanCalculationIncludesZeroWidthToken_CS()
        => AssertSmartIndentInProjection("""
            //------------------------------------------------------------------------------
            // <auto-generated>
            //     This code was generated by a tool.
            //     Runtime Version:4.0.30319.42000
            //
            //     Changes to this file may cause incorrect behavior and will be lost if
            //     the code is regenerated.
            // </auto-generated>
            //------------------------------------------------------------------------------

            namespace ASP {
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Net;
            using System.Web;
            using System.Web.Helpers;
            using System.Web.Security;
            using System.Web.UI;
            using System.Web.WebPages;
            using System.Web.WebPages.Html;
            using WebMatrix.Data;
            using WebMatrix.WebData;
            using Microsoft.Web.WebPages.OAuth;
            using DotNetOpenAuth.AspNet;

            public class _Page_Default_cshtml : System.Web.WebPages.WebPage {
            #line hidden
            public _Page_Default_cshtml() {
            }
            protected System.Web.HttpApplication ApplicationInstance {
            get {
            return ((System.Web.HttpApplication)(Context.ApplicationInstance));
            }
            }
            public override void Execute() {

            #line 1 "C:\Users\basoundr\Documents\Visual Studio 2015\WebSites\WebSite6\Default.cshtml"

                {|S1:[|public class LanguagePreference
                    {

                    }

            if (!File.Exists(physicalPath))
            {
                Context.Response.SetStatus(HttpStatusCode.NotFound);
                return;
            }
            $$
                string[] languages = Context.Request.UserLanguages;

            if(languages == null || languages.Length == 0)
            {

                Response.Redirect()
                }

            |]|}
            #line default
            #line hidden
            }
            }
            }
            """,
            expectedIndentation: 24);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530948"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
    public void CommaSeparatedListEnumMembers()
        => AssertSmartIndent(
            """
            enum MyEnum
            {
                e1,

            }
            """,
            indentationLine: 3,
            expectedIndentation: 4);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530796"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
    public void RelativeIndentationForBracesInExpression()
        => AssertSmartIndent(
            """
            class C
            {
                void M(C c)
                {
                    M(new C()
                    {

                    });
                }
            }

            """,
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
    public void SwitchSection()
    {
        var code = """
            class C
            {
                void Method()
                {
                    switch (i)
                    {

                        case 1:

                        case 2:

                            int i2 = 10;

                        case 4:

                    }
                }
            }
            """;

        AssertSmartIndent(
            code,
            indentationLine: 6,
            expectedIndentation: 12);

        AssertSmartIndent(
            code,
            indentationLine: 8,
            expectedIndentation: 16);

        AssertSmartIndent(
            code,
            indentationLine: 10,
            expectedIndentation: 16);

        AssertSmartIndent(
            code,
            indentationLine: 12,
            expectedIndentation: 16);

        AssertSmartIndent(
            code,
            indentationLine: 14,
            expectedIndentation: 16);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
    public void SwitchSection2()
    {
        var code = """
            class C
            {
                void Method()
                {
                    switch (i)
                    {
                        // test

                        case 1:
                            // test

                        case 2:
                            // test

                            int i2 = 10;
                        // test

                        case 4:
                        // test

                    }
                }
            }
            """;

        AssertSmartIndent(
            code,
            indentationLine: 7,
            expectedIndentation: 12);

        AssertSmartIndent(
            code,
            indentationLine: 10,
            expectedIndentation: 16);

        AssertSmartIndent(
            code,
            indentationLine: 13,
            expectedIndentation: 16);

        AssertSmartIndent(
            code,
            indentationLine: 16,
            expectedIndentation: 12);

        AssertSmartIndent(
            code,
            indentationLine: 19,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
    public void CommentAtTheEndOfLine()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(); /* this is a comment */
                                         // that I would like to keep


                    // properly indented
                }
            }
            """;

        AssertSmartIndent(
            code,
            indentationLine: 8,
            expectedIndentation: 29);

        AssertSmartIndent(
            code,
            indentationLine: 9,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912735"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
    public void CommentAtTheEndOfLineWithExecutableAfterCaret()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    // A
                    // B


                    return;
                }
            }
            """;

        AssertSmartIndent(
            code,
            indentationLine: 8,
            expectedIndentation: 8);

        AssertSmartIndent(
            code,
            indentationLine: 9,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912735"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
    public void CommentAtTheEndOfLineInsideInitializer()
    {
        var code = """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var s = new List<string>
                                    {
                                        "",
                                                "",/*sdfsdfsdfsdf*/
                                                   // dfsdfsdfsdfsdf


                                    };
                }
            }
            """;

        AssertSmartIndent(
            code,
            indentationLine: 12,
            expectedIndentation: 39);

        AssertSmartIndent(
            code,
            indentationLine: 13,
            expectedIndentation: 36);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5495")]
    public void AfterBadQueryContinuationWithSelectOrGroupClause()
        => AssertSmartIndent(
            """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApplication1
            {
                class AutomapperConfig
                {
                    public static IEnumerable<string> ConfigureMappings(string name)
                    {
                        List<User> anEntireSlewOfItems = new List<User>();
                        List<UserViewModel> viewModels = new List<UserViewModel>();

                        var items = (from m in anEntireSlewOfItems into man

                         join at in viewModels on m.id equals at.id
                         join c in viewModels on m.name equals c.name
                         join ct in viewModels on m.phonenumber equals ct.phonenumber
                         where m.id == 1 &&
                             m.name == name
                         select new { M = true, I = at, AT = at }).ToList();
                        //Mapper.CreateMap<User, UserViewModel>()
                        //    .ForMember(t => t.)
                    }
                }

                class User
                {
                    public int id { get; set; }
                    public string name { get; set; }
                    public int phonenumber { get; set; }
                }

                class UserViewModel
                {
                    public int id { get; set; }
                    public string name { get; set; }
                    public int phonenumber { get; set; }
                }
            }
            """,
            indentationLine: 13,
            expectedIndentation: 25);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5495")]
    public void AfterPartialFromClause()
        => AssertSmartIndent(
            """

            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from x

                }
            }

            """,
            indentationLine: 8,
            expectedIndentation: 16);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5635")]
    public void ConstructorInitializerMissingBaseOrThisKeyword()
        => AssertSmartIndent(
            """

            class C
            {
                 C(string s)
                     :

            }

            """,
            indentationLine: 5,
            expectedIndentation: 8);

    [WpfFact]
    public void CreateIndentOperationForBrokenBracketedArgumentList()
        => AssertSmartIndent(
            """

            class Program
            {
                static void M()
                {
                    string (userInput == "Y")

                }
            }

            """,
            indentationLine: 6,
            expectedIndentation: 12);

    [WpfFact]
    public void PatternPropertyIndentFirst()
        => AssertSmartIndent(
            """

            class C
            {
                void M(object o)
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
    public void PatternPropertyIndentSecond()
        => AssertSmartIndent(
            """

            class C
            {
                void M(object o)
                {
                    var y = o is Point
                    {
                        X is 4,

                    }
                }
            }
            """,
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfFact]
    public void PatternPropertyIndentNestedFirst()
        => AssertSmartIndent(
            """

            class C
            {
                void M(object o)
                {
                    var y = o is Point
                    {
                        X is Widget 
                        {

                        },

                    }
                }
            }
            """,
            indentationLine: 9,
            expectedIndentation: 16);

    [WpfFact]
    public void PatternPropertyIndentNestedSecond()
        => AssertSmartIndent(
            """

            class C
            {
                void M(object o)
                {
                    var y = o is Point
                    {
                        X is Widget 
                        {
                            Y is 42,

                        },
                    }
                }
            }
            """,
            indentationLine: 10,
            expectedIndentation: 16);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/33253")]
    public void EnterAfterFluentSequences_1()
    {
        var code = """
            public class Test
            {
                public void Test()
                {
                    new List<DateTime>()
                        .Where(d => d.Kind == DateTimeKind.Local ||
                                    d.Kind == DateTimeKind.Utc)

                        .ToArray();
                }
            }
            """;

        AssertSmartIndent(
            code: code,
            indentationLine: 7,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/33253")]
    public void EnterAfterFluentSequences_2()
    {
        var code = """
            public class Test
            {
                public void Test()
                {
                    new List<DateTime>()
                            .Where(d => d.Kind == DateTimeKind.Local ||
                                        d.Kind == DateTimeKind.Utc)

                            .ToArray();
                }
            }
            """;

        AssertSmartIndent(
            code: code,
            indentationLine: 7,
            expectedIndentation: 16);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/33253")]
    public void EnterAfterFluentSequences_3()
    {
        var code = """
            public class Test
            {
                public void Test()
                {
                    new List<DateTime>().Where(d => d.Kind == DateTimeKind.Local ||
                                                    d.Kind == DateTimeKind.Utc)

                                        .ToArray();
                }
            }
            """;

        AssertSmartIndent(
            code: code,
            indentationLine: 6,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/33253")]
    public void EnterAfterFluentSequences_4()
    {
        var code = """
            public class Test
            {
                public void Test()
                {
                    new List<DateTime>().Where(d => d.Kind == DateTimeKind.Local || d.Kind == DateTimeKind.Utc)

                        .ToArray();
                }
            }
            """;

        AssertSmartIndent(
            code: code,
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/28752")]
    public void EnterAfterBlankLineAfterCommentedOutCode1()
    {
        var code = """
            class Test
            {
                public void Test()
                {
                    // comment


                }
            }
            """;

        AssertSmartIndent(
            code: code,
            indentationLine: 5,
            expectedIndentation: 8);

        AssertSmartIndent(
            code: code,
            indentationLine: 6,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/28752")]
    public void EnterAfterBlankLineAfterCommentedOutCode2()
    {
        var code = """

            class T
            {
                // comment



                // comment
                int i = 1;
            }
            """;

        AssertSmartIndent(
            code: code,
            indentationLine: 4,
            expectedIndentation: 4);

        AssertSmartIndent(
            code: code,
            indentationLine: 5,
            expectedIndentation: 4);

        AssertSmartIndent(
            code: code,
            indentationLine: 6,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38819")]
    public void IndentationOfReturnInFileWithTabs1()
    {
        var code = """

            public class Example
            {
            	public void Test(object session)
            	{
            		if (session == null)
            return;
            	}
            }
            """;
        // Ensure the test code doesn't get switched to spaces
        Assert.Contains("\t\tif (session == null)", code);
        AssertSmartIndent(
            code,
            indentationLine: 6,
            expectedIndentation: 12,
            useTabs: true,
            options: null,
            indentStyle: IndentStyle.Smart);
    }

    [WpfFact]
    public void Operator()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static Class operator +(Class x, Class y)


            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfFact]
    public void CastOperator()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static explicit operator Class(int x)


            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfFact]
    public void OperatorBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static Class operator +(Class x, Class y)
                    {


            """,
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfFact]
    public void CastOperatorBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static explicit operator Class(int x)
                    {


            """,
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfFact]
    public void CheckedOperator()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static Class operator checked +(Class x, Class y)


            """,
            indentationLine: 7,
            expectedIndentation: 12,
            options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void ExplicitCastCheckedOperator()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static explicit operator checked Class(int x)


            """,
            indentationLine: 7,
            expectedIndentation: 12,
            options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void CheckedOperatorBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static Class operator checked +(Class x, Class y)
                    {


            """,
            indentationLine: 8,
            expectedIndentation: 12,
            options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void ExplicitCastCheckedOperatorBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static explicit operator checked Class(int x)
                    {


            """,
            indentationLine: 8,
            expectedIndentation: 12,
            options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void UnsignedRightShift()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static Class operator >>>(Class x, Class y)


            """,
            indentationLine: 7,
            expectedIndentation: 12);

    [WpfFact]
    public void UnsignedRightShiftBody()
        => AssertSmartIndent(
            """
            using System;

            namespace NS
            {
                class Class
                {
                    public static Class operator >>>(Class x, Class y)
                    {


            """,
            indentationLine: 8,
            expectedIndentation: 12);

    [WpfTheory]
    [CombinatorialData]
    public void InstanceIncrementOperator([CombinatorialValues("++", "--")] string op, bool isChecked)
    {
        var code = """
            using System;

            namespace NS
            {
                class Class
                {
                    public void operator 
            """ + (isChecked ? "checked " : "") + op + """
            ()


            """;

        AssertSmartIndent(
            code,
            indentationLine: 7,
            expectedIndentation: 12);
    }

    [WpfTheory]
    [CombinatorialData]
    public void InstanceIncrementOperatorBody([CombinatorialValues("++", "--")] string op, bool isChecked)
    {
        var code = """
            using System;

            namespace NS
            {
                class Class
                {
                    public void operator 
            """ + (isChecked ? "checked " : "") + op + """
            ()
                    {


            """;

        AssertSmartIndent(
            code,
            indentationLine: 8,
            expectedIndentation: 12);
    }

    [WpfTheory]
    [CombinatorialData]
    public void InstanceCompoundAssignmentOperator([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool isChecked)
    {
        var code = """
            using System;

            namespace NS
            {
                class Class
                {
                    public static Class operator 
            """ + (isChecked ? "checked " : "") + op + """
            (Class x)


            """;

        AssertSmartIndent(
            code,
            indentationLine: 7,
            expectedIndentation: 12);
    }

    [WpfTheory]
    [CombinatorialData]
    public void InstanceCompoundAssignmentOperatorBody([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool isChecked)
    {
        var code = """
            using System;

            namespace NS
            {
                class Class
                {
                    public static Class operator 
            """ + (isChecked ? "checked " : "") + op + """
            (Class x)
                    {


            """;

        AssertSmartIndent(
            code,
            indentationLine: 8,
            expectedIndentation: 12);
    }

    private static void AssertSmartIndentInProjection(
        string markup,
        int expectedIndentation,
        CSharpParseOptions options = null,
        IndentStyle indentStyle = IndentStyle.Smart)
    {
        AssertSmartIndentInProjection(markup, expectedIndentation, useTabs: false, options, indentStyle);
        AssertSmartIndentInProjection(markup.Replace("    ", "\t"), expectedIndentation, useTabs: true, options, indentStyle);
    }

    private static void AssertSmartIndentInProjection(
        string markup,
        int expectedIndentation,
        bool useTabs,
        CSharpParseOptions options,
        IndentStyle indentStyle)
    {
        var optionsSet = options != null
                ? new[] { options }
                : [TestOptions.Regular, TestOptions.Script];

        foreach (var option in optionsSet)
        {
            using var workspace = EditorTestWorkspace.CreateCSharp(markup, parseOptions: option, composition: s_compositionWithTestFormattingRules);

            var subjectDocument = workspace.Documents.Single();

            var projectedDocument =
                workspace.CreateProjectionBufferDocument(HtmlMarkup, workspace.Documents);

            var provider = (TestFormattingRuleFactoryServiceFactory.Factory)workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            provider.BaseIndentation = BaseIndentationOfNugget;
            provider.TextSpan = subjectDocument.SelectedSpans.Single();

            var editorOptionsService = workspace.GetService<EditorOptionsService>();

            var indentationLine = projectedDocument.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(projectedDocument.CursorPosition.Value);
            var textView = projectedDocument.GetTextView();
            var buffer = subjectDocument.GetTextBuffer();
            var point = textView.BufferGraph.MapDownToBuffer(indentationLine.Start, PointTrackingMode.Negative, buffer, PositionAffinity.Predecessor);

            var editorOptions = editorOptionsService.Factory.GetOptions(buffer);
            editorOptions.SetOptionValue(DefaultOptions.IndentStyleId, indentStyle.ToEditorIndentStyle());
            editorOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !useTabs);

            TestIndentation(
                point.Value,
                expectedIndentation,
                textView,
                subjectDocument,
                editorOptionsService);
        }
    }

    private void AssertSmartIndent(
        string code,
        int indentationLine,
        int? expectedIndentation,
        CSharpParseOptions options = null,
        IndentStyle indentStyle = IndentStyle.Smart)
    {
        AssertSmartIndent(code, indentationLine, expectedIndentation, useTabs: false, options, indentStyle);
        AssertSmartIndent(code.Replace("    ", "\t"), indentationLine, expectedIndentation, useTabs: true, options, indentStyle);
    }

    private void AssertSmartIndent(
        string code,
        int indentationLine,
        int? expectedIndentation,
        bool useTabs,
        CSharpParseOptions options,
        IndentStyle indentStyle)
    {
        var optionsSet = options != null
            ? new[] { options }
            : [TestOptions.Regular, TestOptions.Script];

        foreach (var option in optionsSet)
        {
            using var workspace = EditorTestWorkspace.CreateCSharp(code, parseOptions: option);

            TestIndentation(workspace, indentationLine, expectedIndentation, indentStyle, useTabs);
        }
    }

    private void AssertSmartIndent(
        string code,
        int? expectedIndentation,
        CSharpParseOptions options = null,
        IndentStyle indentStyle = IndentStyle.Smart)
    {
        AssertSmartIndent(code, expectedIndentation, useTabs: false, options, indentStyle);
        AssertSmartIndent(code.Replace("    ", "\t"), expectedIndentation, useTabs: true, options, indentStyle);
    }

    private void AssertSmartIndent(
        string code,
        int? expectedIndentation,
        bool useTabs,
        CSharpParseOptions options,
        IndentStyle indentStyle)
    {
        var optionsSet = options != null
            ? new[] { options }
            : [TestOptions.Regular, TestOptions.Script];

        foreach (var option in optionsSet)
        {
            using var workspace = EditorTestWorkspace.CreateCSharp(code, parseOptions: option);

            var wpfTextView = workspace.Documents.First().GetTextView();
            var line = wpfTextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(wpfTextView.Caret.Position.BufferPosition).LineNumber;
            TestIndentation(workspace, line, expectedIndentation, indentStyle, useTabs);
        }
    }
}
