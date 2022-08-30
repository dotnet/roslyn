// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MakeStructReadOnly;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeStructReadOnly;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpMakeStructReadOnlyDiagnosticAnalyzer,
    CSharpMakeStructReadOnlyCodeFixProvider>;

public class MakeStructReadOnlyTests
{
    private static Task TestMissingAsync(string testCode, LanguageVersion version = LanguageVersion.Preview)
        => TestAsync(testCode, testCode, version);

    private static async Task TestAsync(string testCode, string fixedCode, LanguageVersion version = LanguageVersion.Preview)
    {
        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = version,
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task ShouldNotTriggerForCSharp7_1()
    {
        await TestMissingAsync(
@"struct S
{
    readonly int i;
}", LanguageVersion.CSharp7_1);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task ShouldTriggerFor7_2()
    {
        await TestAsync(
@"struct [|S|]
{
    readonly int i;
}",
@"readonly struct S
{
    readonly int i;
}",
LanguageVersion.CSharp7_2);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithAlreadyReadOnlyStruct()
    {
        await TestMissingAsync(
@"readonly struct S
{
    readonly int i;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithAlreadyReadOnlyRecordStruct()
    {
        await TestMissingAsync(
@"readonly record struct S
{
    readonly int i;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithMutableField()
    {
        await TestMissingAsync(
@"struct S
{
    int i;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithMutableFieldRecordStruct()
    {
        await TestMissingAsync(
@"record struct S
{
    int i;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithMutableAndReadOnlyField()
    {
        await TestMissingAsync(
@"struct S
{
    int i;
    readonly int j;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithMutableAndReadOnlyFieldRecordStruct1()
    {
        await TestMissingAsync(
@"record struct S
{
    int i;
    readonly int j;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithMutableAndReadOnlyFieldRecordStruct2()
    {
        await TestMissingAsync(
@"record struct S(int j)
{
    int i;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithMutableProperty()
    {
        await TestMissingAsync(
@"struct S
{
    int P { get; set; }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithMutablePropertyRecordStruct1()
    {
        await TestMissingAsync(
@"record struct S
{
    int P { get; set; }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithMutablePropertyRecordStruct2()
    {
        await TestMissingAsync(
@"record struct S(int q)
{
    int P { get; set; }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithEmptyStruct()
    {
        await TestMissingAsync(
@"struct S
{
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithEmptyRecordStruct()
    {
        await TestMissingAsync(
@"record struct S
{
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithEmptyStructPrimaryConstructor()
    {
        await TestMissingAsync(
@"record struct S()
{
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingWithOtherReadonlyPartialPart()
    {
        await TestMissingAsync(
@"partial struct S
{
    readonly int i;
}

readonly partial struct S
{
}
");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnStructWithReadOnlyField()
    {
        await TestAsync(
@"struct [|S|]
{
    readonly int i;
}",
@"readonly struct S
{
    readonly int i;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithReadOnlyField()
    {
        await TestAsync(
@"record struct [|S|]
{
    readonly int i;
}",
@"readonly record struct S
{
    readonly int i;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnStructWithGetOnlyProperty()
    {
        await TestAsync(
@"struct [|S|]
{
    int P { get; }
}",
@"readonly struct S
{
    int P { get; }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithGetOnlyProperty()
    {
        await TestAsync(
@"record struct [|S|]
{
    int P { get; }
}",
@"readonly record struct S
{
    int P { get; }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnStructWithInitOnlyProperty()
    {
        await TestAsync(
@"struct [|S|]
{
    int P { get; init; }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}",
@"readonly struct S
{
    int P { get; init; }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithInitOnlyProperty()
    {
        await TestAsync(
@"record struct [|S|]
{
    int P { get; init; }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}",
@"readonly record struct S
{
    int P { get; init; }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithReadOnlyField2()
    {
        await TestAsync(
@"record struct [|S|]
{
    readonly int i;
}",
@"readonly record struct S
{
    readonly int i;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingRecordStructWithPrimaryConstructorField()
    {
        await TestMissingAsync(
@"record struct S(int i)
{
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestMissingOnRecordStructWithPrimaryConstructorFieldAndNormalField()
    {
        await TestMissingAsync(
@"record struct S(int i)
{
    readonly int j;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestNestedStructs1()
    {
        await TestAsync(
@"struct [|S|]
{
    readonly int i;

    struct [|T|]
    {
        readonly int j;
    }
}",
@"readonly struct S
{
    readonly int i;

    readonly struct T
    {
        readonly int j;
    }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestDocComments1()
    {
        await TestAsync(
@"/// <summary>docs</summary>
record struct [|S|]
{
    readonly int j;
}",
@"/// <summary>docs</summary>
readonly record struct S
{
    readonly int j;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestDocComments2()
    {
        await TestAsync(
@"namespace N
{
    /// <summary>docs</summary>
    record struct [|S|]
    {
        readonly int j;
    }
}",
@"namespace N
{
    /// <summary>docs</summary>
    readonly record struct S
    {
        readonly int j;
    }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestExistingModifier1()
    {
        await TestAsync(
@"public record struct [|S|]
{
    readonly int j;
}",
@"public readonly record struct S
{
    readonly int j;
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestExistingModifier2()
    {
        await TestAsync(
@"namespace N
{
    public record struct [|S|]
    {
        readonly int j;
    }
}",
@"namespace N
{
    public readonly record struct S
    {
        readonly int j;
    }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnStructWithReadOnlyFieldAndMutableNormalProp()
    {
        await TestAsync(
@"struct [|S|]
{
    readonly int i;

    int P { set { } }
}",
@"readonly struct S
{
    readonly int i;

    int P { set { } }
}");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnStructWithReadOnlyFieldAndMutableAutoProp()
    {
        await TestMissingAsync(
@"struct S
{
    readonly int i;

    int P { get; set; }
}");
    }
}
