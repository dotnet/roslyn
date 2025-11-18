// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementAbstractClass;

[Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
public sealed partial class ImplementAbstractClassTests(ITestOutputHelper logger) : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpImplementAbstractClassCodeFixProvider());

    private OptionsCollection AllOptionsOff
        => new(GetLanguage())
        {
             { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    internal Task TestAllOptionsOffAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        int index = 0,
        OptionsCollection? options = null,
        ParseOptions? parseOptions = null)
    {
        options ??= new OptionsCollection(GetLanguage());
        options.AddRange(AllOptionsOff);

        return TestInRegularAndScriptAsync(
            initialMarkup,
            expectedMarkup,
            index: index,
            new(options: options, parseOptions: parseOptions));
    }

    [Fact]
    public Task TestSimpleMethods()
        => TestAllOptionsOffAsync(
            """
            abstract class Goo
            {
                protected abstract string GooMethod();
                public abstract void Blah();
            }

            abstract class Bar : Goo
            {
                public abstract bool BarMethod();

                public override void Blah()
                {
                }
            }

            class [|Program|] : Goo
            {
                static void Main(string[] args)
                {
                }
            }
            """,
            """
            abstract class Goo
            {
                protected abstract string GooMethod();
                public abstract void Blah();
            }

            abstract class Bar : Goo
            {
                public abstract bool BarMethod();

                public override void Blah()
                {
                }
            }

            class Program : Goo
            {
                static void Main(string[] args)
                {
                }

                public override void Blah()
                {
                    throw new System.NotImplementedException();
                }

                protected override string GooMethod()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16434")]
    public Task TestMethodWithTupleNames()
        => TestAllOptionsOffAsync(
            """
            abstract class Base
            {
                protected abstract (int a, int b) Method((string, string d) x);
            }

            class [|Program|] : Base
            {
            }
            """,
            """
            abstract class Base
            {
                protected abstract (int a, int b) Method((string, string d) x);
            }

            class Program : Base
            {
                protected override (int a, int b) Method((string, string d) x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70623")]
    public Task TestMethodWithNullableDynamic()
        => TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract dynamic? M(dynamic? arg);
            }

            class [|Program|] : Base
            {
            }
            """,
            """
            abstract class Base
            {
                public abstract dynamic? M(dynamic? arg);
            }

            class Program : Base
            {
                public override dynamic? M(dynamic? arg)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            new(compilationOptions: new CSharpCompilationOptions
            (
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable
            )));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543234")]
    public Task TestNotAvailableForStruct()
        => TestMissingInRegularAndScriptAsync(
            """
            abstract class Goo
            {
                public abstract void Bar();
            }

            struct [|Program|] : Goo
            {
            }
            """);

    [Fact]
    public Task TestOptionalIntParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(int x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(int x = 3);
            }

            class b : d
            {
                public override void goo(int x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalCharParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(char x = 'a');
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(char x = 'a');
            }

            class b : d
            {
                public override void goo(char x = 'a')
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalStringParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(string x = "x");
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(string x = "x");
            }

            class b : d
            {
                public override void goo(string x = "x")
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalShortParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(short x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(short x = 3);
            }

            class b : d
            {
                public override void goo(short x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalDecimalParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(decimal x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(decimal x = 3);
            }

            class b : d
            {
                public override void goo(decimal x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalDoubleParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(double x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(double x = 3);
            }

            class b : d
            {
                public override void goo(double x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalLongParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(long x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(long x = 3);
            }

            class b : d
            {
                public override void goo(long x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalFloatParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(float x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(float x = 3);
            }

            class b : d
            {
                public override void goo(float x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalUshortParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(ushort x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(ushort x = 3);
            }

            class b : d
            {
                public override void goo(ushort x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalUintParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(uint x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(uint x = 3);
            }

            class b : d
            {
                public override void goo(uint x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalUlongParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void goo(ulong x = 3);
            }

            class [|b|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void goo(ulong x = 3);
            }

            class b : d
            {
                public override void goo(ulong x = 3)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalStructParameter_CSharp7()
        => TestAllOptionsOffAsync(
            """
            struct b
            {
            }

            abstract class d
            {
                public abstract void goo(b x = new b());
            }

            class [|c|] : d
            {
            }
            """,
            """
            struct b
            {
            }

            abstract class d
            {
                public abstract void goo(b x = new b());
            }

            class c : d
            {
                public override void goo(b x = default(b))
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            parseOptions: TestOptions.Regular7);

    [Fact]
    public Task TestOptionalStructParameter()
        => TestAllOptionsOffAsync(
            """
            struct b
            {
            }

            abstract class d
            {
                public abstract void goo(b x = new b());
            }

            class [|c|] : d
            {
            }
            """,
            """
            struct b
            {
            }

            abstract class d
            {
                public abstract void goo(b x = new b());
            }

            class c : d
            {
                public override void goo(b x = default)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
    public Task TestOptionalNullableStructParameter()
        => TestAllOptionsOffAsync(
            """
            struct b
            {
            }

            abstract class d
            {
                public abstract void m(b? x = null, b? y = default(b?));
            }

            class [|c|] : d
            {
            }
            """,
            """
            struct b
            {
            }

            abstract class d
            {
                public abstract void m(b? x = null, b? y = default(b?));
            }

            class c : d
            {
                public override void m(b? x = null, b? y = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
    public Task TestOptionalNullableIntParameter()
        => TestAllOptionsOffAsync(
            """
            abstract class d
            {
                public abstract void m(int? x = 5, int? y = default(int?));
            }

            class [|c|] : d
            {
            }
            """,
            """
            abstract class d
            {
                public abstract void m(int? x = 5, int? y = default(int?));
            }

            class c : d
            {
                public override void m(int? x = 5, int? y = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestOptionalObjectParameter()
        => TestAllOptionsOffAsync(
            """
            class b
            {
            }

            abstract class d
            {
                public abstract void goo(b x = null);
            }

            class [|c|] : d
            {
            }
            """,
            """
            class b
            {
            }

            abstract class d
            {
                public abstract void goo(b x = null);
            }

            class c : d
            {
                public override void goo(b x = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543883")]
    public Task TestDifferentAccessorAccessibility()
        => TestAllOptionsOffAsync(
            """
            abstract class c1
            {
                public abstract c1 this[c1 x] { get; internal set; }
            }

            class [|c2|] : c1
            {
            }
            """,
            """
            abstract class c1
            {
                public abstract c1 this[c1 x] { get; internal set; }
            }

            class c2 : c1
            {
                public override c1 this[c1 x]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    internal set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact]
    public Task TestEvent1()
        => TestAllOptionsOffAsync(
            """
            using System;

            abstract class C
            {
                public abstract event Action E;
            }

            class [|D|] : C
            {
            }
            """,
            """
            using System;

            abstract class C
            {
                public abstract event Action E;
            }

            class D : C
            {
                public override event Action E;
            }
            """);

    [Fact]
    public Task TestIndexer1()
        => TestAllOptionsOffAsync(
            """
            using System;

            abstract class C
            {
                public abstract int this[string s]
                {
                    get
                    {
                    }

                    internal set
                    {
                    }
                }
            }

            class [|D|] : C
            {
            }
            """,
            """
            using System;

            abstract class C
            {
                public abstract int this[string s]
                {
                    get
                    {
                    }

                    internal set
                    {
                    }
                }
            }

            class D : C
            {
                public override int this[string s]
                {
                    get
                    {
                        throw new NotImplementedException();
                    }

                    internal set
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingInHiddenType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            abstract class Goo
            {
                public abstract void F();
            }

            class [|Program|] : Goo
            {
            #line hidden
            }
            #line default
            """);

    [Fact]
    public Task TestGenerateIfLocationAvailable()
        => TestAllOptionsOffAsync(
            """
            #line default
            using System;

            abstract class Goo { public abstract void F(); }

            partial class [|Program|] : Goo
            {
                void Bar()
                {
                }

            #line hidden
            }
            #line default
            """,
            """
            #line default
            using System;

            abstract class Goo { public abstract void F(); }

            partial class Program : Goo
            {
                public override void F()
                {
                    throw new NotImplementedException();
                }

                void Bar()
                {
                }

            #line hidden
            }
            #line default
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545585")]
    public Task TestOnlyGenerateUnimplementedAccessors()
        => TestAllOptionsOffAsync(
            """
            using System;

            abstract class A
            {
                public abstract int X { get; set; }
            }

            abstract class B : A
            {
                public override int X
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            class [|C|] : B
            {
            }
            """,
            """
            using System;

            abstract class A
            {
                public abstract int X { get; set; }
            }

            abstract class B : A
            {
                public override int X
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            class C : B
            {
                public override int X
                {
                    set
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545615")]
    public Task TestParamsArray()
        => TestAllOptionsOffAsync(
            """
            class A
            {
                public virtual void Goo(int x, params int[] y)
                {
                }
            }

            abstract class B : A
            {
                public abstract override void Goo(int x, int[] y = null);
            }

            class [|C|] : B
            {
            }
            """,
            """
            class A
            {
                public virtual void Goo(int x, params int[] y)
                {
                }
            }

            abstract class B : A
            {
                public abstract override void Goo(int x, int[] y = null);
            }

            class C : B
            {
                public override void Goo(int x, params int[] y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestParamsCollection()
        => TestAllOptionsOffAsync(
            """
            using System.Collections.Generic;

            class A
            {
                public virtual void Goo(int x, params IEnumerable<int> y)
                {
                }
            }

            abstract class B : A
            {
                public abstract override void Goo(int x, IEnumerable<int> y = null);
            }

            class [|C|] : B
            {
            }
            """,
            """
            using System.Collections.Generic;
            
            class A
            {
                public virtual void Goo(int x, params IEnumerable<int> y)
                {
                }
            }

            abstract class B : A
            {
                public abstract override void Goo(int x, IEnumerable<int> y = null);
            }

            class C : B
            {
                public override void Goo(int x, params IEnumerable<int> y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545636")]
    public Task TestNullPointerType()
        => TestAllOptionsOffAsync(
            """
            abstract class C
            {
                unsafe public abstract void Goo(int* x = null);
            }

            class [|D|] : C
            {
            }
            """,
            """
            abstract class C
            {
                unsafe public abstract void Goo(int* x = null);
            }

            class D : C
            {
                public override unsafe void Goo(int* x = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545637")]
    public Task TestErrorTypeCalledVar()
        => TestAllOptionsOffAsync(
            """
            extern alias var;

            abstract class C
            {
                public abstract void Goo(var::X x);
            }

            class [|D|] : C
            {
            }
            """,
            """
            extern alias var;

            abstract class C
            {
                public abstract void Goo(var::X x);
            }

            class D : C
            {
                public override void Goo(X x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task Bugfix_581500()
        => TestAllOptionsOffAsync(
            """
            abstract class A<T>
            {
                public abstract void M(T x);

                abstract class B : A<B>
                {
                    class [|T|] : A<T>
                    {
                    }
                }
            }
            """,
            """
            abstract class A<T>
            {
                public abstract void M(T x);

                abstract class B : A<B>
                {
                    class T : A<T>
                    {
                        public override void M(B.T x)
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/625442")]
    public Task Bugfix_625442()
        => TestAllOptionsOffAsync(
            """
            abstract class A<T>
            {
                public abstract void M(T x);
                abstract class B : A<B>
                {
                    class [|T|] : A<B.T> { }
                }
            }
            """,
            """
            abstract class A<T>
            {
                public abstract void M(T x);
                abstract class B : A<B>
                {
                    class T : A<B.T>
                    {
                        public override void M(A<A<T>.B>.B.T x)
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2407")]
    public Task ImplementClassWithInaccessibleMembers()
        => TestAllOptionsOffAsync(
            """
            using System;
            using System.Globalization;

            public class [|x|] : EastAsianLunisolarCalendar
            {
            }
            """,
            """
            using System;
            using System.Globalization;

            public class x : EastAsianLunisolarCalendar
            {
                public override int[] Eras
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                internal override int MinCalendarYear
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                internal override int MaxCalendarYear
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                internal override EraInfo[] CalEraInfo
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                internal override DateTime MinDate
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                internal override DateTime MaxDate
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public override int GetEra(DateTime time)
                {
                    throw new NotImplementedException();
                }

                internal override int GetGregorianYear(int year, int era)
                {
                    throw new NotImplementedException();
                }

                internal override int GetYear(int year, DateTime time)
                {
                    throw new NotImplementedException();
                }

                internal override int GetYearInfo(int LunarYear, int Index)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13149")]
    public Task TestPartialClass1()
        => TestAllOptionsOffAsync(
            """
            using System;

            public abstract class Base
            {
                public abstract void Dispose();
            }

            partial class [|A|] : Base
            {
            }

            partial class A
            {
            }
            """,
            """
            using System;

            public abstract class Base
            {
                public abstract void Dispose();
            }

            partial class A : Base
            {
                public override void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            partial class A
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13149")]
    public Task TestPartialClass2()
        => TestAllOptionsOffAsync(
            """
            using System;

            public abstract class Base
            {
                public abstract void Dispose();
            }

            partial class [|A|]
            {
            }

            partial class A : Base
            {
            }
            """,
            """
            using System;

            public abstract class Base
            {
                public abstract void Dispose();
            }

            partial class A
            {
                public override void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            partial class A : Base
            {
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Method1()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract void M(int x);
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract void M(int x);
            }

            class T : A
            {
                public override void M(int x) => throw new System.NotImplementedException();
            }
            """, new(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Property1()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int M { get; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int M { get; }
            }

            class T : A
            {
                public override int M => throw new System.NotImplementedException();
            }
            """, new(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Property3()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int M { set; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int M { set; }
            }

            class T : A
            {
                public override int M
                {
                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """, new(options: new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent },
            }));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Property4()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int M { get; set; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int M { get; set; }
            }

            class T : A
            {
                public override int M
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """, new(options: new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent },
            }));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Indexers1()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int this[int i] { get; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int this[int i] { get; }
            }

            class T : A
            {
                public override int this[int i] => throw new System.NotImplementedException();
            }
            """, new(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Indexer3()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int this[int i] { set; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int this[int i] { set; }
            }

            class T : A
            {
                public override int this[int i]
                {
                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """, new(options: new OptionsCollection(GetLanguage())
{
    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent },
}));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Indexer4()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int this[int i] { get; set; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int this[int i] { get; set; }
            }

            class T : A
            {
                public override int this[int i]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """, new(options: new OptionsCollection(GetLanguage())
{
    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent },
}));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Accessor1()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int M { get; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int M { get; }
            }

            class T : A
            {
                public override int M { get => throw new System.NotImplementedException(); }
            }
            """, new(options: new OptionsCollection(GetLanguage())
{
    { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never, NotificationOption2.Silent },
    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
}));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Accessor3()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int M { set; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int M { set; }
            }

            class T : A
            {
                public override int M { set => throw new System.NotImplementedException(); }
            }
            """, new(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
    public Task TestCodeStyle_Accessor4()
        => TestInRegularAndScriptAsync(
            """
            abstract class A
            {
                public abstract int M { get; set; }
            }

            class [|T|] : A
            {
            }
            """,
            """
            abstract class A
            {
                public abstract int M { get; set; }
            }

            class T : A
            {
                public override int M { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            }
            """, new(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15387")]
    public async Task TestWithGroupingOff1()
    {
        var options = Option(ImplementTypeOptionsStorage.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd);

        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract int Prop { get; }
            }

            class [|Derived|] : Base
            {
                void Goo() { }
            }
            """,
            """
            abstract class Base
            {
                public abstract int Prop { get; }
            }

            class Derived : Base
            {
                void Goo() { }

                public override int Prop => throw new System.NotImplementedException();
            }
            """, new(options: options));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17274")]
    public Task TestAddedUsingWithBanner1()
        => TestInRegularAndScriptAsync(
            """
            // Copyright ...

            using Microsoft.Win32;

            namespace My
            {
                public abstract class Goo
                {
                    public abstract void Bar(System.Collections.Generic.List<object> values);
                }

                public class [|Goo2|] : Goo // Implement Abstract Class
                {
                }
            }
            """,
            """
            // Copyright ...

            using System.Collections.Generic;
            using Microsoft.Win32;

            namespace My
            {
                public abstract class Goo
                {
                    public abstract void Bar(System.Collections.Generic.List<object> values);
                }

                public class Goo2 : Goo // Implement Abstract Class
                {
                    public override void Bar(List<object> values)
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17562")]
    public Task TestNullableOptionalParameters_CSharp7()
        => TestInRegularAndScriptAsync(
            """
            struct V { }
            abstract class B
            {
                public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
                public abstract void M2<T>(T? i = null) where T : struct;
            }
            sealed class [|D|] : B
            {
            }
            """,
            """
            struct V { }
            abstract class B
            {
                public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
                public abstract void M2<T>(T? i = null) where T : struct;
            }
            sealed class D : B
            {
                public override void M1(int i = 0, string s = null, int? j = null, V v = default(V))
                {
                    throw new System.NotImplementedException();
                }

                public override void M2<T>(T? i = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            new(parseOptions: TestOptions.Regular7));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17562")]
    public Task TestNullableOptionalParametersCSharp7()
        => TestAsync(
            """
            struct V { }
            abstract class B
            {
                public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
                public abstract void M2<T>(T? i = null) where T : struct;
            }
            sealed class [|D|] : B
            {
            }
            """,
            """
            struct V { }
            abstract class B
            {
                public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
                public abstract void M2<T>(T? i = null) where T : struct;
            }
            sealed class D : B
            {
                public override void M1(int i = 0, string s = null, int? j = null, V v = default(V))
                {
                    throw new System.NotImplementedException();
                }

                public override void M2<T>(T? i = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """, new(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17562")]
    public Task TestNullableOptionalParameters()
        => TestInRegularAndScriptAsync(
            """
            struct V { }
            abstract class B
            {
                public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
                public abstract void M2<T>(T? i = null) where T : struct;
            }
            sealed class [|D|] : B
            {
            }
            """,
            """
            struct V { }
            abstract class B
            {
                public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
                public abstract void M2<T>(T? i = null) where T : struct;
            }
            sealed class D : B
            {
                public override void M1(int i = 0, string s = null, int? j = null, V v = default)
                {
                    throw new System.NotImplementedException();
                }

                public override void M2<T>(T? i = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5898")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13932")]
    public async Task TestAutoProperties()
    {
        var options = new OptionsCollection(GetLanguage())
        {
            Option(ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties),
            Option(MemberDisplayOptionsStorage.HideAdvancedMembers, true),
        };

        await TestInRegularAndScriptAsync(
            """
            abstract class AbstractClass
            {
                public abstract int ReadOnlyProp { get; }
                public abstract int ReadWriteProp { get; set; }
                public abstract int WriteOnlyProp { set; }
            }

            class [|C|] : AbstractClass
            {
            }
            """,
            """
            abstract class AbstractClass
            {
                public abstract int ReadOnlyProp { get; }
                public abstract int ReadWriteProp { get; set; }
                public abstract int WriteOnlyProp { set; }
            }

            class C : AbstractClass
            {
                public override int ReadOnlyProp { get; }
                public override int ReadWriteProp { get; set; }
                public override int WriteOnlyProp { set => throw new System.NotImplementedException(); }
            }
            """, parameters: new TestParameters(options: options));
    }

    [Theory, CombinatorialData]
    public Task TestRefWithMethod_Parameters([CombinatorialValues("ref", "in", "ref readonly")] string modifier)
        => TestInRegularAndScriptAsync(
            $$"""
            abstract class TestParent
            {
                public abstract void Method({{modifier}} int p);
            }
            public class [|Test|] : TestParent
            {
            }
            """,
            $$"""
            abstract class TestParent
            {
                public abstract void Method({{modifier}} int p);
            }
            public class Test : TestParent
            {
                public override void Method({{modifier}} int p)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestRefReadOnlyWithMethod_ReturnType()
        => TestInRegularAndScriptAsync(
            """
            abstract class TestParent
            {
                public abstract ref readonly int Method();
            }
            public class [|Test|] : TestParent
            {
            }
            """,
            """
            abstract class TestParent
            {
                public abstract ref readonly int Method();
            }
            public class Test : TestParent
            {
                public override ref readonly int Method()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestRefReadOnlyWithProperty()
        => TestInRegularAndScriptAsync(
            """
            abstract class TestParent
            {
                public abstract ref readonly int Property { get; }
            }
            public class [|Test|] : TestParent
            {
            }
            """,
            """
            abstract class TestParent
            {
                public abstract ref readonly int Property { get; }
            }
            public class Test : TestParent
            {
                public override ref readonly int Property => throw new System.NotImplementedException();
            }
            """);

    [Theory, CombinatorialData]
    public Task TestRefWithIndexer_Parameters([CombinatorialValues("ref", "in", "ref readonly")] string modifier)
        => TestInRegularAndScriptAsync(
            $$"""
            abstract class TestParent
            {
                public abstract int this[{{modifier}} int p] { set; }
            }
            public class [|Test|] : TestParent
            {
            }
            """,
            $$"""
            abstract class TestParent
            {
                public abstract int this[{{modifier}} int p] { set; }
            }
            public class Test : TestParent
            {
                public override int this[{{modifier}} int p] { set => throw new System.NotImplementedException(); }
            }
            """);

    [Fact]
    public Task TestRefReadOnlyWithIndexer_ReturnType()
        => TestInRegularAndScriptAsync(
            """
            abstract class TestParent
            {
                public abstract ref readonly int this[int p] { get; }
            }
            public class [|Test|] : TestParent
            {
            }
            """,
            """
            abstract class TestParent
            {
                public abstract ref readonly int this[int p] { get; }
            }
            public class Test : TestParent
            {
                public override ref readonly int this[int p] => throw new System.NotImplementedException();
            }
            """);

    [Fact]
    public Task TestUnmanagedConstraint()
        => TestInRegularAndScriptAsync(
            """
            public abstract class ParentTest
            {
                public abstract void M<T>() where T : unmanaged;
            }
            public class [|Test|] : ParentTest
            {
            }
            """,
            """
            public abstract class ParentTest
            {
                public abstract void M<T>() where T : unmanaged;
            }
            public class Test : ParentTest
            {
                public override void M<T>()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task NothingOfferedWhenInheritanceIsPreventedByInternalAbstractMember()
        => TestMissingAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public abstract class Base
            {
                internal abstract void Method();
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class [|Derived|] : Base
            {
                Base inner;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30102")]
    public Task TestWithIncompleteGenericInBaseList()
        => TestAllOptionsOffAsync(
            """
            abstract class A<T>
            {
                public abstract void AbstractMethod();
            }

            class [|B|] : A<int
            {

            }
            """,
            """
            abstract class A<T>
            {
                public abstract void AbstractMethod();
            }

            class B : A<int
            {
                public override void AbstractMethod()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44907")]
    public Task TestWithRecords()
        => TestAllOptionsOffAsync(
            """
            abstract record A
            {
                public abstract void AbstractMethod();
            }

            record [|B|] : A
            {

            }
            """,
            """
            abstract record A
            {
                public abstract void AbstractMethod();
            }

            record B : A
            {
                public override void AbstractMethod()
                {
                    throw new System.NotImplementedException();
                }
            }
            """, parseOptions: TestOptions.RegularPreview);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44907")]
    public Task TestWithRecordsWithPositionalMembers()
        => TestAllOptionsOffAsync(
            """
            abstract record A
            {
                public abstract void AbstractMethod();
            }

            record [|B|](int i) : A
            {

            }
            """,
            """
            abstract record A
            {
                public abstract void AbstractMethod();
            }

            record B(int i) : A
            {
                public override void AbstractMethod()
                {
                    throw new System.NotImplementedException();
                }
            }
            """, parseOptions: TestOptions.RegularPreview);

    [Fact]
    public Task TestWithClassWithParameters()
        => TestAllOptionsOffAsync(
            """
            abstract class A
            {
                public abstract void AbstractMethod();
            }

            class [|B|](int i) : A
            {

            }
            """,
            """
            abstract class A
            {
                public abstract void AbstractMethod();
            }

            class B(int i) : A
            {
                public override void AbstractMethod()
                {
                    throw new System.NotImplementedException();
                }
            }
            """, parseOptions: TestOptions.RegularPreview);

    [Fact]
    public Task TestWithClassWithSemicolonBody()
        => TestAllOptionsOffAsync(
            """
            abstract class A
            {
                public abstract void AbstractMethod();
            }

            class [|B|] : A;
            """,
            """
            abstract class A
            {
                public abstract void AbstractMethod();
            }

            class B : A
            {
                public override void AbstractMethod()
                {
                    throw new System.NotImplementedException();
                }
            }
            """, parseOptions: TestOptions.RegularPreview);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48742")]
    public Task TestUnconstrainedGenericNullable()
        => TestAllOptionsOffAsync(
            """
            #nullable enable

            abstract class B<T>
            {
                public abstract T? M();
            }

            class [|D|] : B<int>
            {
            }
            """,
            """
            #nullable enable

            abstract class B<T>
            {
                public abstract T? M();
            }

            class D : B<int>
            {
                public override int M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48742")]
    public Task TestUnconstrainedGenericNullable2()
        => TestAllOptionsOffAsync(
            """
            #nullable enable

            abstract class B<T>
            {
                public abstract T? M();
            }

            class [|D<T>|] : B<T> where T : struct
            {
            }
            """,
            """
            #nullable enable

            abstract class B<T>
            {
                public abstract T? M();
            }

            class D<T> : B<T> where T : struct
            {
                public override T M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48742")]
    public Task TestUnconstrainedGenericNullable_Tuple()
        => TestAllOptionsOffAsync(
            """
            #nullable enable

            abstract class B<T>
            {
                public abstract T? M();
            }

            class [|D<T>|] : B<(T, T)>
            {
            }
            """,
            """
            #nullable enable

            abstract class B<T>
            {
                public abstract T? M();
            }

            class D<T> : B<(T, T)>
            {
                public override (T, T) M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/48742")]
    [InlineData("", "T")]
    [InlineData(" where T : class", "T")]
    [InlineData("", "T?")]
    [InlineData(" where T : class", "T?")]
    [InlineData(" where T : struct", "T?")]
    public Task TestUnconstrainedGenericNullable_NoRegression(string constraint, string passToBase)
        => TestAllOptionsOffAsync(
            $$"""
            #nullable enable

            abstract class B<T>
            {
                public abstract T? M();
            }

            class [|D<T>|] : B<{{passToBase}}>{{constraint}}
            {
            }
            """,
            $$"""
            #nullable enable

            abstract class B<T>
            {
                public abstract T? M();
            }

            class D<T> : B<{{passToBase}}>{{constraint}}
            {
                public override T? M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53012")]
    public Task TestNullableGenericType()
        => TestAllOptionsOffAsync(
            """
            abstract class C
            {
                public abstract void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d);
            }
            class [|D|] : C
            {
            }
            """,
            """
            abstract class C
            {
                public abstract void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d);
            }
            class D : C
            {
                public override void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d)
                    where T1 : default
                    where T3 : default
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62092")]
    public Task TestNullableGenericType2()
        => TestAllOptionsOffAsync(
            """
            interface I<out T> { }

            abstract class C
            {
                protected abstract void M1<T>(T? t);
                protected abstract void M2<T>(I<T?> i);
            }

            class [|C2|] : C
            {
            }
            """,
            """
            interface I<out T> { }

            abstract class C
            {
                protected abstract void M1<T>(T? t);
                protected abstract void M2<T>(I<T?> i);
            }

            class C2 : C
            {
                protected override void M1<T>(T? t) where T : default
                {
                    throw new System.NotImplementedException();
                }

                protected override void M2<T>(I<T?> i) where T : default
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestRequiredMember()
        => TestAllOptionsOffAsync(
            """
            abstract class C
            {
                public abstract required int Property { get; set; }
            }
            class [|D|] : C
            {
            }
            """,
            """
            abstract class C
            {
                public abstract required int Property { get; set; }
            }
            class D : C
            {
                public override required int Property
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70530")]
    public Task TestRecordInheritance1()
        => TestAllOptionsOffAsync(
            """
            abstract record A()
            {
                protected abstract void M();
            }

            class [|C|]() : A()
            {
            }
            """,
            """
            abstract record A()
            {
                protected abstract void M();
            }
            
            class C() : A()
            {
                protected override void M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70530")]
    public Task TestRecordInheritance2()
        => TestAllOptionsOffAsync(
            """
            abstract record A()
            {
                protected abstract void M();
            }

            record [|C|]() : A()
            {
            }
            """,
            """
            abstract record A()
            {
                protected abstract void M();
            }
            
            record C() : A()
            {
                protected override void M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75992")]
    public Task InsertMissingBraces()
        => TestAllOptionsOffAsync(
            """
            abstract class A
            {
                public abstract void M();
            }

            class [|B|] : A

            file class C;
            """,
            """
            abstract class A
            {
                public abstract void M();
            }

            class B : A
            {
                public override void M()
                {
                    throw new System.NotImplementedException();
                }
            }

            file class C;
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71225")]
    public Task TestConstrainedTypeParameter1()
        => TestAllOptionsOffAsync(
            """
            #nullable enable
            using System;

            interface I<out T> { }

            class C { }

            abstract class Problem
            {
                protected abstract void M<T>(I<T?> i) where T : C;
            }

            class [|Bad|] : Problem
            {
            }
            """,
            """
            #nullable enable
            using System;

            interface I<out T> { }
            
            class C { }
            
            abstract class Problem
            {
                protected abstract void M<T>(I<T?> i) where T : C;
            }
            
            class Bad : Problem
            {
                protected override void M<T>(I<T?> i) where T : class
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71225")]
    public Task TestConstrainedTypeParameter2()
        => TestAllOptionsOffAsync(
            """
            #nullable enable
            using System;

            interface I<out T> { }

            class C { }

            abstract class Problem<U>
            {
                protected abstract void M<T>(I<T?> i) where T : U;
            }

            class [|Bad|] : Problem<int>
            {
            }
            """,
            """
            #nullable enable
            using System;

            interface I<out T> { }
            
            class C { }
            
            abstract class Problem<U>
            {
                protected abstract void M<T>(I<T?> i) where T : U;
            }
            
            class Bad : Problem<int>
            {
                protected override void M<T>(I<T> i) where T : struct
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71225")]
    public Task TestConstrainedTypeParameter3()
        => TestAllOptionsOffAsync(
            """
            #nullable enable
            using System;

            interface I<out T> { }

            class C { }

            abstract class Problem<U>
            {
                protected abstract void M<T>(I<T?> i) where T : U;
            }

            class [|Bad|] : Problem<string>
            {
            }
            """,
            """
            #nullable enable
            using System;

            interface I<out T> { }
            
            class C { }
            
            abstract class Problem<U>
            {
                protected abstract void M<T>(I<T?> i) where T : U;
            }
            
            class Bad : Problem<string>
            {
                protected override void M<T>(I<T?> i) where T : class
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71225")]
    public Task TestConstrainedTypeParameter4()
        => TestAllOptionsOffAsync(
            """
            #nullable enable
            using System;

            interface I<out T> { }

            class C { }

            abstract class Problem<U>
            {
                protected abstract void M<T>(I<T?> i) where T : U;
            }

            class [|Bad|] : Problem<int[]>
            {
            }
            """,
            """
            #nullable enable
            using System;

            interface I<out T> { }
            
            class C { }
            
            abstract class Problem<U>
            {
                protected abstract void M<T>(I<T?> i) where T : U;
            }
            
            class Bad : Problem<int[]>
            {
                protected override void M<T>(I<T?> i) where T : class
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78282")]
    public Task TestInstanceCompoundOperator()
        => TestAllOptionsOffAsync(
            """
            abstract class C1
            {
                abstract public void operator ++();

                abstract public void operator -=(int i);
            }

            class [|C2|] : C1
            {
            }
            """,
            """
            abstract class C1
            {
                abstract public void operator ++();
            
                abstract public void operator -=(int i);
            }
            
            class C2 : C1
            {
                public override void operator -=(int i)
                {
                    throw new System.NotImplementedException();
                }
            
                public override void operator ++()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);
}
