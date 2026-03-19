// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class DelegateKeywordRecommenderTests : KeywordRecommenderTests
{
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
    public async Task TestInUsingAlias()
    {
        await VerifyKeywordAsync(
            @"using Goo = $$");
        await VerifyKeywordAsync(
            @"using Goo = d$$");
    }

    [Fact]
    public async Task TestInGlobalUsingAlias()
    {
        await VerifyKeywordAsync(
            @"global using Goo = $$");
        await VerifyKeywordAsync(
            @"global using Goo = d$$");
    }

    [Fact]
    public Task TestInUsingAliasTypeParameter()
        => VerifyKeywordAsync(
            @"using Goo = T<$$");

    [Fact]
    public Task TestInGlobalUsingAliasTypeParameter()
        => VerifyKeywordAsync(
            @"global using Goo = T<$$");

    [Fact]
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            @"$$"));

    [Fact]
    public Task TestInCompilationUnit()
        => VerifyKeywordAsync(
            @"$$");

    [Fact]
    public Task TestAfterExtern()
        => VerifyKeywordAsync(
            """
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync(
            """
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestAfterFileScopedNamespace()
        => VerifyKeywordAsync(
            """
            namespace N;
            $$
            """);

    [Fact]
    public Task TestAfterTypeDeclaration()
        => VerifyKeywordAsync(
            """
            class C {}
            $$
            """);

    [Fact]
    public Task TestAfterDelegateDeclaration()
        => VerifyKeywordAsync(
            """
            delegate void Goo();
            $$
            """);

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
    public Task TestNotBeforeUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            using Goo;
            """);

    [Fact]
    public Task TestNotBeforeGlobalUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            global using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeGlobalUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            global using Goo;
            """);

    [Fact]
    public Task TestAfterAssemblyAttribute()
        => VerifyKeywordAsync(
            """
            [assembly: goo]
            $$
            """);

    [Fact]
    public Task TestAfterRootAttribute()
        => VerifyKeywordAsync(
            """
            [goo]
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
        => VerifyKeywordAsync("""
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
    public async Task TestNotAfterAbstract()
        => await VerifyAbsenceAsync(@"abstract $$");

    [Fact]
    public Task TestAfterInternal()
        => VerifyKeywordAsync(
            @"internal $$");

    [Fact]
    public Task TestAfterPublic()
        => VerifyKeywordAsync(
            @"public $$");

    [Fact]
    public Task TestAfterPrivate()
        => VerifyKeywordAsync(
            @"private $$");

    [Fact]
    public Task TestAfterProtected()
        => VerifyKeywordAsync(
            @"protected $$");

    [Fact]
    public async Task TestNotAfterSealed()
        => await VerifyAbsenceAsync(@"sealed $$");

    [Fact]
    public async Task TestAfterStatic()
    {
        await VerifyKeywordAsync(SourceCodeKind.Regular, @"static $$");
        await VerifyKeywordAsync(SourceCodeKind.Script, @"static $$");
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
    [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunction))]
    public async Task TestAfterKeywordIndicatingLocalFunction(string keyword)
    {
        await VerifyKeywordAsync(SourceCodeKind.Regular, AddInsideMethod(@$"{keyword} $$"));
        await VerifyKeywordAsync(SourceCodeKind.Script, AddInsideMethod(@$"{keyword} $$"));
    }

    [Fact]
    public async Task TestAfterStaticPublic()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static public $$");
        await VerifyKeywordAsync(SourceCodeKind.Script, @"static public $$");
    }

    [Fact]
    public async Task TestAfterDelegate()
        => await VerifyKeywordAsync(@"delegate $$");

    [Fact]
    public Task TestDelegateAsArgument()
        => VerifyKeywordAsync(AddInsideMethod(
            @"Assert.Throws<InvalidOperationException>($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInConstMemberInitializer1()
        => VerifyAbsenceAsync(
            """
            class E {
                const int a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInEnumMemberInitializer1()
        => VerifyAbsenceAsync(
            """
            enum E {
                a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInConstLocalInitializer1()
        => VerifyAbsenceAsync(
            """
            class E {
              void Goo() {
                const int a = $$
              }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestInMemberInitializer1()
        => VerifyKeywordAsync(
            """
            class E {
                int a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestInTypeOf()
        => VerifyKeywordAsync(AddInsideMethod(
            @"typeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestInDefault()
        => VerifyKeywordAsync(AddInsideMethod(
            @"default($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestInSizeOf()
        => VerifyKeywordAsync(AddInsideMethod(
            @"sizeof($$"));

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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607197")]
    public Task TestAfterAsyncInMethodBody()
        => VerifyKeywordAsync("""
            using System;
            class C
            {
                void M()
                {
                    Action a = async $$
            """);

    [Fact]
    public Task TestAfterAsyncInMemberDeclaration()
        => VerifyKeywordAsync("""
            using System;
            class C
            {
                async $$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeList()
        => VerifyKeywordAsync("""
            using System;
            class C
            {
                delegate*<$$
            """);

    [Fact]
    public Task TestNotInEnumBaseList()
        => VerifyAbsenceAsync("enum E : $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70076")]
    public Task TestNotInAttribute()
        => VerifyAbsenceAsync("""
            class C
            {
                [$$]
                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68399")]
    public Task TestNotInRecordParameterAttribute()
        => VerifyAbsenceAsync(
            """
            record R([$$] int i) { }
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
