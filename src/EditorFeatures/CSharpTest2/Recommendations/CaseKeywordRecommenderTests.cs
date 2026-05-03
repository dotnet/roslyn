// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class CaseKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotAfterExpr()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var q = goo $$"));

    [Fact]
    public Task TestNotAfterDottedName()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var q = goo.Current $$"));

    [Fact]
    public Task TestAfterSwitch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                $$
            """));

    [Fact]
    public Task TestAfterCase()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                case 0:
                $$
            """));

    [Fact]
    public Task TestAfterDefault()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                $$
            """));

    [Fact]
    public Task TestAfterPatternCase()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                case String s:
                $$
            """));

    [Fact]
    public Task TestAfterOneStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  Console.WriteLine();
                $$
            """));

    [Fact]
    public Task TestAfterOneStatementPatternCase()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                case String s:
                  Console.WriteLine();
                $$
            """));

    [Fact]
    public Task TestAfterTwoStatements()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  Console.WriteLine();
                  Console.WriteLine();
                $$
            """));

    [Fact]
    public Task TestAfterBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default: {
                }
                $$
            """));

    [Fact]
    public Task TestAfterBlockPatternCase()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                case String s: {
                }
                $$
            """));

    [Fact]
    public Task TestAfterIfElse()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  if (goo) {
                  } else {
                  }
                $$
            """));

    [Fact]
    public Task TestNotAfterIncompleteStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                   Console.WriteLine(
                $$
            """));

    [Fact]
    public Task TestNotInsideBlock()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            switch (expr) {
                default: {
                  $$
            """));

    [Fact]
    public Task TestAfterIf()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  if (goo)
                    Console.WriteLine();
                $$
            """));

    [Fact]
    public Task TestNotAfterIf()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  if (goo)
                    $$
            """));

    [Fact]
    public Task TestAfterWhile()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  while (true) {
                  }
                $$
            """));

    [Fact]
    public Task TestAfterGotoInSwitch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  goto $$
            """));

    [Fact]
    public Task TestNotAfterGotoOutsideSwitch()
        => VerifyAbsenceAsync(AddInsideMethod(
@"goto $$"));
}
