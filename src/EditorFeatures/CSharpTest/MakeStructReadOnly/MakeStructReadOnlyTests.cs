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
    private static async Task TestMissingAsync(string input, LanguageVersion version = LanguageVersion.Preview)
    {
        await new VerifyCS.Test
        {
            TestCode = input,
            FixedCode = input,
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
        await new VerifyCS.Test
        {
            TestCode =
@"struct [|S|]
{
    readonly int i;
}",
            FixedCode =
@"readonly struct S
{
    readonly int i;
}",
            LanguageVersion = LanguageVersion.CSharp7_2,
        }.RunAsync();
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
    public async Task TestOnStructWithReadOnlyField()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"struct [|S|]
{
    readonly int i;
}",
            FixedCode =
@"readonly struct S
{
    readonly int i;
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithReadOnlyField()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"record struct S
{
    readonly int i;
}",
            FixedCode =
@"readonly record struct S
{
    readonly int i;
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnStructWithGetOnlyProperty()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"struct [|S|]
{
    int P { get; }
}",
            FixedCode =
@"readonly struct S
{
    int P { get; }
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithGetOnlyProperty()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"record struct S
{
    int P { get; }
}",
            FixedCode =
@"readonly struct S
{
    int P { get; }
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnStructWithInitOnlyProperty()
    {
        await new VerifyCS.Test
        {
            TestCode =
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
            FixedCode =
@"readonly struct S
{
    int P { get; init; }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithInitOnlyProperty()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"record struct S
{
    int P { get; init; }
}",
            FixedCode =
@"readonly record struct S
{
    int P { get; init; }
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithReadOnlyField2()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"record struct S
{
    readonly int i;
}",
            FixedCode =
@"readonly record struct S
{
    readonly int i;
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithPrimaryConstructorField()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"record struct S(int i)
{
}",
            FixedCode =
@"readonly record struct S(int i)
{
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestOnRecordStructWithPrimaryConstructorFieldAndNormalField()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"record struct S(int i)
{
    readonly int j;
}",
            FixedCode =
@"readonly record struct S(int i)
{
    readonly int j;
}"
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
    public async Task TestNestedStructs1()
    {
        await new VerifyCS.Test
        {
            TestCode =
@"struct [|S|]
{
    readonly int i;

    struct [|T|]
    {
        readonly int j;
    }
}",
            FixedCode =
@"readonly struct S
{
    readonly int i;

    readonly struct T
    {
        readonly int j;
    }
}"
        }.RunAsync();
    }
}
