// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ContinueKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestBeforeStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            $$
            return true;
            """));

    [Fact]
    public Task TestAfterStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            return true;
            $$
            """));

    [Fact]
    public Task TestAfterBlock()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """));

    [Fact]
    public Task TestAfterIf()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            if (true) 
                $$
            """));

    [Fact]
    public Task TestAfterDo()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            do 
                $$
            """));

    [Fact]
    public Task TestAfterWhile()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            while (true) 
                $$
            """));

    [Fact]
    public Task TestAfterFor()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            for (int i = 0; i < 10; i++) 
                $$
            """));

    [Fact]
    public Task TestAfterForeach()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            foreach (var v in bar)
                $$
            """));

    [Fact]
    public Task TestNotInsideLambda()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            foreach (var v in bar) {
               var d = () => {
                 $$
            """));

    [Fact]
    public Task TestOutsideLambda()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            foreach (var v in bar) {
               var d = () => {
               };
               $$
            """));

    [Fact]
    public Task TestNotInsideAnonymousMethod()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            foreach (var v in bar) {
               var d = delegate {
                 $$
            """));

    [Fact]
    public Task TestNotInsideSwitch()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            switch (a) {
                case 0:
                  $$
            """));

    [Fact]
    public Task TestNotAfterContinue()
        => VerifyAbsenceAsync(AddInsideMethod(
@"continue $$"));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);
}
