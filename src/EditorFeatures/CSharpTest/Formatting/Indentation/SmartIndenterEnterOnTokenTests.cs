// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
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
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void MethodBody1()
        {
            var code = @"class Class1
{
    void method()
                { }
}
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Preprocessor1()
        {
            var code = @"class A
{
    #region T
#endregion
}
";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Preprocessor2()
        {
            var code = @"class A
{
#line 1
#lien 2
}
";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Preprocessor3()
        {
            var code = @"#region stuff
#endregion
";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 2,
                expectedIndentation: 0);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Comments()
        {
            var code = @"using System;

class Class
{
    // Comments
// Comments
";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void UsingDirective()
        {
            var code = @"using System;
using System.Linq;
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                'u',
                indentationLine: 1,
                expectedIndentation: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void AfterTopOfFileComment()
        {
            var code = @"// comment

class
";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 2,
                expectedIndentation: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void DottedName()
        {
            var code = @"using System.
Collection;
";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 1,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Namespace()
        {
            var code = @"using System;

namespace NS
{
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 3,
                expectedIndentation: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void NamespaceDottedName()
        {
            var code = @"using System;

namespace NS.
NS2
";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void NamespaceBody()
        {
            var code = @"using System;

namespace NS
{
class
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                'c',
                indentationLine: 4,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void NamespaceCloseBrace()
        {
            var code = @"using System;

namespace NS
{
}
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '}',
                indentationLine: 4,
                expectedIndentation: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Class()
        {
            var code = @"using System;

namespace NS
{
    class Class
{
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 5,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void ClassBody()
        {
            var code = @"using System;

namespace NS
{
    class Class
    {
int
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                'i',
                indentationLine: 6,
                expectedIndentation: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void ClassCloseBrace()
        {
            var code = @"using System;

namespace NS
{
    class Class
    {
}
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '}',
                indentationLine: 6,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Method()
        {
            var code = @"using System;

namespace NS
{
    class Class
    {
        void Method()
{
";

            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 7,
                expectedIndentation: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void MethodBody()
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

            AssertIndentUsingSmartTokenFormatter(
                code,
                'i',
                indentationLine: 8,
                expectedIndentation: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void MethodCloseBrace()
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

            AssertIndentUsingSmartTokenFormatter(
                code,
                '}',
                indentationLine: 8,
                expectedIndentation: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Statement()
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

            AssertIndentUsingSmartTokenFormatter(
                code,
                'i',
                indentationLine: 9,
                expectedIndentation: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void MethodCall()
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

            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Switch()
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

            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 9,
                expectedIndentation: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void SwitchBody()
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

            AssertIndentUsingSmartTokenFormatter(
                code,
                'c',
                indentationLine: 10,
                expectedIndentation: 16);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void SwitchCase()
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

            AssertIndentUsingSmartTokenFormatter(
                code,
                'i',
                indentationLine: 11,
                expectedIndentation: 20);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void SwitchCaseBlock()
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

            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 11,
                expectedIndentation: 20);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Block()
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

            AssertIndentUsingSmartTokenFormatter(
                code,
                'i',
                indentationLine: 12,
                expectedIndentation: 24);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void MultilineStatement1()
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

            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 9,
                expectedIndentation: 16);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void MultilineStatement2()
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

            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 10,
                expectedIndentation: 20);
        }

        // Bug number 902477
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Comments2()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                'i',
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void AfterCompletedBlock()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                'i',
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void AfterTopLevelAttribute()
        {
            var code = @"class Program
{
    [Attr]
[
}

";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '[',
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WorkItem(537802)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void EmbededStatement()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                'i',
                indentationLine: 6,
                expectedIndentation: 8);
        }

        [WorkItem(537808)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void MethodBraces1()
        {
            var code = @"class Class1
{
    void method()
{ }
}
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WorkItem(537808)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void MethodBraces2()
        {
            var code = @"class Class1
{
    void method()
    {
}
}
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '}',
                indentationLine: 4,
                expectedIndentation: 4);
        }

        [WorkItem(537795)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Property1()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                '}',
                indentationLine: 6,
                expectedIndentation: 4);
        }

        [WorkItem(537563)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Class1()
        {
            var code = @"class C
{
}
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '}',
                indentationLine: 2,
                expectedIndentation: 0);
        }

        [WpfFact]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void ArrayInitializer1()
        {
            var code = @"class C
{
    var a = new [] 
{ 1, 2, 3 }
}
";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void ArrayInitializer2()
        {
            var code = @"class C
{
    var a = new [] 
    {
        1, 2, 3 
}
}
";
            AssertIndentUsingSmartTokenFormatter(
                code,
                '}',
                indentationLine: 5,
                expectedIndentation: 4);
        }

        [WpfFact]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void ArrayInitializer3()
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

            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 7,
                expectedIndentation: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void QueryExpression2()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                'w',
                indentationLine: 5,
                expectedIndentation: 16);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void QueryExpression3()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                'w',
                indentationLine: 5,
                expectedIndentation: 16);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void QueryExpression4()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                's',
                indentationLine: 5,
                expectedIndentation: 16);
        }

        [WpfFact]
        [WorkItem(853748)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void ArrayInitializer()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                '}',
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [WpfFact]
        [WorkItem(939305)]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void ArrayExpression()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 6,
                expectedIndentation: 14);
        }

        [WpfFact]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void CollectionExpression()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 6,
                expectedIndentation: 12);
        }

        [WpfFact]
        [WorkItem(1070773)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void ObjectInitializer()
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
            AssertIndentUsingSmartTokenFormatter(
                code,
                '{',
                indentationLine: 6,
                expectedIndentation: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void Preprocessor()
        {
            var code = @"
#line 1 """"Bar""""class Foo : [|IComparable|]#line default#line hidden";

            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 1,
                expectedIndentation: 0);
        }

        [WpfFact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInitializerWithTypeBody_Implicit()
        {
            var code = @"class X {
    int[] a = {
        1,

    };
}";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 3,
                expectedIndentation: 8);
        }

        [WpfFact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInitializerWithTypeBody_ImplicitNew()
        {
            var code = @"class X {
    int[] a = new[] {
        1,

    };
}";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 3,
                expectedIndentation: 8);
        }

        [WpfFact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInitializerWithTypeBody_Explicit()
        {
            var code = @"class X {
    int[] a = new int[] {
        1,

    };
}";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 3,
                expectedIndentation: 8);
        }

        [WpfFact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInitializerWithTypeBody_Collection()
        {
            var code = @"using System.Collections.Generic;
class X {
    private List<int> a = new List<int>() {
        1,

    };
}";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 4,
                expectedIndentation: 8);
        }

        [WpfFact]
        [WorkItem(1070774)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInitializerWithTypeBody_ObjectInitializers()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationString_1()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 0);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationString_2()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 0);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationString_3()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 0);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationString_4()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 0);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void OutsideInterpolationString()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationSyntax_1()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationSyntax_2()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 6,
                expectedIndentation: 12);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationSyntax_3()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationSyntax_4()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationSyntax_5()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationSyntax_6()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void InsideInterpolationSyntax_7()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [WpfFact]
        [WorkItem(872)]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void IndentLambdaBodyOneIndentationToFirstTokenOfTheStatement()
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
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [WpfFact]
        [WorkItem(1339, "https://github.com/dotnet/roslyn/issues/1339")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public void IndentAutoPropertyInitializerAsPartOfTheDeclaration()
        {
            var code = @"class Program
{
    public int d { get; } 
= 3;
    static void Main(string[] args)
    {
    }
}";
            AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
                code,
                indentationLine: 3,
                expectedIndentation: 8);
        }

        private void AssertIndentUsingSmartTokenFormatter(
            string code,
            char ch,
            int indentationLine,
            int? expectedIndentation)
        {
            // create tree service
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(code))
            {
                var hostdoc = workspace.Documents.First();

                var buffer = hostdoc.GetTextBuffer();

                var snapshot = buffer.CurrentSnapshot;
                var line = snapshot.GetLineFromLineNumber(indentationLine);

                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

                var root = document.GetSyntaxRootAsync().Result as CompilationUnitSyntax;

                Assert.True(
                    CSharpIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(
                        Formatter.GetDefaultFormattingRules(workspace, root.Language),
                        root, line, workspace.Options, CancellationToken.None));

                var actualIndentation = GetSmartTokenFormatterIndentationWorker(workspace, buffer, indentationLine, ch);
                Assert.Equal(expectedIndentation.Value, actualIndentation);
            }
        }

        private void AssertIndentNotUsingSmartTokenFormatterButUsingIndenter(
            string code,
            int indentationLine,
            int? expectedIndentation)
        {
            // create tree service
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(code))
            {
                var hostdoc = workspace.Documents.First();
                var buffer = hostdoc.GetTextBuffer();
                var snapshot = buffer.CurrentSnapshot;

                var line = snapshot.GetLineFromLineNumber(indentationLine);

                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

                var root = document.GetSyntaxRootAsync().Result as CompilationUnitSyntax;
                Assert.False(
                    CSharpIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(
                        Formatter.GetDefaultFormattingRules(workspace, root.Language),
                        root, line, workspace.Options, CancellationToken.None));

                TestIndentation(indentationLine, expectedIndentation, workspace);
            }
        }
    }
}
