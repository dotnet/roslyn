// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Formatting.Indentation;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation
{
    public class SmartIndenterEnterOnTokenTests : FormatterTestsBase
    {
        [WorkItem(537808)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [WorkItem(537802)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [WorkItem(537808)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [WorkItem(537808)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [WorkItem(537795)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [WorkItem(537563)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1070773)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(853748)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(939305)]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Preprocessor()
        {
            var code = @"
#line 1 """"Bar""""class Foo : [|IComparable|]#line default#line hidden";

            await AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
                code,
                indentationLine: 1,
                expectedIndentation: 0);
        }

        [Fact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        [Fact]
        [WorkItem(1339, "https://github.com/dotnet/roslyn/issues/1339")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
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

        private async Task AssertIndentUsingSmartTokenFormatterAsync(
            string code,
            char ch,
            int indentationLine,
            int? expectedIndentation)
        {
            // create tree service
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(code))
            {
                var hostdoc = workspace.Documents.First();

                var buffer = hostdoc.GetTextBuffer();

                var snapshot = buffer.CurrentSnapshot;
                var line = snapshot.GetLineFromLineNumber(indentationLine);

                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

                var root = (await document.GetSyntaxRootAsync()) as CompilationUnitSyntax;

                Assert.True(
                    CSharpIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(
                        Formatter.GetDefaultFormattingRules(workspace, root.Language),
                        root, line, workspace.Options, CancellationToken.None));

                var actualIndentation = await GetSmartTokenFormatterIndentationWorkerAsync(workspace, buffer, indentationLine, ch);
                Assert.Equal(expectedIndentation.Value, actualIndentation);
            }
        }

        private async Task AssertIndentNotUsingSmartTokenFormatterButUsingIndenterAsync(
            string code,
            int indentationLine,
            int? expectedIndentation)
        {
            // create tree service
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(code))
            {
                var hostdoc = workspace.Documents.First();
                var buffer = hostdoc.GetTextBuffer();
                var snapshot = buffer.CurrentSnapshot;

                var line = snapshot.GetLineFromLineNumber(indentationLine);

                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

                var root = (await document.GetSyntaxRootAsync()) as CompilationUnitSyntax;
                Assert.False(
                    CSharpIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(
                        Formatter.GetDefaultFormattingRules(workspace, root.Language),
                        root, line, workspace.Options, CancellationToken.None));

                await TestIndentationAsync(indentationLine, expectedIndentation, workspace);
            }
        }
    }
}
