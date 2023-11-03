﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class OperatorKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestAfterImplicit()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterExplicit()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static explicit $$
                """);
        }

        [Fact]
        public async Task TestNotAfterType()
        {
            await VerifyAbsenceAsync(
                """
                class Goo {
                    int $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542271")]
        public async Task TestAfterPublicStaticType()
        {
            await VerifyAbsenceAsync(
                """
                class Goo {
                    public static int $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542271")]
        public async Task TestAfterPublicStaticExternType()
        {
            await VerifyAbsenceAsync(
                """
                class Goo {
                    public static extern int $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542271")]
        public async Task TestAfterGenericType()
        {
            await VerifyAbsenceAsync(
                """
                class Goo {
                    public static IList<int> $$
                """);
        }

        [Fact]
        public async Task TestNotInInterface()
        {
            await VerifyAbsenceAsync(
                """
                interface Goo {
                    public static int $$
                """);
        }
    }
}
