// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class AssemblyKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestNotInAttributeInsideClass()
        => VerifyAbsenceAsync(
            """
            class C {
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterAttributeInsideClass()
        => VerifyAbsenceAsync(
            """
            class C {
                [Goo]
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterMethod()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                }
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterProperty()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo {
                    get;
                }
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterField()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo;
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterEvent()
        => VerifyAbsenceAsync(
            """
            class C {
                event Action<int> Goo;
                [$$
            """);

    [Fact]
    public Task TestInOuterAttribute()
        => VerifyKeywordAsync(
@"[$$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestNotInAttributeNestClass()
        => VerifyAbsenceAsync(
            """
            class A
            {
                [$$
                class B
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInAttributeBeforeNamespace()
        => VerifyKeywordAsync(
            """
            [$$
            namespace Goo {
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInAttributeBeforeFileScopedNamespace()
        => VerifyKeywordAsync(
            """
            [$$
            namespace Goo;
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestNotInAttributeBeforeNamespaceWithoutOpenBracket()
        => VerifyAbsenceAsync(
            """
            $$
            namespace Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestNotInAttributeBeforeNamespaceAndAfterUsingWithNoOpenBracket()
        => VerifyAbsenceAsync(
            """
            using System;

            $$
            namespace Goo {}
            """);

    [Fact]
    public Task TestNotInAttributeBeforeNamespaceAndAfterGlobalUsingWithNoOpenBracket()
        => VerifyAbsenceAsync(
            """
            global using System;

            $$
            namespace Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInAttributeBeforeNamespaceAndAfterUsingWithOpenBracket()
        => VerifyKeywordAsync(
            """
            using System;

            [$$
            namespace Goo {}
            """);

    [Fact]
    public Task TestInAttributeBeforeNamespaceAndAfterGlobalUsingWithOpenBracket()
        => VerifyKeywordAsync(
            """
            global using System;

            [$$
            namespace Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInAttributeBeforeAssemblyWithOpenBracket()
        => VerifyKeywordAsync(
            """
            [$$
            [assembly: Whatever]
            namespace Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInAttributeBeforeClass()
        => VerifyKeywordAsync(
            """
            [$$
            class Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInAttributeBeforeInterface()
        => VerifyKeywordAsync(
            """
            [$$
            interface IGoo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInAttributeBeforeStruct()
        => VerifyKeywordAsync(
            """
            [$$
            struct Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInAttributeBeforeEnum()
        => VerifyKeywordAsync(
            """
            [$$
            enum Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestNotInAttributeBeforeOtherAttributeWithoutOpenBracket()
        => VerifyAbsenceAsync(
            """
            $$
            [assembly: Whatever]
            namespace Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestNotInAttributeBeforeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        => VerifyAbsenceAsync(
            """
            using System;

            $$
            [assembly: Whatever]
            namespace Goo {}
            """);

    [Fact]
    public Task TestNotInAttributeBeforeAssemblyAttributeAndAfterGlobalUsingWithoutOpenBracket()
        => VerifyAbsenceAsync(
            """
            global using System;

            $$
            [assembly: Whatever]
            namespace Goo {}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestInBeforeAttributeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        => VerifyKeywordAsync(
            """
            using System;

            [$$
            [assembly: Whatever]
            namespace Goo {}
            """);

    [Fact]
    public Task TestInBeforeAttributeAssemblyAttributeAndAfterGlobalUsingWithoutOpenBracket()
        => VerifyKeywordAsync(
            """
            global using System;

            [$$
            [assembly: Whatever]
            namespace Goo {}
            """);

    [Fact]
    public Task TestNotInOuterAttributeInNamespace()
        => VerifyAbsenceAsync(
            """
            namespace Goo {
                 [$$
            """);

    [Fact]
    public Task TestNotInParameterAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo([$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestNotInElementAccess()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(string[] array) {
                    array[$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
    public Task TestNotInIndex()
        => VerifyAbsenceAsync(
            """
            class C {
                public int this[$$
            """);

    [Fact]
    public Task TestNotInPropertyAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { [$$
            """);

    [Fact]
    public Task TestNotInEventAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);

    [Fact]
    public Task TestNotInClassAssemblyParameters()
        => VerifyAbsenceAsync(
@"class C<[$$");

    [Fact]
    public Task TestNotInDelegateAssemblyParameters()
        => VerifyAbsenceAsync(
@"delegate void D<[$$");

    [Fact]
    public Task TestNotInMethodAssemblyParameters()
        => VerifyAbsenceAsync(
            """
            class C {
                void M<[$$
            """);

    [Fact]
    public Task TestNotInInterface()
        => VerifyAbsenceAsync(
            """
            interface I {
                [$$
            """);

    [Fact]
    public Task TestNotInStruct()
        => VerifyAbsenceAsync(
            """
            struct S {
                [$$
            """);

    [Fact]
    public Task TestNotInEnum()
        => VerifyAbsenceAsync(
            """
            enum E {
                [$$
            """);
}
