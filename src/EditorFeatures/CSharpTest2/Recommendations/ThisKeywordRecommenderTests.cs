// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ThisKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            @"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
            @"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
            @"global using Goo = $$");

    [Fact]
    public Task TestNotAfterAngle()
        => VerifyAbsenceAsync(
            @"interface IGoo<$$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterIn()
        => VerifyAbsenceAsync(
            @"interface IGoo<in $$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterComma()
        => VerifyAbsenceAsync(
            @"interface IGoo<Goo, $$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterAttribute()
        => VerifyAbsenceAsync(
            @"interface IGoo<[Goo]$$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterAngle()
        => VerifyAbsenceAsync(
            @"delegate void D<$$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterComma()
        => VerifyAbsenceAsync(
            @"delegate void D<Goo, $$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterAttribute()
        => VerifyAbsenceAsync(
            @"delegate void D<[Goo]$$");

    [Fact]
    public Task TestNotThisBaseListAfterAngle()
        => VerifyAbsenceAsync(
            @"interface IGoo : Bar<$$");

    [Fact]
    public Task TestNotInGenericMethod()
        => VerifyAbsenceAsync(
            """
            interface IGoo {
                void Goo<$$
            """);

    [Fact]
    public Task TestNotAfterRef()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(ref $$
            """);

    [Theory, CombinatorialData]
    public Task TestNotAfterIn([CombinatorialValues("in", "ref readonly")] string modifier)
        => VerifyAbsenceAsync($$"""
            class C {
                void Goo({{modifier}} $$
            """);

    [Fact]
    public Task TestNotAfterThis_InBogusMethod()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(this $$
            """);

    [Fact]
    public Task TestNotAfterOut()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(out $$
            """);

    [Fact]
    public Task TestNotAfterMethodOpenParen()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo($$
            """);

    [Fact]
    public Task TestNotAfterMethodComma()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(int i, $$
            """);

    [Fact]
    public Task TestNotAfterMethodAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(int i, [Goo]$$
            """);

    [Fact]
    public Task TestNotAfterConstructorOpenParen()
        => VerifyAbsenceAsync(
            """
            class C {
                public C($$
            """);

    [Fact]
    public Task TestNotAfterConstructorComma()
        => VerifyAbsenceAsync(
            """
            class C {
                public C(int i, $$
            """);

    [Fact]
    public Task TestNotAfterConstructorAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                public C(int i, [Goo]$$
            """);

    [Fact]
    public Task TestNotAfterDelegateOpenParen()
        => VerifyAbsenceAsync(
            @"delegate void D($$");

    [Fact]
    public Task TestNotAfterDelegateComma()
        => VerifyAbsenceAsync(
            @"delegate void D(int i, $$");

    [Fact]
    public Task TestNotAfterDelegateAttribute()
        => VerifyAbsenceAsync(
            @"delegate void D(int i, [Goo]$$");

    [Fact]
    public Task TestNotAfterOperator()
        => VerifyAbsenceAsync(
            """
            class C {
                static int operator +($$
            """);

    [Fact]
    public Task TestNotAfterDestructor()
        => VerifyAbsenceAsync(
            """
            class C {
                ~C($$
            """);

    [Fact]
    public Task TestNotAfterIndexer()
        => VerifyAbsenceAsync(
            """
            class C {
                int this[$$
            """);

    [Fact]
    public Task TestNotInInstanceMethodInInstanceClass()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo($$
            """);

    [Fact]
    public Task TestNotInStaticMethodInInstanceClass()
        => VerifyAbsenceAsync(
            """
            class C {
                static int Goo($$
            """);

    [Fact]
    public Task TestNotInInstanceMethodInStaticClass()
        => VerifyAbsenceAsync(
            """
            static class C {
                int Goo($$
            """);

    [Fact]
    public Task TestInStaticMethodInStaticClass()
        => VerifyKeywordAsync(
            """
            static class C {
                static int Goo($$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27028")]
    public Task TestInLocalFunction()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Method()
                {
                    void local()
                    {
                        $$
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27028")]
    public Task TestInNestedLocalFunction()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Method()
                {
                    void local()
                    {
                        void nested()
                        {
                            $$
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27028")]
    public Task TestInLocalFunctionInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C {
                static int Method()
                {
                    void local()
                    {
                        $$
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27028")]
    public Task TestInNestedLocalFunctionInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static int Method()
                {
                    void local()
                    {
                        void nested()
                        {
                            $$
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35644")]
    public Task TestInStaticLocalFunction()
        => VerifyAbsenceAsync(
            """
            class C {
                int Method()
                {
                    static void local()
                    {
                        $$
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35644")]
    public Task TestInNestedInStaticLocalFunction()
        => VerifyAbsenceAsync(
            """
            class C
            {
                int Method()
                {
                    static void local()
                    {
                        void nested()
                        {
                            $$
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethod()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Method()
                {
                    Action a = delegate
                    {
                        $$
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInNestedAnonymousMethod()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Method()
                {
                    Action a = delegate
                    {
                        Action b = delegate
                        {
                            $$
                        };
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static int Method()
                {
                    Action a = delegate
                    {
                        $$
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInNestedAnonymousMethodInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static int Method()
                {
                    Action a = delegate
                    {
                        Action b = delegate
                        {
                            $$
                        };
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInLambdaExpression()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Method()
                {
                    Action a = () =>
                    {
                        $$
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInNestedLambdaExpression()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Method()
                {
                    Action a = () =>
                    {
                        Action b = () =>
                        {
                            $$
                        };
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInLambdaExpressionInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static int Method()
                {
                    Action a = () =>
                    {
                        $$
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInNestedLambdaExpressionInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static int Method()
                {
                    Action a = () =>
                    {
                        Action b = () =>
                        {
                            $$
                        };
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInNestedLambdaExpressionInAnonymousMethod()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Method()
                {
                    Action a = delegate
                    {
                        Action b = () =>
                        {
                            $$
                        };
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInNestedAnonymousInLambdaExpression()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Method()
                {
                    Action a = () =>
                    {
                        Action b = delegate
                        {
                            $$
                        };
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInNestedAnonymousMethodInLambdaExpressionInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static int Method()
                {
                    Action a = () =>
                    {
                        Action b = delegate
                        {
                            $$
                        };
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInNestedLambdaExpressionInAnonymousMethodInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static int Method()
                {
                    Action a = delegate
                    {
                        Action b = () =>
                        {
                            $$
                        };
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInAProperty()
        => VerifyKeywordAsync(
            """
            class C
            {
                Action A 
                { 
                    get { return delegate { $$ } }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInAPropertyInitializer()
        => VerifyKeywordAsync(
            """
            class C
            {
                Action B { get; } = delegate { $$ }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInAExpressionProperty()
        => VerifyKeywordAsync(
            """
            class C
            {
                Action A => delegate { $$ }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInAFieldInitializer()
        => VerifyKeywordAsync(
            """
            class C
            {
                Action A = delegate { $$ }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInAStaticProperty()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static Action A
                {
                    get { return delegate { $$ } }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInAStaticPropertyInitializer()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static Action B { get; } = delegate { $$ }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInAStaticExpressionProperty()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static Action A => delegate { $$ }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
    public Task TestInAnonymousMethodInAStaticFieldInitializer()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static Action A = delegate { $$ }
            }
            """);
    [Fact]
    public Task TestAfterAttribute()
        => VerifyKeywordAsync(
            """
            static class C {
                static int Goo([Bar]$$
            """);

    [Fact]
    public Task TestNotAfterSecondAttribute()
        => VerifyAbsenceAsync(
            """
            static class C {
                static int Goo(this int i, [Bar]$$
            """);

    [Fact]
    public Task TestNotAfterThis()
        => VerifyAbsenceAsync(
            """
            static class C {
                static int Goo(this $$
            """);

    [Fact]
    public Task TestNotAfterFirstParameter()
        => VerifyAbsenceAsync(
            """
            static class C {
                static int Goo(this int a, $$
            """);

    [Fact]
    public Task TestInClassConstructorInitializer()
        => VerifyKeywordAsync(
            """
            class C {
                public C() : $$
            """);

    [Fact]
    public Task TestNotInStaticClassConstructorInitializer()
        => VerifyAbsenceAsync(
            """
            class C {
                static C() : $$
            """);

    [Fact]
    public Task TestInStructConstructorInitializer()
        => VerifyKeywordAsync(
            """
            struct C {
                public C() : $$
            """);

    [Fact]
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            @"$$"));

    [Fact]
    public Task TestAfterCast()
        => VerifyKeywordAsync(AddInsideMethod(
            @"stack.Push(((IEnumerable<Segment>)((TreeSegment)$$"));

    [Fact]
    public Task TestAfterReturn()
        => VerifyKeywordAsync(AddInsideMethod(
            @"return $$"));

    [Fact]
    public Task TestAfterIndexer()
        => VerifyKeywordAsync(AddInsideMethod(
            @"return this.items[$$"));

    [Fact]
    public Task TestAfterSimpleCast()
        => VerifyKeywordAsync(AddInsideMethod(
            @"return ((IEnumerable<T>)$$"));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync(
            """
            class C {
                $$
            """);

    [Fact]
    public Task TestNotAfterVoid()
        => VerifyAbsenceAsync(
            """
            class C {
                void $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
    public Task TestAfterType()
        => VerifyAbsenceAsync(
            """
            class C {
                int $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
    public Task TestAfterTypeArray()
        => VerifyAbsenceAsync(
            """
            class C {
                internal byte[] $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
    public Task TestAfterTypeArrayBeforeArguments()
        => VerifyAbsenceAsync(
            """
            class C {
                internal byte[] $$[int i] { get; }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
    public Task TestAfterTypeBeforeArguments()
        => VerifyAbsenceAsync(
            """
            class C {
                internal byte $$[int i] { get; }
            """);

    [Fact]
    public Task TestAfterMultiply()
        => VerifyKeywordAsync(
            """
            class C {
                internal CustomAttributeRow this[uint rowId] //  This is 1 based...
                {
                  get
                    // ^ requires rowId <= this.NumberOfRows;
                  {
                    int rowOffset = (int)(rowId - 1) * $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInStaticMethod()
        => VerifyAbsenceAsync(
            """
            class C {
                static void Goo() { int i = $$ }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInStaticProperty()
        => VerifyAbsenceAsync(
            """
            class C {
                static int Goo { get { int i = $$ }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestInInstanceProperty()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo { get { int i = $$ }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInStaticConstructor()
        => VerifyAbsenceAsync(
            """
            class C {
                static C() { int i = $$ }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestInInstanceConstructor()
        => VerifyKeywordAsync(
            """
            class C {
                public C() { int i = $$ }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInEnumMemberInitializer1()
        => VerifyAbsenceAsync(
            """
            enum E {
                a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539334")]
    public Task TestNotAfterPartialInType()
        => VerifyAbsenceAsync(
            """
            class C
            {
                partial $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540476")]
    public Task TestNotAfterIncompleteTypeName()
        => VerifyAbsenceAsync(
            """
            class C
            {
                Goo.$$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541712")]
    public Task TestNotInStaticMethodContext()
        => VerifyAbsenceAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    $$
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task TestNotInObjectInitializerMemberContext()
        => VerifyAbsenceAsync("""
            class C
            {
                public int x, y;
                void M()
                {
                    var c = new C { x = 2, y = 3, $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
    public Task TestInExpressionBodiedMembersProperty()
        => VerifyKeywordAsync("""
            class C
            {
                int x;
                int M => $$
                int p;
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
    public Task TestInExpressionBodiedMembersMethod()
        => VerifyKeywordAsync("""
            class C
            {
                int x;
                int give() => $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
    public Task TestInExpressionBodiedMembersIndexer()
        => VerifyKeywordAsync("""
            class C
            {
                int x;
                public object this[int i] => $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
    public Task TestNotInExpressionBodiedMembers_Static()
        => VerifyAbsenceAsync("""
            class C
            {
                int x;
                static int M => $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
    public Task TestNotInExpressionBodiedMembersOperator()
        => VerifyAbsenceAsync("""
            class C
            {
                int x;
                public static C operator - (C c1) => $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
    public Task TestNotInExpressionBodiedMembersConversionOperator()
        => VerifyAbsenceAsync("""
            class F
            {
            }

            class C
            {
                int x;
                public static explicit operator F(C c1) => $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
    public Task TestOutsideExpressionBodiedMember()
        => VerifyAbsenceAsync("""
            class C
            {
                int x;
                int M => this.x;$$
                int p;
            }
            """);

    [Fact]
    public Task Preselection()
        => VerifyKeywordAsync("""
            class Program
            {
                void Main(string[] args)
                {
                    Helper($$)
                }
                void Helper(Program x) { }
            }
            """);

    [Fact]
    public async Task TestExtensionMethods_FirstParameter_AfterRefKeyword_InClass()
    {
        await VerifyKeywordAsync("""
            public static class Extensions
            {
                public static void Extension(ref $$
            """);

        await VerifyKeywordAsync("""
            public static class Extensions
            {
                public static void Extension(ref $$ object obj, int x) { }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestExtensionMethods_FirstParameter_AfterInKeyword_InClass([CombinatorialValues("in", "ref readonly")] string modifier)
    {
        await VerifyKeywordAsync($$"""
            public static class Extensions
            {
                public static void Extension({{modifier}} $$
            """);

        await VerifyKeywordAsync($$"""
            public static class Extensions
            {
                public static void Extension({{modifier}} $$ object obj, int x) { }
            }
            """);
    }

    [Fact]
    public async Task TestExtensionMethods_FirstParameter_AfterOutKeyword_InClass()
    {
        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public static void Extension(out $$
            """);

        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public static void Extension(out $$ object obj, int x) { }
            }
            """);
    }

    [Fact]
    public async Task TestExtensionMethods_SecondParameter_AfterRefKeyword_InClass()
    {
        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public static void Extension(int x, ref $$
            """);

        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public static void Extension(int x, ref $$ object obj) { }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestExtensionMethods_SecondParameter_AfterInKeyword_InClass([CombinatorialValues("in", "ref readonly")] string modifier)
    {
        await VerifyAbsenceAsync($$"""
            public static class Extensions
            {
                public static void Extension(int x, {{modifier}} $$
            """);

        await VerifyAbsenceAsync($$"""
            public static class Extensions
            {
                public static void Extension(int x, {{modifier}} $$ object obj) { }
            }
            """);
    }

    [Fact]
    public async Task TestExtensionMethods_SecondParameter_AfterOutKeyword_InClass()
    {
        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public static void Extension(int x, out $$
            """);

        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public static void Extension(int x, out $$ object obj) { }
            }
            """);
    }

    [Fact]
    public async Task TestExtensionMethods_FirstParameter_AfterRefKeyword_OutsideClass()
    {
        await VerifyAbsenceAsync("public static void Extension(ref $$");

        await VerifyAbsenceAsync("public static void Extension(ref $$ object obj, int x) { }");
    }

    [Theory, CombinatorialData]
    public async Task TestExtensionMethods_FirstParameter_AfterInKeyword_OutsideClass([CombinatorialValues("in", "ref readonly")] string modifier)
    {
        await VerifyAbsenceAsync($"public static void Extension({modifier} $$");

        await VerifyAbsenceAsync($"public static void Extension({modifier} $$ object obj, int x) {{ }}");
    }

    [Fact]
    public async Task TestExtensionMethods_FirstParameter_AfterOutKeyword_OutsideClass()
    {
        await VerifyAbsenceAsync("public static void Extension(out $$");

        await VerifyAbsenceAsync("public static void Extension(out $$ object obj, int x) { }");
    }

    [Fact]
    public async Task TestExtensionMethods_FirstParameter_AfterRefKeyword_NonStaticClass()
    {
        await VerifyAbsenceAsync("""
            public class Extensions
            {
                public static void Extension(ref $$
            """);

        await VerifyAbsenceAsync("""
            public class Extensions
            {
                public static void Extension(ref $$ object obj, int x) { }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestExtensionMethods_FirstParameter_AfterInKeyword_NonStaticClass([CombinatorialValues("in", "ref readonly")] string modifier)
    {
        await VerifyAbsenceAsync($$"""
            public class Extensions
            {
                public static void Extension({{modifier}} $$
            """);

        await VerifyAbsenceAsync($$"""
            public class Extensions
            {
                public static void Extension({{modifier}} $$ object obj, int x) { }
            }
            """);
    }

    [Fact]
    public async Task TestExtensionMethods_FirstParameter_AfterOutKeyword_NonStaticClass()
    {
        await VerifyAbsenceAsync("""
            public class Extensions
            {
                public static void Extension(out $$
            """);

        await VerifyAbsenceAsync("""
            public class Extensions
            {
                public static void Extension(out $$ object obj, int x) { }
            }
            """);
    }

    [Fact]
    public async Task TestExtensionMethods_FirstParameter_AfterRefKeyword_NonStaticMethod()
    {
        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public void Extension(ref $$
            """);

        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public void Extension(ref $$ object obj, int x) { }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestExtensionMethods_FirstParameter_AfterInKeyword_NonStaticMethod([CombinatorialValues("in", "ref readonly")] string modifier)
    {
        await VerifyAbsenceAsync($$"""
            public static class Extensions
            {
                public void Extension({{modifier}} $$
            """);

        await VerifyAbsenceAsync($$"""
            public static class Extensions
            {
                public void Extension({{modifier}} $$ object obj, int x) { }
            }
            """);
    }

    [Fact]
    public async Task TestExtensionMethods_FirstParameter_AfterOutKeyword_NonStaticMethod()
    {
        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public void Extension(out $$
            """);

        await VerifyAbsenceAsync("""
            public static class Extensions
            {
                public void Extension(out $$ object obj, int x) { }
            }
            """);
    }

    [Fact]
    public Task TestAfterRefExpression()
        => VerifyKeywordAsync(AddInsideMethod(
            @"ref int x = ref $$"));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/78979")]
    public Task TestInsideNameofInAttribute(bool isStatic)
        => VerifyKeywordAsync($$"""
            public class Example
            {
                private string _field;

                [MemberNotNull(nameof($$))]
                public {{(isStatic ? "static " : " ")}}void Method()
                {
                }
            }
            """);

    #region Collection expressions

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [$$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [new object(), $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. ($$
            }
            """);

    #endregion
}
