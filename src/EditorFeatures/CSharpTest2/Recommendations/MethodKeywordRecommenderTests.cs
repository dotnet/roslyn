// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public class MethodKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public async Task TestNotAtRoot_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
    }

    [Fact]
    public async Task TestNotAfterClass_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);
    }

    [Fact]
    public async Task TestNotAfterGlobalStatement_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);
    }

    [Fact]
    public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);
    }

    [Fact]
    public async Task TestNotInUsingAlias()
    {
        await VerifyAbsenceAsync(
@"using Goo = $$");
    }

    [Fact]
    public async Task TestNotInGlobalUsingAlias()
    {
        await VerifyAbsenceAsync(
@"global using Goo = $$");
    }

    [Fact]
    public async Task TestNotInEmptyStatement()
    {
        await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
    }

    [Fact]
    public async Task TestInAttributeInsideClass()
    {
        await VerifyKeywordAsync(
            """
            class C {
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterAttributeInsideClass()
    {
        await VerifyKeywordAsync(
            """
            class C {
                [Goo]
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterMethod()
    {
        await VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                }
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterProperty()
    {
        await VerifyKeywordAsync(
            """
            class C {
                int Goo {
                    get;
                }
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterField()
    {
        await VerifyKeywordAsync(
            """
            class C {
                int Goo;
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterEvent()
    {
        await VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo;
                [$$
            """);
    }

    [Fact]
    public async Task TestNotInOuterAttribute()
    {
        await VerifyAbsenceAsync(
@"[$$");
    }

    [Fact]
    public async Task TestNotInParameterAttribute()
    {
        await VerifyAbsenceAsync(
            """
            class C {
                void Goo([$$
            """);
    }

    [Fact]
    public async Task TestInPropertyAttribute1()
    {
        await VerifyKeywordAsync(
            """
            class C {
                int Goo { [$$
            """);
    }

    [Fact]
    public async Task TestInPropertyAttribute2()
    {
        await VerifyKeywordAsync(
            """
            class C {
                int Goo { get { } [$$
            """);
    }

    [Fact]
    public async Task TestInEventAttribute1()
    {
        await VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);
    }

    [Fact]
    public async Task TestInEventAttribute2()
    {
        await VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo { add { } [$$
            """);
    }

    [Fact]
    public async Task TestNotInTypeParameters()
    {
        await VerifyAbsenceAsync(
@"class C<[$$");
    }

    [Fact]
    public async Task TestInInterface()
    {
        await VerifyKeywordAsync(
            """
            interface I {
                [$$
            """);
    }

    [Fact]
    public async Task TestInStruct()
    {
        await VerifyKeywordAsync(
            """
            struct S {
                [$$
            """);
    }

    [Fact]
    public async Task TestNotInEnum()
    {
        await VerifyAbsenceAsync(
            """
            enum E {
                [$$
            """);
    }

    [Fact]
    public async Task TestPrimaryConstructor1()
    {
        await VerifyKeywordAsync("""
            [$$
            class C()
            {
            }
            """);
    }

    [Fact]
    public async Task TestPrimaryConstructor2()
    {
        await VerifyKeywordAsync("""
            [$$
            struct C()
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70077")]
    public async Task TestLocalFunction()
    {
        await VerifyKeywordAsync("""
            class C
            {
                void M()
                {
                    [$$
                    void F()
                    {
                    }
                }
            }
            """);
    }
}
