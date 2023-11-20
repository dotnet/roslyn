// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public class DelegateKeywordRecommenderTests : KeywordRecommenderTests
{
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
    public async Task TestInUsingAliasTypeParameter()
    {
        // Valid case: using Goo = System.Collections.Generic.IList<delegate*<void>[]>;
        await VerifyKeywordAsync(
            @"using Goo = T<$$");
    }

    [Fact]
    public async Task TestInGlobalUsingAliasTypeParameter()
    {
        // Valid case: global using Goo = System.Collections.Generic.IList<delegate*<void>[]>;
        await VerifyKeywordAsync(
            @"global using Goo = T<$$");
    }

    [Fact]
    public async Task TestInEmptyStatement()
    {
        await VerifyKeywordAsync(AddInsideMethod(
            @"$$"));
    }

    [Fact]
    public async Task TestInCompilationUnit()
    {
        await VerifyKeywordAsync(
            @"$$");
    }

    [Fact]
    public async Task TestAfterExtern()
    {
        await VerifyKeywordAsync(
            """
            extern alias Goo;
            $$
            """);
    }

    [Fact]
    public async Task TestAfterUsing()
    {
        await VerifyKeywordAsync(
            """
            using Goo;
            $$
            """);
    }

    [Fact]
    public async Task TestAfterGlobalUsing()
    {
        await VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """);
    }

    [Fact]
    public async Task TestAfterNamespace()
    {
        await VerifyKeywordAsync(
            """
            namespace N {}
            $$
            """);
    }

    [Fact]
    public async Task TestAfterFileScopedNamespace()
    {
        await VerifyKeywordAsync(
            """
            namespace N;
            $$
            """);
    }

    [Fact]
    public async Task TestAfterTypeDeclaration()
    {
        await VerifyKeywordAsync(
            """
            class C {}
            $$
            """);
    }

    [Fact]
    public async Task TestAfterDelegateDeclaration()
    {
        await VerifyKeywordAsync(
            """
            delegate void Goo();
            $$
            """);
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
    public async Task TestNotBeforeUsing()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            using Goo;
            """);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public async Task TestNotBeforeUsing_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            using Goo;
            """);
    }

    [Fact]
    public async Task TestNotBeforeGlobalUsing()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            global using Goo;
            """);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public async Task TestNotBeforeGlobalUsing_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            global using Goo;
            """);
    }

    [Fact]
    public async Task TestAfterAssemblyAttribute()
    {
        await VerifyKeywordAsync(
            """
            [assembly: goo]
            $$
            """);
    }

    [Fact]
    public async Task TestAfterRootAttribute()
    {
        await VerifyKeywordAsync(
            """
            [goo]
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
        await VerifyKeywordAsync("""
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
    public async Task TestNotAfterAbstract()
        => await VerifyAbsenceAsync(@"abstract $$");

    [Fact]
    public async Task TestAfterInternal()
    {
        await VerifyKeywordAsync(
            @"internal $$");
    }

    [Fact]
    public async Task TestAfterPublic()
    {
        await VerifyKeywordAsync(
            @"public $$");
    }

    [Fact]
    public async Task TestAfterPrivate()
    {
        await VerifyKeywordAsync(
            @"private $$");
    }

    [Fact]
    public async Task TestAfterProtected()
    {
        await VerifyKeywordAsync(
            @"protected $$");
    }

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
    public async Task TestDelegateAsArgument()
    {
        await VerifyKeywordAsync(AddInsideMethod(
            @"Assert.Throws<InvalidOperationException>($$"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public async Task TestNotInConstMemberInitializer1()
    {
        await VerifyAbsenceAsync(
            """
            class E {
                const int a = $$
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public async Task TestNotInEnumMemberInitializer1()
    {
        await VerifyAbsenceAsync(
            """
            enum E {
                a = $$
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public async Task TestNotInConstLocalInitializer1()
    {
        await VerifyAbsenceAsync(
            """
            class E {
              void Goo() {
                const int a = $$
              }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public async Task TestInMemberInitializer1()
    {
        await VerifyKeywordAsync(
            """
            class E {
                int a = $$
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public async Task TestInTypeOf()
    {
        await VerifyKeywordAsync(AddInsideMethod(
            @"typeof($$"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public async Task TestInDefault()
    {
        await VerifyKeywordAsync(AddInsideMethod(
            @"default($$"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public async Task TestInSizeOf()
    {
        await VerifyKeywordAsync(AddInsideMethod(
            @"sizeof($$"));
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607197")]
    public async Task TestAfterAsyncInMethodBody()
    {
        await VerifyKeywordAsync("""
            using System;
            class C
            {
                void M()
                {
                    Action a = async $$
            """);
    }

    [Fact]
    public async Task TestAfterAsyncInMemberDeclaration()
    {
        await VerifyKeywordAsync("""
            using System;
            class C
            {
                async $$
            """);
    }

    [Fact]
    public async Task TestInFunctionPointerTypeList()
    {
        await VerifyKeywordAsync("""
            using System;
            class C
            {
                delegate*<$$
            """);
    }

    [Fact]
    public async Task TestNotInEnumBaseList()
    {
        await VerifyAbsenceAsync("enum E : $$");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70076")]
    public async Task TestNotInAttribute()
    {
        await VerifyAbsenceAsync("""
            class C
            {
                [$$]
                void M()
                {
                }
            }
            """);
    }

    #region Collection expressions

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_BeforeFirstElementToVar()
    {
        await VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [$$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_BeforeFirstElementToReturn()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [$$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_AfterFirstElementToVar()
    {
        await VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [new object(), $$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_AfterFirstElementToReturn()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, $$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. $$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. $$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [($$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, ($$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. ($$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public async Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. ($$
            }
            """);
    }

    #endregion
}
