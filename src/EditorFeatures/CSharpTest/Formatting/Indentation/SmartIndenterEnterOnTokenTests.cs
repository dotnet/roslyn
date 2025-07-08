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
    public async Task MethodBody1()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class Class1
{
    void method()
                { }
}
",
            '{',
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Preprocessor1()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class A
{
    #region T
#endregion
}
",
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Preprocessor2()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class A
{
#line 1
#lien 2
}
",
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Preprocessor3()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"#region stuff
#endregion
",
            indentationLine: 2,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task Comments()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"using System;

class Class
{
    // Comments
// Comments
",
            indentationLine: 5,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task UsingDirective()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;
using System.Linq;
",
            'u',
            indentationLine: 1,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task AfterTopOfFileComment()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"// comment

class
",
            indentationLine: 2,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task DottedName()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"using System.
Collection;
",
            indentationLine: 1,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Namespace()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
",
            '{',
            indentationLine: 3,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task NamespaceDottedName()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"using System;

namespace NS.
NS2
",
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task NamespaceBody()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
class
",
            'c',
            indentationLine: 4,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task NamespaceCloseBrace()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
}
",
            '}',
            indentationLine: 4,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task Class()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
{
",
            '{',
            indentationLine: 5,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task ClassBody()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
    {
int
",
            'i',
            indentationLine: 6,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task ClassCloseBrace()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
    {
}
",
            '}',
            indentationLine: 6,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Method()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
    {
        void Method()
{
",
            '{',
            indentationLine: 7,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task MethodBody()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
int
",
            'i',
            indentationLine: 8,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task MethodCloseBrace()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
}
",
            '}',
            indentationLine: 8,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task Statement()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            int i = 10;
int
",
            'i',
            indentationLine: 9,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task MethodCall()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class c
{
    void Method()
    {
        M(
a: 1, 
            b: 1);
    }
}",
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task Switch()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            switch (10)
{
",
            '{',
            indentationLine: 9,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task SwitchBody()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            switch (10)
            {
case
",
            'c',
            indentationLine: 10,
            expectedIndentation: 16);
    }

    [WpfFact]
    public async Task SwitchCase()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

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
",
            'i',
            indentationLine: 11,
            expectedIndentation: 20);
    }

    [WpfFact]
    public async Task SwitchCaseBlock()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

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
",
            '{',
            indentationLine: 11,
            expectedIndentation: 20);
    }

    [WpfFact]
    public async Task Block()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"using System;

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
",
            'i',
            indentationLine: 12,
            expectedIndentation: 24);
    }

    [WpfFact]
    public async Task MultilineStatement1()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            int i = 10 +
1
",
            indentationLine: 9,
            expectedIndentation: 16);
    }

    [WpfFact]
    public async Task MultilineStatement2()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            int i = 10 +
                    20 +
30
",
            indentationLine: 10,
            expectedIndentation: 20);
    }

    // Bug number 902477
    [WpfFact]
    public async Task Comments2()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class Class
{
    void Method()
    {
        if (true) // Test
int
    }
}
",
            'i',
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task AfterCompletedBlock()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        foreach(var a in x) {}
int
    }
}

",
            'i',
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task AfterTopLevelAttribute()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class Program
{
    [Attr]
[
}

",
            '[',
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537802")]
    public async Task EmbededStatement()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            Console.WriteLine(1);
int
    }
}

",
            'i',
            indentationLine: 6,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537808")]
    public async Task MethodBraces1()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class Class1
{
    void method()
{ }
}
",
            '{',
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537808")]
    public async Task MethodBraces2()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class Class1
{
    void method()
    {
}
}
",
            '}',
            indentationLine: 4,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537795")]
    public async Task Property1()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
    string Name
    { 
        get; 
        set;
}
}
",
            '}',
            indentationLine: 6,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537563")]
    public async Task Class1()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
}
",
            '}',
            indentationLine: 2,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public async Task ArrayInitializer1()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class C
{
    var a = new [] 
{ 1, 2, 3 }
}
",
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task ArrayInitializer2()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
    var a = new [] 
    {
        1, 2, 3 
}
}
",
            '}',
            indentationLine: 5,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public async Task ArrayInitializer3()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            var a = new []
{
        }",
            indentationLine: 7,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task QueryExpression2()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
    void Method()
    {
        var a = from c in b
    where
    }
}
",
            'w',
            indentationLine: 5,
            expectedIndentation: 16);
    }

    [WpfFact]
    public async Task QueryExpression3()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
    void Method()
    {
        var a = from c in b
    where select
    }
}
",
            'w',
            indentationLine: 5,
            expectedIndentation: 16);
    }

    [WpfFact]
    public async Task QueryExpression4()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
    void Method()
    {
        var a = from c in b where c > 10
        select
    }
}
",
            's',
            indentationLine: 5,
            expectedIndentation: 16);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853748")]
    public async Task ArrayInitializer()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
    void Method()
    {
        var l = new int[] {
        }
    }
}
",
            '}',
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939305")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public async Task ArrayExpression()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class C
{
    void M(object[] q)
    {
        M(
              q: new object[] 
{ });
    }
}
",
            indentationLine: 6,
            expectedIndentation: 14);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public async Task CollectionExpression()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
    void M(List<int> e)
    {
        M(
            new List<int> 
{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
    }
}
",
            '{',
            indentationLine: 6,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public async Task ObjectInitializer()
    {
        await AssertIndentUsingSmartTokenFormatterAsync(
            @"class C
{
    void M(What dd)
    {
        M(
            new What 
{ d = 3, dd = "" });
    }
}

class What
{
    public int d;
    public string dd;
}",
            '{',
            indentationLine: 6,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task Preprocessor()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"
#line 1 """"Bar""""class Goo : [|IComparable|]#line default#line hidden",
            indentationLine: 1,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_Implicit()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class X {
    int[] a = {
        1,

    };
}",
            indentationLine: 3,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_ImplicitNew()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class X {
    int[] a = new[] {
        1,

    };
}",
            indentationLine: 3,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_Explicit()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class X {
    int[] a = new int[] {
        1,

    };
}",
            indentationLine: 3,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_Collection()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"using System.Collections.Generic;
class X {
    private List<int> a = new List<int>() {
        1,

    };
}",
            indentationLine: 4,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_ObjectInitializers()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class C
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
}",
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationString_1()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""
{Program.number}"";
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationString_2()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""Comment
{Program.number}"";
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationString_3()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""Comment{Program.number}
"";
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationString_4()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""Comment{Program.number}Comment here
"";
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task OutsideInterpolationString()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""Comment{Program.number}Comment here""
;
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_1()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""{
Program.number}"";
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_2()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""{
            Program
.number}"";
    }

    static int number;
}",
            indentationLine: 6,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_3()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""{
}"";
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_4()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($@""PPP{ 
((Func<int, int>)((int s) => { return number; })).Invoke(3):(408) ###-####}"");
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_5()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($@""PPP{ ((Func<int, int>)((int s) 
=> { return number; })).Invoke(3):(408) ###-####}"");
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_6()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($@""PPP{ ((Func<int, int>)((int s) => { return number; }))
.Invoke(3):(408) ###-####}"");
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_7()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($@""PPP{ ((Func<int, int>)((int s) => 
{ return number; })).Invoke(3):(408) ###-####}"");
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task IndentLambdaBodyOneIndentationToFirstTokenOfTheStatement()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(((Func<int, int>)((int s) => 
{ return number; })).Invoke(3));
    }

    static int number;
}",
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/1339")]
    public async Task IndentAutoPropertyInitializerAsPartOfTheDeclaration()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"class Program
{
    public int d { get; } 
= 3;
    static void Main(string[] args)
    {
    }
}",
            indentationLine: 3,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task IndentPatternPropertyFirst()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"
class C
{
    void Main(object o)
    {
        var y = o is Point
        {

        }
    }
}",
            indentationLine: 7,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task IndentPatternPropertySecond()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"
class C
{
    void Main(object o)
    {
        var y = o is Point
        {
            X is 13,

        }
    }
}",
            indentationLine: 8,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task IndentListPattern()
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @"
class C
{
    void Main(object o)
    {
        var y = o is
        [

        ]
    }
}",
            indentationLine: 7,
            expectedIndentation: 12);
    }

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
    public async Task IndentPatternsInLocalDeclarationCSharp9(string line1, string line2, int expectedIndentation)
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @$"
class C
{{
    void M()
    {{
        var x = 7;
        var y = {line1}
{line2}
    }}
}}",
            indentationLine: 7,
            expectedIndentation);
    }

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
    public async Task IndentPatternsInFieldDeclarationCSharp9(string line1, string line2, int expectedIndentation)
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @$"
class C
{{
    static int x = 7;
    bool y = {line1}
{line2}
}}",
            indentationLine: 5,
            expectedIndentation);
    }

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
    public async Task IndentPatternsInSwitchCSharp9(string line1, string line2, int expectedIndentation)
    {
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            @$"
class C
{{
    void M()
    {{
        var x = 7;
        var y = x switch
        {{
            {line1}
{line2} => true,
            _ => false
        }};
    }}
}}",
            indentationLine: 9,
            expectedIndentation);
    }

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
