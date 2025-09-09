// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

public sealed class CharKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterClass_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInUsingAlias()
        => VerifyKeywordAsync(
@"using Goo = $$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInGlobalUsingAlias()
        => VerifyKeywordAsync(
@"global using Goo = $$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterStackAlloc()
        => VerifyKeywordAsync(
            """
            class C {
                 int* goo = stackalloc $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInFixedStatement()
        => VerifyKeywordAsync(
@"fixed ($$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInDelegateReturnType()
        => VerifyKeywordAsync(
@"public delegate $$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInCastType()
        => VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInCastType2()
        => VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$)items) as string;"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterConstInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                const $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                ref $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefReadonlyInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                ref readonly $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterConstInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefReadonlyInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterConstLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$ int local;"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefReadonlyLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefLocalFunction()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefReadonlyLocalFunction()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int Function();"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRefExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestNotInEnumBaseTypes()
        => VerifyAbsenceAsync(
@"enum E : $$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInGenericType1()
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<$$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInGenericType2()
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<int,$$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInGenericType3()
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<int[],$$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInGenericType4()
        => VerifyKeywordAsync(AddInsideMethod(
@"IList<IGoo<int?,byte*>,$$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestNotInBaseList()
        => VerifyAbsenceAsync(
@"class C : $$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInGenericType_InBaseList()
        => VerifyKeywordAsync(
@"class C : IList<$$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterIs()
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = goo is $$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterAs()
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = goo as $$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
              void Goo() {}
              $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterField()
        => VerifyKeywordAsync(
            """
            class C {
              int i;
              $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
              int i { get; }
              $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedAttribute()
        => VerifyKeywordAsync(
            """
            class C {
              [goo]
              $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInsideStruct()
        => VerifyKeywordAsync(
            """
            struct S {
               $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInsideInterface()
        => VerifyKeywordAsync(
            """
            interface I {
               $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
               $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public async Task TestNotAfterPartial()
        => await VerifyAbsenceAsync(@"partial $$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedPartial()
        => VerifyKeywordAsync(
            """
            class C {
                partial $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedAbstract()
        => VerifyKeywordAsync(
            """
            class C {
                abstract $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedInternal()
        => VerifyKeywordAsync(
            """
            class C {
                internal $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedStaticPublic()
        => VerifyKeywordAsync(
            """
            class C {
                static public $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedPublicStatic()
        => VerifyKeywordAsync(
            """
            class C {
                public static $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterVirtualPublic()
        => VerifyKeywordAsync(
            """
            class C {
                virtual public $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedPublic()
        => VerifyKeywordAsync(
            """
            class C {
                public $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               private $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedProtected()
        => VerifyKeywordAsync(
            """
            class C {
                protected $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedSealed()
        => VerifyKeywordAsync(
            """
            class C {
                sealed $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNestedStatic()
        => VerifyKeywordAsync(
            """
            class C {
                static $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInLocalVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInForVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInForeachVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInUsingVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"using ($$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInFromVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = from $$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInJoinVariableDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from a in b 
                      join $$
            """));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterMethodOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo($$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterMethodComma()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo(int i, $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterMethodAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo(int i, [Goo]$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterConstructorOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                public C($$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterConstructorComma()
        => VerifyKeywordAsync(
            """
            class C {
                public C(int i, $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterConstructorAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                public C(int i, [Goo]$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterDelegateOpenParen()
        => VerifyKeywordAsync(
@"delegate void D($$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterDelegateComma()
        => VerifyKeywordAsync(
@"delegate void D(int i, $$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterDelegateAttribute()
        => VerifyKeywordAsync(
@"delegate void D(int i, [Goo]$$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterThis()
        => VerifyKeywordAsync(
            """
            static class C {
                 public static void Goo(this $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterRef()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo(ref $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterOut()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo(out $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterLambdaRef()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo() {
                      System.Func<int, int> f = (ref $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterLambdaOut()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo() {
                      System.Func<int, int> f = (out $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterParams()
        => VerifyKeywordAsync(
            """
            class C {
                 void Goo(params $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInImplicitOperator()
        => VerifyKeywordAsync(
            """
            class C {
                 public static implicit operator $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInExplicitOperator()
        => VerifyKeywordAsync(
            """
            class C {
                 public static explicit operator $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterIndexerBracket()
        => VerifyKeywordAsync(
            """
            class C {
                int this[$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterIndexerBracketComma()
        => VerifyKeywordAsync(
            """
            class C {
                int this[int i, $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestAfterNewInExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"new $$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestInTypeOf()
        => VerifyKeywordAsync(AddInsideMethod(
@"typeof($$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestInDefault()
        => VerifyKeywordAsync(AddInsideMethod(
@"default($$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestInSizeOf()
        => VerifyKeywordAsync(AddInsideMethod(
@"sizeof($$"));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task TestNotInObjectInitializerMemberContext()
        => VerifyAbsenceAsync("""
            class C
            {
                public int x, y;
                void M()
                {
                    var c = new C { x = 2, y = 3, $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546938")]
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

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546955")]
    public Task TestInCrefContextNotAfterDot()
        => VerifyAbsenceAsync("""
            /// <see cref="System.$$" />
            class C { }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public async Task TestNotAfterAsync()
        => await VerifyAbsenceAsync(@"class c { async $$ }");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public async Task TestNotAfterAsyncAsType()
        => await VerifyAbsenceAsync(@"class c { async async $$ }");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/988025")]
    public Task TestInGenericMethodTypeParameterList1()
        => VerifyKeywordAsync("""
            class Class1<T, D>
            {
                public static Class1<T, D> Create() { return null; }
            }
            static class Class2
            {
                public static void Test<T,D>(this Class1<T, D> arg)
                {
                }
            }
            class Program
            {
                static void Main(string[] args)
                {
                    Class1<string, int>.Create().Test<$$
                }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/988025")]
    public Task TestInGenericMethodTypeParameterList2()
        => VerifyKeywordAsync("""
            class Class1<T, D>
            {
                public static Class1<T, D> Create() { return null; }
            }
            static class Class2
            {
                public static void Test<T,D>(this Class1<T, D> arg)
                {
                }
            }
            class Program
            {
                static void Main(string[] args)
                {
                    Class1<string, int>.Create().Test<string,$$
                }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/1468")]
    public Task TestNotInCrefTypeParameter()
        => VerifyAbsenceAsync("""
            using System;
            /// <see cref="List{$$}" />
            class C { }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task Preselection()
        => VerifyKeywordAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    Helper($$)
                }
                static void Helper(char x) { }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TestInTupleWithinType()
        => VerifyKeywordAsync("""
            class Program
            {
                ($$
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TestInTupleWithinMember()
        => VerifyKeywordAsync("""
            class Program
            {
                void Method()
                {
                    ($$
                }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInFunctionPointerType()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInFunctionPointerTypeAfterComma()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<int, $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestInFunctionPointerTypeAfterModifier()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<ref $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestNotAfterDelegateAsterisk()
        => VerifyAbsenceAsync("""
            class C
            {
                delegate*$$
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53585"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithoutAsync))]
    public Task TestAfterKeywordIndicatingLocalFunctionWithoutAsync(string keyword)
        => VerifyKeywordAsync(AddInsideMethod($"""
            {keyword} $$
            """));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/60341"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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
