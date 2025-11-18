// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

public sealed class CSharpPropSnippetProviderTests : AbstractCSharpAutoPropertySnippetProviderTests
{
    protected override string SnippetIdentifier => "prop";

    protected override string DefaultPropertyBlockText => "{ get; set; }";

    public override Task InsertSnippetInReadonlyStructTest()
        => VerifyPropertyAsync("""
            readonly struct MyStruct
            {
                $$
            }
            """, "public {|0:int|} {|1:MyProperty|} { get; }");

    public override Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration()
        => VerifyPropertyAsync("""
            partial struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """, "public {|0:int|} {|1:MyProperty|} { get; }");

    public override Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration_MissingPartialModifier()
        => VerifyPropertyAsync("""
            struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """, "public {|0:int|} {|1:MyProperty|} { get; }");

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
