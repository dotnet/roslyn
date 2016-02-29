// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation
{
    public partial class SmartIndenterTests : FormatterTestsBase
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EmptyFile()
        {
            await AssertSmartIndentAsync(
                code: string.Empty,
                indentationLine: 0,
                expectedIndentation: 0);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NoPreviousLine()
        {
            var code = @"#region Test

#warning 0
#undef SYMBOL
#define SYMBOL
#if false
#elif true
#else
#endif
#pragma warning disable 99999
#foo

#endregion

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 13,
                expectedIndentation: 0);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EndOfFileInactive()
        {
            var code = @"
    // Line 1
#if false
#endif

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 4,
                expectedIndentation: 4);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EndOfFileInactive2()
        {
            var code = @"
    // Line 1
#if false
#endif
// Line 2

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
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
    /// Xml Comments

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 4);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task UsingDirective()
        {
            var code = @"using System;

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 1,
                expectedIndentation: 0);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task DottedName()
        {
            var code = @"using System.

";
            await AssertSmartIndentAsync(
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

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NamespaceDottedName()
        {
            var code = @"using System;

namespace NS.

";
            await AssertSmartIndentAsync(
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

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 4,
                expectedIndentation: 4);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Class()
        {
            var code = @"using System;

namespace NS
{
    class Class

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
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

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 8);
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

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 12);
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

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 12);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Property()
        {
            var code = @"using System;

namespace NS
{
    class Class
    {
        public static string Name

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 12);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task PropertyGetBody()
        {
            var code = @"using System;

namespace NS
{
    class Class
    {
        private string name;
        public string Names
        {
            get

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task PropertySetBody()
        {
            var code = @"using System;

namespace NS
{
    class Class
    {
        private static string name;
        public static string Names
        {
            set

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 16);
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

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 9,
                expectedIndentation: 12);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task FieldInitializer()
        {
            var code = @"class C
{
    int i = 2;
";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task FieldInitializerWithNamespace()
        {
            var code = @"namespace NS
{
    class C
    {
        C c = new C();

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
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

            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 12);
            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: null);
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

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 9,
                expectedIndentation: 16);
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

";

            await AssertSmartIndentAsync(
                code,
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

";

            await AssertSmartIndentAsync(
                code,
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

";

            await AssertSmartIndentAsync(
                code,
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

";

            await AssertSmartIndentAsync(
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

";

            await AssertSmartIndentAsync(
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

    }
}
";
            await AssertSmartIndentAsync(
                code,
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

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task AfterCompletedBlockNestedInOtherBlock()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        foreach(var a in x) {{}

        }
    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task AfterTopLevelAttribute()
        {
            var code = @"class Program
{
    [Attr]

}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WorkItem(537802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537802")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EmbeddedStatement()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            Console.WriteLine(1);

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 8);
        }

        [WorkItem(537883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537883")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EnterAfterComment()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int a; // enter

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [WorkItem(538121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538121")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedBlock1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        {

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedEmbeddedStatement1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            if (true)
                if (true)
                    args = null;

    }
}
";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedEmbeddedStatement2()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            if (true)
                if (true)
                    { }

    }
}
";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedEmbeddedStatement3()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            if (true)
                if (true)
                    { return; }

    }
}
";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedEmbeddedStatement4()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            if (true)
                if (true)
                    args = null;

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedEmbeddedStatement5()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            if (true)
                if (true)
                    { }

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedEmbeddedStatement6()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            if (true)
                if (true)
                    { return; }

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedEmbeddedStatement7()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        if (true)
            if (true)
                if (true)
                    return;
                else
                    return;

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task NestedEmbeddedStatement8()
        {
            var code = @"class Program
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
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Label1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
    Label:
        Console.WriteLine(1);

    }
}

";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Label2()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
    Label: Console.WriteLine(1);

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Label3()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        switch(args.GetType())
        {
            case 1:
                Console.WriteLine(1);

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Label4()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        switch(args.GetType())
        {
            case 1: Console.WriteLine(1);

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Label5()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        switch(args.GetType())
        {
            case 1:

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Label6()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
    Label:

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var a = from c in

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 20);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression2()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var a = from c in b

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression3()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var a = from c in b.

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression4()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var a = from c in b where c > 10

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression5()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var a = from c in
                    from b in G

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 20);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression6()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var a = from c in
                    from b in G
                    select b

    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 20);
        }

        [Fact]
        [WorkItem(538779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538779")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression7()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var q = from string s in args

                where s == null
                select s;
    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 16);
        }

        [Fact]
        [WorkItem(538779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538779")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression8()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var q = from string s in args.
                              b.c.

                where s == null
                select s;
    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 30);
        }

        [Fact]
        [WorkItem(538780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538780")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression9()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var q = from string s in args
                where s == null

                select s;
    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 16);
        }

        [Fact]
        [WorkItem(538780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538780")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task QueryExpression10()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var q = from string s in args
                where s == null
                        == 1

                select s;
    }
}

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 24);
        }

        [WorkItem(538333, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538333")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Statement1()
        {
            var code = @"class Program
{
    void Test() { }

}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [WorkItem(538933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538933")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EndOfFile1()
        {
            var code = @"class Program
{
    void Test() 
    {
        int i;


";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 8);
        }

        [WorkItem(539059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539059")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task VerbatimString()
        {
            var code = @"class Program
{
    void Test() 
    {
        var foo = @""Foo

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 0);
        }

        [Fact]
        [WorkItem(539892, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539892")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Bug5994()
        {
            var code = @"using System;
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
";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 11,
                expectedIndentation: 15);
        }

        [Fact]
        [WorkItem(539990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539990")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Bug6124()
        {
            var code = @"class Program
{
    void Main()
    {
        var commandLine = string.Format(
            "",
            0,
            42,
            string.Format("",
                0,
                0),

            0);
    }
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 11,
                expectedIndentation: 12);
        }

        [Fact]
        [WorkItem(539990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539990")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task Bug6124_1()
        {
            var code = @"class Program
{
    void Main()
    {
        var commandLine = string.Format(
            "",
            0,
            42,
            string.Format("",
                0,
                0

),
            0);
    }
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 11,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task AfterIfWithSingleStatementInTopLevelMethod_Bug7291_1()
        {
            var code = @"int fact(int x)
{
    if (x < 1)
        return 1;

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 4,
                expectedIndentation: 4,
                options: Options.Script);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task AfterIfWithSingleStatementInTopLevelMethod_Bug7291_2()
        {
            var code = @"int fact(int x)
{
    if (x < 1)
        return 1;

}
";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 4,
                expectedIndentation: 4,
                options: Options.Script);
        }

        [WorkItem(540634, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540634")]
        [WorkItem(544268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544268")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task FirstArgumentInArgumentList()
        {
            var code = @"class Program
{
    public Program(
        string a,
        int b,
        bool c)
        : this(
            a,
            new Program(

                "",
                3,
                true),
            b,
            c)
    {
    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 9,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task ForLoop()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        for (;      
        ;) { }
    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task CallBaseCtor()
        {
            var code = @"class Program
{
    public Program() :           
    base() { }
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 8);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task MultipleDeclarations()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int i,
        j = 42;
    }
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task CloseBracket()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var i = new int[1]
        ;
    }
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task SwitchLabel()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        switch (args[0])
        {
            case ""foo"":

            case ""bar"":
                break;
        }
    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 16);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task TypeParameters()
        {
            var code = @"class Program
{
    static void Foo<T1,                 
T2>() { }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 8);
        }

        [Fact]
        [WorkItem(542428, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542428")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task TypeArguments()
        {
            var code = @"class Program
{
        static void Foo<T1, T2>(T1 t1, T2 t2) { }

        static void Main(string[] args)
        {
            Foo<int, 
            int>(4, 2);
        }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 16);
        }

        [Fact]
        [WorkItem(542983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542983")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task ConstructorInitializer1()
        {
            var code = @"public class Asset
{
    public Asset() : this(

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 8);
        }

        [Fact]
        [WorkItem(542983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542983")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task ConstructorInitializer2()
        {
            var code = @"public class Asset
{
    public Asset()
        : this(

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 4,
                expectedIndentation: 14);
        }

        [Fact]
        [WorkItem(542983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542983")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task ConstructorInitializer3()
        {
            var code = @"public class Asset
{
    public Asset() :
        this(

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 4,
                expectedIndentation: 12);
        }

        [Fact]
        [WorkItem(543131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543131")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task LockStatement1()
        {
            var code = @"using System;
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
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 12);
        }

        [Fact]
        [WorkItem(543533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543533")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task ConstructorInitializer()
        {
            var code = @"public class Asset
{
    public Asset() :

";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 8);
        }

        [Fact]
        [WorkItem(952803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/952803")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task ArrayInitializer()
        {
            var code = @"using System.Collections.ObjectModel;

class Program
{
    static void Main(string[] args)
    {
        new ReadOnlyCollection<int>(new int[]
        {

        });
    }
}
";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 12);
        }

        [Fact]
        [WorkItem(543563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task LambdaEmbededInExpression()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;
 
class Program
{
    static void Main(string[] args)
    {
        using (var var = new FooClass(() =>
        {

        }))
        {
            var var2 = var;
        }
    }
}
 
class FooClass : IDisposable
{
    public FooClass(Action a)
    {
    }
 
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 12);
        }

        [Fact]
        [WorkItem(543563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task LambdaEmbededInExpression_1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        using (var var = new FooClass(() =>

    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 16);
        }

        [Fact]
        [WorkItem(543563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task LambdaEmbededInExpression_3()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        using (var var = new FooClass(() =>
        {

        }))
        {
            var var2 = var;
        }
    }
}

class FooClass : IDisposable
{
    public FooClass(Action a)
    {
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 12);
        }

        [Fact]
        [WorkItem(543563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task LambdaEmbededInExpression_2()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        using (var var = new FooClass(
            () =>

    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 16);
        }

        [Fact]
        [WorkItem(543563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543563")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task LambdaEmbededInExpression_4()
        {
            var code = @"using System;
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
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 16);
        }

        [Fact]
        [WorkItem(530074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530074")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EnterInArgumentList1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        Main(args,

    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [Fact]
        [WorkItem(530074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530074")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EnterInArgumentList2()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        Main(args,
)
    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [Fact]
        [WorkItem(806266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/806266")]
        [Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task EnterInArgumentList3()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var s = string.Format(1,

    }
}";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 12);
        }

        [WorkItem(9216, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task FollowPreviousLineInMultilineStatements()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var accessibleConstructors = normalType.InstanceConstructors
                                       .Where(c => c.IsAccessibleWithin(within))
                                       .Where(s => s.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
.Sort(symbolDisplayService, invocationExpression.GetLocation(), semanticModel);
    }
}";
            await AssertSmartIndentAsync(code, indentationLine: 7, expectedIndentation: 39);
        }

        [WorkItem(648068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/648068")]
        [WorkItem(674611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674611")]
        [WpfFact(Skip = "674611"), Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task AtBeginningOfSpanInNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|
$$Console.WriteLine();|]|}
#line default
#line hidden
    }
}";
            await AssertSmartIndentInProjectionAsync(
                markup, BaseIndentationOfNugget + 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task AtEndOfSpanInNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|Console.WriteLine();
$$|]|}
#line default
#line hidden
    }
}";
            await AssertSmartIndentInProjectionAsync(
                markup, BaseIndentationOfNugget + 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task InMiddleOfSpanAtStartOfNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|Console.Wri
$$teLine();|]|}
#line default
#line hidden
    }
}";

            // Again, it doesn't matter where Console _is_ in this case -we format based on
            // where we think it _should_ be.  So the position is one indent level past the base
            // for the nugget (where we think the statement should be), plus one more since it is
            // a continuation
            await AssertSmartIndentInProjectionAsync(
                markup, BaseIndentationOfNugget + 8);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task InMiddleOfSpanInsideOfNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|
              Console.Wri
$$teLine();|]|}
#line default
#line hidden
    }
}";

            // Again, it doesn't matter where Console _is_ in this case -we format based on
            // where we think it _should_ be.  So the position is one indent level past the base
            // for the nugget (where we think the statement should be), plus one more since it is
            // a continuation
            await AssertSmartIndentInProjectionAsync(
                markup, BaseIndentationOfNugget + 8);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task AfterStatementInNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|
              Console.WriteLine();
$$
            |]|}
#line default
#line hidden
    }
}";
            await AssertSmartIndentInProjectionAsync(
                markup, BaseIndentationOfNugget + 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task AfterStatementOnFirstLineOfNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|Console.WriteLine();
$$
|]|}
#line default
#line hidden
    }
}";

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
            await AssertSmartIndentInProjectionAsync(
                markup, BaseIndentationOfNugget + 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task InQueryOnFistLineOfNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|var q = from
$$
|]|}
#line default
#line hidden
    }
}";
            await AssertSmartIndentInProjectionAsync(
                markup, BaseIndentationOfNugget + 8);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task InQueryInNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|
              var q = from
$$
|]|}
#line default
#line hidden
    }
}";
            await AssertSmartIndentInProjectionAsync(
                markup, BaseIndentationOfNugget + 8);
        }

        [WorkItem(9216, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task InsideBracesInNugget()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
                    {|S1:[|if (true)
        {
$$
        }|]|}
#line default
#line hidden
    }
}";
            await AssertSmartIndentInProjectionAsync(markup, BaseIndentationOfNugget + 8);
        }

        [WorkItem(9216, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task AfterEmbeddedStatementOnFirstLineOfNugget()
        {
            var markup = @"class Program
        {
            static void Main(string[] args)
            {
        #line ""Foo.aspx"", 27
                            {|S1:[|if (true)
                {
                }
                $$
|]|}
        #line default
        #line hidden
            }
        }";

            // In this case, we align the next statement with the "if" (though we _don't_
            // align the braces with it :S)
            await AssertSmartIndentInProjectionAsync(markup,
                expectedIndentation: BaseIndentationOfNugget + 2);
        }

        [WorkItem(9216, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task AfterEmbeddedStatementInNugget()
        {
            var markup = @"class Program
        {
            static void Main(string[] args)
            {
        #line ""Foo.aspx"", 27
                            {|S1:[|
            if (true)
            {
            }
$$
|]|}
        #line default
        #line hidden
            }
        }";

            // In this case we align with the "if", - the base indentation we pass in doesn't matter.
            await AssertSmartIndentInProjectionAsync(markup,
                expectedIndentation: BaseIndentationOfNugget + 4);
        }

        // this is the special case where the smart indenter 
        // aligns with the base or base + 4th position.
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task AfterSwitchStatementAtEndOfNugget()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|switch (10)
            {
                case 10:
$$
            }|]|}
#line default
#line hidden
    }
}";

            // It's yuck that I saw differences depending on where the end of the nugget is
            // but I did, so lets add a test.
            await AssertSmartIndentInProjectionAsync(markup,
                expectedIndentation: BaseIndentationOfNugget + 12);
        }

        // this is the special case where the smart indenter 
        // aligns with the base or base + 4th position.
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task AfterSwitchStatementInNugget()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
#line ""Foo.aspx"", 27
            {|S1:[|switch (10)
            {
                case 10:
$$
            }
|]|}
#line default
#line hidden
    }
}";

            await AssertSmartIndentInProjectionAsync(markup,
                expectedIndentation: BaseIndentationOfNugget + 12);
        }

        [WpfFact, WorkItem(529876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529876"), Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task InEmptyNugget()
        {
            var markup = @"class Program
        {
            static void Main(string[] args)
            {
        #line ""Foo.aspx"", 27
            {|S1:[|
$$|]|}
        #line default
        #line hidden
            }
        }";

            await AssertSmartIndentInProjectionAsync(markup,
                expectedIndentation: BaseIndentationOfNugget + 4);
        }

        [WorkItem(1190278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1190278")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task GetNextTokenForFormattingSpanCalculationIncludesZeroWidthToken_CS()
        {
            var markup = @"//------------------------------------------------------------------------------
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

#line 1 ""C:\Users\basoundr\Documents\Visual Studio 2015\WebSites\WebSite6\Default.cshtml""

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
}";

            await AssertSmartIndentInProjectionAsync(markup,
                expectedIndentation: 16);
        }

        [Fact, WorkItem(530948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530948"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task CommaSeparatedListEnumMembers()
        {
            var code = @"enum MyEnum
{
    e1,

}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 3,
                expectedIndentation: 4);
        }

        [Fact, WorkItem(530796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530796"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task RelativeIndentationForBracesInExpression()
        {
            var code = @"class C
{
    void M(C c)
    {
        M(new C()
        {

        });
    }
}
";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 12);
        }

        [Fact, WorkItem(584599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task SwitchSection()
        {
            var code = @"class C
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
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 12);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 16);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 16);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 12,
                expectedIndentation: 16);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 14,
                expectedIndentation: 16);
        }

        [Fact, WorkItem(584599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task SwitchSection2()
        {
            var code = @"class C
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
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 7,
                expectedIndentation: 12);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 10,
                expectedIndentation: 16);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 13,
                expectedIndentation: 16);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 16,
                expectedIndentation: 12);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 19,
                expectedIndentation: 12);
        }

        [Fact, WorkItem(584599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task CommentAtTheEndOfLine()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(); /* this is a comment */
                             // that I would like to keep


        // properly indented
    }
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 29);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 9,
                expectedIndentation: 8);
        }

        [Fact, WorkItem(912735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912735"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task CommentAtTheEndOfLineWithExecutableAfterCaret()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        // A
        // B


        return;
    }
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 8);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 9,
                expectedIndentation: 8);
        }

        [Fact, WorkItem(912735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912735"), Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task CommentAtTheEndOfLineInsideInitializer()
        {
            var code = @"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var s = new List<string>
                        {
                            """",
                                    """",/*sdfsdfsdfsdf*/
                                       // dfsdfsdfsdfsdf


                        };
    }
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 12,
                expectedIndentation: 39);

            await AssertSmartIndentAsync(
                code,
                indentationLine: 13,
                expectedIndentation: 36);
        }

        [WorkItem(5495, "https://github.com/dotnet/roslyn/issues/5495")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task AfterBadQueryContinuationWithSelectOrGroupClause()
        {
            var code = @"using System.Collections.Generic;
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
}";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 13,
                expectedIndentation: 25);
        }

        [WorkItem(5495, "https://github.com/dotnet/roslyn/issues/5495")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task AfterPartialFromClause()
        {
            var code = @"
using System.Linq;

class C
{
    void M()
    {
        var q = from x

    }
}
";
            await AssertSmartIndentAsync(
                code,
                indentationLine: 8,
                expectedIndentation: 16);
        }

        [WorkItem(5635, "https://github.com/dotnet/roslyn/issues/5635")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task ConstructorInitializerMissingBaseOrThisKeyword()
        {
            var code = @"
class C
{
     C(string s)
         :

}
";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 5,
                expectedIndentation: 8);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SmartIndent)]
        public async Task DontCreateIndentOperationForBrokenBracketedArgumentList()
        {
            var code = @"
class Program
{
    static void M()
    {
        string (userInput == ""Y"")

    }
}
";

            await AssertSmartIndentAsync(
                code,
                indentationLine: 6,
                expectedIndentation: 12);
        }

        private static async Task AssertSmartIndentInProjectionAsync(string markup, int expectedIndentation, CSharpParseOptions options = null)
        {
            var optionsSet = options != null
                    ? new[] { options }
                    : new[] { Options.Regular, Options.Script };

            foreach (var option in optionsSet)
            {
                using (var workspace = await TestWorkspace.CreateCSharpAsync(markup, parseOptions: option))
                {
                    var subjectDocument = workspace.Documents.Single();

                    var projectedDocument =
                        workspace.CreateProjectionBufferDocument(HtmlMarkup, workspace.Documents, LanguageNames.CSharp);

                    var provider = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>()
                                        as TestFormattingRuleFactoryServiceFactory.Factory;
                    if (provider != null)
                    {
                        provider.BaseIndentation = BaseIndentationOfNugget;
                        provider.TextSpan = subjectDocument.SelectedSpans.Single();
                    }

                    var indentationLine = projectedDocument.TextBuffer.CurrentSnapshot.GetLineFromPosition(projectedDocument.CursorPosition.Value);
                    var point = projectedDocument.GetTextView().BufferGraph.MapDownToBuffer(indentationLine.Start, PointTrackingMode.Negative, subjectDocument.TextBuffer, PositionAffinity.Predecessor);

                    await TestIndentationAsync(point.Value, expectedIndentation, projectedDocument.GetTextView(), subjectDocument);
                }
            }
        }

        private static async Task AssertSmartIndentAsync(
            string code,
            int indentationLine,
            int? expectedIndentation,
            CSharpParseOptions options = null)
        {
            var optionsSet = options != null
                ? new[] { options }
                : new[] { Options.Regular, Options.Script };

            foreach (var option in optionsSet)
            {
                using (var workspace = await TestWorkspace.CreateCSharpAsync(code, parseOptions: option))
                {
                    await TestIndentationAsync(indentationLine, expectedIndentation, workspace);
                }
            }
        }

        private static async Task AssertSmartIndentAsync(
            string code,
            int? expectedIndentation,
            CSharpParseOptions options = null)
        {
            var optionsSet = options != null
                ? new[] { options }
                : new[] { Options.Regular, Options.Script };

            foreach (var option in optionsSet)
            {
                using (var workspace = await TestWorkspace.CreateCSharpAsync(code, parseOptions: option))
                {
                    var wpfTextView = workspace.Documents.First().GetTextView();
                    var line = wpfTextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(wpfTextView.Caret.Position.BufferPosition).LineNumber;
                    await TestIndentationAsync(line, expectedIndentation, workspace);
                }
            }
        }
    }
}