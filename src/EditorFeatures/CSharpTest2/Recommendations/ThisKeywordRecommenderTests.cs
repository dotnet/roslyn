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
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            VerifyAbsence(
@"interface IGoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"interface IGoo<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"interface IGoo<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAngle()
        {
            VerifyAbsence(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"delegate void D<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"delegate void D<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotThisBaseListAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInGenericMethod()
        {
            VerifyAbsence(
@"interface IGoo {
    void Goo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRef()
        {
            VerifyAbsence(
@"class C {
    void Goo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIn()
        {
            VerifyAbsence(
@"class C {
    void Goo(in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterThis_InBogusMethod()
        {
            VerifyAbsence(
@"class C {
    void Goo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOut()
        {
            VerifyAbsence(
@"class C {
    void Goo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMethodOpenParen()
        {
            VerifyAbsence(
@"class C {
    void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMethodComma()
        {
            VerifyAbsence(
@"class C {
    void Goo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMethodAttribute()
        {
            VerifyAbsence(
@"class C {
    void Goo(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConstructorOpenParen()
        {
            VerifyAbsence(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConstructorComma()
        {
            VerifyAbsence(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConstructorAttribute()
        {
            VerifyAbsence(
@"class C {
    public C(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateOpenParen()
        {
            VerifyAbsence(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateComma()
        {
            VerifyAbsence(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateAttribute()
        {
            VerifyAbsence(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOperator()
        {
            VerifyAbsence(
@"class C {
    static int operator +($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDestructor()
        {
            VerifyAbsence(
@"class C {
    ~C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIndexer()
        {
            VerifyAbsence(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInstanceMethodInInstanceClass()
        {
            VerifyAbsence(
@"class C {
    int Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticMethodInInstanceClass()
        {
            VerifyAbsence(
@"class C {
    static int Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInstanceMethodInStaticClass()
        {
            VerifyAbsence(
@"static class C {
    int Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInStaticMethodInStaticClass()
        {
            VerifyKeyword(
@"static class C {
    static int Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27028, "https://github.com/dotnet/roslyn/issues/27028")]
        public async Task TestInLocalFunction()
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
        public async Task TestInNestedLocalFunction()
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
        public async Task TestInLocalFunctionInStaticMethod()
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
        public async Task TestInNestedLocalFunctionInStaticMethod()
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
        public async Task TestInStaticLocalFunction()
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
        public async Task TestInNestedInStaticLocalFunction()
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
        public async Task TestInAnonymousMethod()
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
        public async Task TestInNestedAnonymousMethod()
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
        public async Task TestInAnonymousMethodInStaticMethod()
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
        public async Task TestInNestedAnonymousMethodInStaticMethod()
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
        public async Task TestInLambdaExpression()
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
        public async Task TestInNestedLambdaExpression()
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
        public async Task TestInLambdaExpressionInStaticMethod()
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
        public async Task TestInNestedLambdaExpressionInStaticMethod()
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
        public async Task TestInNestedLambdaExpressionInAnonymousMethod()
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
        public async Task TestInNestedAnonymousInLambdaExpression()
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
        public async Task TestInNestedAnonymousMethodInLambdaExpressionInStaticMethod()
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
        public async Task TestInNestedLambdaExpressionInAnonymousMethodInStaticMethod()
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
        public async Task TestInAnonymousMethodInAProperty()
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
        public async Task TestInAnonymousMethodInAPropertyInitializer()
        {
            VerifyKeyword(
@"class C
{
    Action B { get; } = delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAExpressionProperty()
        {
            VerifyKeyword(
@"class C
{
    Action A => delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAFieldInitializer()
        {
            VerifyKeyword(
@"class C
{
    Action A = delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAStaticProperty()
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
        public async Task TestInAnonymousMethodInAStaticPropertyInitializer()
        {
            VerifyAbsence(
@"class C
{
    static Action B { get; } = delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAStaticExpressionProperty()
        {
            VerifyAbsence(
@"class C
{
    static Action A => delegate { $$ }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(27923, "https://github.com/dotnet/roslyn/issues/27923")]
        public async Task TestInAnonymousMethodInAStaticFieldInitializer()
        {
            VerifyAbsence(
@"class C
{
    static Action A = delegate { $$ }
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAttribute()
        {
            VerifyKeyword(
@"static class C {
    static int Goo([Bar]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSecondAttribute()
        {
            VerifyAbsence(
@"static class C {
    static int Goo(this int i, [Bar]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterThis()
        {
            VerifyAbsence(
@"static class C {
    static int Goo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterFirstParameter()
        {
            VerifyAbsence(
@"static class C {
    static int Goo(this int a, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInClassConstructorInitializer()
        {
            VerifyKeyword(
@"class C {
    public C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticClassConstructorInitializer()
        {
            VerifyAbsence(
@"class C {
    static C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInStructConstructorInitializer()
        {
            VerifyKeyword(
@"struct C {
    public C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCast()
        {
            VerifyKeyword(AddInsideMethod(
@"stack.Push(((IEnumerable<Segment>)((TreeSegment)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReturn()
        {
            VerifyKeyword(AddInsideMethod(
@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexer()
        {
            VerifyKeyword(AddInsideMethod(
@"return this.items[$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSimpleCast()
        {
            VerifyKeyword(AddInsideMethod(
@"return ((IEnumerable<T>)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            VerifyAbsence(
@"class C {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVoid()
        {
            VerifyAbsence(
@"class C {
    void $$");
        }

        [WorkItem(542636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterType()
        {
            VerifyAbsence(
@"class C {
    int $$");
        }

        [WorkItem(542636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeArray()
        {
            VerifyAbsence(
@"class C {
    internal byte[] $$");
        }

        [WorkItem(542636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeArrayBeforeArguments()
        {
            VerifyAbsence(
@"class C {
    internal byte[] $$[int i] { get; }");
        }

        [WorkItem(542636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542636")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeBeforeArguments()
        {
            VerifyAbsence(
@"class C {
    internal byte $$[int i] { get; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMultiply()
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
        public async Task TestNotInStaticMethod()
        {
            VerifyAbsence(
@"class C {
    static void Goo() { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticProperty()
        {
            VerifyAbsence(
@"class C {
    static int Goo { get { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInstanceProperty()
        {
            VerifyKeyword(
@"class C {
    int Goo { get { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticConstructor()
        {
            VerifyAbsence(
@"class C {
    static C() { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInstanceConstructor()
        {
            VerifyKeyword(
@"class C {
    public C() { int i = $$ }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEnumMemberInitializer1()
        {
            VerifyAbsence(
@"enum E {
    a = $$
}");
        }

        [WorkItem(539334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539334")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartialInType()
        {
            VerifyAbsence(
@"class C
{
    partial $$
}");
        }

        [WorkItem(540476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540476")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIncompleteTypeName()
        {
            VerifyAbsence(
@"class C
{
    Goo.$$
}");
        }

        [WorkItem(541712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541712")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticMethodContext()
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
        public async Task TestNotInObjectInitializerMemberContext()
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
        public async Task TestInExpressionBodiedMembersProperty()
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
        public async Task TestInExpressionBodiedMembersMethod()
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
        public async Task TestInExpressionBodiedMembersIndexer()
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
        public async Task TestNotInExpressionBodiedMembers_Static()
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
        public async Task TestNotInExpressionBodiedMembersOperator()
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
        public async Task TestNotInExpressionBodiedMembersConversionOperator()
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
        public async Task TestOutsideExpressionBodiedMember()
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
        public async Task Preselection()
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
        public async Task TestExtensionMethods_FirstParameter_AfterRefKeyword_InClass()
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
        public async Task TestExtensionMethods_FirstParameter_AfterInKeyword_InClass()
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
        public async Task TestExtensionMethods_FirstParameter_AfterOutKeyword_InClass()
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
        public async Task TestExtensionMethods_SecondParameter_AfterRefKeyword_InClass()
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
        public async Task TestExtensionMethods_SecondParameter_AfterInKeyword_InClass()
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
        public async Task TestExtensionMethods_SecondParameter_AfterOutKeyword_InClass()
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
        public async Task TestExtensionMethods_FirstParameter_AfterRefKeyword_OutsideClass()
        {
            VerifyAbsence("public static void Extension(ref $$");

            VerifyAbsence("public static void Extension(ref $$ object obj, int x) { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterInKeyword_OutsideClass()
        {
            VerifyAbsence("public static void Extension(in $$");

            VerifyAbsence("public static void Extension(in $$ object obj, int x) { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterOutKeyword_OutsideClass()
        {
            VerifyAbsence("public static void Extension(out $$");

            VerifyAbsence("public static void Extension(out $$ object obj, int x) { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterRefKeyword_NonStaticClass()
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
        public async Task TestExtensionMethods_FirstParameter_AfterInKeyword_NonStaticClass()
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
        public async Task TestExtensionMethods_FirstParameter_AfterOutKeyword_NonStaticClass()
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
        public async Task TestExtensionMethods_FirstParameter_AfterRefKeyword_NonStaticMethod()
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
        public async Task TestExtensionMethods_FirstParameter_AfterInKeyword_NonStaticMethod()
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
        public async Task TestExtensionMethods_FirstParameter_AfterOutKeyword_NonStaticMethod()
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
        public async Task TestAfterRefExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"ref int x = ref $$"));
        }
    }
}
