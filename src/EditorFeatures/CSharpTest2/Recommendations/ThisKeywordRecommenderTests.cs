// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ThisKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact]
        public async Task TestNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<$$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<in $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<Goo, $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<[Goo]$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"delegate void D<$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"delegate void D<Goo, $$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"delegate void D<[Goo]$$");
        }

        [Fact]
        public async Task TestNotThisBaseListAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo : Bar<$$");
        }

        [Fact]
        public async Task TestNotInGenericMethod()
        {
            await VerifyAbsenceAsync(
                """
                interface IGoo {
                    void Goo<$$
                """);
        }

        [Fact]
        public async Task TestNotAfterRef()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(ref $$
                """);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterIn([CombinatorialValues("in", "ref readonly")] string modifier)
        {
            await VerifyAbsenceAsync($$"""
                class C {
                    void Goo({{modifier}} $$
                """);
        }

        [Fact]
        public async Task TestNotAfterThis_InBogusMethod()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(this $$
                """);
        }

        [Fact]
        public async Task TestNotAfterOut()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(out $$
                """);
        }

        [Fact]
        public async Task TestNotAfterMethodOpenParen()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo($$
                """);
        }

        [Fact]
        public async Task TestNotAfterMethodComma()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(int i, $$
                """);
        }

        [Fact]
        public async Task TestNotAfterMethodAttribute()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(int i, [Goo]$$
                """);
        }

        [Fact]
        public async Task TestNotAfterConstructorOpenParen()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    public C($$
                """);
        }

        [Fact]
        public async Task TestNotAfterConstructorComma()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    public C(int i, $$
                """);
        }

        [Fact]
        public async Task TestNotAfterConstructorAttribute()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    public C(int i, [Goo]$$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegateOpenParen()
        {
            await VerifyAbsenceAsync(
@"delegate void D($$");
        }

        [Fact]
        public async Task TestNotAfterDelegateComma()
        {
            await VerifyAbsenceAsync(
@"delegate void D(int i, $$");
        }

        [Fact]
        public async Task TestNotAfterDelegateAttribute()
        {
            await VerifyAbsenceAsync(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact]
        public async Task TestNotAfterOperator()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static int operator +($$
                """);
        }

        [Fact]
        public async Task TestNotAfterDestructor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    ~C($$
                """);
        }

        [Fact]
        public async Task TestNotAfterIndexer()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int this[$$
                """);
        }

        [Fact]
        public async Task TestNotInInstanceMethodInInstanceClass()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo($$
                """);
        }

        [Fact]
        public async Task TestNotInStaticMethodInInstanceClass()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static int Goo($$
                """);
        }

        [Fact]
        public async Task TestNotInInstanceMethodInStaticClass()
        {
            await VerifyAbsenceAsync(
                """
                static class C {
                    int Goo($$
                """);
        }

        [Fact]
        public async Task TestInStaticMethodInStaticClass()
        {
            await VerifyKeywordAsync(
                """
                static class C {
                    static int Goo($$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27028")]
        public async Task TestInLocalFunction()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27028")]
        public async Task TestInNestedLocalFunction()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27028")]
        public async Task TestInLocalFunctionInStaticMethod()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27028")]
        public async Task TestInNestedLocalFunctionInStaticMethod()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35644")]
        public async Task TestInStaticLocalFunction()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35644")]
        public async Task TestInNestedInStaticLocalFunction()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethod()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInNestedAnonymousMethod()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInStaticMethod()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInNestedAnonymousMethodInStaticMethod()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInLambdaExpression()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInNestedLambdaExpression()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInLambdaExpressionInStaticMethod()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInNestedLambdaExpressionInStaticMethod()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInNestedLambdaExpressionInAnonymousMethod()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInNestedAnonymousInLambdaExpression()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInNestedAnonymousMethodInLambdaExpressionInStaticMethod()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInNestedLambdaExpressionInAnonymousMethodInStaticMethod()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAProperty()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    Action A 
                    { 
                        get { return delegate { $$ } }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAPropertyInitializer()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    Action B { get; } = delegate { $$ }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAExpressionProperty()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    Action A => delegate { $$ }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAFieldInitializer()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    Action A = delegate { $$ }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAStaticProperty()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    static Action A
                    {
                        get { return delegate { $$ } }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAStaticPropertyInitializer()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    static Action B { get; } = delegate { $$ }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAStaticExpressionProperty()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    static Action A => delegate { $$ }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAStaticFieldInitializer()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    static Action A = delegate { $$ }
                }
                """);
        }
        [Fact]
        public async Task TestAfterAttribute()
        {
            await VerifyKeywordAsync(
                """
                static class C {
                    static int Goo([Bar]$$
                """);
        }

        [Fact]
        public async Task TestNotAfterSecondAttribute()
        {
            await VerifyAbsenceAsync(
                """
                static class C {
                    static int Goo(this int i, [Bar]$$
                """);
        }

        [Fact]
        public async Task TestNotAfterThis()
        {
            await VerifyAbsenceAsync(
                """
                static class C {
                    static int Goo(this $$
                """);
        }

        [Fact]
        public async Task TestNotAfterFirstParameter()
        {
            await VerifyAbsenceAsync(
                """
                static class C {
                    static int Goo(this int a, $$
                """);
        }

        [Fact]
        public async Task TestInClassConstructorInitializer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C() : $$
                """);
        }

        [Fact]
        public async Task TestNotInStaticClassConstructorInitializer()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static C() : $$
                """);
        }

        [Fact]
        public async Task TestInStructConstructorInitializer()
        {
            await VerifyKeywordAsync(
                """
                struct C {
                    public C() : $$
                """);
        }

        [Fact]
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAfterCast()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"stack.Push(((IEnumerable<Segment>)((TreeSegment)$$"));
        }

        [Fact]
        public async Task TestAfterReturn()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return $$"));
        }

        [Fact]
        public async Task TestAfterIndexer()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return this.items[$$"));
        }

        [Fact]
        public async Task TestAfterSimpleCast()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return ((IEnumerable<T>)$$"));
        }

        [Fact]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    $$
                """);
        }

        [Fact]
        public async Task TestNotAfterVoid()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        public async Task TestAfterType()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        public async Task TestAfterTypeArray()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    internal byte[] $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        public async Task TestAfterTypeArrayBeforeArguments()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    internal byte[] $$[int i] { get; }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        public async Task TestAfterTypeBeforeArguments()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    internal byte $$[int i] { get; }
                """);
        }

        [Fact]
        public async Task TestAfterMultiply()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    internal CustomAttributeRow this[uint rowId] //  This is 1 based...
                    {
                      get
                        // ^ requires rowId <= this.NumberOfRows;
                      {
                        int rowOffset = (int)(rowId - 1) * $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestNotInStaticMethod()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static void Goo() { int i = $$ }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestNotInStaticProperty()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static int Goo { get { int i = $$ }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestInInstanceProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int Goo { get { int i = $$ }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestNotInStaticConstructor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static C() { int i = $$ }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestInInstanceConstructor()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C() { int i = $$ }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestNotInEnumMemberInitializer1()
        {
            await VerifyAbsenceAsync(
                """
                enum E {
                    a = $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539334")]
        public async Task TestNotAfterPartialInType()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    partial $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540476")]
        public async Task TestNotAfterIncompleteTypeName()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    Goo.$$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541712")]
        public async Task TestNotInStaticMethodContext()
        {
            await VerifyAbsenceAsync(
                """
                class Program
                {
                    static void Main(string[] args)
                    {
                        $$
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    public int x, y;
                    void M()
                    {
                        var c = new C { x = 2, y = 3, $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
        public async Task TestInExpressionBodiedMembersProperty()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    int x;
                    int M => $$
                    int p;
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
        public async Task TestInExpressionBodiedMembersMethod()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    int x;
                    int give() => $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
        public async Task TestInExpressionBodiedMembersIndexer()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    int x;
                    public object this[int i] => $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
        public async Task TestNotInExpressionBodiedMembers_Static()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    int x;
                    static int M => $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
        public async Task TestNotInExpressionBodiedMembersOperator()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    int x;
                    public static C operator - (C c1) => $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
        public async Task TestNotInExpressionBodiedMembersConversionOperator()
        {
            await VerifyAbsenceAsync("""
                class F
                {
                }

                class C
                {
                    int x;
                    public static explicit operator F(C c1) => $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/725")]
        public async Task TestOutsideExpressionBodiedMember()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    int x;
                    int M => this.x;$$
                    int p;
                }
                """);
        }

        [Fact]
        public async Task Preselection()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    void Main(string[] args)
                    {
                        Helper($$)
                    }
                    void Helper(Program x) { }
                }
                """);
        }

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
        public async Task TestAfterRefExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact]
        public async Task TestNotInExtensionForType()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E for $$
                """);
        }

        #region Collection expressions

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [$$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [new object(), $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. ($$
                }
                """);
        }

        #endregion
    }
}
