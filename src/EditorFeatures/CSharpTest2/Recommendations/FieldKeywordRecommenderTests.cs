// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class FieldKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public async Task TestNotAtRoot_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
            """$$""");
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
            """using Goo = $$""");
    }

    [Fact]
    public async Task TestNotInGlobalUsingAlias()
    {
        await VerifyAbsenceAsync(
            """global using Goo = $$""");
    }

    [Fact]
    public async Task TestNotInEmptyStatement()
    {
        await VerifyAbsenceAsync(AddInsideMethod(
            """$$"""));
    }

    [Fact]
    public async Task TestInAttributeInsideClass()
    {
        await VerifyKeywordAsync(
            """
            class C {
                [$$
            """);
    }

    [Theory]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public async Task TestInAttributeInsideRecord(string record)
    {
        // The recommender doesn't work in record in script
        // Tracked by https://github.com/dotnet/roslyn/issues/44865
        await VerifyWorkerAsync(
            $$"""
            {{record}} C {
                [$$
            """, absent: false, TestOptions.RegularPreview);
    }

    [Fact]
    public async Task TestInAttributeAfterAttributeInsideClass()
    {
        await VerifyKeywordAsync(
            """
            class C {
                [Goo]
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterMethod()
    {
        await VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                }
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterProperty()
    {
        await VerifyKeywordAsync(
            """
            class C {
                int Goo {
                    get;
                }
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterField()
    {
        await VerifyKeywordAsync(
            """
            class C {
                int Goo;
                [$$
            """);
    }

    [Fact]
    public async Task TestInAttributeAfterEvent()
    {
        await VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo;
                [$$
            """);
    }

    [Fact]
    public async Task TestNotInOuterAttribute()
    {
        await VerifyAbsenceAsync(
            """[$$""");
    }

    [Fact]
    public async Task TestNotInParameterAttribute()
    {
        await VerifyAbsenceAsync(
            """
            class C {
                void Goo([$$
            """);
    }

    [Fact]
    public async Task TestNotInPropertyAttribute()
    {
        await VerifyAbsenceAsync(
            """
            class C {
                int Goo { [$$
            """);
    }

    [Fact]
    public async Task TestNotInEventAttribute()
    {
        await VerifyAbsenceAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);
    }

    [Fact]
    public async Task TestNotInTypeParameters()
    {
        await VerifyAbsenceAsync(
            """class C<[$$""");
    }

    [Fact]
    public async Task TestNotInInterface()
    {
        await VerifyAbsenceAsync(
            """
            interface I {
                [$$
            """);
    }

    [Fact]
    public async Task TestInStruct()
    {
        await VerifyKeywordAsync(
            """
            struct S {
                [$$
            """);
    }

    [Fact]
    public async Task TestInEnum()
    {
        await VerifyKeywordAsync(
            """
            enum E {
                [$$
            """);
    }

    [Fact]
    public async Task TestNotInPropertyInitializer()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                int Goo { get; } = $$
            }
            """);
    }

    [Fact]
    public async Task TestInPropertyExpressionBody()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                int Goo => $$
            }
            """);
    }

    [Fact]
    public async Task TestNotInPropertyExpressionBody_NotPrimary()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                int Goo => this.$$
            }
            """);
    }

    [Fact]
    public async Task TestInPropertyAccessor1()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get => $$ }
            }
            """);
    }

    [Fact]
    public async Task TestInPropertyAccessor2()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { return $$ } }
            }
            """);
    }

    [Fact]
    public async Task TestInPropertyStatement()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { $$ } }
            }
            """);
    }

    [Fact]
    public async Task TestInPropertyExpressionContext()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { var v = 1 + $$ } }
            }
            """);
    }

    [Fact]
    public async Task TestInPropertyArgument1()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { Bar($$) } }
            }
            """);
    }

    [Fact]
    public async Task TestInPropertyArgument2()
    {
        await VerifyKeywordAsync(
            """
            class C
            {
                int Goo { get { Bar(ref $$) } }
            }
            """);
    }

    [Fact]
    public async Task TestNotInPropertyNameof()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                int Goo { get { Bar(nameof($$)) } }
            }
            """);
    }

    [Fact]
    public async Task TestInLocalFunctionInProperty()
    {
        await VerifyKeywordAsync(
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
    }

    [Fact]
    public async Task TestInLambdaInProperty()
    {
        await VerifyKeywordAsync(
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
    }

    [Fact]
    public async Task TestNotInAccessorAttribute()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                [Bar($$)]
                int Goo { get; }
            }
            """);
    }

    [Fact]
    public async Task TestNotInIndexer1()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                int this[int index] => $$
            }
            """);
    }

    [Fact]
    public async Task TestNotInIndexer2()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                int this[int index] { get => $$ }
            }
            """);
    }

    [Fact]
    public async Task TestNotInIndexer3()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                int this[int index] { get { return $$ } }
            }
            """);
    }

    [Fact]
    public async Task TestNotInEvent1()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                event Action E { add { $$ } }
            }
            """);
    }

    [Fact]
    public async Task TestNotInMethodContext()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                void M()
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotInMethodExpressionContext()
    {
        await VerifyAbsenceAsync(
            """
            class C
            {
                void M()
                {
                    Goo($$);
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotInGlobalStatement()
    {
        await VerifyAbsenceAsync(
            """
            $$
            """);
    }
}
