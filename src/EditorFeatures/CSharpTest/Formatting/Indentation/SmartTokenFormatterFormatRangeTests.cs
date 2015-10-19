// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void BeginningOfFile()
        {
            var code = @"        using System;$$";
            var expected = @"        using System;";

            Assert.NotNull(Record.Exception(() => AutoFormatOnSemicolon(code, expected, SyntaxKind.None)));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace1()
        {
            var code = @"using System;
namespace NS
{

    }$$";

            var expected = @"using System;
namespace NS
{

}";

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace2()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace3()
        {
            var code = @"using System;
namespace NS { }$$";

            var expected = @"using System;
namespace NS { }";

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace4()
        {
            var code = @"using System;
namespace NS { 
}$$";

            var expected = @"using System;
namespace NS
{
}";

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace5()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace6()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace7()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace8()
        {
            var code = @"using System;
namespace NS { class Class { } namespace NS2 { } }$$";

            var expected = @"using System;
namespace NS { class Class { } namespace NS2 { } }";

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Class1()
        {
            var code = @"using System;
    class Class { 
}$$";

            var expected = @"using System;
class Class
{
}";

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Class2()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Class3()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Class4()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Class5()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Interface1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Struct1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Enum1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AccessorList1()
        {
            var code = @"using System;
class Class
{
    int Prop { get { return 1; }$$";

            var expected = @"using System;
class Class
{
    int Prop { get { return 1; }";

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AccessorList2()
        {
            var code = @"using System;
class Class
{
    int Prop { get { return 1; } }$$";

            var expected = @"using System;
class Class
{
    int Prop { get { return 1; } }";

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.IntKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AccessorList3()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.IntKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AccessorList4()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.GetKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AccessorList5()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.GetKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AccessorList6()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.IntKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AccessorList7()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block2()
        {
            var code = @"using System;
class Class
{
    public int Method() { }$$";

            var expected = @"using System;
class Class
{
    public int Method() { }";

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block3()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block4()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block5()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block6()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block7()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block8()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void SwitchStatement1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void SwitchStatement2()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void SwitchStatement3()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.ColonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void SwitchStatement4()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.ColonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Initializer1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Initializer2()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Initializer3()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Initializer4()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Initializer5()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Initializer6()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement2()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement3()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement4()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement5()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement6()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement7()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement8()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement9()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FieldlInitializer()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void ArrayFieldlInitializer()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void ExpressionValuedPropertyInitializer()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement10()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement11()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement12()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement13()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement14()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement15()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement16()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmbeddedStatement17()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FollowPreviousElement1()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FollowPreviousElement2()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.CloseBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FollowPreviousElement3()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.CloseBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FollowPreviousElement4()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FollowPreviousElement5()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FollowPreviousElement6()
        {
            var code = @"        using System;
        using System.Linq;$$";

            var expected = @"        using System;
using System.Linq;";

            AutoFormatOnSemicolon(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FollowPreviousElement7()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.CloseBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FollowPreviousElement8()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.CloseBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void IfStatement1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void IfStatement2()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void IfStatement3()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void IfStatement4()
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

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TryStatement1()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TryStatement2()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TryStatement3()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TryStatement4()
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

            AutoFormatOnCloseBrace(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        [WorkItem(537555)]
        public void SingleLine()
        {
            var code = @"class C { void M() { C.M(    );$$ } }";

            var expected = @"class C { void M() { C.M(); } }";

            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void StringLiterals()
        {
            var code = @"class C { void M() { C.M(""Test {0}$$";

            var expected = string.Empty;
            AutoFormatOnMarker(code, expected, SyntaxKind.StringLiteralToken, SyntaxKind.None);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void CharLiterals()
        {
            var code = @"class C { void M() { C.M('}$$";

            var expected = string.Empty;
            AutoFormatOnMarker(code, expected, SyntaxKind.CharacterLiteralToken, SyntaxKind.None);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void CharLiterals1()
        {
            var code = @"''';$$";

            var expected = string.Empty;
            AutoFormatOnMarker(code, expected, SyntaxKind.EndOfFileToken, SyntaxKind.None);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Comments()
        {
            var code = @"class C { void M() { // { }$$";

            var expected = string.Empty;
            AutoFormatOnMarker(code, expected, SyntaxKind.OpenBraceToken, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FirstLineInFile()
        {
            var code = @"using System;$$";

            AutoFormatOnSemicolon(code, "using System;", SyntaxKind.UsingKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Label1()
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
            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Label2()
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
            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Label3()
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
            AutoFormatOnSemicolon(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Label4()
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
            AutoFormatOnSemicolon(code, expected, SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Label6()
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
            AutoFormatOnSemicolon(code, expected, SyntaxKind.OpenBraceToken);
        }

        [WorkItem(537776)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void DisappearedTokens()
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
            AutoFormatOnCloseBrace(
                code,
                expected,
                SyntaxKind.ClassKeyword);
        }

        [WorkItem(537779)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void DisappearedTokens2()
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
            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.SemicolonToken);
        }

        [WorkItem(537793)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Delegate1()
        {
            var code = @"delegate void MyDelegate(int a,int b);$$";

            var expected = @"delegate void MyDelegate(int a, int b);";

            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.DelegateKeyword);
        }

        [WorkItem(537827)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void DoubleInitializer()
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

            AutoFormatOnCloseBrace(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WorkItem(537825)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void MissingToken1()
        {
            var code = @"public class Class1
{
    int a = 1}$$;
}";

            var expected = @"public class Class1
{
    int a = 1};
}";

            AutoFormatOnCloseBrace(
                code,
                expected,
                SyntaxKind.PublicKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void ArrayInitializer1()
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

            AutoFormatOnCloseBrace(
                code,
                expected,
                SyntaxKind.NewKeyword);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void ArrayInitializer2()
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

            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(537825)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void MalformedCode()
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

            AutoFormatOnCloseBrace(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(537804)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Colon_SwitchLabel()
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

            AutoFormatOnColon(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(584599)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Colon_SwitchLabel_Comment()
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

            AutoFormatOnColon(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(584599)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Colon_SwitchLabel_Comment2()
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

            AutoFormatOnColon(
                code,
                expected,
                SyntaxKind.ColonToken);
        }

        [WpfFact]
        [WorkItem(537804)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Colon_Label()
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

            AutoFormatOnColon(
                code,
                expected,
                SyntaxKind.None);
        }

        [WpfFact]
        [WorkItem(538793)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Colon_Label2()
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

            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(3186, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void SemicolonInElseIfStatement()
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

            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [WorkItem(538391)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void SemicolonInElseIfStatement2()
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

            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [WorkItem(8385, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void NullCoalescingOperator()
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

            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(541517)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void SwitchDefault()
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

            AutoFormatOnColon(
                code,
                expected,
                SyntaxKind.SemicolonToken);
        }

        [WpfFact]
        [WorkItem(542538)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void MissingTokens1()
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

            AutoFormatOnMarker(
                code,
                expected,
                SyntaxKind.ColonColonToken,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(542538)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void MissingTokens2()
        {
            var code = @"class C { void M() { M(() => { }$$ } }";

            var expected = @"class C { void M() { M(() => { } } }";

            AutoFormatOnCloseBrace(
                code,
                expected,
                SyntaxKind.EqualsGreaterThanToken);
        }

        [WpfFact]
        [WorkItem(542953)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void UsingAlias()
        {
            var code = @"using Alias=System;$$";

            var expected = @"using Alias = System;";

            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.UsingKeyword);
        }

        [WpfFact]
        [WorkItem(542953)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void NoLineChangeWithSyntaxError()
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

            AutoFormatOnSemicolon(
                code,
                expected,
                SyntaxKind.OpenBraceToken);
        }

        [WpfFact]
        [WorkItem(620568)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void SkippedTokens1()
        {
            var code = @";$$*";

            var expected = @";*";

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(530830)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AutoPropertyAccessor()
        {
            var code = @"class C
{
    int Prop {          get             ;$$
}";

            var expected = @"class C
{
    int Prop {          get;
}";

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(530830)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AutoPropertyAccessor2()
        {
            var code = @"class C
{
    int Prop {          get;                set             ;$$
}";

            var expected = @"class C
{
    int Prop {          get;                set;
}";

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(530830)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AutoPropertyAccessor3()
        {
            var code = @"class C
{
    int Prop {          get;                set             ;           }$$
}";

            var expected = @"class C
{
    int Prop { get; set; }
}";

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(784674)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AutoPropertyAccessor4()
        {
            var code = @"class C
{
    int Prop {          get;$$             }
}";

            var expected = @"class C
{
    int Prop { get; }
}";

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(924469)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AutoPropertyAccessor5()
        {
            var code = @"class C
{
    int Prop {          get;                set             ;$$           }
}";

            var expected = @"class C
{
    int Prop { get; set; }
}";
            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(924469)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AutoPropertyAccessor6()
        {
            var code = @"class C
{
    int Prop { get;set;$$}
}";

            var expected = @"class C
{
    int Prop { get; set; }
}";

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(924469)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AutoPropertyAccessor7()
        {
            var code = @"class C
{
    int Prop     { get;set;$$}    
}";

            var expected = @"class C
{
    int Prop     { get; set; }    
}";

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(912965)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void NestedUsingStatement()
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

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(912965)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void NestedNotUsingStatement()
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

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(954386)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FormattingRangeForFirstStatementOfBlock()
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

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(954386)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FormattingRangeForFirstMemberofType()
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

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(954386)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FormattingRangeForFirstMethodMemberofType()
        {
            var code = @"interface C
{void s();$$
}";

            var expected = @"interface C
{
    void s();
}";

            AutoFormatToken(code, expected);
        }

        [WpfFact]
        [WorkItem(954386)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void FormattingRangeForFirstMemberOfNamespace()
        {
            var code = @"namespace C
{delegate void s();$$
}";

            var expected = @"namespace C
{
    delegate void s();
}";

            AutoFormatToken(code, expected);
        }

        [WorkItem(981821)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDirectiveTriviaAlwaysToColumnZero()
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

            AutoFormatToken(code, expected);
        }

        [WorkItem(981821)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDirectiveTriviaAlwaysToColumnZeroWithCode()
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

            AutoFormatToken(code, expected);
        }

        [WorkItem(981821)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDirectiveTriviaAlwaysToColumnZeroWithBrokenElseDirective()
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

            AutoFormatToken(code, expected);
        }

        internal static void AutoFormatToken(string markup, string expected)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(new string[] { markup }))
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

        private void AutoFormatOnColon(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        {
            AutoFormatOnMarker(codeWithMarker, expected, SyntaxKind.ColonToken, startTokenKind);
        }

        private void AutoFormatOnSemicolon(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        {
            AutoFormatOnMarker(codeWithMarker, expected, SyntaxKind.SemicolonToken, startTokenKind);
        }

        private void AutoFormatOnCloseBrace(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        {
            AutoFormatOnMarker(codeWithMarker, expected, SyntaxKind.CloseBraceToken, startTokenKind);
        }

        private void AutoFormatOnMarker(string initialMarkup, string expected, SyntaxKind tokenKind, SyntaxKind startTokenKind)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(initialMarkup))
            {
                var tuple = GetService(workspace);
                var testDocument = workspace.Documents.Single();
                var buffer = testDocument.GetTextBuffer();
                var position = testDocument.CursorPosition.Value;

                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

                var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;
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
