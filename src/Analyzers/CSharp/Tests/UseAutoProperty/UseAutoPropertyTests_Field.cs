// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseAutoProperty;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
public sealed partial class UseAutoPropertyTests
{
    private static readonly ParseOptions CSharp13 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13);
    private static readonly ParseOptions CSharp14 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp14);

    [Fact]
    public Task TestNotInCSharp13()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestFieldSimplestCase()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestFieldWithInitializer()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s = ""|];

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
                } = "";
            }
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestFieldAccessOffOfThis()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestStaticField()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestGetterWithMultipleStatements_Field()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement_Field()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement_Field2()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestSimpleFieldInExpressionBody()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestMultipleFields_NoClearChoice()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                int [|x|], y;

                int Total => x + y;
            }
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestMultipleFields_NoClearChoice2()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestMultipleFields_ClearChoice()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestMultipleFields_PickByName1()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestMultipleFields_PickByName2()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWhenAlreadyUsingField()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWhenUsingNameof1()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWhenUsingNameof2()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWhenUsingNameof3()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWhenUsingNameof4()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWhenUsingNameof5()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s = nameof(s)|];

                string P => s;
            }
            """, new(parseOptions: CSharp13));

    [Fact]
    public Task TestWithRefArgumentUseInside()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWithRefArgumentUseOutside()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestWithRefUseInside()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWithRefUseOutside()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestWithAddressOfInside()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWithAddressOfOutside()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotChainedPattern1()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestLazyInit1()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestRefSetAccessor1()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestRefSetAccessor2()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestAttributesOnField()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestAttributesOnField2()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestAttributesOnField3()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestAttributesOnField4()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestAttributesOnField5()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestAttributesOnField6()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestAttributesOnField7()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestFieldUsedInObjectInitializer()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestSimpleFieldInExpressionBody_FieldWrittenElsewhere1()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestSimpleFieldInExpressionBody_FieldWrittenElsewhere2()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestSimpleFieldInExpressionBody_FieldWrittenElsewhere3()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestSimpleFieldInExpressionBody_FieldWrittenElsewhere4()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNonTrivialGetterWithExternalRead1()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNonTrivialGetterWithExternalRead2()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNonTrivialSetterWithExternalWrite1()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNonTrivialSetterWithExternalWrite2()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNonTrivialSetterWithNoExternalWrite1()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNonTrivialGetterWithExternalReadWrite1()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNonTrivialSetterWithExternalReadWrite1()
        => TestMissingInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestTrivialGetterWithExternalRead1()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNoSetterWithExternalWrite1()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestFormatString()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNoSetterButWrittenOutside()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp14));

    [Fact]
    public Task TestNotWithNameofInAttribute()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [|private string prop;|]
                [ThisIsMyBackingField(nameof(prop))]
                public string Prop { get => prop; set => prop = value; }
            }
            """, new(parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75516")]
    public Task TestBackingFieldUsedAsArgument1()
        => TestInRegularAndScriptAsync("""
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
            """, new(parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75516")]
    public Task TestBackingFieldUsedAsArgument2()
        => TestInRegularAndScriptAsync("""
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
            """, new(parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26527")]
    public Task TestFixAllInDocument3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            public sealed class SomeViewModel
            {
                private bool {|FixAllInDocument:a|} = true;
                public bool A { get => a; set => Set(ref a, value); }

                private bool b = true;
                public bool B { get => b; set => Set(ref b, value); }

                private bool c = true;
                public bool C { get => c; set => Set(ref c, value); }

                private void Set<T>(ref T field, T value) => throw new NotImplementedException();
            }
            """,
            """
            using System;
            
            public sealed class SomeViewModel
            {
                public bool A { get; set => Set(ref field, value); } = true;
                public bool B { get; set => Set(ref field, value); } = true;
                public bool C { get; set => Set(ref field, value); } = true;
            
                private void Set<T>(ref T field, T value) => throw new NotImplementedException();
            }
            """, new TestParameters(parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76790")]
    public Task TestFixAllInDocument4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int {|FixAllInDocument:a|};
                public int A => a;

                void M()
                {
                    nameMustDiffer = true;
                }

                private bool nameMustDiffer;
                public bool B => !nameMustDiffer;
            }
            """,
            """
            class C
            {
                public int A { get; }

                void M()
                {
                    B = true;
                }

                public bool B { get => !field; private set; }
            }
            """, new TestParameters(parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76790")]
    public Task TestWrittenInConstructor()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    nameMustDiffer = true;
                }

                private bool [|nameMustDiffer|];
                public bool B => !nameMustDiffer;
            }
            """,
            """
            class C
            {
                void M()
                {
                    B = true;
                }

                public bool B { get => !field; private set; }
            }
            """, new TestParameters(parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76901")]
    public Task TestReadAndWrite()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int [|_g|];

                public int CustomGetter
                {
                    get => _g < 0 ? 0 : _g; // Synthesized return value
                    set => _g = value;
                }
            }
            """,
            """
            class C
            {
                public int CustomGetter
                {
                    get => field < 0 ? 0 : field; // Synthesized return value
                    set;
                }
            }
            """, new TestParameters(parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76901")]
    public Task TestContractCall()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int [|_s|];

                public int CustomSetter
                {
                    get => _s;
                    set
                    {
                        Assumes.True(value >= 0); // Validation
                        _s = value;
                    }
                }
            }
            """,
            """
            class C
            {
                public int CustomSetter
                {
                    get;
                    set
                    {
                        Assumes.True(value >= 0); // Validation
                        field = value;
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76901")]
    public Task TestDelegateInvoke()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private int [|_s|];
                public event Action<string> OnChanged;

                public int ObservableProp
                {
                    get => _s;
                    set
                    {
                        _s = value;
                        OnChanged.Invoke(nameof(ObservableProp));
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                public event Action<string> OnChanged;
            
                public int ObservableProp
                {
                    get;
                    set
                    {
                        field = value;
                        OnChanged.Invoke(nameof(ObservableProp));
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp14));
}
