// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class UnmanagedKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestNotAfterName_Type()
        => VerifyAbsenceAsync(
@"class Test $$");

    [Fact]
    public Task TestNotAfterWhereClause_Type()
        => VerifyAbsenceAsync(
@"class Test<T> where $$");

    [Fact]
    public Task TestNotAfterWhereClauseType_Type()
        => VerifyAbsenceAsync(
@"class Test<T> where T $$");

    [Fact]
    public Task TestAfterWhereClauseColon_Type()
        => VerifyKeywordAsync(
@"class Test<T> where T : $$");

    [Fact]
    public Task TestNotAfterTypeConstraint_Type()
        => VerifyAbsenceAsync(
@"class Test<T> where T : I $$");

    [Fact]
    public Task TestAfterTypeConstraintComma_Type()
        => VerifyKeywordAsync(
@"class Test<T> where T : I, $$");

    [Fact]
    public Task TestNotAfterName_Method()
        => VerifyAbsenceAsync(
            """
            class Test {
                void M $$
            """);

    [Fact]
    public Task TestNotAfterWhereClause_Method()
        => VerifyAbsenceAsync(
            """
            class Test {
                void M<T> where $$
            """);

    [Fact]
    public Task TestNotAfterWhereClauseType_Method()
        => VerifyAbsenceAsync(
            """
            class Test {
                void M<T> where T $$
            """);

    [Fact]
    public Task TestAfterWhereClauseColon_Method()
        => VerifyKeywordAsync(
            """
            class Test {
                void M<T> where T : $$
            """);

    [Fact]
    public Task TestNotAfterTypeConstraint_Method()
        => VerifyAbsenceAsync(
            """
            class Test {
                void M<T> where T : I $$
            """);

    [Fact]
    public Task TestAfterTypeConstraintComma_Method()
        => VerifyKeywordAsync(
            """
            class Test {
                void M<T> where T : I, $$
            """);

    [Fact]
    public Task TestNotAfterName_Delegate()
        => VerifyAbsenceAsync(
@"delegate void D $$");

    [Fact]
    public Task TestNotAfterWhereClause_Delegate()
        => VerifyAbsenceAsync(
@"delegate void D<T>() where $$");

    [Fact]
    public Task TestNotAfterWhereClauseType_Delegate()
        => VerifyAbsenceAsync(
@"delegate void D<T>() where T $$");

    [Fact]
    public Task TestAfterWhereClauseColon_Delegate()
        => VerifyKeywordAsync(
@"delegate void D<T>() where T : $$");

    [Fact]
    public Task TestNotAfterTypeConstraint_Delegate()
        => VerifyAbsenceAsync(
@"delegate void D<T>() where T : I $$");

    [Fact]
    public Task TestAfterTypeConstraintComma_Delegate()
        => VerifyKeywordAsync(
@"delegate void D<T>() where T : I, $$");

    [Fact]
    public Task TestNotAfterName_LocalFunction()
        => VerifyAbsenceAsync(
            """
            class Test {
                void N() {
                    void M $$
            """);

    [Fact]
    public Task TestNotAfterWhereClause_LocalFunction()
        => VerifyAbsenceAsync(
            """
            class Test {
                void N() {
                    void M<T> where $$
            """);

    [Fact]
    public Task TestNotAfterWhereClauseType_LocalFunction()
        => VerifyAbsenceAsync(
            """
            class Test {
                void N() {
                    void M<T> where T $$
            """);

    [Fact]
    public Task TestAfterWhereClauseColon_LocalFunction()
        => VerifyKeywordAsync(
            """
            class Test {
                void N() {
                    void M<T> where T : $$
            """);

    [Fact]
    public Task TestNotAfterTypeConstraint_LocalFunction()
        => VerifyAbsenceAsync(
            """
            class Test {
                void N() {
                    void M<T> where T : I $$
            """);

    [Fact]
    public Task TestAfterTypeConstraintComma_LocalFunction()
        => VerifyKeywordAsync(
            """
            class Test {
                void N() {
                    void M<T> where T : I, $$
            """);

    [Fact]
    public Task TestInFunctionPointerDeclaration()
        => VerifyKeywordAsync(
            """
            class Test {
                unsafe void N() {
                    delegate* $$
            """);

    [Fact]
    public Task TestInFunctionPointerDeclarationTouchingAsterisk()
        => VerifyKeywordAsync(
            """
            class Test {
                unsafe void N() {
                    delegate*$$
            """);
}
