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
    public class FloatKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
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
        public async Task TestAfterStackAlloc()
        {
            VerifyKeyword(
@"class C {
     int* goo = stackalloc $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFixedStatement()
        {
            VerifyKeyword(
@"fixed ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDelegateReturnType()
        {
            VerifyKeyword(
@"public delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCastType()
        {
            VerifyKeyword(AddInsideMethod(
@"var str = (($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCastType2()
        {
            VerifyKeyword(AddInsideMethod(
@"var str = (($$)items) as string;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstInMemberContext()
        {
            VerifyKeyword(
@"class C {
    const $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefInMemberContext()
        {
            VerifyKeyword(
@"class C {
    ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefReadonlyInMemberContext()
        {
            VerifyKeyword(
@"class C {
    ref readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstInStatementContext()
        {
            VerifyKeyword(AddInsideMethod(
@"const $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefInStatementContext()
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefReadonlyInStatementContext()
        {
            VerifyKeyword(AddInsideMethod(
@"ref readonly $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstLocalDeclaration()
        {
            VerifyKeyword(AddInsideMethod(
@"const $$ int local;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefLocalDeclaration()
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$ int local;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefReadonlyLocalDeclaration()
        {
            VerifyKeyword(AddInsideMethod(
@"ref readonly $$ int local;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefLocalFunction()
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$ int Function();"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefReadonlyLocalFunction()
        {
            VerifyKeyword(AddInsideMethod(
@"ref readonly $$ int Function();"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEnumBaseTypes()
        {
            VerifyAbsence(
@"enum E : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInGenericType1()
        {
            VerifyKeyword(AddInsideMethod(
@"IList<$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInGenericType2()
        {
            VerifyKeyword(AddInsideMethod(
@"IList<int,$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInGenericType3()
        {
            VerifyKeyword(AddInsideMethod(
@"IList<int[],$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInGenericType4()
        {
            VerifyKeyword(AddInsideMethod(
@"IList<IGoo<int?,byte*>,$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInBaseList()
        {
            VerifyAbsence(
@"class C : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInGenericType_InBaseList()
        {
            VerifyKeyword(
@"class C : IList<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIs()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = goo is $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAs()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = goo as $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [goo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInterface()
        {
            VerifyKeyword(
@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartial()
            => VerifyAbsence(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedPartial()
        {
            VerifyAbsence(
@"class C {
    partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStaticPublic()
        {
            VerifyKeyword(
@"class C {
    static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPublicStatic()
        {
            VerifyKeyword(
@"class C {
    public static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterVirtualPublic()
        {
            VerifyKeyword(
@"class C {
    virtual public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPublic()
        {
            VerifyKeyword(
@"class C {
    public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
   private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedProtected()
        {
            VerifyKeyword(
@"class C {
    protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStatic()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodOpenParen()
        {
            VerifyKeyword(
@"class C {
    void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodComma()
        {
            VerifyKeyword(
@"class C {
    void Goo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodAttribute()
        {
            VerifyKeyword(
@"class C {
    void Goo(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorOpenParen()
        {
            VerifyKeyword(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorComma()
        {
            VerifyKeyword(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorAttribute()
        {
            VerifyKeyword(
@"class C {
    public C(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateOpenParen()
        {
            VerifyKeyword(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateComma()
        {
            VerifyKeyword(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateAttribute()
        {
            VerifyKeyword(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterThis()
        {
            VerifyKeyword(
@"static class C {
     public static void Goo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRef()
        {
            VerifyKeyword(
@"class C {
     void Goo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterOut()
        {
            VerifyKeyword(
@"class C {
     void Goo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterLambdaRef()
        {
            VerifyKeyword(
@"class C {
     void Goo() {
          System.Func<int, int> f = (ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterLambdaOut()
        {
            VerifyKeyword(
@"class C {
     void Goo() {
          System.Func<int, int> f = (out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterParams()
        {
            VerifyKeyword(
@"class C {
     void Goo(params $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInImplicitOperator()
        {
            VerifyKeyword(
@"class C {
     public static implicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInExplicitOperator()
        {
            VerifyKeyword(
@"class C {
     public static explicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerBracket()
        {
            VerifyKeyword(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerBracketComma()
        {
            VerifyKeyword(
@"class C {
    int this[int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"new $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterOut2()
        {
            VerifyKeyword(
@"class C {
    private static void RoundToFloat(double d, out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterOut3()
        {
            VerifyKeyword(
@"class C {
    private static void RoundToFloat(double d, out $$ float f)");
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInTypeOf()
        {
            VerifyKeyword(AddInsideMethod(
@"typeof($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDefault()
        {
            VerifyKeyword(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInSizeOf()
        {
            VerifyKeyword(AddInsideMethod(
@"sizeof($$"));
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

        [WorkItem(546938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546938")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCrefContext()
        {
            VerifyKeyword(@"
class Program
{
    /// <see cref=""$$"">
    static void Main(string[] args)
    {
        
    }
}");
        }

        [WorkItem(546955, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546955")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCrefContextNotAfterDot()
        {
            VerifyAbsence(@"
/// <see cref=""System.$$"" />
class C { }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAsync()
            => VerifyKeyword(@"class c { async $$ }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAsyncAsType()
            => VerifyAbsence(@"class c { async async $$ }");

        [WorkItem(1468, "https://github.com/dotnet/roslyn/issues/1468")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInCrefTypeParameter()
        {
            VerifyAbsence(@"
using System;
/// <see cref=""List{$$}"" />
class C { }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task Preselection()
        {
            VerifyKeyword(@"
class Program
{
    static void Main(string[] args)
    {
        Helper($$)
    }
    static void Helper(float x) { }
}
");
        }

        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInTupleWithinType()
        {
            VerifyKeyword(@"
class Program
{
    ($$
}");
        }

        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInTupleWithinMember()
        {
            VerifyKeyword(@"
class Program
{
    void Method()
    {
        ($$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerType()
        {
            VerifyKeyword(@"
class C
{
    delegate*<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerTypeAfterComma()
        {
            VerifyKeyword(@"
class C
{
    delegate*<int, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerTypeAfterModifier()
        {
            VerifyKeyword(@"
class C
{
    delegate*<ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateAsterisk()
        {
            VerifyAbsence(@"
class C
{
    delegate*$$");
        }
    }
}
