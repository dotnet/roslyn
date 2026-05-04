// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class GetKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { $$
            """);

    [Fact]
    public Task TestAfterPropertyPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { private $$
            """);

    [Fact]
    public Task TestAfterPropertyAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { [Bar] $$
            """);

    [Fact]
    public Task TestAfterPropertyAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { [Bar] private $$
            """);

    [Fact]
    public Task TestAfterPropertySet()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { set; $$
            """);

    [Fact]
    public Task TestAfterPropertySetAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { set; private $$
            """);

    [Fact]
    public Task TestAfterPropertySetAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { set; [Bar] $$
            """);

    [Fact]
    public Task TestAfterPropertySetAndAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { set; [Bar] private $$
            """);

    [Fact]
    public Task TestAfterSetAccessorBlock()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { set { } $$
            """);

    [Fact]
    public Task TestAfterSetAccessorBlockAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { set { } private $$
            """);

    [Fact]
    public Task TestAfterSetAccessorBlockAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { set { } [Bar] $$
            """);

    [Fact]
    public Task TestAfterSetAccessorBlockAndAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { set { } [Bar] private $$
            """);

    [Fact]
    public Task TestNotAfterPropertyGetKeyword()
        => VerifyAbsenceAsync(
            """
            class C {
               int Goo { get $$
            """);

    [Fact]
    public Task TestNotAfterPropertyGetAccessor()
        => VerifyAbsenceAsync(
            """
            class C {
               int Goo { get; $$
            """);

    [Fact]
    public Task TestNotInEvent()
        => VerifyAbsenceAsync(
            """
            class C {
               event Goo E { $$
            """);

    [Fact]
    public Task TestAfterIndexer()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { $$
            """);

    [Fact]
    public Task TestAfterIndexerPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { private $$
            """);

    [Fact]
    public Task TestAfterIndexerAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { [Bar] $$
            """);

    [Fact]
    public Task TestAfterIndexerAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { [Bar] private $$
            """);

    [Fact]
    public Task TestAfterIndexerSet()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { set; $$
            """);

    [Fact]
    public Task TestAfterIndexerSetAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { set; private $$
            """);

    [Fact]
    public Task TestAfterIndexerSetAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { set; [Bar] $$
            """);

    [Fact]
    public Task TestAfterIndexerSetAndAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { set; [Bar] private $$
            """);

    [Fact]
    public Task TestAfterIndexerSetBlock()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { set { } $$
            """);

    [Fact]
    public Task TestAfterIndexerSetBlockAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { set { } private $$
            """);

    [Fact]
    public Task TestAfterIndexerSetBlockAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { set { } [Bar] $$
            """);

    [Fact]
    public Task TestAfterIndexerSetBlockAndAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { set { } [Bar] private $$
            """);

    [Fact]
    public Task TestNotAfterIndexerGetKeyword()
        => VerifyAbsenceAsync(
            """
            class C {
               int this[int i] { get $$
            """);

    [Fact]
    public Task TestNotAfterIndexerGetAccessor()
        => VerifyAbsenceAsync(
            """
            class C {
               int this[int i] { get; $$
            """);

    [Fact]
    public Task TestBeforeSemicolon()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { $$; }
            """);

    [Fact]
    public Task TestAfterProtectedInternal()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { protected internal $$ }
            """);

    [Fact]
    public Task TestAfterInternalProtected()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { internal protected $$ }
            """);
}
