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
    public class AssemblyKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInAttributeInsideClass()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    [$$
                """);
        }

        [Fact]
        public async Task TestNotInAttributeAfterAttributeInsideClass()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    [Goo]
                    [$$
                """);
        }

        [Fact]
        public async Task TestNotInAttributeAfterMethod()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                    }
                    [$$
                """);
        }

        [Fact]
        public async Task TestNotInAttributeAfterProperty()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo {
                        get;
                    }
                    [$$
                """);
        }

        [Fact]
        public async Task TestNotInAttributeAfterField()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo;
                    [$$
                """);
        }

        [Fact]
        public async Task TestNotInAttributeAfterEvent()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    event Action<int> Goo;
                    [$$
                """);
        }

        [Fact]
        public async Task TestInOuterAttribute()
        {
            await VerifyKeywordAsync(
@"[$$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeNestClass()
        {
            await VerifyAbsenceAsync(
                """
                class A
                {
                    [$$
                    class B
                    {
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeNamespace()
        {
            await VerifyKeywordAsync(
                """
                [$$
                namespace Goo {
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeFileScopedNamespace()
        {
            await VerifyKeywordAsync(
                """
                [$$
                namespace Goo;
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeBeforeNamespaceWithoutOpenBracket()
        {
            await VerifyAbsenceAsync(
                """
                $$
                namespace Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeBeforeNamespaceAndAfterUsingWithNoOpenBracket()
        {
            await VerifyAbsenceAsync(
                """
                using System;

                $$
                namespace Goo {}
                """);
        }

        [Fact]
        public async Task TestNotInAttributeBeforeNamespaceAndAfterGlobalUsingWithNoOpenBracket()
        {
            await VerifyAbsenceAsync(
                """
                global using System;

                $$
                namespace Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeNamespaceAndAfterUsingWithOpenBracket()
        {
            await VerifyKeywordAsync(
                """
                using System;

                [$$
                namespace Goo {}
                """);
        }

        [Fact]
        public async Task TestInAttributeBeforeNamespaceAndAfterGlobalUsingWithOpenBracket()
        {
            await VerifyKeywordAsync(
                """
                global using System;

                [$$
                namespace Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeAssemblyWithOpenBracket()
        {
            await VerifyKeywordAsync(
                """
                [$$
                [assembly: Whatever]
                namespace Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeClass()
        {
            await VerifyKeywordAsync(
                """
                [$$
                class Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeInterface()
        {
            await VerifyKeywordAsync(
                """
                [$$
                interface IGoo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeStruct()
        {
            await VerifyKeywordAsync(
                """
                [$$
                struct Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeEnum()
        {
            await VerifyKeywordAsync(
                """
                [$$
                enum Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeBeforeOtherAttributeWithoutOpenBracket()
        {
            await VerifyAbsenceAsync(
                """
                $$
                [assembly: Whatever]
                namespace Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeBeforeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        {
            await VerifyAbsenceAsync(
                """
                using System;

                $$
                [assembly: Whatever]
                namespace Goo {}
                """);
        }

        [Fact]
        public async Task TestNotInAttributeBeforeAssemblyAttributeAndAfterGlobalUsingWithoutOpenBracket()
        {
            await VerifyAbsenceAsync(
                """
                global using System;

                $$
                [assembly: Whatever]
                namespace Goo {}
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInBeforeAttributeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        {
            await VerifyKeywordAsync(
                """
                using System;

                [$$
                [assembly: Whatever]
                namespace Goo {}
                """);
        }

        [Fact]
        public async Task TestInBeforeAttributeAssemblyAttributeAndAfterGlobalUsingWithoutOpenBracket()
        {
            await VerifyKeywordAsync(
                """
                global using System;

                [$$
                [assembly: Whatever]
                namespace Goo {}
                """);
        }

        [Fact]
        public async Task TestNotInOuterAttributeInNamespace()
        {
            await VerifyAbsenceAsync(
                """
                namespace Goo {
                     [$$
                """);
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInElementAccess()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(string[] array) {
                        array[$$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInIndex()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    public int this[$$
                """);
        }

        [Fact]
        public async Task TestNotInPropertyAttribute()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { [$$
                """);
        }

        [Fact]
        public async Task TestNotInEventAttribute()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    event Action<int> Goo { [$$
                """);
        }

        [Fact]
        public async Task TestNotInClassAssemblyParameters()
        {
            await VerifyAbsenceAsync(
@"class C<[$$");
        }

        [Fact]
        public async Task TestNotInDelegateAssemblyParameters()
        {
            await VerifyAbsenceAsync(
@"delegate void D<[$$");
        }

        [Fact]
        public async Task TestNotInMethodAssemblyParameters()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void M<[$$
                """);
        }

        [Fact]
        public async Task TestNotInInterface()
        {
            await VerifyAbsenceAsync(
                """
                interface I {
                    [$$
                """);
        }

        [Fact]
        public async Task TestNotInStruct()
        {
            await VerifyAbsenceAsync(
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
    }
}
