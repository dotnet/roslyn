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
    public class ByteKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$", options: CSharp9ParseOptions);
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
        public async Task TestAfterGlobalStatement()
        {
            await VerifyKeywordAsync(
                """
                System.Console.WriteLine();
                $$
                """, options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration()
        {
            await VerifyKeywordAsync(
                """
                int i = 0;
                $$
                """, options: CSharp9ParseOptions);
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
        public async Task TestInGlobalUsingAlias()
        {
            await VerifyKeywordAsync(
@"global using Goo = $$");
        }

        [Fact]
        public async Task TestAfterStackAlloc()
        {
            await VerifyKeywordAsync(
                """
                class C {
                     int* goo = stackalloc $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInFixedStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"fixed ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestInDelegateReturnType()
        {
            await VerifyKeywordAsync(
@"public delegate $$");
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInCastType(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInCastType2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$)items) as string;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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

        [Theory]
        [CombinatorialData]
        public async Task TestAfterConstInStatementContext(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefInStatementContext(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefReadonlyInStatementContext(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterConstLocalDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefLocalDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefReadonlyLocalDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefLocalFunction(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefReadonlyLocalFunction(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestEnumBaseTypes()
        {
            await VerifyKeywordAsync(
@"enum E : $$");
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInGenericType1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInGenericType2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<int,$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInGenericType3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<int[],$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInGenericType4(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"IList<IGoo<int?,byte*>,$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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

        [Theory]
        [CombinatorialData]
        public async Task TestAfterIs(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = goo is $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterAs(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = goo as $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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

        [Theory]
        [CombinatorialData]
        public async Task TestInLocalVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInForVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInForeachVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInUsingVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInFromVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInJoinVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from a in b 
                          join $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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
        public async Task TestInImplicitOperator()
        {
            await VerifyKeywordAsync(
                """
                class C {
                     public static implicit operator $$
                """);
        }

        [Fact]
        public async Task TestInExplicitOperator()
        {
            await VerifyKeywordAsync(
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

        [Theory]
        [CombinatorialData]
        public async Task TestAfterNewInExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [CombinatorialData]
        public async Task TestInTypeOf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"typeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [CombinatorialData]
        public async Task TestInDefault(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"default($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [CombinatorialData]
        public async Task TestInSizeOf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"sizeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546938")]
        public async Task TestInCrefContext()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    /// <see cref="$$">
                    static void Main(string[] args)
                    {

                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546955")]
        public async Task TestInCrefContextNotAfterDot()
        {
            await VerifyAbsenceAsync("""
                /// <see cref="System.$$" />
                class C { }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        public async Task TestNotAfterAsync()
            => await VerifyAbsenceAsync(@"class c { async $$ }");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        public async Task TestNotAfterAsyncAsType()
            => await VerifyAbsenceAsync(@"class c { async async $$ }");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1468")]
        public async Task TestNotInCrefTypeParameter()
        {
            await VerifyAbsenceAsync("""
                using System;
                /// <see cref="List{$$}" />
                class C { }
                """);
        }

        [Fact]
        public async Task Preselection()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        Helper($$)
                    }
                    static void Helper(byte x) { }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
        public async Task TestInTupleWithinType()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    ($$
                }
                """);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
        [CombinatorialData]
        public async Task TestInTupleWithinMember(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

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

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
        [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithoutAsync))]
        public async Task TestAfterKeywordIndicatingLocalFunctionWithoutAsync(string keyword)
        {
            await VerifyKeywordAsync(AddInsideMethod($@"
{keyword} $$"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithAsync))]
        public async Task TestNotAfterKeywordIndicatingLocalFunctionWithAsync(string keyword)
        {
            await VerifyAbsenceAsync(AddInsideMethod($@"
{keyword} $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64585")]
        public async Task TestAfterRequired()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    required $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        public async Task TestAfterRefAtTopLevel1()
        {
            // Could be defining a ref-local in top-level-code
            await VerifyKeywordAsync(
@"ref $$");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        [CombinatorialData]
        public async Task TestAfterReadonlyAtTopLevel1(bool script)
        {
            if (script)
            {
                // A legal top level script field.
                await VerifyKeywordAsync(
@"readonly $$", Options.Script);
            }
            else
            {
                // no legal top level statement can start with `readonly string`
                await VerifyAbsenceAsync(
@"readonly $$", CSharp9ParseOptions, CSharp9ParseOptions);
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        public async Task TestAfterRefReadonlyAtTopLevel1()
        {
            // Could be defining a ref-local in top-level-code
            await VerifyKeywordAsync(
@"ref readonly $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        public async Task TestNotAfterRefInNamespace()
        {
            // This is only legal for a struct declaration
            await VerifyAbsenceAsync(
                """
                namespace N
                {
                    ref $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        public async Task TestNotAfterReadonlyInNamespace()
        {
            // This is only legal for a struct declaration
            await VerifyAbsenceAsync(
                """
                namespace N
                {
                    readonly $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        public async Task TestNotAfterRefReadonlyInNamespace()
        {
            // This is only legal for a struct declaration
            await VerifyAbsenceAsync(
                """
                namespace N
                {
                    ref readonly $$
                }
                """);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        [InlineData("class")]
        [InlineData("interface")]
        [InlineData("struct")]
        [InlineData("record")]
        public async Task TestAfterRefInClassInterfaceStructRecord(string type)
        {
            await VerifyKeywordAsync(
$@"{type} N
{{
    ref $$
}}");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        [InlineData("class")]
        [InlineData("interface")]
        [InlineData("struct")]
        [InlineData("record")]
        public async Task TestAfterReadonlyInClassInterfaceStructRecord(string type)
        {
            await VerifyKeywordAsync(
$@"{type} N
{{
    readonly $$
}}");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
        [InlineData("class")]
        [InlineData("interface")]
        [InlineData("struct")]
        [InlineData("record")]
        public async Task TestAfterRefReadonlyInClassInterfaceStructRecord(string type)
        {
            await VerifyKeywordAsync(
$@"{type} N
{{
    ref readonly $$
}}");
        }
    }
}
