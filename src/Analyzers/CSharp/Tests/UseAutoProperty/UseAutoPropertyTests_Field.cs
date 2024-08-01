﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseAutoProperty;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
public sealed partial class UseAutoPropertyTests
{
    private readonly ParseOptions CSharp13 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13);
    private readonly ParseOptions Preview = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    [Fact]
    public async Task TestFieldSimplestCase()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        return s.Trim();
                    }
                }
            }
            """,
            """
            class Class
            {
                string P
                {
                    get
                    {
                        return field.Trim();
                    }
                }
            }
            """, parseOptions: CSharp13);
    }

    [Fact]
    public async Task TestFieldAccessOffOfThis()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        return this.s.Trim();
                    }
                }
            }
            """,
            """
            class Class
            {
                string P
                {
                    get
                    {
                        return field.Trim();
                    }
                }
            }
            """, parseOptions: CSharp13);
    }

    [Fact]
    public async Task TestGetterWithMultipleStatements_Field()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        ;
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P
                {
                    get
                    {
                        ;
                        return field;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement_Field()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }

                    set
                    {
                        ;
                        i = value;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P
                {
                    get;

                    set
                    {
                        ;
                        field = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimpleFieldInExpressionBody()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|string s|];

                string P => s.Trim();
            }
            """,
            """
            class Class
            {
                string P => field.Trim();
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFields_NoClearChoice()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                int [|x|], y;

                int Total => x + y;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFields_NoClearChoice2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                int [|x|], y;

                int Total
                {
                    get => x + y;
                    set
                    {
                        x = value;
                        y = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFields_ClearChoice()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                int [|x|], y;

                int Total
                {
                    get => x + y;
                    set
                    {
                        x = value;
                    }
                }
            }
            """,
            """
            class Class
            {
                int y;

                int Total
                {
                    get => field + y;
                    set;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWhenAlreadyUsingField()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        var v = field.Trim();
                        return s.Trim();
                    }
                }
            }
            """, new TestParameters(parseOptions: Preview));
    }

    [Fact]
    public async Task TestNotWhenUsingNameof1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        if (s is null)
                            throw new ArgumentNullException(nameof(s));
                        return s.Trim();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWhenUsingNameof2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        if (s is null)
                            throw new ArgumentNullException(nameof(this.s));
                        return s.Trim();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWhenUsingNameof3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        return s.Trim();
                    }
                }

                void M()
                {
                    if (s is null)
                        throw new ArgumentNullException(nameof(s));
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWhenUsingNameof4()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        return s.Trim();
                    }
                }

                void M()
                {
                    if (s is null)
                        throw new ArgumentNullException(nameof(this.s));
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithRefArgumentUseInside()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P => Init(ref s);

                void Init(ref string s)
                {
                }
            }
            """,
            """
            class Class
            {
                string P => Init(ref field);
            
                void Init(ref string s)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWithRefArgumentUseOutside()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P => s.Trim();

                void M()
                {
                    Init(ref s);
                }

                void Init(ref string s)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithRefUseInside()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        ref string s1 = ref s;
                        return s.Trim();
                    }
                }
            }
            """,
            """
            class Class
            {
                string P
                {
                    get
                    {
                        ref string s1 = ref field;
                        return field.Trim();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWithRefUseOutside()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        return s.Trim();
                    }
                }

                void M()
                {
                    ref string s1 = ref s;
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithAddressOfInside()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|int s|];

                int P
                {
                    get
                    {
                        unsafe
                        {
                            int* p = &s;
                            return s;
                        }
                    }
                }
            }
            """,
            """
            class Class
            {
                int P
                {
                    get
                    {
                        unsafe
                        {
                            int* p = &field;
                            return field;
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWithAddressOfOutside()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int s|];

                int P
                {
                    get
                    {
                        unsafe
                        {
                            return s;
                        }
                    }
                }
            
                unsafe void M()
                {
                    int* p = &s;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotChainedPattern1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Builder
            {
                [|private bool _strictMode;|]
                private Builder _builder;

                public bool StrictMode
                {
                    get { return _strictMode ?? _builder.StrictMode; }
                    set { this._strictMode = value; }
                }
            }
            """,
            """
            class Builder
            {
                private Builder _builder;

                public bool StrictMode
                {
                    get { return field ?? _builder.StrictMode; }
                    set;
                }
            }
            """);
    }

    [Fact]
    public async Task TestLazyInit1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Builder
            {
                [|private List<int>? _list|]

                public List<int> List => _list ??= new();
            }
            """,
            """
            using System.Collections.Generic;

            class Builder
            {
                public List<int> List => field ??= new();
            }
            """);
    }

    [Fact]
    public async Task TestRefSetAccessor1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Builder
            {
                [|private int prop;|]
                public int Prop { get => prop; set => Set(ref prop, value); }

                void Set(ref int a, int b) { }
            }
            """,
            """
            class Builder
            {
                public int Prop { get; set => Set(ref field, value); }
            
                void Set(ref int a, int b) { }
            }
            """);
    }

    [Fact]
    public async Task TestRefSetAccessor2()
    {
        await TestInRegularAndScriptAsync(
            """
            class Builder
            {
                [|private int prop;|]

                public int Prop
                {
                    get => prop;
                    set 
                    {
                        if (!Set(ref prop, value)) return;
                        OnPropChanged();
                    }
                }
            
                void Set(ref int a, int b) { }
                void OnPropChanged() { }
            }
            """,
            """
            class Builder
            {
                public int Prop
                {
                    get;
                    set 
                    {
                        if (!Set(ref field, value)) return;
                        OnPropChanged();
                    }
                }
            
                void Set(ref int a, int b) { }
                void OnPropChanged() { }
            }
            """);
    }

    [Fact]
    public async Task TestAttributesOnField()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [Something]
                [|private int prop;|]
                public int Prop { get => prop; set => prop = value; }
            }
            """,
            """
            class C
            {
                [field: Something]
                public int Prop { get => prop; set => prop = value; }
            }
            """);
    }

    [Fact]
    public async Task TestAttributesOnField2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [Something]
                [|private string prop;|]
                public int Prop => prop.Trim();
            }
            """,
            """
            class C
            {
                [field: Something]
                public int Prop => field.Trim();
            }
            """);
    }
}
