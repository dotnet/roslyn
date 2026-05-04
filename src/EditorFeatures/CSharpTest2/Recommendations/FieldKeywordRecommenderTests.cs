// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class FieldKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """$$""");

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
            """using Goo = $$""");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
            """global using Goo = $$""");

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
            """$$"""));

    [Fact]
    public Task TestInAttributeInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeArgumentInsideClass1()
        => VerifyAbsenceAsync(
            """
            class C {
                [field: $$
            """);

    [Fact]
    public Task TestNotInAttributeArgumentInsideClass2()
        => VerifyAbsenceAsync(
            """
            class C {
                [field: Goo($$)]
            """);

    [Fact]
    public Task TestNotInAttributeArgumentInsideClass3()
        => VerifyAbsenceAsync(
            """
            class C {
                [field: Goo($$)] int Prop { get; }
            """);

    [Theory]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task TestInAttributeInsideRecord(string record)
        => VerifyWorkerAsync(
            $$"""
            {{record}} C {
                [$$
            """, absent: false, TestOptions.RegularPreview);

    [Fact]
    public Task TestInAttributeAfterAttributeInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
                [Goo]
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                }
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo {
                    get;
                }
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterField()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo;
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterEvent()
        => VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo;
                [$$
            """);

    [Fact]
    public Task TestNotInOuterAttribute()
        => VerifyAbsenceAsync(
            """[$$""");

    [Fact]
    public Task TestNotInParameterAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo([$$
            """);

    [Fact]
    public Task TestNotInPropertyAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { [$$
            """);

    [Fact]
    public Task TestNotInEventAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);

    [Fact]
    public Task TestNotInTypeParameters()
        => VerifyAbsenceAsync(
            """class C<[$$""");

    [Fact]
    public Task TestNotInInterface()
        => VerifyAbsenceAsync(
            """
            interface I {
                [$$
            """);

    [Fact]
    public Task TestInStruct()
        => VerifyKeywordAsync(
            """
            struct S {
                [$$
            """);

    [Fact]
    public Task TestInEnum()
        => VerifyKeywordAsync(
            """
            enum E {
                [$$
            """);

    [Fact]
    public Task TestNotInPropertyInitializer()
        => VerifyAbsenceAsync(
            """
            class C
            {
                int Goo { get; } = $$
            }
            """);

    [Fact]
    public Task TestInPropertyExpressionBody()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo => $$
            }
            """);

    [Fact]
    public Task TestNotInPropertyExpressionBody_NotPrimary()
        => VerifyAbsenceAsync(
            """
            class C
            {
                int Goo => this.$$
            }
            """);

    [Fact]
    public Task TestInPropertyAccessor1()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get => $$ }
            }
            """);

    [Fact]
    public Task TestInPropertyAccessor2()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { return $$ } }
            }
            """);

    [Fact]
    public Task TestInPropertyStatement()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { $$ } }
            }
            """);

    [Fact]
    public Task TestInPropertyExpressionContext()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { var v = 1 + $$ } }
            }
            """);

    [Fact]
    public Task TestInPropertyArgument1()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { Bar($$) } }
            }
            """);

    [Fact]
    public Task TestInPropertyArgument2()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { Bar(ref $$) } }
            }
            """);

    [Fact]
    public Task TestNotInPropertyNameof()
        => VerifyAbsenceAsync(
            """
            class C
            {
                int Goo { get { Bar(nameof($$)) } }
            }
            """);

    [Fact]
    public Task TestInLocalFunctionInProperty()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        void Bar() { return $$ }
                    }
                }
            }
            """);

    [Fact]
    public Task TestInLambdaInProperty()
        => VerifyKeywordAsync(
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        var v = customers.Where(c => c.Age > $$);
                    }
                }
            }
            """);

    [Fact]
    public Task TestNotInAccessorAttribute()
        => VerifyAbsenceAsync(
            """
            class C
            {
                [Bar($$)]
                int Goo { get; }
            }
            """);

    [Fact]
    public Task TestNotInIndexer1()
        => VerifyAbsenceAsync(
            """
            class C
            {
                int this[int index] => $$
            }
            """);

    [Fact]
    public Task TestNotInIndexer2()
        => VerifyAbsenceAsync(
            """
            class C
            {
                int this[int index] { get => $$ }
            }
            """);

    [Fact]
    public Task TestNotInIndexer3()
        => VerifyAbsenceAsync(
            """
            class C
            {
                int this[int index] { get { return $$ } }
            }
            """);

    [Fact]
    public Task TestNotInEvent1()
        => VerifyAbsenceAsync(
            """
            class C
            {
                event Action E { add { $$ } }
            }
            """);

    [Fact]
    public Task TestNotInMethodContext()
        => VerifyAbsenceAsync(
            """
            class C
            {
                void M()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task TestNotInMethodExpressionContext()
        => VerifyAbsenceAsync(
            """
            class C
            {
                void M()
                {
                    Goo($$);
                }
            }
            """);

    [Fact]
    public Task TestNotInGlobalStatement()
        => VerifyAbsenceAsync(
            """
            $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68399")]
    public Task TestInRecordParameterAttribute()
        => VerifyKeywordAsync(
            """
                record R([$$] int i) { }
                """);
}
