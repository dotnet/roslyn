// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.CSharp.Formatting.Indentation;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation
{
    public class SmartTokenFormatterFormatRangeTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task BeginningOfFile()
        {
            var code = @"        using System;$$";
            var expected = @"        using System;";

            Assert.NotNull(await Record.ExceptionAsync(async () => await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.None)));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Namespace1()
        {
            var code = @"using System;
namespace NS
{

    }$$";

            var expected = @"using System;
namespace NS
{

}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Namespace2()
        {
            var code = @"using System;
namespace NS
{
        class Class
                {
        }
    }$$";

            var expected = @"using System;
namespace NS
{
    class Class
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Namespace3()
        {
            var code = @"using System;
namespace NS { }$$";

            var expected = @"using System;
namespace NS { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Namespace4()
        {
            var code = @"using System;
namespace NS { 
}$$";

            var expected = @"using System;
namespace NS
{
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Namespace5()
        {
            var code = @"using System;
namespace NS
{
    class Class { } 
}$$";

            var expected = @"using System;
namespace NS
{
    class Class { }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Namespace6()
        {
            var code = @"using System;
namespace NS
{
    class Class { 
} 
}$$";

            var expected = @"using System;
namespace NS
{
    class Class
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Namespace7()
        {
            var code = @"using System;
namespace NS
{
    class Class { 
} 
            namespace NS2
{}
}$$";

            var expected = @"using System;
namespace NS
{
    class Class
    {
    }
    namespace NS2
    { }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Namespace8()
        {
            var code = @"using System;
namespace NS { class Class { } namespace NS2 { } }$$";

            var expected = @"using System;
namespace NS { class Class { } namespace NS2 { } }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Class1()
        {
            var code = @"using System;
    class Class { 
}$$";

            var expected = @"using System;
class Class
{
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Class2()
        {
            var code = @"using System;
    class Class
{
    void Method(int i) {
                }
}$$";

            var expected = @"using System;
class Class
{
    void Method(int i)
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Class3()
        {
            var code = @"using System;
    class Class
{
    void Method(int i) { }
}$$";

            var expected = @"using System;
class Class
{
    void Method(int i) { }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Class4()
        {
            var code = @"using System;
    class Class
{
            delegate void Test(int i);
}$$";

            var expected = @"using System;
class Class
{
    delegate void Test(int i);
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Class5()
        {
            var code = @"using System;
    class Class
{
            delegate void Test(int i);
    void Method()
        {
                }
}$$";

            var expected = @"using System;
class Class
{
    delegate void Test(int i);
    void Method()
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Interface1()
        {
            var code = @"using System;
    interface II
{
            delegate void Test(int i);
int Prop { get; set; }
}$$";

            var expected = @"using System;
interface II
{
    delegate void Test(int i);
    int Prop { get; set; }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Struct1()
        {
            var code = @"using System;
    struct Struct
{
            Struct(int i)
    {
                }
}$$";

            var expected = @"using System;
struct Struct
{
    Struct(int i)
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Enum1()
        {
            var code = @"using System;
    enum Enum
{
                A = 1, B = 2,
    C = 3
            }$$";

            var expected = @"using System;
enum Enum
{
    A = 1, B = 2,
    C = 3
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AccessorList1()
        {
            var code = @"using System;
class Class
{
    int Prop { get { return 1; }$$";

            var expected = @"using System;
class Class
{
    int Prop { get { return 1; }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AccessorList2()
        {
            var code = @"using System;
class Class
{
    int Prop { get { return 1; } }$$";

            var expected = @"using System;
class Class
{
    int Prop { get { return 1; } }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.IntKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AccessorList3()
        {
            var code = @"using System;
class Class
{
    int Prop { get { return 1; }  
}$$";

            var expected = @"using System;
class Class
{
    int Prop
    {
        get { return 1; }
    }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.IntKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AccessorList4()
        {
            var code = @"using System;
class Class
{
    int Prop { get { return 1;   
}$$";

            var expected = @"using System;
class Class
{
    int Prop { get
        {
            return 1;
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.GetKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AccessorList5()
        {
            var code = @"using System;
class Class
{
    int Prop {
        get { return 1;   
}$$";

            var expected = @"using System;
class Class
{
    int Prop {
        get
        {
            return 1;
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.GetKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AccessorList6()
        {
            var code = @"using System;
class Class
{
    int Prop 
        { 
get { return 1;   
} }$$";

            var expected = @"using System;
class Class
{
    int Prop
    {
        get
        {
            return 1;
        }
    }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.IntKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AccessorList7()
        {
            var code = @"using System;
class Class
{
    int Prop
    {
        get
        {
return 1;$$
        }
    }";

            var expected = @"using System;
class Class
{
    int Prop
    {
        get
        {
            return 1;
        }
    }";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Block1()
        {
            var code = @"using System;
class Class
{
    public int Method()
    { }$$";

            var expected = @"using System;
class Class
{
    public int Method()
    { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Block2()
        {
            var code = @"using System;
class Class
{
    public int Method() { }$$";

            var expected = @"using System;
class Class
{
    public int Method() { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Block3()
        {
            var code = @"using System;
class Class
{
    public int Method() { 
}$$
}";

            var expected = @"using System;
class Class
{
    public int Method()
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Block4()
        {
            var code = @"using System;
class Class
{
    public static Class operator +(Class c1, Class c2) {
            }$$
}";

            var expected = @"using System;
class Class
{
    public static Class operator +(Class c1, Class c2)
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Block5()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        { }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Block6()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        { 
}$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        {
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Block7()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        { { }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        { { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Block8()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        { { 
}$$
        }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        {
            {
            }
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task SwitchStatement1()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        switch (a) {
            case 1:
                break;
}$$
    }
}";

            var expected = @"using System;
class Class
{
    void Method()
    {
        switch (a)
        {
            case 1:
                break;
        }
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task SwitchStatement2()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        switch (true) { }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        switch (true) { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task SwitchStatement3()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        switch (true)
        {
            case 1: { }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        switch (true)
        {
            case 1: { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.ColonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task SwitchStatement4()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        switch (true)
        {
            case 1: { 
}$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        switch (true)
        {
            case 1:
                {
                }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.ColonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Initializer1()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        var arr = new int[] { }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        var arr = new int[] { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Initializer2()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        var arr = new int[] { 
}$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        var arr = new int[] {
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Initializer3()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        var arr = new { A = 1, B = 2
}$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        var arr = new
        {
            A = 1,
            B = 2
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Initializer4()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        var arr = new { A = 1, B = 2 }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        var arr = new { A = 1, B = 2 }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Initializer5()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        var arr = new[] { 
            1, 2, 3, 4,
            5 }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        var arr = new[] {
            1, 2, 3, 4,
            5 }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Initializer6()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        var arr = new int[] { 
            1, 2, 3, 4,
            5 }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        var arr = new int[] {
            1, 2, 3, 4,
            5 }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement1()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        if (true) { }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        if (true) { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement2()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        if (true) { 
        }$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        if (true)
        {
        }
    }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement3()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        if (true)
        { }$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        if (true)
        { }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement4()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        while (true) {
}$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        while (true)
        {
        }
    }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement5()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        do {
} while(true);$$
    }
}";

            var expected = @"using System;
class Class
{
    void Method()
    {
        do
        {
        } while (true);
    }
}";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement6()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        for (int i = 0; i < 10; i++)             {
}$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        for (int i = 0; i < 10; i++)
        {
        }
    }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement7()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        foreach (var i in collection)            {
}$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        foreach (var i in collection)
        {
        }
    }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement8()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        using (var resource = GetResource())           {
}$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        using (var resource = GetResource())
        {
        }
    }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement9()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        if (true)
                int i = 10;$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        if (true)
            int i = 10;";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FieldlInitializer()
        {
            var code = @"using System;
class Class
{
          string str =              Console.Title;$$
";

            var expected = @"using System;
class Class
{
    string str = Console.Title;
";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task ArrayFieldlInitializer()
        {
            var code = @"using System;
namespace NS
{
    class Class
    {
                    string[] strArr = {           ""1"",                       ""2"" };$$
";

            var expected = @"using System;
namespace NS
{
    class Class
    {
        string[] strArr = { ""1"", ""2"" };
";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task ExpressionValuedPropertyInitializer()
        {
            var code = @"using System;
class Class
{
          public int  Three =>   1+2;$$
";

            var expected = @"using System;
class Class
{
    public int Three => 1 + 2;
";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement10()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
        if (true)
                int i = 10;$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        if (true)
            int i = 10;
    }";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement11()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
                using (var resource = GetResource()) resource.Do();$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        using (var resource = GetResource()) resource.Do();";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement12()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
                using (var resource = GetResource()) 
    resource.Do();$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        using (var resource = GetResource())
            resource.Do();";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement13()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
                using (var resource = GetResource()) 
    resource.Do();$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        using (var resource = GetResource())
            resource.Do();
    }";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement14()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
                do i = 10;$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        do i = 10;";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement15()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
                do
    i = 10;$$";

            var expected = @"using System;
class Class
{
    void Method()
    {
        do
            i = 10;";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement16()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
                do
    i = 10;$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        do
            i = 10;
    }";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmbeddedStatement17()
        {
            var code = @"using System;
class Class
{
    void Method()
    {
                do
    i = 10;
while (true);$$
    }";

            var expected = @"using System;
class Class
{
    void Method()
    {
        do
            i = 10;
        while (true);
    }";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FollowPreviousElement1()
        {
            var code = @"using System;
class Class
{
                    int i = 10;
                    int i2 = 10;$$";

            var expected = @"using System;
class Class
{
                    int i = 10;
    int i2 = 10;";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FollowPreviousElement2()
        {
            var code = @"using System;
class Class
{
            void Method(int i)
            {
            }

            void Method2()
            {
            }$$
}";

            var expected = @"using System;
class Class
{
            void Method(int i)
            {
            }

    void Method2()
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.CloseBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FollowPreviousElement3()
        {
            var code = @"using System;
class Class
{
            void Method(int i)
            {
            }

            A a = new A 
            {
                Prop = 1,
                Prop2 = 2
            };$$
}";

            var expected = @"using System;
class Class
{
            void Method(int i)
            {
            }

    A a = new A
    {
        Prop = 1,
        Prop2 = 2
    };
}";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.CloseBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FollowPreviousElement4()
        {
            var code = @"using System;
class Class
{
            void Method(int i)
            {
                        int i = 10;
             int i2 = 10;$$";

            var expected = @"using System;
class Class
{
            void Method(int i)
            {
                        int i = 10;
        int i2 = 10;";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FollowPreviousElement5()
        {
            var code = @"using System;
class Class
{
            void Method(int i)
            {
                        int i = 10;
                if (true)
i = 50;$$";

            var expected = @"using System;
class Class
{
            void Method(int i)
            {
                        int i = 10;
        if (true)
            i = 50;";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FollowPreviousElement6()
        {
            var code = @"        using System;
        using System.Linq;$$";

            var expected = @"        using System;
using System.Linq;";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FollowPreviousElement7()
        {
            var code = @"            using System;

            namespace NS
            {
            }

        namespace NS2
        {
        }$$";

            var expected = @"            using System;

            namespace NS
            {
            }

namespace NS2
{
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.CloseBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FollowPreviousElement8()
        {
            var code = @"using System;

namespace NS
{
            class Class
            {
            }

        class Class1
        {
        }$$
}";

            var expected = @"using System;

namespace NS
{
            class Class
            {
            }

    class Class1
    {
    }
}";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.CloseBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task IfStatement1()
        {
            var code = @"using System;

class Class
{
    void Method()
    {
            if (true)
        {
    }$$";

            var expected = @"using System;

class Class
{
    void Method()
    {
        if (true)
        {
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task IfStatement2()
        {
            var code = @"using System;

class Class
{
    void Method()
    {
            if (true)
        {
    }
else
        {
                }$$";

            var expected = @"using System;

class Class
{
    void Method()
    {
        if (true)
        {
        }
        else
        {
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task IfStatement3()
        {
            var code = @"using System;

class Class
{
    void Method()
    {
            if (true)
        {
    }
else    if (false)
        {
                }$$";

            var expected = @"using System;

class Class
{
    void Method()
    {
        if (true)
        {
        }
        else if (false)
        {
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task IfStatement4()
        {
            var code = @"using System;

class Class
{
    void Method()
    {
            if (true)
        return          ;
else    if (false)
                    return          ;$$";

            var expected = @"using System;

class Class
{
    void Method()
    {
        if (true)
            return;
        else if (false)
            return;";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task TryStatement1()
        {
            var code = @"using System;

class Class
{
    void Method()
    {
                try
    {
        }$$";

            var expected = @"using System;

class Class
{
    void Method()
    {
        try
        {
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task TryStatement2()
        {
            var code = @"using System;

class Class
{
    void Method()
    {
                try
    {
        }
catch    (  Exception       ex)
                {
    }$$";

            var expected = @"using System;

class Class
{
    void Method()
    {
        try
        {
        }
        catch (Exception ex)
        {
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task TryStatement3()
        {
            var code = @"using System;

class Class
{
    void Method()
    {
                try
    {
        }
catch    (  Exception       ex)
                {
    }
            catch               (Exception          ex2)
                      {
   }$$";

            var expected = @"using System;

class Class
{
    void Method()
    {
        try
        {
        }
        catch (Exception ex)
        {
        }
        catch (Exception ex2)
        {
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task TryStatement4()
        {
            var code = @"using System;

class Class
{
    void Method()
    {
                try
    {
        }
                                finally
                      {
   }$$";

            var expected = @"using System;

class Class
{
    void Method()
    {
        try
        {
        }
        finally
        {
        }";

            await AutoFormatOnCloseBraceAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        [WorkItem(537555, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537555")]
        public async Task SingleLine()
        {
            var code = @"class C { void M() { C.M(    );$$ } }";

            var expected = @"class C { void M() { C.M(); } }";

            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task StringLiterals()
        {
            var code = @"class C { void M() { C.M(""Test {0}$$";

            var expected = string.Empty;
            await AutoFormatOnMarkerAsync(code, expected, SyntaxKind.StringLiteralToken, SyntaxKind.None);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task CharLiterals()
        {
            var code = @"class C { void M() { C.M('}$$";

            var expected = string.Empty;
            await AutoFormatOnMarkerAsync(code, expected, SyntaxKind.CharacterLiteralToken, SyntaxKind.None);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task CharLiterals1()
        {
            var code = @"''';$$";

            var expected = string.Empty;
            await AutoFormatOnMarkerAsync(code, expected, SyntaxKind.EndOfFileToken, SyntaxKind.None);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Comments()
        {
            var code = @"class C { void M() { // { }$$";

            var expected = string.Empty;
            await AutoFormatOnMarkerAsync(code, expected, SyntaxKind.OpenBraceToken, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FirstLineInFile()
        {
            var code = @"using System;$$";

            await AutoFormatOnSemicolonAsync(code, "using System;", SyntaxKind.UsingKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Label1()
        {
            var code = @"class C
{
    void Method()
    {
                L           :               int             i               =               20;$$
    }
}";
            var expected = @"class C
{
    void Method()
    {
    L: int i = 20;
    }
}";
            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Label2()
        {
            var code = @"class C
{
    void Method()
    {
                L           :               
int             i               =               20;$$
    }
}";
            var expected = @"class C
{
    void Method()
    {
    L:
        int i = 20;
    }
}";
            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Label3()
        {
            var code = @"class C
{
    void Method()
    {
        int base = 10;
                L           :               
int             i               =               20;$$
    }
}";
            var expected = @"class C
{
    void Method()
    {
        int base = 10;
    L:
        int i = 20;
    }
}";
            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Label4()
        {
            var code = @"class C
{
    void Method()
    {
        int base = 10;
    L:
        int i = 20;
int         nextLine            =           30          ;$$
    }
}";
            var expected = @"class C
{
    void Method()
    {
        int base = 10;
    L:
        int i = 20;
        int nextLine = 30;
    }
}";
            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Label6()
        {
            var code = @"class C
{
    void Method()
    {
    L:
        int i = 20;
int         nextLine            =           30          ;$$
    }
}";
            var expected = @"class C
{
    void Method()
    {
    L:
        int i = 20;
        int nextLine = 30;
    }
}";
            await AutoFormatOnSemicolonAsync(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WorkItem(537776, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537776")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task DisappearedTokens()
        {
            var code = @"class Class1
{
    int foo()
        return 0;
        }$$
}";

            var expected = @"class Class1
{
    int foo()
        return 0;
        }
}";
            await AutoFormatOnCloseBraceAsync(
                code,
                expected,
                SyntaxKind.ClassKeyword);
        }

        [WorkItem(537779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537779")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task DisappearedTokens2()
        {
            var code = @"class Class1
{
    void Foo()
    {
        Object o=new Object);$$
    }
}";

            var expected = @"class Class1
{
    void Foo()
    {
        Object o=new Object);
    }
}";
            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.SemicolonToken);
        }

        [WorkItem(537793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537793")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Delegate1()
        {
            var code = @"delegate void MyDelegate(int a,int b);$$";

            var expected = @"delegate void MyDelegate(int a, int b);";

            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.DelegateKeyword);
        }

        [WorkItem(537827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537827")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task DoubleInitializer()
        {
            var code = @"class C
{
    void Method()
    {
        int[,] a ={{ 1 , 1 }$$
    }
}";

            var expected = @"class C
{
    void Method()
    {
        int[,] a ={{ 1 , 1 }
    }
}";

            await AutoFormatOnCloseBraceAsync(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WorkItem(537825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537825")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task MissingToken1()
        {
            var code = @"public class Class1
{
    int a = 1}$$;
}";

            var expected = @"public class Class1
{
    int a = 1};
}";

            await AutoFormatOnCloseBraceAsync(
                code,
                expected,
                SyntaxKind.PublicKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task ArrayInitializer1()
        {
            var code = @"public class Class1
{
    var a = new [] 
    {
        1, 2, 3, 4
        }$$
}";

            var expected = @"public class Class1
{
    var a = new[]
    {
        1, 2, 3, 4
        }
}";

            await AutoFormatOnCloseBraceAsync(
                code,
                expected,
                SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task ArrayInitializer2()
        {
            var code = @"public class Class1
{
    var a = new [] 
    {
        1, 2, 3, 4
        }   ;$$
}";

            var expected = @"public class Class1
{
    var a = new[]
    {
        1, 2, 3, 4
        };
}";

            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(537825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537825")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task MalformedCode()
        {
            var code = @"namespace ClassLibrary1
{
    public class Class1
    {
        int a}$$;
    }
}";

            var expected = @"namespace ClassLibrary1
{
    public class Class1
    {
        int a};
    }
}";

            await AutoFormatOnCloseBraceAsync(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(537804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537804")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Colon_SwitchLabel()
        {
            var code = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
            switch(E.Type)
            {
                    case 1 :$$
            }
        }
    }
}";

            var expected = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
            switch(E.Type)
            {
                case 1:
            }
        }
    }
}";

            await AutoFormatOnColonAsync(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(584599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Colon_SwitchLabel_Comment()
        {
            var code = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
            switch(E.Type)
            {
                        // test
                    case 1 :$$
            }
        }
    }
}";

            var expected = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
            switch(E.Type)
            {
                // test
                case 1:
            }
        }
    }
}";

            await AutoFormatOnColonAsync(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(584599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Colon_SwitchLabel_Comment2()
        {
            var code = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
            switch(E.Type)
            {
                case 2:
                    // test
                    case 1 :$$
            }
        }
    }
}";

            var expected = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
            switch(E.Type)
            {
                case 2:
                // test
                case 1:
            }
        }
    }
}";

            await AutoFormatOnColonAsync(
                code,
                expected,
                SyntaxKind.ColonToken);
        }

        [Fact]
        [WorkItem(537804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537804")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Colon_Label()
        {
            var code = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
                    label   :$$
        }
    }
}";

            var expected = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
                    label   :
        }
    }
}";

            await AutoFormatOnColonAsync(
                code,
                expected,
                SyntaxKind.None);
        }

        [WpfFact]
        [WorkItem(538793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538793")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task Colon_Label2()
        {
            var code = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
                    label   :   Console.WriteLine(10) ;$$
        }
    }
}";

            var expected = @"namespace ClassLibrary1
{
    public class Class1
    {
        void Test()
        {
        label: Console.WriteLine(10);
        }
    }
}";

            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(3186, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task SemicolonInElseIfStatement()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int a = 0;
        if (a == 0)
            a = 1;
        else if (a == 1)
            a=2;$$
        else
            a = 3;

    }
}";

            var expected = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int a = 0;
        if (a == 0)
            a = 1;
        else if (a == 1)
            a = 2;
        else
            a = 3;

    }
}";

            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [WorkItem(538391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538391")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task SemicolonInElseIfStatement2()
        {
            var code = @"public class Class1
{
    void Method()
    {
        int a = 1;
        if (a == 0)
            a = 8;$$
                    else
                        a = 10;
    }
}";

            var expected = @"public class Class1
{
    void Method()
    {
        int a = 1;
        if (a == 0)
            a = 8;
        else
            a = 10;
    }
}";

            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [WorkItem(8385, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task NullCoalescingOperator()
        {
            var code = @"class C
{
    void M()
    {
        object o2 = null??null;$$
    }
}";

            var expected = @"class C
{
    void M()
    {
        object o2 = null ?? null;
    }
}";

            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(541517, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541517")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task SwitchDefault()
        {
            var code = @"using System;
class Program
{
    static void Main()
    {
        switch (DayOfWeek.Monday)
        {
            case DayOfWeek.Monday:
            case DayOfWeek.Tuesday:
                break;
            case DayOfWeek.Wednesday:
                break;
                default:$$
        }
    }
}";

            var expected = @"using System;
class Program
{
    static void Main()
    {
        switch (DayOfWeek.Monday)
        {
            case DayOfWeek.Monday:
            case DayOfWeek.Tuesday:
                break;
            case DayOfWeek.Wednesday:
                break;
            default:
        }
    }
}";

            await AutoFormatOnColonAsync(
                code,
                expected,
                SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [WorkItem(542538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542538")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task MissingTokens1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        gl::$$
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        gl::
    }
}";

            await AutoFormatOnMarkerAsync(
                code,
                expected,
                SyntaxKind.ColonColonToken,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(542538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542538")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task MissingTokens2()
        {
            var code = @"class C { void M() { M(() => { }$$ } }";

            var expected = @"class C { void M() { M(() => { } } }";

            await AutoFormatOnCloseBraceAsync(
                code,
                expected,
                SyntaxKind.EqualsGreaterThanToken);
        }

        [WpfFact]
        [WorkItem(542953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542953")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task UsingAlias()
        {
            var code = @"using Alias=System;$$";

            var expected = @"using Alias = System;";

            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.UsingKeyword);
        }

        [WpfFact]
        [WorkItem(542953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542953")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task NoLineChangeWithSyntaxError()
        {
            var code = @"struct Foo { public int member; }
class Program{
    void Main()
    {
        var f = new Foo { member;$$ }
    }
}";

            var expected = @"struct Foo { public int member; }
class Program{
    void Main()
    {
        var f = new Foo { member; }
    }
}";

            await AutoFormatOnSemicolonAsync(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(620568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/620568")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task SkippedTokens1()
        {
            var code = @";$$*";

            var expected = @";*";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(530830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530830")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AutoPropertyAccessor()
        {
            var code = @"class C
{
    int Prop {          get             ;$$
}";

            var expected = @"class C
{
    int Prop {          get;
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(530830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530830")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AutoPropertyAccessor2()
        {
            var code = @"class C
{
    int Prop {          get;                set             ;$$
}";

            var expected = @"class C
{
    int Prop {          get;                set;
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(530830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530830")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AutoPropertyAccessor3()
        {
            var code = @"class C
{
    int Prop {          get;                set             ;           }$$
}";

            var expected = @"class C
{
    int Prop { get; set; }
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(784674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784674")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AutoPropertyAccessor4()
        {
            var code = @"class C
{
    int Prop {          get;$$             }
}";

            var expected = @"class C
{
    int Prop { get; }
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(924469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924469")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AutoPropertyAccessor5()
        {
            var code = @"class C
{
    int Prop {          get;                set             ;$$           }
}";

            var expected = @"class C
{
    int Prop { get; set; }
}";
            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(924469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924469")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AutoPropertyAccessor6()
        {
            var code = @"class C
{
    int Prop { get;set;$$}
}";

            var expected = @"class C
{
    int Prop { get; set; }
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(924469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924469")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task AutoPropertyAccessor7()
        {
            var code = @"class C
{
    int Prop     { get;set;$$}    
}";

            var expected = @"class C
{
    int Prop     { get; set; }    
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(912965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task NestedUsingStatement()
        {
            var code = @"class C
{
    public void M()
    {
        using (null)
            using(null)$$
    }
}";

            var expected = @"class C
{
    public void M()
    {
        using (null)
        using (null)
    }
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(912965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task NestedNotUsingStatement()
        {
            var code = @"class C
{
    public void M()
    {
        using (null)
            for(;;)$$
    }
}";

            var expected = @"class C
{
    public void M()
    {
        using (null)
            for(;;)
    }
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(954386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954386")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FormattingRangeForFirstStatementOfBlock()
        {
            var code = @"class C
{
    public void M()
    {int s;$$
    }
}";

            var expected = @"class C
{
    public void M()
    {
        int s;
    }
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(954386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954386")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FormattingRangeForFirstMemberofType()
        {
            var code = @"class C
{int s;$$
    public void M()
    {
    }
}";

            var expected = @"class C
{
    int s;
    public void M()
    {
    }
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(954386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954386")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FormattingRangeForFirstMethodMemberofType()
        {
            var code = @"interface C
{void s();$$
}";

            var expected = @"interface C
{
    void s();
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WpfFact]
        [WorkItem(954386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954386")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task FormattingRangeForFirstMemberOfNamespace()
        {
            var code = @"namespace C
{delegate void s();$$
}";

            var expected = @"namespace C
{
    delegate void s();
}";

            await AutoFormatTokenAsync(code, expected);
        }

        [WorkItem(981821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981821")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatDirectiveTriviaAlwaysToColumnZero()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
#if
        #$$
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
#if
#
    }
}
";

            await AutoFormatTokenAsync(code, expected);
        }

        [WorkItem(981821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981821")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatDirectiveTriviaAlwaysToColumnZeroWithCode()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
#if
        int s = 10;
        #$$
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
#if
        int s = 10;
#
    }
}
";

            await AutoFormatTokenAsync(code, expected);
        }

        [WorkItem(981821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981821")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatDirectiveTriviaAlwaysToColumnZeroWithBrokenElseDirective()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
#else
        #$$
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
#else
#
    }
}
";

            await AutoFormatTokenAsync(code, expected);
        }

        internal static async Task AutoFormatTokenAsync(string markup, string expected)
        {
            using (var workspace = await TestWorkspace.CreateCSharpAsync(markup))
            {
                var subjectDocument = workspace.Documents.Single();

                var optionService = workspace.Services.GetService<IOptionService>();
                var textUndoHistory = new Mock<ITextUndoHistoryRegistry>();
                var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
                var editorOperations = new Mock<IEditorOperations>();
                editorOperationsFactory.Setup(x => x.GetEditorOperations(subjectDocument.GetTextView())).Returns(editorOperations.Object);

                var commandHandler = new FormatCommandHandler(TestWaitIndicator.Default, textUndoHistory.Object, editorOperationsFactory.Object);
                var typedChar = subjectDocument.GetTextBuffer().CurrentSnapshot.GetText(subjectDocument.CursorPosition.Value - 1, 1);
                commandHandler.ExecuteCommand(new TypeCharCommandArgs(subjectDocument.GetTextView(), subjectDocument.TextBuffer, typedChar[0]), () => { });

                var newSnapshot = subjectDocument.TextBuffer.CurrentSnapshot;

                Assert.Equal(expected, newSnapshot.GetText());
            }
        }

        private static Tuple<OptionSet, IEnumerable<IFormattingRule>> GetService(
            TestWorkspace workspace)
        {
            var options = workspace.Options;
            return Tuple.Create(options, Formatter.GetDefaultFormattingRules(workspace, LanguageNames.CSharp));
        }

        private Task AutoFormatOnColonAsync(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        {
            return AutoFormatOnMarkerAsync(codeWithMarker, expected, SyntaxKind.ColonToken, startTokenKind);
        }

        private Task AutoFormatOnSemicolonAsync(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        {
            return AutoFormatOnMarkerAsync(codeWithMarker, expected, SyntaxKind.SemicolonToken, startTokenKind);
        }

        private Task AutoFormatOnCloseBraceAsync(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        {
            return AutoFormatOnMarkerAsync(codeWithMarker, expected, SyntaxKind.CloseBraceToken, startTokenKind);
        }

        private async Task AutoFormatOnMarkerAsync(string initialMarkup, string expected, SyntaxKind tokenKind, SyntaxKind startTokenKind)
        {
            using (var workspace = await TestWorkspace.CreateCSharpAsync(initialMarkup))
            {
                var tuple = GetService(workspace);
                var testDocument = workspace.Documents.Single();
                var buffer = testDocument.GetTextBuffer();
                var position = testDocument.CursorPosition.Value;

                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

                var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync();
                var endToken = root.FindToken(position);
                if (position == endToken.SpanStart && !endToken.GetPreviousToken().IsKind(SyntaxKind.None))
                {
                    endToken = endToken.GetPreviousToken();
                }

                Assert.Equal(tokenKind, endToken.Kind());
                var formatter = new SmartTokenFormatter(tuple.Item1, tuple.Item2, root);

                var tokenRange = FormattingRangeHelper.FindAppropriateRange(endToken);
                if (tokenRange == null)
                {
                    Assert.Equal(startTokenKind, SyntaxKind.None);
                    return;
                }

                Assert.Equal(startTokenKind, tokenRange.Value.Item1.Kind());
                if (tokenRange.Value.Item1.Equals(tokenRange.Value.Item2))
                {
                    return;
                }

                var changes = formatter.FormatRange(workspace, tokenRange.Value.Item1, tokenRange.Value.Item2, CancellationToken.None);
                var actual = GetFormattedText(buffer, changes);
                Assert.Equal(expected, actual);
            }
        }

        private static string GetFormattedText(ITextBuffer buffer, IList<TextChange> changes)
        {
            using (var edit = buffer.CreateEdit())
            {
                foreach (var change in changes)
                {
                    edit.Replace(change.Span.ToSpan(), change.NewText);
                }

                edit.Apply();
            }

            return buffer.CurrentSnapshot.GetText();
        }
    }
}
