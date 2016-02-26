// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ULongKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Foo = $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterStackAlloc()
        {
            await VerifyKeywordAsync(
@"class C {
     int* foo = stackalloc $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InFixedStatement()
        {
            await VerifyKeywordAsync(
@"fixed ($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InDelegateReturnType()
        {
            await VerifyKeywordAsync(
@"public delegate $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InCastType()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InCastType2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$)items) as string;"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterOuterConst()
        {
            await VerifyKeywordAsync(
@"class C {
    const $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterInnerConst()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task EnumBaseTypes()
        {
            await VerifyKeywordAsync(
@"enum E : $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InGenericType1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InGenericType2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<int,$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InGenericType3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<int[],$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InGenericType4()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<IFoo<int?,byte*>,$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInBaseList()
        {
            await VerifyAbsenceAsync(
@"class C : $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InGenericType_InBaseList()
        {
            await VerifyKeywordAsync(
@"class C : IList<$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterIs()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = foo is $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterAs()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = foo as $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterMethod()
        {
            await VerifyKeywordAsync(
@"class C {
  void Foo() {}
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterField()
        {
            await VerifyKeywordAsync(
@"class C {
  int i;
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterProperty()
        {
            await VerifyKeywordAsync(
@"class C {
  int i { get; }
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
  [foo]
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InsideStruct()
        {
            await VerifyKeywordAsync(
@"struct S {
   $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InsideInterface()
        {
            await VerifyKeywordAsync(
@"interface I {
   $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InsideClass()
        {
            await VerifyKeywordAsync(
@"class C {
   $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotAfterPartial()
        {
            await VerifyAbsenceAsync(@"partial $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotAfterNestedPartial()
        {
            await VerifyAbsenceAsync(
@"class C {
    partial $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedAbstract()
        {
            await VerifyKeywordAsync(
@"class C {
    abstract $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedInternal()
        {
            await VerifyKeywordAsync(
@"class C {
    internal $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedStaticPublic()
        {
            await VerifyKeywordAsync(
@"class C {
    static public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedPublicStatic()
        {
            await VerifyKeywordAsync(
@"class C {
    public static $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterVirtualPublic()
        {
            await VerifyKeywordAsync(
@"class C {
    virtual public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedPublic()
        {
            await VerifyKeywordAsync(
@"class C {
    public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedPrivate()
        {
            await VerifyKeywordAsync(
@"class C {
   private $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedProtected()
        {
            await VerifyKeywordAsync(
@"class C {
    protected $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedSealed()
        {
            await VerifyKeywordAsync(
@"class C {
    sealed $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNestedStatic()
        {
            await VerifyKeywordAsync(
@"class C {
    static $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InLocalVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InForVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InForeachVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InUsingVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"using ($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InFromVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InJoinVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from a in b 
          join $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterMethodOpenParen()
        {
            await VerifyKeywordAsync(
@"class C {
    void Foo($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterMethodComma()
        {
            await VerifyKeywordAsync(
@"class C {
    void Foo(int i, $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterMethodAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
    void Foo(int i, [Foo]$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterConstructorOpenParen()
        {
            await VerifyKeywordAsync(
@"class C {
    public C($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterConstructorComma()
        {
            await VerifyKeywordAsync(
@"class C {
    public C(int i, $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterConstructorAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
    public C(int i, [Foo]$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterDelegateOpenParen()
        {
            await VerifyKeywordAsync(
@"delegate void D($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterDelegateComma()
        {
            await VerifyKeywordAsync(
@"delegate void D(int i, $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterDelegateAttribute()
        {
            await VerifyKeywordAsync(
@"delegate void D(int i, [Foo]$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterThis()
        {
            await VerifyKeywordAsync(
@"static class C {
     public static void Foo(this $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterRef()
        {
            await VerifyKeywordAsync(
@"class C {
     void Foo(ref $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterOut()
        {
            await VerifyKeywordAsync(
@"class C {
     void Foo(out $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterLambdaRef()
        {
            await VerifyKeywordAsync(
@"class C {
     void Foo() {
          System.Func<int, int> f = (ref $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterLambdaOut()
        {
            await VerifyKeywordAsync(
@"class C {
     void Foo() {
          System.Func<int, int> f = (out $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterParams()
        {
            await VerifyKeywordAsync(
@"class C {
     void Foo(params $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InImplicitOperator()
        {
            await VerifyKeywordAsync(
@"class C {
     public static implicit operator $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InExplicitOperator()
        {
            await VerifyKeywordAsync(
@"class C {
     public static explicit operator $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterIndexerBracket()
        {
            await VerifyKeywordAsync(
@"class C {
    int this[$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterIndexerBracketComma()
        {
            await VerifyKeywordAsync(
@"class C {
    int this[int i, $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterNewInExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InTypeOf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"typeof($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InDefault()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"default($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InSizeOf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"sizeof($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InCrefContext()
        {
            await VerifyKeywordAsync(@"
class Program
{
    /// <see cref=""$$"">
    static void Main(string[] args)
    {
        
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InCrefContextNotAfterDot()
        {
            await VerifyAbsenceAsync(@"
/// <see cref=""System.$$"" />
class C { }
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterAsync()
        {
            await VerifyKeywordAsync(@"class c { async $$ }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotAfterAsyncAsType()
        {
            await VerifyAbsenceAsync(@"class c { async async $$ }");
        }

        [WorkItem(1468, "https://github.com/dotnet/roslyn/issues/1468")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInCrefTypeParameter()
        {
            await VerifyAbsenceAsync(@"
using System;
/// <see cref=""List{$$}"" />
class C { }
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task Preselection()
        {
            await VerifyKeywordAsync(@"
class Program
{
    static void Main(string[] args)
    {
        Helper($$)
    }
    static void Helper(ulong x) { }
}
", matchPriority: MatchPriority.Keyword);
        }
    }
}
