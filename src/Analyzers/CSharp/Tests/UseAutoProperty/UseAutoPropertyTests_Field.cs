﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseAutoProperty;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
public sealed partial class UseAutoPropertyTests
{
    private readonly ParseOptions CSharp13 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13);
    private readonly ParseOptions Preview = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    [Fact]
    public async Task TestNotInCSharp13()
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
            }
            """, new(parseOptions: CSharp13));
    }

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
            """, parseOptions: Preview);
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
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestStaticField()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|static string s|];

                static string P
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
                static string P
                {
                    get
                    {
                        return field.Trim();
                    }
                }
            }
            """, parseOptions: Preview);
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
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement_Field()
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
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement_Field2()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get => i;

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
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestSimpleFieldInExpressionBody()
    {
        await TestInRegularAndScriptAsync(
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
            """, parseOptions: Preview);
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
            """, new(parseOptions: Preview));
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
            """, new(parseOptions: Preview));
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
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestMultipleFields_PickByName1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                int [|x|], y;

                int X => x + y;
            }
            """,
            """
            class Class
            {
                int y;

                int X => field + y;
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestMultipleFields_PickByName2()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                int [|_x|], y;

                int X => _x + y;
            }
            """,
            """
            class Class
            {
                int y;

                int X => field + y;
            }
            """, parseOptions: Preview);
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
            """, new(parseOptions: Preview));
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
            """, new(parseOptions: Preview));
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
            """, new(parseOptions: Preview));
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
            """, new(parseOptions: Preview));
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
            """, new(parseOptions: Preview));
    }

    [Fact]
    public async Task TestNotWhenUsingNameof5()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s = nameof(s)|];

                string P => s;
            }
            """, new(parseOptions: CSharp13));
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
            """, parseOptions: Preview);
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
            """, new(parseOptions: Preview));
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
            """, parseOptions: Preview);
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
            """, new(parseOptions: Preview));
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
            """, parseOptions: Preview);
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
            """, new(parseOptions: Preview));
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
            """, parseOptions: Preview);
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
            """, parseOptions: Preview);
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
            """, parseOptions: Preview);
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
            """, parseOptions: Preview);
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
                public int Prop { get; set; }
            }
            """, parseOptions: Preview);
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
                public string Prop => prop.Trim();
            }
            """,
            """
            class C
            {
                [field: Something]
                public string Prop => field.Trim();
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestAttributesOnField3()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [Something]
                [|private string prop;|]

                [PropAttribute]
                public string Prop => prop.Trim();
            }
            """,
            """
            class C
            {
                [field: Something]
                [PropAttribute]
                public string Prop => field.Trim();
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestAttributesOnField4()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [Something]
                [|private string prop;|]

                /// Docs
                [PropAttribute]
                public string Prop => prop.Trim();
            }
            """,
            """
            class C
            {
                /// Docs
                [field: Something]
                [PropAttribute]
                public string Prop => field.Trim();
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestAttributesOnField5()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [Something]
                [|private string prop;|]

                /// Docs
                [PropAttribute][PropAttribute2]
                public string Prop => prop.Trim();
            }
            """,
            """
            class C
            {
                /// Docs
                [field: Something]
                [PropAttribute][PropAttribute2]
                public string Prop => field.Trim();
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestAttributesOnField6()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [Something]
                [|private string prop;|]

                /// Docs
                public string Prop => prop.Trim();
            }
            """,
            """
            class C
            {
                /// Docs
                [field: Something]
                public string Prop => field.Trim();
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestAttributesOnField7()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// FieldDocs
                [Something]
                [|private string prop;|]

                /// Docs
                public string Prop => prop.Trim();
            }
            """,
            """
            class C
            {
                /// Docs
                [field: Something]
                public string Prop => field.Trim();
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestFieldUsedInObjectInitializer()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|private string prop;|]

                public string Prop
                {
                    get
                    {
                        var v = new C { prop = "" };
                        return prop.Trim();
                    }
                }
            }
            """,
            """
            class C
            {
                public string Prop
                {
                    get
                    {
                        var v = new C { Prop = "" };
                        return field.Trim();
                    }
                }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestSimpleFieldInExpressionBody_FieldWrittenElsewhere1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                public string P => s.Trim();

                void M()
                {
                    s = "";
                }
            }
            """,
            """
            class Class
            {
                public string P { get => field.Trim(); private set; }
            
                void M()
                {
                    P = "";
                }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestSimpleFieldInExpressionBody_FieldWrittenElsewhere2()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                public string P => s ??= "";

                void M()
                {
                    s = "";
                }
            }
            """,
            """
            class Class
            {
                public string P { get => field ??= ""; private set; }
            
                void M()
                {
                    P = "";
                }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestSimpleFieldInExpressionBody_FieldWrittenElsewhere3()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                public string P
                {
                    get => s ??= "";
                }

                void M()
                {
                    s = "";
                }
            }
            """,
            """
            class Class
            {
                public string P
                {
                    get => field ??= ""; private set;
                }
            
                void M()
                {
                    P = "";
                }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestSimpleFieldInExpressionBody_FieldWrittenElsewhere4()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                public string P
                {
                    get
                    {
                        return s ??= "";
                    }
                }

                void M()
                {
                    s = "";
                }
            }
            """,
            """
            class Class
            {
                public string P
                {
                    get
                    {
                        return field ??= "";
                    }

                    private set;
                }
            
                void M()
                {
                    P = "";
                }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestNonTrivialGetterWithExternalRead1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public int I => i / 2;

                void M()
                {
                    Console.WriteLine(i);
                }
            }
            """, new(parseOptions: Preview));
    }

    [Fact]
    public async Task TestNonTrivialGetterWithExternalRead2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public int I => i / 2;

                void M()
                {
                    Console.WriteLine(this.i);
                }
            }
            """, new(parseOptions: Preview));
    }

    [Fact]
    public async Task TestNonTrivialSetterWithExternalWrite1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public int I { get => i; set => value = i / 2; }

                void M()
                {
                    i = 1;
                }
            }
            """, new(parseOptions: Preview));
    }

    [Fact]
    public async Task TestNonTrivialSetterWithExternalWrite2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];
            
                public int I { get => i; set => value = i / 2; }

                void M()
                {
                    this.i = 1;
                }
            }
            """, new(parseOptions: Preview));
    }

    [Fact]
    public async Task TestNonTrivialSetterWithNoExternalWrite1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public int I { get => i; set => i = value / 2; }
            }
            """,
            """
            class Class
            {
                public int I { get; set => field = value / 2; }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestNonTrivialGetterWithExternalReadWrite1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public int I => i / 2;

                void M()
                {
                    Console.WriteLine(this.i++);
                }
            }
            """, new(parseOptions: Preview));
    }

    [Fact]
    public async Task TestNonTrivialSetterWithExternalReadWrite1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public int I { get => i; set => i = value / 2; }

                void M()
                {
                    Console.WriteLine(this.i++);
                }
            }
            """, new(parseOptions: Preview));
    }

    [Fact]
    public async Task TestTrivialGetterWithExternalRead1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public int I => i;

                void M()
                {
                    Console.WriteLine(i);
                }
            }
            """,
            """
            class Class
            {
                public int I { get; }

                void M()
                {
                    Console.WriteLine(I);
                }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestNoSetterWithExternalWrite1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public int I => i;

                void M()
                {
                    i = 1;
                }
            }
            """,
            """
            class Class
            {
                public int I { get; private set; }

                void M()
                {
                    I = 1;
                }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestFormatString()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|private string prop;|]
                public string Prop => $"{prop:prop}";
            }
            """,
            """
            class C
            {
                public string Prop => $"{field:prop}";
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestNoSetterButWrittenOutside()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|private string prop;|]
                public string Prop => prop ?? "";

                void M() { prop = "..."; }
            }
            """,
            """
            class C
            {
                public string Prop { get => field ?? ""; private set; }
            
                void M() { Prop = "..."; }
            }
            """, parseOptions: Preview);
    }

    [Fact]
    public async Task TestNotWithNameofInAttribute()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [|private string prop;|]
                [ThisIsMyBackingField(nameof(prop))]
                public string Prop { get => prop; set => prop = value; }
            }
            """, new(parseOptions: Preview));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75516")]
    public async Task TestBackingFieldUsedAsArgument1()
    {
        await TestInRegularAndScriptAsync("""
            class C
            {
                [|int _i;|]
                int P
                {
                    get => _i;
                    set
                    {
                        M(_i);
                        _i = value;
                    }
                }

                void M(int i) { }
            }
            """, """
            class C
            {
                int P
                {
                    get;
                    set
                    {
                        M(field);
                        field = value;
                    }
                }

                void M(int i) { }
            }
            """, parseOptions: Preview);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75516")]
    public async Task TestBackingFieldUsedAsArgument2()
    {
        await TestInRegularAndScriptAsync("""
            class C
            {
                [|int _i;|]
                int P
                {
                    get => _i;
                    set
                    {
                        M(ref _i);
                        _i = value;
                    }
                }

                void M(ref int i) { }
            }
            """, """
            class C
            {
                int P
                {
                    get;
                    set
                    {
                        M(ref field);
                        field = value;
                    }
                }

                void M(ref int i) { }
            }
            """, parseOptions: Preview);
    }
}
