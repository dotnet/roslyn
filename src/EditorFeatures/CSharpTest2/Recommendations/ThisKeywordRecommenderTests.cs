// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ThisKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInterfaceTypeVarianceNotAfterIn()
        {
            VerifyAbsence(
@"interface IGoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInterfaceTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"interface IGoo<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInterfaceTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"interface IGoo<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestDelegateTypeVarianceNotAfterAngle()
        {
            VerifyAbsence(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestDelegateTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"delegate void D<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestDelegateTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"delegate void D<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotThisBaseListAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInGenericMethod()
        {
            VerifyAbsence(
@"interface IGoo {
    void Goo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterRef()
        {
            VerifyAbsence(
@"class C {
    void Goo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterIn()
        {
            VerifyAbsence(
@"class C {
    void Goo(in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterThis_InBogusMethod()
        {
            VerifyAbsence(
@"class C {
    void Goo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterOut()
        {
            VerifyAbsence(
@"class C {
    void Goo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterMethodOpenParen()
        {
            VerifyAbsence(
@"class C {
    void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterMethodComma()
        {
            VerifyAbsence(
@"class C {
    void Goo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterMethodAttribute()
        {
            VerifyAbsence(
@"class C {
    void Goo(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterConstructorOpenParen()
        {
            VerifyAbsence(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterConstructorComma()
        {
            VerifyAbsence(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterConstructorAttribute()
        {
            VerifyAbsence(
@"class C {
    public C(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDelegateOpenParen()
        {
            VerifyAbsence(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDelegateComma()
        {
            VerifyAbsence(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDelegateAttribute()
        {
            VerifyAbsence(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterOperator()
        {
            VerifyAbsence(
@"class C {
    static int operator +($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDestructor()
        {
            VerifyAbsence(
@"class C {
    ~C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterIndexer()
        {
            VerifyAbsence(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInInstanceMethodInInstanceClass()
        {
            VerifyAbsence(
@"class C {
    int Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInStaticMethodInInstanceClass()
        {
            VerifyAbsence(
@"class C {
    static int Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInInstanceMethodInStaticClass()
        {
            VerifyAbsence(
@"static class C {
    int Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInStaticMethodInStaticClass()
        {
            VerifyKeyword(
@"static class C {
    static int Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27028, "https://github.com/dotnet/roslyn/issues/27028")]
        public void TestInLocalFunction()
        {
            VerifyKeyword(
@"class C
{
    int Method()
    {
        void local()
        {
            $$
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27028, "https://github.com/dotnet/roslyn/issues/27028")]
        public void TestInNestedLocalFunction()
        {
            VerifyKeyword(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27028, "https://github.com/dotnet/roslyn/issues/27028")]
        public void TestInLocalFunctionInStaticMethod()
        {
            VerifyAbsence(
@"class C {
    static int Method()
    {
        void local()
        {
            $$
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27028, "https://github.com/dotnet/roslyn/issues/27028")]
        public void TestInNestedLocalFunctionInStaticMethod()
        {
            VerifyAbsence(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(35644, "https://github.com/dotnet/roslyn/issues/35644")]
        public void TestInStaticLocalFunction()
        {
            VerifyAbsence(
@"class C {
    int Method()
    {
        static void local()
        {
            $$
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(35644, "https://github.com/dotnet/roslyn/issues/35644")]
        public void TestInNestedInStaticLocalFunction()
        {
            VerifyAbsence(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethod()
        {
            VerifyKeyword(
@"class C
{
    int Method()
    {
        Action a = delegate
        {
            $$
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInNestedAnonymousMethod()
        {
            VerifyKeyword(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInStaticMethod()
        {
            VerifyAbsence(
@"class C
{
    static int Method()
    {
        Action a = delegate
        {
            $$
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInNestedAnonymousMethodInStaticMethod()
        {
            VerifyAbsence(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInLambdaExpression()
        {
            VerifyKeyword(
@"class C
{
    int Method()
    {
        Action a = () =>
        {
            $$
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInNestedLambdaExpression()
        {
            VerifyKeyword(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInLambdaExpressionInStaticMethod()
        {
            VerifyAbsence(
@"class C
{
    static int Method()
    {
        Action a = () =>
        {
            $$
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInNestedLambdaExpressionInStaticMethod()
        {
            VerifyAbsence(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInNestedLambdaExpressionInAnonymousMethod()
        {
            VerifyKeyword(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInNestedAnonymousInLambdaExpression()
        {
            VerifyKeyword(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInNestedAnonymousMethodInLambdaExpressionInStaticMethod()
        {
            VerifyAbsence(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInNestedLambdaExpressionInAnonymousMethodInStaticMethod()
        {
            VerifyAbsence(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInAProperty()
        {
            VerifyKeyword(
@"class C
{
    Action A 
    { 
        get { return delegate { $$ } }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInAPropertyInitializer()
        {
            VerifyKeyword(
@"class C
{
    Action B { get; } = delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInAExpressionProperty()
        {
            VerifyKeyword(
@"class C
{
    Action A => delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInAFieldInitializer()
        {
            VerifyKeyword(
@"class C
{
    Action A = delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInAStaticProperty()
        {
            VerifyAbsence(
@"class C
{
    static Action A
    {
        get { return delegate { $$ } }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInAStaticPropertyInitializer()
        {
            VerifyAbsence(
@"class C
{
    static Action B { get; } = delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInAStaticExpressionProperty()
        {
            VerifyAbsence(
@"class C
{
    static Action A => delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public void TestInAnonymousMethodInAStaticFieldInitializer()
        {
            VerifyAbsence(
@"class C
{
    static Action A = delegate { $$ }
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAttribute()
        {
            VerifyKeyword(
@"static class C {
    static int Goo([Bar]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterSecondAttribute()
        {
            VerifyAbsence(
@"static class C {
    static int Goo(this int i, [Bar]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterThis()
        {
            VerifyAbsence(
@"static class C {
    static int Goo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterFirstParameter()
        {
            VerifyAbsence(
@"static class C {
    static int Goo(this int a, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInClassConstructorInitializer()
        {
            VerifyKeyword(
@"class C {
    public C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInStaticClassConstructorInitializer()
        {
            VerifyAbsence(
@"class C {
    static C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInStructConstructorInitializer()
        {
            VerifyKeyword(
@"struct C {
    public C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterCast()
        {
            VerifyKeyword(AddInsideMethod(
@"stack.Push(((IEnumerable<Segment>)((TreeSegment)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterReturn()
        {
            VerifyKeyword(AddInsideMethod(
@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexer()
        {
            VerifyKeyword(AddInsideMethod(
@"return this.items[$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterSimpleCast()
        {
            VerifyKeyword(AddInsideMethod(
@"return ((IEnumerable<T>)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInClass()
        {
            VerifyAbsence(
@"class C {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterVoid()
        {
            VerifyAbsence(
@"class C {
    void $$");
        }

        [WorkItem(542636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterType()
        {
            VerifyAbsence(
@"class C {
    int $$");
        }

        [WorkItem(542636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeArray()
        {
            VerifyAbsence(
@"class C {
    internal byte[] $$");
        }

        [WorkItem(542636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeArrayBeforeArguments()
        {
            VerifyAbsence(
@"class C {
    internal byte[] $$[int i] { get; }");
        }

        [WorkItem(542636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeBeforeArguments()
        {
            VerifyAbsence(
@"class C {
    internal byte $$[int i] { get; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMultiply()
        {
            VerifyKeyword(
@"class C {
    internal CustomAttributeRow this[uint rowId] //  This is 1 based...
    {
      get
        // ^ requires rowId <= this.NumberOfRows;
      {
        int rowOffset = (int)(rowId - 1) * $$");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInStaticMethod()
        {
            VerifyAbsence(
@"class C {
    static void Goo() { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInStaticProperty()
        {
            VerifyAbsence(
@"class C {
    static int Goo { get { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInInstanceProperty()
        {
            VerifyKeyword(
@"class C {
    int Goo { get { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInStaticConstructor()
        {
            VerifyAbsence(
@"class C {
    static C() { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInInstanceConstructor()
        {
            VerifyKeyword(
@"class C {
    public C() { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEnumMemberInitializer1()
        {
            VerifyAbsence(
@"enum E {
    a = $$
}");
        }

        [WorkItem(539334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539334")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPartialInType()
        {
            VerifyAbsence(
@"class C
{
    partial $$
}");
        }

        [WorkItem(540476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540476")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterIncompleteTypeName()
        {
            VerifyAbsence(
@"class C
{
    Goo.$$
}");
        }

        [WorkItem(541712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541712")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInStaticMethodContext()
        {
            VerifyAbsence(
@"class Program
{
    static void Main(string[] args)
    {
        $$
    }
}");
        }

        [WorkItem(544219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInObjectInitializerMemberContext()
        {
            VerifyAbsence(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInExpressionBodiedMembersProperty()
        {
            VerifyKeyword(@"
class C
{
    int x;
    int M => $$
    int p;
}");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInExpressionBodiedMembersMethod()
        {
            VerifyKeyword(@"
class C
{
    int x;
    int give() => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInExpressionBodiedMembersIndexer()
        {
            VerifyKeyword(@"
class C
{
    int x;
    public object this[int i] => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInExpressionBodiedMembers_Static()
        {
            VerifyAbsence(@"
class C
{
    int x;
    static int M => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInExpressionBodiedMembersOperator()
        {
            VerifyAbsence(@"
class C
{
    int x;
    public static C operator - (C c1) => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInExpressionBodiedMembersConversionOperator()
        {
            VerifyAbsence(@"
class F
{
}

class C
{
    int x;
    public static explicit operator F(C c1) => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107414")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOutsideExpressionBodiedMember()
        {
            VerifyAbsence(@"
class C
{
    int x;
    int M => this.x;$$
    int p;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void Preselection()
        {
            VerifyKeyword(@"
class Program
{
    void Main(string[] args)
    {
        Helper($$)
    }
    void Helper(Program x) { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterRefKeyword_InClass()
        {
            VerifyKeyword(@"
public static class Extensions
{
    public static void Extension(ref $$");

            VerifyKeyword(@"
public static class Extensions
{
    public static void Extension(ref $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterInKeyword_InClass()
        {
            VerifyKeyword(@"
public static class Extensions
{
    public static void Extension(in $$");

            VerifyKeyword(@"
public static class Extensions
{
    public static void Extension(in $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterOutKeyword_InClass()
        {
            VerifyAbsence(@"
public static class Extensions
{
    public static void Extension(out $$");

            VerifyAbsence(@"
public static class Extensions
{
    public static void Extension(out $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter_AfterRefKeyword_InClass()
        {
            VerifyAbsence(@"
public static class Extensions
{
    public static void Extension(int x, ref $$");

            VerifyAbsence(@"
public static class Extensions
{
    public static void Extension(int x, ref $$ object obj) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter_AfterInKeyword_InClass()
        {
            VerifyAbsence(@"
public static class Extensions
{
    public static void Extension(int x, in $$");

            VerifyAbsence(@"
public static class Extensions
{
    public static void Extension(int x, in $$ object obj) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter_AfterOutKeyword_InClass()
        {
            VerifyAbsence(@"
public static class Extensions
{
    public static void Extension(int x, out $$");

            VerifyAbsence(@"
public static class Extensions
{
    public static void Extension(int x, out $$ object obj) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterRefKeyword_OutsideClass()
        {
            VerifyAbsence("public static void Extension(ref $$");

            VerifyAbsence("public static void Extension(ref $$ object obj, int x) { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterInKeyword_OutsideClass()
        {
            VerifyAbsence("public static void Extension(in $$");

            VerifyAbsence("public static void Extension(in $$ object obj, int x) { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterOutKeyword_OutsideClass()
        {
            VerifyAbsence("public static void Extension(out $$");

            VerifyAbsence("public static void Extension(out $$ object obj, int x) { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterRefKeyword_NonStaticClass()
        {
            VerifyAbsence(@"
public class Extensions
{
    public static void Extension(ref $$");

            VerifyAbsence(@"
public class Extensions
{
    public static void Extension(ref $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterInKeyword_NonStaticClass()
        {
            VerifyAbsence(@"
public class Extensions
{
    public static void Extension(in $$");

            VerifyAbsence(@"
public class Extensions
{
    public static void Extension(in $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterOutKeyword_NonStaticClass()
        {
            VerifyAbsence(@"
public class Extensions
{
    public static void Extension(out $$");

            VerifyAbsence(@"
public class Extensions
{
    public static void Extension(out $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterRefKeyword_NonStaticMethod()
        {
            VerifyAbsence(@"
public static class Extensions
{
    public void Extension(ref $$");

            VerifyAbsence(@"
public static class Extensions
{
    public void Extension(ref $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterInKeyword_NonStaticMethod()
        {
            VerifyAbsence(@"
public static class Extensions
{
    public void Extension(in $$");

            VerifyAbsence(@"
public static class Extensions
{
    public void Extension(in $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterOutKeyword_NonStaticMethod()
        {
            VerifyAbsence(@"
public static class Extensions
{
    public void Extension(out $$");

            VerifyAbsence(@"
public static class Extensions
{
    public void Extension(out $$ object obj, int x) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRefExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"ref int x = ref $$"));
        }
    }
}
