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
    public class FixedKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInsideEmptyMethod()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestInsideUnsafeBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                unsafe {
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterFixed()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                unsafe {
                    fixed (int* = bar) {
                    }
                    $$
                """));
        }

        [Fact]
        public async Task TestNotAfterFixed()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                fixed (int* = bar) {
                  }
                  $$
                """));
        }

        [Fact]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    $$
                """);
        }

        [Fact]
        public async Task TestNotInStruct()
        {
            await VerifyAbsenceAsync(
                """
                struct S {
                    $$
                """);
        }

        [Fact]
        public async Task TestNotInRecordStruct()
        {
            await VerifyAbsenceAsync(
                """
                record struct S {
                    $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeStruct()
        {
            await VerifyKeywordAsync(
                """
                unsafe struct S {
                    $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeNestedStruct1()
        {
            await VerifyKeywordAsync(
                """
                unsafe struct S {
                    struct T {
                      $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeNestedStruct2()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                    unsafe struct T {
                      $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStatic()
        {
            await VerifyAbsenceAsync(
                """
                unsafe struct S {
                    static $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52296")]
        public async Task TestInUnsafeLocalFunction()
        {
            await VerifyKeywordAsync(
                """
                public class C
                {
                    public void M()
                    {
                        unsafe void Local()
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52296")]
        public async Task TestNotInOrdinaryLocalFunction()
        {
            await VerifyAbsenceAsync(
                """
                public class C
                {
                    public void M()
                    {
                        void Local()
                        {
                            $$
                        }
                    }
                }
                """);
        }
    }
}
