// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class DynamicKeywordRecommenderTests : RecommenderTests
{
    protected override string KeywordText => "dynamic";

    private readonly DynamicKeywordRecommender _recommender = new();

    public DynamicKeywordRecommenderTests()
    {
        this.RecommendKeywordsAsync = async (position, context) => _recommender.RecommendKeywords(position, context, CancellationToken.None);
    }

    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestAfterClass_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsing()
        => VerifyAbsenceAsync(
@"using $$");

    [Fact]
    public Task TestInUsingAlias()
        => VerifyKeywordAsync(
@"using Goo = $$");

    [Fact]
    public Task TestInUsingAlias_Tuple()
        => VerifyKeywordAsync(
@"using Goo = ($$");

    [Fact]
    public Task TestNotAfterStackAlloc()
        => VerifyAbsenceAsync(
            """
            class C {
                 int* goo = stackalloc $$
            """);

    [Fact]
    public Task TestNotInFixedStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"fixed ($$"));

    [Fact]
    public Task TestInDelegateReturnType()
        => VerifyKeywordAsync(
@"public delegate $$");

    [Fact]
    public Task TestInCastType()
        => VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$"));

    [Fact]
    public Task TestInCastType2()
        => VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$)items) as string;"));

    [Fact]
    public Task TestAfterConstInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                const $$
            """);

    [Fact]
    public Task TestAfterRefInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                ref $$
            """);

    [Fact]
    public Task TestAfterRefReadonlyInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                ref readonly $$
            """);

    [Fact]
    public Task TestAfterConstInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$"));

    [Fact]
    public Task TestAfterRefInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$"));

    [Fact]
    public Task TestAfterRefReadonlyInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$"));

    [Fact]
    public Task TestAfterConstLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$ int local;"));

    [Fact]
    public Task TestAfterRefLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;"));

    [Fact]
    public Task TestAfterRefReadonlyLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;"));

    [Fact]
    public Task TestAfterRefLocalFunction()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();"));

    [Fact]
    public Task TestAfterRefReadonlyLocalFunction()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int Function();"));

    [Fact]
    public Task TestNotAfterRefExpression()
        => VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$"));

    [Fact]
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestNotInEnumBaseTypes()
        => VerifyAbsenceAsync(
@"enum E : $$");

    [Fact]
    public Task TestInGenericType1()
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<$$"));

    [Fact]
    public Task TestInGenericType2()
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<int,$$"));

    [Fact]
    public Task TestInGenericType3()
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<int[],$$"));

    [Fact]
    public Task TestInGenericType4()
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<IGoo<int?,byte*>,$$"));

    [Fact]
    public Task TestNotInBaseList()
        => VerifyAbsenceAsync(
@"class C : $$");

    [Fact]
    public Task TestInGenericType_InBaseList()
        => VerifyKeywordAsync(
@"class C : IList<$$");

    [Fact]
    public Task TestAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public Task TestAfterField()
        => VerifyKeywordAsync(
            """
            class C {
              int i;
              $$
            """);

    [Fact]
    public Task TestAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
              int i { get; }
              $$
            """);

    [Fact]
    public Task TestAfterNestedAttribute()
        => VerifyKeywordAsync(
            """
            class C {
              [goo]
              $$
            """);

    [Fact]
    public Task TestInsideStruct()
        => VerifyKeywordAsync(
            """
            struct S {
               $$
            """);

    [Fact]
    public Task TestInsideInterface()
        => VerifyKeywordAsync(
            """
            interface I {
               $$
            """);

    [Fact]
    public Task TestInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
               $$
            """);

    [Fact]
    public async Task TestNotAfterPartial()
        => await VerifyAbsenceAsync(@"partial $$");

    [Fact]
    public Task TestAfterNestedPartial()
        => VerifyKeywordAsync(
            """
            class C {
                partial $$
            """);

    [Fact]
    public Task TestAfterNestedAbstract()
        => VerifyKeywordAsync(
            """
            class C {
                abstract $$
            """);

    [Fact]
    public Task TestAfterNestedInternal()
        => VerifyKeywordAsync(
            """
            class C {
                internal $$
            """);

    [Fact]
    public Task TestAfterNestedStaticPublic()
        => VerifyKeywordAsync(
            """
            class C {
                static public $$
            """);

    [Fact]
    public Task TestAfterNestedPublicStatic()
        => VerifyKeywordAsync(
            """
            class C {
                public static $$
            """);

    [Fact]
    public Task TestAfterVirtualPublic()
        => VerifyKeywordAsync(
            """
            class C {
                virtual public $$
            """);

    [Fact]
    public Task TestAfterNestedPublic()
        => VerifyKeywordAsync(
            """
            class C {
                public $$
            """);

    [Fact]
    public Task TestAfterNestedPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               private $$
            """);

    [Fact]
    public Task TestAfterNestedProtected()
        => VerifyKeywordAsync(
            """
            class C {
                protected $$
            """);

    [Fact]
    public Task TestAfterNestedSealed()
        => VerifyKeywordAsync(
            """
            class C {
                sealed $$
            """);

    [Fact]
    public Task TestAfterNestedStatic()
        => VerifyKeywordAsync(
            """
            class C {
                static $$
            """);

    [Fact]
    public Task TestInLocalVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInForVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$"));

    [Fact]
    public Task TestInForeachVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$"));

    [Fact]
    public Task TestInUsingVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"using ($$"));

    [Fact]
    public Task TestInFromVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = from $$"));

    [Fact]
    public Task TestInJoinVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from a in b 
                      join $$
            """));

    [Fact]
    public Task TestAfterMethodOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo($$
            """);

    [Fact]
    public Task TestAfterMethodComma()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo(int i, $$
            """);

    [Fact]
    public Task TestAfterMethodAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo(int i, [Goo]$$
            """);

    [Fact]
    public Task TestAfterConstructorOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                public C($$
            """);

    [Fact]
    public Task TestAfterConstructorComma()
        => VerifyKeywordAsync(
            """
            class C {
                public C(int i, $$
            """);

    [Fact]
    public Task TestAfterConstructorAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                public C(int i, [Goo]$$
            """);

    [Fact]
    public Task TestAfterDelegateOpenParen()
        => VerifyKeywordAsync(
@"delegate void D($$");

    [Fact]
    public Task TestAfterDelegateComma()
        => VerifyKeywordAsync(
@"delegate void D(int i, $$");

    [Fact]
    public Task TestAfterDelegateAttribute()
        => VerifyKeywordAsync(
@"delegate void D(int i, [Goo]$$");

    [Fact]
    public Task TestAfterThis()
        => VerifyKeywordAsync(
            """
            static class C {
                 public static void Goo(this $$
            """);

    [Fact]
    public Task TestAfterRef()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo(ref $$
            """);

    [Fact]
    public Task TestAfterOut()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo(out $$
            """);

    [Fact]
    public Task TestAfterLambdaRef()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo() {
                      System.Func<int, int> f = (ref $$
            """);

    [Fact]
    public Task TestAfterLambdaOut()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo() {
                      System.Func<int, int> f = (out $$
            """);

    [Fact]
    public Task TestAfterParams()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo(params $$
            """);

    [Fact]
    public Task TestNotInImplicitOperator()
        => VerifyAbsenceAsync(
            """
            class C {
                 public static implicit operator $$
            """);

    [Fact]
    public Task TestNotInExplicitOperator()
        => VerifyAbsenceAsync(
            """
            class C {
                 public static explicit operator $$
            """);

    [Fact]
    public Task TestAfterIndexerBracket()
        => VerifyKeywordAsync(
            """
            class C {
                int this[$$
            """);

    [Fact]
    public Task TestAfterIndexerBracketComma()
        => VerifyKeywordAsync(
            """
            class C {
                int this[int i, $$
            """);

    [Fact]
    public Task TestAfterNewInExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"new $$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInTypeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestInDefault()
        => VerifyKeywordAsync(AddInsideMethod(
@"default($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInSizeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545303")]
    public Task TestNotInPreProcessor()
        => VerifyAbsenceAsync(
            """
            class Program
            {
                #region $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public async Task TestNotAfterAsync()
        => await VerifyAbsenceAsync(@"class c { async $$ }");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public async Task TestNotAfterAsyncAsType()
        => await VerifyAbsenceAsync(@"class c { async async $$ }");

    [Fact]
    public Task TestInFunctionPointerType()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<$$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeAfterComma()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<int, $$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeAfterModifier()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<ref $$
            """);

    [Fact]
    public Task TestNotAfterDelegateAsterisk()
        => VerifyAbsenceAsync("""
            class C
            {
                delegate*$$
            """);

    [Fact]
    public Task TestWithinExtension()
        => VerifyKeywordAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """, CSharpNextParseOptions);
}
