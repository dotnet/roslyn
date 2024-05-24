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
public class SmartIndenterEnterOnTokenTests : CSharpFormatterTestsBase
{
    public SmartIndenterEnterOnTokenTests(ITestOutputHelper output) : base(output) { }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537808")]
    public async Task MethodBody1()
    {
        var code = @"class Class1
{
    void method()
                { }
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Preprocessor1()
    {
        var code = @"class A
{
    #region T
#endregion
}
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Preprocessor2()
    {
        var code = @"class A
{
#line 1
#lien 2
}
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Preprocessor3()
    {
        var code = @"#region stuff
#endregion
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 2,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task Comments()
    {
        var code = @"using System;

class Class
{
    // Comments
// Comments
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task UsingDirective()
    {
        var code = @"using System;
using System.Linq;
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'u',
            indentationLine: 1,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task AfterTopOfFileComment()
    {
        var code = @"// comment

class
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 2,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task DottedName()
    {
        var code = @"using System.
Collection;
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 1,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Namespace()
    {
        var code = @"using System;

namespace NS
{
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 3,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task NamespaceDottedName()
    {
        var code = @"using System;

namespace NS.
NS2
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task NamespaceBody()
    {
        var code = @"using System;

namespace NS
{
class
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'c',
            indentationLine: 4,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task NamespaceCloseBrace()
    {
        var code = @"using System;

namespace NS
{
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '}',
            indentationLine: 4,
            expectedIndentation: 0);
    }

    [WpfFact]
    public async Task Class()
    {
        var code = @"using System;

namespace NS
{
    class Class
{
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 5,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task ClassBody()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
int
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'i',
            indentationLine: 6,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task ClassCloseBrace()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '}',
            indentationLine: 6,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task Method()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
{
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 7,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task MethodBody()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
int
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'i',
            indentationLine: 8,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task MethodCloseBrace()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
}
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '}',
            indentationLine: 8,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task Statement()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            int i = 10;
int
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'i',
            indentationLine: 9,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task MethodCall()
    {
        var code = @"class c
{
    void Method()
    {
        M(
a: 1, 
            b: 1);
    }
}";

        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task Switch()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            switch (10)
{
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 9,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task SwitchBody()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            switch (10)
            {
case
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'c',
            indentationLine: 10,
            expectedIndentation: 16);
    }

    [WpfFact]
    public async Task SwitchCase()
    {
        var code = @"using System;

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
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'i',
            indentationLine: 11,
            expectedIndentation: 20);
    }

    [WpfFact]
    public async Task SwitchCaseBlock()
    {
        var code = @"using System;

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
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 11,
            expectedIndentation: 20);
    }

    [WpfFact]
    public async Task Block()
    {
        var code = @"using System;

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
";

        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'i',
            indentationLine: 12,
            expectedIndentation: 24);
    }

    [WpfFact]
    public async Task MultilineStatement1()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            int i = 10 +
1
";

        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 9,
            expectedIndentation: 16);
    }

    [WpfFact]
    public async Task MultilineStatement2()
    {
        var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
        {
            int i = 10 +
                    20 +
30
";

        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 10,
            expectedIndentation: 20);
    }

    // Bug number 902477
    [WpfFact]
    public async Task Comments2()
    {
        var code = @"class Class
{
    void Method()
    {
        if (true) // Test
int
    }
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'i',
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task AfterCompletedBlock()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        foreach(var a in x) {}
int
    }
}

";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'i',
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task AfterTopLevelAttribute()
    {
        var code = @"class Program
{
    [Attr]
[
}

";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '[',
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537802")]
    public async Task EmbededStatement()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            Console.WriteLine(1);
int
    }
}

";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'i',
            indentationLine: 6,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537808")]
    public async Task MethodBraces1()
    {
        var code = @"class Class1
{
    void method()
{ }
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537808")]
    public async Task MethodBraces2()
    {
        var code = @"class Class1
{
    void method()
    {
}
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '}',
            indentationLine: 4,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537795")]
    public async Task Property1()
    {
        var code = @"class C
{
    string Name
    { 
        get; 
        set;
}
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '}',
            indentationLine: 6,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537563")]
    public async Task Class1()
    {
        var code = @"class C
{
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '}',
            indentationLine: 2,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public async Task ArrayInitializer1()
    {
        var code = @"class C
{
    var a = new [] 
{ 1, 2, 3 }
}
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 3,
            expectedIndentation: 4);
    }

    [WpfFact]
    public async Task ArrayInitializer2()
    {
        var code = @"class C
{
    var a = new [] 
    {
        1, 2, 3 
}
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '}',
            indentationLine: 5,
            expectedIndentation: 4);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public async Task ArrayInitializer3()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            var a = new []
{
        }";

        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 7,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task QueryExpression2()
    {
        var code = @"class C
{
    void Method()
    {
        var a = from c in b
    where
    }
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'w',
            indentationLine: 5,
            expectedIndentation: 16);
    }

    [WpfFact]
    public async Task QueryExpression3()
    {
        var code = @"class C
{
    void Method()
    {
        var a = from c in b
    where select
    }
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            'w',
            indentationLine: 5,
            expectedIndentation: 16);
    }

    [WpfFact]
    public async Task QueryExpression4()
    {
        var code = @"class C
{
    void Method()
    {
        var a = from c in b where c > 10
        select
    }
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            's',
            indentationLine: 5,
            expectedIndentation: 16);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853748")]
    public async Task ArrayInitializer()
    {
        var code = @"class C
{
    void Method()
    {
        var l = new int[] {
        }
    }
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '}',
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939305")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public async Task ArrayExpression()
    {
        var code = @"class C
{
    void M(object[] q)
    {
        M(
              q: new object[] 
{ });
    }
}
";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 6,
            expectedIndentation: 14);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public async Task CollectionExpression()
    {
        var code = @"class C
{
    void M(List<int> e)
    {
        M(
            new List<int> 
{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
    }
}
";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 6,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public async Task ObjectInitializer()
    {
        var code = @"class C
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
}";
        await AssertIndentUsingSmartTokenFormatterAsync(
            code,
            '{',
            indentationLine: 6,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task Preprocessor()
    {
        var code = @"
#line 1 """"Bar""""class Goo : [|IComparable|]#line default#line hidden";

        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 1,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_Implicit()
    {
        var code = @"class X {
    int[] a = {
        1,

    };
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 3,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_ImplicitNew()
    {
        var code = @"class X {
    int[] a = new[] {
        1,

    };
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 3,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_Explicit()
    {
        var code = @"class X {
    int[] a = new int[] {
        1,

    };
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 3,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_Collection()
    {
        var code = @"using System.Collections.Generic;
class X {
    private List<int> a = new List<int>() {
        1,

    };
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 4,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070774")]
    public async Task InsideInitializerWithTypeBody_ObjectInitializers()
    {
        var code = @"class C
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
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationString_1()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""
{Program.number}"";
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationString_2()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""Comment
{Program.number}"";
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationString_3()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""Comment{Program.number}
"";
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationString_4()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""Comment{Program.number}Comment here
"";
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 0);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task OutsideInterpolationString()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""Comment{Program.number}Comment here""
;
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_1()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""{
Program.number}"";
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_2()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""{
            Program
.number}"";
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 6,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_3()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = $@""{
}"";
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_4()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($@""PPP{ 
((Func<int, int>)((int s) => { return number; })).Invoke(3):(408) ###-####}"");
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_5()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($@""PPP{ ((Func<int, int>)((int s) 
=> { return number; })).Invoke(3):(408) ###-####}"");
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_6()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($@""PPP{ ((Func<int, int>)((int s) => { return number; }))
.Invoke(3):(408) ###-####}"");
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 12);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task InsideInterpolationSyntax_7()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($@""PPP{ ((Func<int, int>)((int s) => 
{ return number; })).Invoke(3):(408) ###-####}"");
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/872")]
    public async Task IndentLambdaBodyOneIndentationToFirstTokenOfTheStatement()
    {
        var code = @"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(((Func<int, int>)((int s) => 
{ return number; })).Invoke(3));
    }

    static int number;
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 5,
            expectedIndentation: 8);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/1339")]
    public async Task IndentAutoPropertyInitializerAsPartOfTheDeclaration()
    {
        var code = @"class Program
{
    public int d { get; } 
= 3;
    static void Main(string[] args)
    {
    }
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 3,
            expectedIndentation: 8);
    }

    [WpfFact]
    public async Task IndentPatternPropertyFirst()
    {
        var code = @"
class C
{
    void Main(object o)
    {
        var y = o is Point
        {

        }
    }
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 7,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task IndentPatternPropertySecond()
    {
        var code = @"
class C
{
    void Main(object o)
    {
        var y = o is Point
        {
            X is 13,

        }
    }
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
            indentationLine: 8,
            expectedIndentation: 12);
    }

    [WpfFact]
    public async Task IndentListPattern()
    {
        var code = @"
class C
{
    void Main(object o)
    {
        var y = o is
        [

        ]
    }
}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
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
        var code = @$"
class C
{{
    void M()
    {{
        var x = 7;
        var y = {line1}
{line2}
    }}
}}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
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
        var code = @$"
class C
{{
    static int x = 7;
    bool y = {line1}
{line2}
}}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
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
        var code = @$"
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
}}";
        await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            code,
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
