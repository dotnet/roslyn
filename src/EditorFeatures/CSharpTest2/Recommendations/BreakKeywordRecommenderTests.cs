// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class BreakKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot()
        => VerifyAbsenceAsync(
@"$$", options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement()
        => VerifyAbsenceAsync(
            """
            System.Console.WriteLine();
            $$
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration()
        => VerifyAbsenceAsync(
            """
            int i = 0;
            $$
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Theory, CombinatorialData]
    public Task TestEmptyStatement(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestBeforeStatement(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            $$
            return true;
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterStatement(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            return true;
            $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBlock(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterIf(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            if (true) 
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterDo(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            do 
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterWhile(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            while (true) 
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterFor(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            for (int i = 0; i < 10; i++) 
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterForeach(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            foreach (var v in bar)
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotInsideLambda(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            foreach (var v in bar) {
               var d = () => {
                 $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotInsideAnonymousMethod(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            foreach (var v in bar) {
               var d = delegate {
                 $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInsideSwitch(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (a) {
                case 0:
                   $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotInsideSwitchWithLambda(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            switch (a) {
                case 0:
                  var d = () => {
                    $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInsideSwitchOutsideLambda(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (a) {
                case 0:
                  var d = () => {
                  };
                  $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterBreak(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"break $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);

    [Theory, CombinatorialData]
    public Task TestAfterYield(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"yield $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterSwitchInSwitch(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  switch (expr) {
                  }
                  $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBlockInSwitch(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  {
                  }
                  $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
}
