// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class FixedKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotInsideEmptyMethod()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInsideUnsafeBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            unsafe {
                $$
            """));

    [Fact]
    public Task TestAfterFixed()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            unsafe {
                fixed (int* = bar) {
                }
                $$
            """));

    [Fact]
    public Task TestNotAfterFixed()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            fixed (int* = bar) {
              }
              $$
            """));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync(
            """
            class C {
                $$
            """);

    [Fact]
    public Task TestNotInStruct()
        => VerifyAbsenceAsync(
            """
            struct S {
                $$
            """);

    [Fact]
    public Task TestNotInRecordStruct()
        => VerifyAbsenceAsync(
            """
            record struct S {
                $$
            """);

    [Fact]
    public Task TestInUnsafeStruct()
        => VerifyKeywordAsync(
            """
            unsafe struct S {
                $$
            """);

    [Fact]
    public Task TestInUnsafeNestedStruct1()
        => VerifyKeywordAsync(
            """
            unsafe struct S {
                struct T {
                  $$
            """);

    [Fact]
    public Task TestInUnsafeNestedStruct2()
        => VerifyKeywordAsync(
            """
            struct S {
                unsafe struct T {
                  $$
            """);

    [Fact]
    public Task TestNotAfterStatic()
        => VerifyAbsenceAsync(
            """
            unsafe struct S {
                static $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52296")]
    public Task TestInUnsafeLocalFunction()
        => VerifyKeywordAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52296")]
    public Task TestNotInOrdinaryLocalFunction()
        => VerifyAbsenceAsync(
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
