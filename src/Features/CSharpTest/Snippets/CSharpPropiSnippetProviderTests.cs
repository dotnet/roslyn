// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

public sealed class CSharpPropiSnippetProviderTests : AbstractCSharpAutoPropertySnippetProviderTests
{
    protected override string SnippetIdentifier => "propi";

    protected override string DefaultPropertyBlockText => "{ get; init; }";

    public override Task InsertSnippetInReadonlyStructTest()
        => VerifyDefaultPropertyAsync("""
            readonly struct MyStruct
            {
                $$
            }
            """);

    public override Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration()
        => VerifyDefaultPropertyAsync("""
            partial struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """);

    public override Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration_MissingPartialModifier()
        => VerifyDefaultPropertyAsync("""
            struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """);

    public override Task VerifySnippetInInterfaceTest()
        => VerifyDefaultPropertyAsync("""
            interface MyInterface
            {
                $$
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public override Task InsertSnippetAfterAllowedAccessibilityModifierTest(string modifier)
        => base.InsertSnippetAfterAllowedAccessibilityModifierTest(modifier);
}
