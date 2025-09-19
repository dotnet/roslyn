// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class BoolKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(
@"$$", options: CSharp9ParseOptions);

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
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration()
        => VerifyKeywordAsync(
            """
            int i = 0;
            $$
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotInUsing()
        => VerifyAbsenceAsync(
@"using $$");

    [Fact]
    public Task TesInUsingAlias()
        => VerifyKeywordAsync(
@"using Goo = $$");

    [Fact]
    public Task TestInGlobalUsingAlias()
        => VerifyKeywordAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestAfterStackAlloc()
        => VerifyKeywordAsync(
            """
            class C {
                 int* goo = stackalloc $$
            """);

    [Theory, CombinatorialData]
    public Task TestInFixedStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"fixed ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

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

    [Theory, CombinatorialData]
    public Task TestAfterConstInStatementContext(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterRefInStatementContext(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterRefReadonlyInStatementContext(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterConstLocalDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterRefLocalDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterRefReadonlyLocalDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterRefLocalFunction(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterRefReadonlyLocalFunction(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterRefExpression(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInEmptyStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotEnumBaseTypes()
        => VerifyAbsenceAsync(
@"enum E : $$");

    [Theory, CombinatorialData]
    public Task TestInGenericType1(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInGenericType2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<int,$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInGenericType3(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<int[],$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInGenericType4(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<IGoo<int?,byte*>,$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotInBaseList()
        => VerifyAbsenceAsync(
@"class C : $$");

    [Fact]
    public Task TestInGenericType_InBaseList()
        => VerifyKeywordAsync(
@"class C : IList<$$");

    [Theory, CombinatorialData]
    public Task TestAfterIs(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = goo is $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterAs(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = goo as $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

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

    [Theory, CombinatorialData]
    public Task TestInForVariableDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInForeachVariableDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInUsingVariableDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInFromVariableDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = from $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInJoinVariableDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from a in b 
                      join $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

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
    public Task TestInImplicitOperator()
        => VerifyKeywordAsync(
            """
            class C {
                 public static implicit operator $$
            """);

    [Fact]
    public Task TestInExplicitOperator()
        => VerifyKeywordAsync(
            """
            class C {
                 public static explicit operator $$
            """);

    [Theory, CombinatorialData]
    public Task TestAfterNewInExpression(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"new $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInCastType(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInCastType2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$)items) as string;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestInDelegateReturnType()
        => VerifyKeywordAsync(
@"public delegate $$");

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    [CombinatorialData]
    public Task TestInTypeOf(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"typeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    [CombinatorialData]
    public Task TestInDefault(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"default($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    [CombinatorialData]
    public Task TestInSizeOf(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"sizeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546938")]
    public Task TestInCrefContext()
        => VerifyKeywordAsync("""
            class Program
            {
                /// <see cref="$$">
                static void Main(string[] args)
                {

                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546955")]
    public Task TestInCrefContextNotAfterDot()
        => VerifyAbsenceAsync("""
            /// <see cref="System.$$" />
            class C { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public async Task TestNotAfterAsync()
        => await VerifyAbsenceAsync(@"class c { async $$ }");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public async Task TestNotAfterAsyncAsType()
        => await VerifyAbsenceAsync(@"class c { async async $$ }");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1468")]
    public Task TestNotInCrefTypeParameter()
        => VerifyAbsenceAsync("""
            using System;
            /// <see cref="List{$$}" />
            class C { }
            """);

    [Fact]
    public Task Preselection()
        => VerifyKeywordAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    Helper($$)
                }
                static void Helper(bool x) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TestInTupleWithinType()
        => VerifyKeywordAsync("""
            class Program
            {
                ($$
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    [CombinatorialData]
    public Task TestInTupleWithinMember(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
    [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithoutAsync))]
    public Task TestAfterKeywordIndicatingLocalFunctionWithoutAsync(string keyword)
        => VerifyKeywordAsync(AddInsideMethod($"""
            {keyword} $$
            """));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithAsync))]
    public Task TestNotAfterKeywordIndicatingLocalFunctionWithAsync(string keyword)
        => VerifyAbsenceAsync(AddInsideMethod($"""
            {keyword} $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64585")]
    public Task TestAfterRequired()
        => VerifyKeywordAsync("""
            class C
            {
                required $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
    public Task TestAfterRefAtTopLevel1()
        => VerifyKeywordAsync(
@"ref $$");

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
    public Task TestAfterRefReadonlyAtTopLevel1()
        => VerifyKeywordAsync(
@"ref readonly $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
    public Task TestNotAfterRefInNamespace()
        => VerifyAbsenceAsync(
            """
            namespace N
            {
                ref $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
    public Task TestNotAfterReadonlyInNamespace()
        => VerifyAbsenceAsync(
            """
            namespace N
            {
                readonly $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
    public Task TestNotAfterRefReadonlyInNamespace()
        => VerifyAbsenceAsync(
            """
            namespace N
            {
                ref readonly $$
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
    [InlineData("class")]
    [InlineData("interface")]
    [InlineData("struct")]
    [InlineData("record")]
    public Task TestAfterRefInClassInterfaceStructRecord(string type)
        => VerifyKeywordAsync(
            $$"""
            {{type}} N
            {
                ref $$
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
    [InlineData("class")]
    [InlineData("interface")]
    [InlineData("struct")]
    [InlineData("record")]
    public Task TestAfterReadonlyInClassInterfaceStructRecord(string type)
        => VerifyKeywordAsync(
            $$"""
            {{type}} N
            {
                readonly $$
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67061")]
    [InlineData("class")]
    [InlineData("interface")]
    [InlineData("struct")]
    [InlineData("record")]
    public Task TestAfterRefReadonlyInClassInterfaceStructRecord(string type)
        => VerifyKeywordAsync(
            $$"""
            {{type}} N
            {
                ref readonly $$
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
