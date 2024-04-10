// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ParamsKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<$$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<in $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<Goo, $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<[Goo]$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"delegate void D<$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"delegate void D<Goo, $$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"delegate void D<[Goo]$$");
        }

        [Fact]
        public async Task TestNotParamsBaseListAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo : Bar<$$");
        }

        [Fact]
        public async Task TestNotInGenericMethod()
        {
            await VerifyAbsenceAsync(
                """
                interface IGoo {
                    void Goo<$$
                """);
        }

        [Fact]
        public async Task TestNotAfterParams()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterOut()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(out $$
                """);
        }

        [Fact]
        public async Task TestNotAfterThis()
        {
            await VerifyAbsenceAsync(
                """
                static class C {
                    static void Goo(this $$
                """);
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
        public async Task TestAfterLambdaOpenParen()
        {
            await VerifyKeywordAsync(
@"var lam = ($$");
        }

        [Fact]
        public async Task TestAfterLambdaComma()
        {
            await VerifyKeywordAsync(
@"var lam = (int i, $$");
        }

        [Fact]
        public async Task TestNotAfterOperator()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static int operator +($$
                """);
        }

        [Fact]
        public async Task TestNotAfterDestructor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    ~C($$
                """);
        }

        [Fact]
        public async Task TestAfterIndexer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int this[$$
                """);
        }

        [Fact]
        public async Task TestNotInObjectCreationAfterOpenParen()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      new Bar($$
                """);
        }

        [Fact]
        public async Task TestNotAfterParamsParam()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      new Bar(ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterOutParam()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      new Bar(out $$
                """);
        }

        [Fact]
        public async Task TestNotInObjectCreationAfterComma()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      new Bar(baz, $$
                """);
        }

        [Fact]
        public async Task TestNotInObjectCreationAfterSecondComma()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      new Bar(baz, quux, $$
                """);
        }

        [Fact]
        public async Task TestNotInObjectCreationAfterSecondNamedParam()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      new Bar(baz: 4, quux: $$
                """);
        }

        [Fact]
        public async Task TestNotInInvocationExpression()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      Bar($$
                """);
        }

        [Fact]
        public async Task TestNotInInvocationAfterComma()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      Bar(baz, $$
                """);
        }

        [Fact]
        public async Task TestNotInInvocationAfterSecondComma()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      Bar(baz, quux, $$
                """);
        }

        [Fact]
        public async Task TestNotInInvocationAfterSecondNamedParam()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      Bar(baz: 4, quux: $$
                """);
        }
    }
}
