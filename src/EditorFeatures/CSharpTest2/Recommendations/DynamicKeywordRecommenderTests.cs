// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class DynamicKeywordRecommenderTests : RecommenderTests
    {
        protected override string KeywordText => "dynamic";

        private readonly DynamicKeywordRecommender _recommender = new();

        public DynamicKeywordRecommenderTests()
        {
            this.RecommendKeywordsAsync = (position, context) => Task.FromResult(_recommender.RecommendKeywords(position, context, CancellationToken.None));
        }

        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsing()
        {
            await VerifyAbsenceAsync(
@"using $$");
        }

        [Fact]
        public async Task TestInUsingAlias()
        {
            await VerifyKeywordAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestInUsingAlias_Tuple()
        {
            await VerifyKeywordAsync(
@"using Goo = ($$");
        }

        [Fact]
        public async Task TestNotAfterStackAlloc()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                     int* goo = stackalloc $$
                """);
        }

        [Fact]
        public async Task TestNotInFixedStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"fixed ($$"));
        }

        [Fact]
        public async Task TestInDelegateReturnType()
        {
            await VerifyKeywordAsync(
@"public delegate $$");
        }

        [Fact]
        public async Task TestInCastType()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$"));
        }

        [Fact]
        public async Task TestInCastType2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$)items) as string;"));
        }

        [Fact]
        public async Task TestAfterConstInMemberContext()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    const $$
                """);
        }

        [Fact]
        public async Task TestAfterRefInMemberContext()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    ref $$
                """);
        }

        [Fact]
        public async Task TestAfterRefReadonlyInMemberContext()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    ref readonly $$
                """);
        }

        [Fact]
        public async Task TestAfterConstInStatementContext()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$"));
        }

        [Fact]
        public async Task TestAfterRefInStatementContext()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$"));
        }

        [Fact]
        public async Task TestAfterRefReadonlyInStatementContext()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$"));
        }

        [Fact]
        public async Task TestAfterConstLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$ int local;"));
        }

        [Fact]
        public async Task TestAfterRefLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;"));
        }

        [Fact]
        public async Task TestAfterRefReadonlyLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;"));
        }

        [Fact]
        public async Task TestAfterRefLocalFunction()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();"));
        }

        [Fact]
        public async Task TestAfterRefReadonlyLocalFunction()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int Function();"));
        }

        [Fact]
        public async Task TestNotAfterRefExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact]
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestNotInEnumBaseTypes()
        {
            await VerifyAbsenceAsync(
@"enum E : $$");
        }

        [Fact]
        public async Task TestInGenericType1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<$$"));
        }

        [Fact]
        public async Task TestInGenericType2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<int,$$"));
        }

        [Fact]
        public async Task TestInGenericType3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<int[],$$"));
        }

        [Fact]
        public async Task TestInGenericType4()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<IGoo<int?,byte*>,$$"));
        }

        [Fact]
        public async Task TestNotInBaseList()
        {
            await VerifyAbsenceAsync(
@"class C : $$");
        }

        [Fact]
        public async Task TestInGenericType_InBaseList()
        {
            await VerifyKeywordAsync(
@"class C : IList<$$");
        }

        [Fact]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i { get; }
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  [goo]
                  $$
                """);
        }

        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync(
                """
                interface I {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPartial()
            => await VerifyAbsenceAsync(@"partial $$");

        [Fact]
        public async Task TestNotAfterNestedPartial()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    partial $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    internal $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedStaticPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static public $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPublicStatic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public static $$
                """);
        }

        [Fact]
        public async Task TestAfterVirtualPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    virtual public $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   private $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedProtected()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    protected $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedStatic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static $$
                """);
        }

        [Fact]
        public async Task TestInLocalVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestInForVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$"));
        }

        [Fact]
        public async Task TestInForeachVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$"));
        }

        [Fact]
        public async Task TestInUsingVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"using ($$"));
        }

        [Fact]
        public async Task TestInFromVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from $$"));
        }

        [Fact]
        public async Task TestInJoinVariableDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from a in b 
                          join $$
                """));
        }

        [Fact]
        public async Task TestAfterMethodOpenParen()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo($$
                """);
        }

        [Fact]
        public async Task TestAfterMethodComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo(int i, $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo(int i, [Goo]$$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorOpenParen()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C($$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C(int i, $$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C(int i, [Goo]$$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateOpenParen()
        {
            await VerifyKeywordAsync(
@"delegate void D($$");
        }

        [Fact]
        public async Task TestAfterDelegateComma()
        {
            await VerifyKeywordAsync(
@"delegate void D(int i, $$");
        }

        [Fact]
        public async Task TestAfterDelegateAttribute()
        {
            await VerifyKeywordAsync(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact]
        public async Task TestAfterThis()
        {
            await VerifyKeywordAsync(
                """
                static class C {
                     public static void Goo(this $$
                """);
        }

        [Fact]
        public async Task TestAfterRef()
        {
            await VerifyKeywordAsync(
                """
                class C {
                     void Goo(ref $$
                """);
        }

        [Fact]
        public async Task TestAfterOut()
        {
            await VerifyKeywordAsync(
                """
                class C {
                     void Goo(out $$
                """);
        }

        [Fact]
        public async Task TestAfterLambdaRef()
        {
            await VerifyKeywordAsync(
                """
                class C {
                     void Goo() {
                          System.Func<int, int> f = (ref $$
                """);
        }

        [Fact]
        public async Task TestAfterLambdaOut()
        {
            await VerifyKeywordAsync(
                """
                class C {
                     void Goo() {
                          System.Func<int, int> f = (out $$
                """);
        }

        [Fact]
        public async Task TestAfterParams()
        {
            await VerifyKeywordAsync(
                """
                class C {
                     void Goo(params $$
                """);
        }

        [Fact]
        public async Task TestNotInImplicitOperator()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                     public static implicit operator $$
                """);
        }

        [Fact]
        public async Task TestNotInExplicitOperator()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                     public static explicit operator $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerBracket()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int this[$$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerBracketComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int this[int i, $$
                """);
        }

        [Fact]
        public async Task TestAfterNewInExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new $$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInTypeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestInDefault()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"default($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInSizeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545303")]
        public async Task TestNotInPreProcessor()
        {
            await VerifyAbsenceAsync(
                """
                class Program
                {
                    #region $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        public async Task TestNotAfterAsync()
            => await VerifyAbsenceAsync(@"class c { async $$ }");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        public async Task TestNotAfterAsyncAsType()
            => await VerifyAbsenceAsync(@"class c { async async $$ }");

        [Fact]
        public async Task TestInFunctionPointerType()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<$$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterComma()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<int, $$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterModifier()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegateAsterisk()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    delegate*$$
                """);
        }
    }
}
