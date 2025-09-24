// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Formatting)]
[Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
public sealed class SmartTokenFormatterFormatRangeTests
{
    [Fact]
    public async Task BeginningOfFile()
    {
        Assert.NotNull(await Record.ExceptionAsync(() => AutoFormatOnSemicolonAsync(@"        using System;$$", @"        using System;", SyntaxKind.None)));
    }

    [WpfFact]
    public Task Namespace1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            namespace NS
            {

                }$$
            """, """
            using System;
            namespace NS
            {

            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Namespace2()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            namespace NS
            {
                    class Class
                            {
                    }
                }$$
            """, """
            using System;
            namespace NS
            {
                class Class
                {
                }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Namespace3()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            namespace NS { }$$
            """, """
            using System;
            namespace NS { }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Namespace4()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            namespace NS { 
            }$$
            """, """
            using System;
            namespace NS
            {
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Namespace5()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            namespace NS
            {
                class Class { } 
            }$$
            """, """
            using System;
            namespace NS
            {
                class Class { }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Namespace6()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            namespace NS
            {
                class Class { 
            } 
            }$$
            """, """
            using System;
            namespace NS
            {
                class Class
                {
                }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Namespace7()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            namespace NS
            {
                class Class { 
            } 
                        namespace NS2
            {}
            }$$
            """, """
            using System;
            namespace NS
            {
                class Class
                {
                }
                namespace NS2
                { }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Namespace8()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            namespace NS { class Class { } namespace NS2 { } }$$
            """, """
            using System;
            namespace NS { class Class { } namespace NS2 { } }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Class1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
                class Class { 
            }$$
            """, """
            using System;
            class Class
            {
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Class2()
        => AutoFormatOnCloseBraceAsync("""
            using System;
                class Class
            {
                void Method(int i) {
                            }
            }$$
            """, """
            using System;
            class Class
            {
                void Method(int i)
                {
                }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Class3()
        => AutoFormatOnCloseBraceAsync("""
            using System;
                class Class
            {
                void Method(int i) { }
            }$$
            """, """
            using System;
            class Class
            {
                void Method(int i) { }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Class4()
        => AutoFormatOnCloseBraceAsync("""
            using System;
                class Class
            {
                        delegate void Test(int i);
            }$$
            """, """
            using System;
            class Class
            {
                delegate void Test(int i);
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Class5()
        => AutoFormatOnCloseBraceAsync("""
            using System;
                class Class
            {
                        delegate void Test(int i);
                void Method()
                    {
                            }
            }$$
            """, """
            using System;
            class Class
            {
                delegate void Test(int i);
                void Method()
                {
                }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Interface1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
                interface II
            {
                        delegate void Test(int i);
            int Prop { get; set; }
            }$$
            """, """
            using System;
            interface II
            {
                delegate void Test(int i);
                int Prop { get; set; }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Struct1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
                struct Struct
            {
                        Struct(int i)
                {
                            }
            }$$
            """, """
            using System;
            struct Struct
            {
                Struct(int i)
                {
                }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Enum1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
                enum Enum
            {
                            A = 1, B = 2,
                C = 3
                        }$$
            """, """
            using System;
            enum Enum
            {
                A = 1, B = 2,
                C = 3
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task AccessorList1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                int Prop { get { return 1; }$$
            """, """
            using System;
            class Class
            {
                int Prop { get { return 1; }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task AccessorList2()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                int Prop { get { return 1; } }$$
            """, """
            using System;
            class Class
            {
                int Prop { get { return 1; } }
            """, SyntaxKind.IntKeyword);

    [WpfFact]
    public Task AccessorList3()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                int Prop { get { return 1; }  
            }$$
            """, """
            using System;
            class Class
            {
                int Prop
                {
                    get { return 1; }
                }
            """, SyntaxKind.IntKeyword);

    [WpfFact]
    public Task AccessorList4()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                int Prop { get { return 1;   
            }$$
            """, """
            using System;
            class Class
            {
                int Prop { get
                    {
                        return 1;
                    }
            """, SyntaxKind.GetKeyword);

    [WpfFact]
    public Task AccessorList5()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                int Prop {
                    get { return 1;   
            }$$
            """, """
            using System;
            class Class
            {
                int Prop {
                    get { return 1;
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16984")]
    public Task AccessorList5b()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                int Prop {
                    get { return 1;   
            }$$
            }
            }
            """, """
            using System;
            class Class
            {
                int Prop {
                    get
                    {
                        return 1;
                    }
            }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task AccessorList6()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                int Prop 
                    { 
            get { return 1;   
            } }$$
            """, """
            using System;
            class Class
            {
                int Prop
                {
                    get
                    {
                        return 1;
                    }
                }
            """, SyntaxKind.IntKeyword);

    [WpfFact]
    public Task AccessorList7()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                int Prop
                {
                    get
                    {
            return 1;$$
                    }
                }
            """, """
            using System;
            class Class
            {
                int Prop
                {
                    get
                    {
                        return 1;
                    }
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16984")]
    public Task AccessorList8()
        => AutoFormatOnCloseBraceAsync("""
            class C
            {
                int Prop
                {
            get
                    {
                        return 0;
                    }$$
                }
            }
            """, """
            class C
            {
                int Prop
                {
                    get
                    {
                        return 0;
                    }
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/16984")]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("init")]
    public Task AccessorList9(string accessor)
        => AutoFormatOnCloseBraceAsync($$"""
            class C
            {
                int Prop
                {
            {{accessor}}
                    {
                        ;
                    }$$
                }
            }
            """, $$"""
            class C
            {
                int Prop
                {
                    {{accessor}}
                    {
                        ;
                    }
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16984")]
    public Task AccessorList10()
        => AutoFormatOnCloseBraceAsync("""
            class C
            {
                event EventHandler E
                {
            add
                    {
                    }$$
                    remove
                    {
                    }
                }

            }
            """, """
            class C
            {
                event EventHandler E
                {
                    add
                    {
                    }
                    remove
                    {
                    }
                }

            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16984")]
    public Task AccessorList11()
        => AutoFormatOnCloseBraceAsync("""
            class C
            {
                event EventHandler E
                {
                    add
                    {
                    }
            remove
                    {
                    }$$
                }

            }
            """, """
            class C
            {
                event EventHandler E
                {
                    add
                    {
                    }
                    remove
                    {
                    }
                }

            }
            """, SyntaxKind.CloseBraceToken);

    [WpfFact]
    public Task Block1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                public int Method()
                { }$$
            """, """
            using System;
            class Class
            {
                public int Method()
                { }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Block2()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                public int Method() { }$$
            """, """
            using System;
            class Class
            {
                public int Method() { }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Block3()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                public int Method() { 
            }$$
            }
            """, """
            using System;
            class Class
            {
                public int Method()
                {
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Block4()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                public static Class operator +(Class c1, Class c2) {
                        }$$
            }
            """, """
            using System;
            class Class
            {
                public static Class operator +(Class c1, Class c2)
                {
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Block5()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    { }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    { }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Block6()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    { 
            }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    {
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Block7()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    { { }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    { { }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Block8()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    { { 
            }$$
                    }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    {
                        {
                        }
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task SwitchStatement1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    switch (a) {
                        case 1:
                            break;
            }$$
                }
            }
            """, """
            using System;
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
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task SwitchStatement2()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    switch (true) { }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    switch (true) { }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task SwitchStatement3()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    switch (true)
                    {
                        case 1: { }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    switch (true)
                    {
                        case 1: { }
            """, SyntaxKind.ColonToken);

    [WpfFact]
    public Task SwitchStatement4()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    switch (true)
                    {
                        case 1: { 
            }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    switch (true)
                    {
                        case 1:
                            {
                            }
            """, SyntaxKind.ColonToken);

    [WpfFact]
    public Task Initializer1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new int[] { }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new int[] { }
            """, SyntaxKind.NewKeyword);

    [WpfFact]
    public Task Initializer2()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new int[] { 
            }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new int[] {
            }
            """, SyntaxKind.NewKeyword);

    [WpfFact]
    public Task Initializer3()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new { A = 1, B = 2
            }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new
                    {
                        A = 1,
                        B = 2
                    }
            """, SyntaxKind.NewKeyword);

    [WpfFact]
    public Task Initializer4()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new { A = 1, B = 2 }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new { A = 1, B = 2 }
            """, SyntaxKind.NewKeyword);

    [WpfFact]
    public Task Initializer5()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new[] { 
                        1, 2, 3, 4,
                        5 }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new[] {
                        1, 2, 3, 4,
                        5 }
            """, SyntaxKind.NewKeyword);

    [WpfFact]
    public Task Initializer6()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new int[] { 
                        1, 2, 3, 4,
                        5 }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    var arr = new int[] {
                        1, 2, 3, 4,
                        5 }
            """, SyntaxKind.NewKeyword);

    [WpfFact]
    public Task EmbeddedStatement1()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    if (true) { }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    if (true) { }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement2()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    if (true) { 
                    }$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    if (true)
                    {
                    }
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement3()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    if (true)
                    { }$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    if (true)
                    { }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement4()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    while (true) {
            }$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    while (true)
                    {
                    }
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/8413")]
    public Task EmbeddedStatementDoBlockAlone()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    do {
            }$$
                }
            }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    do
                    {
                    }
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement5()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    do {
            } while(true);$$
                }
            }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    do
                    {
                    } while (true);
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement6()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    for (int i = 0; i < 10; i++)             {
            }$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    for (int i = 0; i < 10; i++)
                    {
                    }
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement7()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    foreach (var i in collection)            {
            }$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    foreach (var i in collection)
                    {
                    }
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement8()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    using (var resource = GetResource())           {
            }$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    using (var resource = GetResource())
                    {
                    }
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement9()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    if (true)
                            int i = 10;$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    if (true)
                        int i = 10;
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task FieldlInitializer()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                      string str =              Console.Title;$$

            """, """
            using System;
            class Class
            {
                string str = Console.Title;

            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task ArrayFieldlInitializer()
        => AutoFormatOnSemicolonAsync("""
            using System;
            namespace NS
            {
                class Class
                {
                                string[] strArr = {           "1",                       "2" };$$

            """, """
            using System;
            namespace NS
            {
                class Class
                {
                    string[] strArr = { "1", "2" };

            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task ExpressionValuedPropertyInitializer()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                      public int  Three =>   1+2;$$

            """, """
            using System;
            class Class
            {
                public int Three => 1 + 2;

            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement10()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                    if (true)
                            int i = 10;$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    if (true)
                        int i = 10;
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement11()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                            using (var resource = GetResource()) resource.Do();$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    using (var resource = GetResource()) resource.Do();
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement12()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                            using (var resource = GetResource()) 
                resource.Do();$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    using (var resource = GetResource())
                        resource.Do();
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement13()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                            using (var resource = GetResource()) 
                resource.Do();$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    using (var resource = GetResource())
                        resource.Do();
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement14()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                            do i = 10;$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    do i = 10;
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement15()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                            do
                i = 10;$$
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    do
                        i = 10;
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement16()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                            do
                i = 10;$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    do
                        i = 10;
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task EmbeddedStatement17()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                void Method()
                {
                            do
                i = 10;
            while (true);$$
                }
            """, """
            using System;
            class Class
            {
                void Method()
                {
                    do
                        i = 10;
                    while (true);
                }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task FollowPreviousElement1()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                                int i = 10;
                                int i2 = 10;$$
            """, """
            using System;
            class Class
            {
                                int i = 10;
                int i2 = 10;
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task FollowPreviousElement2()
        => AutoFormatOnCloseBraceAsync("""
            using System;
            class Class
            {
                        void Method(int i)
                        {
                        }

                        void Method2()
                        {
                        }$$
            }
            """, """
            using System;
            class Class
            {
                        void Method(int i)
                        {
                        }

                void Method2()
                {
                }
            }
            """, SyntaxKind.CloseBraceToken);

    [WpfFact]
    public Task FollowPreviousElement3()
        => AutoFormatOnSemicolonAsync("""
            using System;
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
            }
            """, """
            using System;
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
            }
            """, SyntaxKind.CloseBraceToken);

    [WpfFact]
    public Task FollowPreviousElement4()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                        void Method(int i)
                        {
                                    int i = 10;
                         int i2 = 10;$$
            """, """
            using System;
            class Class
            {
                        void Method(int i)
                        {
                                    int i = 10;
                    int i2 = 10;
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task FollowPreviousElement5()
        => AutoFormatOnSemicolonAsync("""
            using System;
            class Class
            {
                        void Method(int i)
                        {
                                    int i = 10;
                            if (true)
            i = 50;$$
            """, """
            using System;
            class Class
            {
                        void Method(int i)
                        {
                                    int i = 10;
                    if (true)
                        i = 50;
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task FollowPreviousElement6()
        => AutoFormatOnSemicolonAsync("""
                    using System;
                    using System.Linq;$$
            """, """
                    using System;
            using System.Linq;
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task FollowPreviousElement7()
        => AutoFormatOnCloseBraceAsync("""
                        using System;

                        namespace NS
                        {
                        }

                    namespace NS2
                    {
                    }$$
            """, """
                        using System;

                        namespace NS
                        {
                        }

            namespace NS2
            {
            }
            """, SyntaxKind.CloseBraceToken);

    [WpfFact]
    public Task FollowPreviousElement8()
        => AutoFormatOnCloseBraceAsync("""
            using System;

            namespace NS
            {
                        class Class
                        {
                        }

                    class Class1
                    {
                    }$$
            }
            """, """
            using System;

            namespace NS
            {
                        class Class
                        {
                        }

                class Class1
                {
                }
            }
            """, SyntaxKind.CloseBraceToken);

    [WpfFact]
    public Task IfStatement1()
        => AutoFormatOnCloseBraceAsync("""
            using System;

            class Class
            {
                void Method()
                {
                        if (true)
                    {
                }$$
            """, """
            using System;

            class Class
            {
                void Method()
                {
                    if (true)
                    {
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task IfStatement2()
        => AutoFormatOnCloseBraceAsync("""
            using System;

            class Class
            {
                void Method()
                {
                        if (true)
                    {
                }
            else
                    {
                            }$$
            """, """
            using System;

            class Class
            {
                void Method()
                {
                    if (true)
                    {
                    }
                    else
                    {
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task IfStatement3()
        => AutoFormatOnCloseBraceAsync("""
            using System;

            class Class
            {
                void Method()
                {
                        if (true)
                    {
                }
            else    if (false)
                    {
                            }$$
            """, """
            using System;

            class Class
            {
                void Method()
                {
                    if (true)
                    {
                    }
                    else if (false)
                    {
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task IfStatement4()
        => AutoFormatOnSemicolonAsync("""
            using System;

            class Class
            {
                void Method()
                {
                        if (true)
                    return          ;
            else    if (false)
                                return          ;$$
            """, """
            using System;

            class Class
            {
                void Method()
                {
                    if (true)
                        return;
                    else if (false)
                        return;
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task TryStatement1()
        => AutoFormatOnCloseBraceAsync("""
            using System;

            class Class
            {
                void Method()
                {
                            try
                {
                    }$$
            """, """
            using System;

            class Class
            {
                void Method()
                {
                    try
                    {
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task TryStatement2()
        => AutoFormatOnCloseBraceAsync("""
            using System;

            class Class
            {
                void Method()
                {
                            try
                {
                    }
            catch    (  Exception       ex)
                            {
                }$$
            """, """
            using System;

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
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task TryStatement3()
        => AutoFormatOnCloseBraceAsync("""
            using System;

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
               }$$
            """, """
            using System;

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
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task TryStatement4()
        => AutoFormatOnCloseBraceAsync("""
            using System;

            class Class
            {
                void Method()
                {
                            try
                {
                    }
                                            finally
                                  {
               }$$
            """, """
            using System;

            class Class
            {
                void Method()
                {
                    try
                    {
                    }
                    finally
                    {
                    }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/6645")]
    public Task TryStatement5()
        => AutoFormatOnCloseBraceAsync("""
            using System;

            class Class
            {
                void Method()
                {
                    try {
                    }$$
                }
            }
            """, """
            using System;

            class Class
            {
                void Method()
                {
                    try
                    {
                    }
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537555")]
    public Task SingleLine()
        => AutoFormatOnSemicolonAsync(@"class C { void M() { C.M(    );$$ } }", @"class C { void M() { C.M(); } }", SyntaxKind.OpenBraceToken);

    [Fact]
    public async Task StringLiterals()
    {
        var expected = string.Empty;
        await AutoFormatOnMarkerAsync(@"class C { void M() { C.M(""Test {0}$$", expected, SyntaxKind.StringLiteralToken, SyntaxKind.None);
    }

    [Fact]
    public async Task CharLiterals()
    {
        var expected = string.Empty;
        await AutoFormatOnMarkerAsync(@"class C { void M() { C.M('}$$", expected, SyntaxKind.CharacterLiteralToken, SyntaxKind.None);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public async Task CharLiterals1()
    {
        var expected = string.Empty;
        await AutoFormatOnMarkerAsync(@"';$$", expected, SyntaxKind.CharacterLiteralToken, SyntaxKind.None);
    }

    [Fact]
    public async Task Comments()
    {
        var expected = string.Empty;
        await AutoFormatOnMarkerAsync(@"class C { void M() { // { }$$", expected, SyntaxKind.OpenBraceToken, SyntaxKind.OpenBraceToken);
    }

    [WpfFact]
    public Task FirstLineInFile()
        => AutoFormatOnSemicolonAsync(@"using System;$$", "using System;", SyntaxKind.UsingKeyword);

    [WpfFact]
    public Task Label1()
        => AutoFormatOnSemicolonAsync("""
            class C
            {
                void Method()
                {
                            L           :               int             i               =               20;$$
                }
            }
            """, """
            class C
            {
                void Method()
                {
                L: int i = 20;
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Label2()
        => AutoFormatOnSemicolonAsync("""
            class C
            {
                void Method()
                {
                            L           :               
            int             i               =               20;$$
                }
            }
            """, """
            class C
            {
                void Method()
                {
                L:
                    int i = 20;
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact]
    public Task Label3()
        => AutoFormatOnSemicolonAsync("""
            class C
            {
                void Method()
                {
                    int base = 10;
                            L           :               
            int             i               =               20;$$
                }
            }
            """, """
            class C
            {
                void Method()
                {
                    int base = 10;
                L:
                    int i = 20;
                }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Label4()
        => AutoFormatOnSemicolonAsync("""
            class C
            {
                void Method()
                {
                    int base = 10;
                L:
                    int i = 20;
            int         nextLine            =           30          ;$$
                }
            }
            """, """
            class C
            {
                void Method()
                {
                    int base = 10;
                L:
                    int i = 20;
                    int nextLine = 30;
                }
            }
            """, SyntaxKind.SemicolonToken);

    [WpfFact]
    public Task Label6()
        => AutoFormatOnSemicolonAsync("""
            class C
            {
                void Method()
                {
                L:
                    int i = 20;
            int         nextLine            =           30          ;$$
                }
            }
            """, """
            class C
            {
                void Method()
                {
                L:
                    int i = 20;
                    int nextLine = 30;
                }
            }
            """, SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537776")]
    public Task DisappearedTokens()
        => AutoFormatOnCloseBraceAsync(
            """
            class Class1
            {
                int goo()
                    return 0;
                    }$$
            }
            """,
            """
            class Class1
            {
                int goo()
                    return 0;
                    }
            }
            """,
            SyntaxKind.ClassKeyword);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537779")]
    public Task DisappearedTokens2()
        => AutoFormatOnSemicolonAsync(
            """
            class Class1
            {
                void Goo()
                {
                    Object o=new Object);$$
                }
            }
            """,
            """
            class Class1
            {
                void Goo()
                {
                    Object o = new Object);
                }
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537793")]
    public Task Delegate1()
        => AutoFormatOnSemicolonAsync(
            @"delegate void MyDelegate(int a,int b);$$",
            @"delegate void MyDelegate(int a, int b);",
            SyntaxKind.DelegateKeyword);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537827")]
    public Task DoubleInitializer()
        => AutoFormatOnCloseBraceAsync(
            """
            class C
            {
                void Method()
                {
                    int[,] a ={{ 1 , 1 }$$
                }
            }
            """,
            """
            class C
            {
                void Method()
                {
                    int[,] a ={{ 1 , 1 }
                }
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537825")]
    public Task MissingToken1()
        => AutoFormatOnCloseBraceAsync(
            """
            public class Class1
            {
                int a = 1}$$;
            }
            """,
            """
            public class Class1
            {
                int a = 1};
            }
            """,
            SyntaxKind.PublicKeyword);

    [WpfFact]
    public Task ArrayInitializer1()
        => AutoFormatOnCloseBraceAsync(
            """
            public class Class1
            {
                var a = new [] 
                {
                    1, 2, 3, 4
                    }$$
            }
            """,
            """
            public class Class1
            {
                var a = new[]
                {
                    1, 2, 3, 4
                    }
            }
            """,
            SyntaxKind.NewKeyword);

    [WpfFact]
    public Task ArrayInitializer2()
        => AutoFormatOnSemicolonAsync(
            """
            public class Class1
            {
                var a = new [] 
                {
                    1, 2, 3, 4
                    }   ;$$
            }
            """,
            """
            public class Class1
            {
                var a = new[]
                {
                    1, 2, 3, 4
                    };
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537825")]
    public Task MalformedCode()
        => AutoFormatOnCloseBraceAsync(
            """
            namespace ClassLibrary1
            {
                public class Class1
                {
                    int a}$$;
                }
            }
            """,
            """
            namespace ClassLibrary1
            {
                public class Class1
                {
                    int a};
                }
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537804")]
    public Task Colon_SwitchLabel()
        => AutoFormatOnColonAsync(
            """
            namespace ClassLibrary1
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
            }
            """,
            """
            namespace ClassLibrary1
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
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599")]
    public Task Colon_SwitchLabel_Comment()
        => AutoFormatOnColonAsync(
            """
            namespace ClassLibrary1
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
            }
            """,
            """
            namespace ClassLibrary1
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
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/584599")]
    public Task Colon_SwitchLabel_Comment2()
        => AutoFormatOnColonAsync(
            """
            namespace ClassLibrary1
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
            }
            """,
            """
            namespace ClassLibrary1
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
            }
            """,
            SyntaxKind.ColonToken);

    [Fact]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537804")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13981")]
    public Task Colon_Label()
        => AutoFormatOnColonAsync(
            """
            namespace ClassLibrary1
            {
                public class Class1
                {
                    void Test()
                    {
                                label   :$$
                    }
                }
            }
            """,
            """
            namespace ClassLibrary1
            {
                public class Class1
                {
                    void Test()
                    {
                    label:
                    }
                }
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538793")]
    public Task Colon_Label2()
        => AutoFormatOnSemicolonAsync(
            """
            namespace ClassLibrary1
            {
                public class Class1
                {
                    void Test()
                    {
                                label   :   Console.WriteLine(10) ;$$
                    }
                }
            }
            """,
            """
            namespace ClassLibrary1
            {
                public class Class1
                {
                    void Test()
                    {
                    label: Console.WriteLine(10);
                    }
                }
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem(3186, "DevDiv_Projects/Roslyn")]
    public Task SemicolonInElseIfStatement()
        => AutoFormatOnSemicolonAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """,
            SyntaxKind.SemicolonToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538391")]
    public Task SemicolonInElseIfStatement2()
        => AutoFormatOnSemicolonAsync(
            """
            public class Class1
            {
                void Method()
                {
                    int a = 1;
                    if (a == 0)
                        a = 8;$$
                                else
                                    a = 10;
                }
            }
            """,
            """
            public class Class1
            {
                void Method()
                {
                    int a = 1;
                    if (a == 0)
                        a = 8;
                    else
                        a = 10;
                }
            }
            """,
            SyntaxKind.SemicolonToken);

    [WpfFact, WorkItem(8385, "DevDiv_Projects/Roslyn")]
    public Task NullCoalescingOperator()
        => AutoFormatOnSemicolonAsync(
            """
            class C
            {
                void M()
                {
                    object o2 = null??null;$$
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    object o2 = null ?? null;
                }
            }
            """,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541517")]
    public Task SwitchDefault()
        => AutoFormatOnColonAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """,
            SyntaxKind.SemicolonToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542538")]
    public Task MissingTokens1()
        => AutoFormatOnMarkerAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    gl::$$
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    gl::
                }
            }
            """,
            SyntaxKind.ColonColonToken,
            SyntaxKind.OpenBraceToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542538")]
    public Task MissingTokens2()
        => AutoFormatOnCloseBraceAsync(
            @"class C { void M() { M(() => { }$$ } }",
            @"class C { void M() { M(() => { } } }",
            SyntaxKind.EqualsGreaterThanToken);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542953")]
    public Task UsingAlias()
        => AutoFormatOnSemicolonAsync(
            @"using Alias=System;$$",
            @"using Alias = System;",
            SyntaxKind.UsingKeyword);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542953")]
    public Task NoLineChangeWithSyntaxError()
        => AutoFormatOnSemicolonAsync(
            """
            struct Goo { public int member; }
            class Program{
                void Main()
                {
                    var f = new Goo { member;$$ }
                }
            }
            """,
            """
            struct Goo { public int member; }
            class Program{
                void Main()
                {
                    var f = new Goo { member; }
                }
            }
            """,
            SyntaxKind.None);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/620568")]
    public void SkippedTokens1(bool useTabs)
        => AutoFormatToken(@";$$*", @";*", useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530830")]
    public void AutoPropertyAccessor(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                int Prop {          get             ;$$
            }
            """, """
            class C
            {
                int Prop {          get;
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530830")]
    public void AutoPropertyAccessor2(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                int Prop {          get;                set             ;$$
            }
            """, """
            class C
            {
                int Prop {          get;                set;
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530830")]
    public void AutoPropertyAccessor3(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                int Prop {          get;                set             ;           }$$
            }
            """, """
            class C
            {
                int Prop { get; set; }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784674")]
    public void AutoPropertyAccessor4(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                int Prop {          get;$$             }
            }
            """, """
            class C
            {
                int Prop { get; }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924469")]
    public void AutoPropertyAccessor5(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                int Prop {          get;                set             ;$$           }
            }
            """, """
            class C
            {
                int Prop { get; set; }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924469")]
    public void AutoPropertyAccessor6(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                int Prop { get;set;$$}
            }
            """, """
            class C
            {
                int Prop { get; set; }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924469")]
    public void AutoPropertyAccessor7(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                int Prop     { get;set;$$}    
            }
            """, """
            class C
            {
                int Prop     { get; set; }    
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
    public void NestedUsingStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    using (null)
                        using(null)$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    using (null)
                    using (null)
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
    public void NestedNotUsingStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    using (null)
                        for(;;)$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    using (null)
                        for(;;)
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    public void UsingStatementWithNestedFixedStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    using (null)
                    fixed (void* ptr = &i)
                    {
                    }$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    using (null)
                        fixed (void* ptr = &i)
                        {
                        }
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    public void UsingStatementWithNestedCheckedStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    using (null)
                    checked
                    {
                    }$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    using (null)
                        checked
                        {
                        }
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    public void UsingStatementWithNestedUncheckedStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    using (null)
                    unchecked
                    {
                    }$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    using (null)
                        unchecked
                        {
                        }
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    public void FixedStatementWithNestedUsingStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    fixed (void* ptr = &i)
                    using (null)$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    fixed (void* ptr = &i)
                        using (null)
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    public void FixedStatementWithNestedFixedStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    fixed (void* ptr1 = &i)
                        fixed (void* ptr2 = &i)
                        {
                        }$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    fixed (void* ptr1 = &i)
                    fixed (void* ptr2 = &i)
                    {
                    }
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    public void FixedStatementWithNestedNotFixedStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    fixed (void* ptr = &i)
                    if (false)
                    {
                    }$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    fixed (void* ptr = &i)
                        if (false)
                        {
                        }
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    public void NotFixedStatementWithNestedFixedStatement(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {
                    if (false)
                    fixed (void* ptr = &i)
                    {
                    }$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    if (false)
                        fixed (void* ptr = &i)
                        {
                        }
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954386")]
    public void FormattingRangeForFirstStatementOfBlock(bool useTabs)
        => AutoFormatToken("""
            class C
            {
                public void M()
                {int s;$$
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    int s;
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954386")]
    public void FormattingRangeForFirstMemberofType(bool useTabs)
        => AutoFormatToken("""
            class C
            {int s;$$
                public void M()
                {
                }
            }
            """, """
            class C
            {
                int s;
                public void M()
                {
                }
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954386")]
    public void FormattingRangeForFirstMethodMemberofType(bool useTabs)
        => AutoFormatToken("""
            interface C
            {void s();$$
            }
            """, """
            interface C
            {
                void s();
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/17257")]
    public void FormattingRangeForConstructor(bool useTabs)
        => AutoFormatToken("""
            class C
            {public C()=>f=1;$$
            }
            """, """
            class C
            {
                public C() => f = 1;
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/17257")]
    public void FormattingRangeForDestructor(bool useTabs)
        => AutoFormatToken("""
            class C
            {~C()=>f=1;$$
            }
            """, """
            class C
            {
                ~C() => f = 1;
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/17257")]
    public void FormattingRangeForOperator(bool useTabs)
        => AutoFormatToken("""
            class C
            {public static C operator +(C left, C right)=>field=1;$$
                static int field;
            }
            """, """
            class C
            {
                public static C operator +(C left, C right) => field = 1;
                static int field;
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954386")]
    public void FormattingRangeForFirstMemberOfNamespace(bool useTabs)
        => AutoFormatToken("""
            namespace C
            {delegate void s();$$
            }
            """, """
            namespace C
            {
                delegate void s();
            }
            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981821")]
    public void FormatDirectiveTriviaAlwaysToColumnZero(bool useTabs)
        => AutoFormatToken("""
            class Program
            {
                static void Main(string[] args)
                {
            #if
                    #$$
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
            #if
            #
                }
            }

            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981821")]
    public void FormatDirectiveTriviaAlwaysToColumnZeroWithCode(bool useTabs)
        => AutoFormatToken("""
            class Program
            {
                static void Main(string[] args)
                {
            #if
                    int s = 10;
                    #$$
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
            #if
                    int s = 10;
            #
                }
            }

            """, useTabs);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981821")]
    public void FormatDirectiveTriviaAlwaysToColumnZeroWithBrokenElseDirective(bool useTabs)
        => AutoFormatToken("""
            class Program
            {
                static void Main(string[] args)
                {
            #else
                    #$$
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
            #else
            #
                }
            }

            """, useTabs);

    internal static void AutoFormatToken(string markup, string expected, bool useTabs)
    {
        if (useTabs)
        {
            markup = markup.Replace("    ", "\t");
            expected = expected.Replace("    ", "\t");
        }

        using var workspace = EditorTestWorkspace.CreateCSharp(markup);

        var subjectDocument = workspace.Documents.Single();
        var textBuffer = subjectDocument.GetTextBuffer();
        var optionsService = workspace.GetService<EditorOptionsService>();
        var editorOptions = optionsService.Factory.GetOptions(textBuffer);
        editorOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !useTabs);

        var commandHandler = workspace.GetService<FormatCommandHandler>();
        var typedChar = textBuffer.CurrentSnapshot.GetText(subjectDocument.CursorPosition.Value - 1, 1);
        commandHandler.ExecuteCommand(new TypeCharCommandArgs(subjectDocument.GetTextView(), textBuffer, typedChar[0]), () => { }, TestCommandExecutionContext.Create());

        var newSnapshot = textBuffer.CurrentSnapshot;

        Assert.Equal(expected, newSnapshot.GetText());
    }

    private static Task AutoFormatOnColonAsync(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        => AutoFormatOnMarkerAsync(codeWithMarker, expected, SyntaxKind.ColonToken, startTokenKind);

    private static Task AutoFormatOnSemicolonAsync(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        => AutoFormatOnMarkerAsync(codeWithMarker, expected, SyntaxKind.SemicolonToken, startTokenKind);

    private static Task AutoFormatOnCloseBraceAsync(string codeWithMarker, string expected, SyntaxKind startTokenKind)
        => AutoFormatOnMarkerAsync(codeWithMarker, expected, SyntaxKind.CloseBraceToken, startTokenKind);

    private static async Task AutoFormatOnMarkerAsync(string initialMarkup, string expected, SyntaxKind tokenKind, SyntaxKind startTokenKind)
    {
        await AutoFormatOnMarkerAsync(initialMarkup, expected, useTabs: false, tokenKind, startTokenKind).ConfigureAwait(false);
        await AutoFormatOnMarkerAsync(initialMarkup.Replace("    ", "\t"), expected.Replace("    ", "\t"), useTabs: true, tokenKind, startTokenKind).ConfigureAwait(false);
    }

    private static async Task AutoFormatOnMarkerAsync(string initialMarkup, string expected, bool useTabs, SyntaxKind tokenKind, SyntaxKind startTokenKind)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(initialMarkup);

        var testDocument = workspace.Documents.Single();
        var buffer = testDocument.GetTextBuffer();
        var position = testDocument.CursorPosition.Value;

        var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
        var documentSyntax = await ParsedDocument.CreateAsync(document, CancellationToken.None);
        var rules = Formatter.GetDefaultFormattingRules(document);

        var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync();
        var endToken = root.FindToken(position);
        if (position == endToken.SpanStart && !endToken.GetPreviousToken().IsKind(SyntaxKind.None))
        {
            endToken = endToken.GetPreviousToken();
        }

        Assert.Equal(tokenKind, endToken.Kind());

        var options = new IndentationOptions(
            new CSharpSyntaxFormattingOptions() { LineFormatting = new() { UseTabs = useTabs } });

        var formatter = new CSharpSmartTokenFormatter(options, rules, (CompilationUnitSyntax)documentSyntax.Root, documentSyntax.Text);

        var tokenRange = FormattingRangeHelper.FindAppropriateRange(endToken);
        if (tokenRange == null)
        {
            Assert.Equal(SyntaxKind.None, startTokenKind);
            return;
        }

        Assert.Equal(startTokenKind, tokenRange.Value.startToken.Kind());
        if (tokenRange.Value.startToken.Equals(tokenRange.Value.endToken))
            return;

        var changes = formatter.FormatRange(tokenRange.Value.startToken, tokenRange.Value.endToken, CancellationToken.None);
        var actual = GetFormattedText(buffer, changes);
        Assert.Equal(expected, actual);
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
