// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

public sealed class CSharpPropiSnippetProviderTests : AbstractCSharpAutoPropertySnippetProviderTests
{
    protected override string SnippetIdentifier => "propi";

    protected override string DefaultPropertyBlockText => "{ get; init; }";

    public override async Task InsertSnippetInReadonlyStructTest()
    {
        await VerifyDefaultPropertyAsync("""
            readonly struct MyStruct
            {
                $$
            }
            """);
    }

    public override async Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration()
    {
        await VerifyDefaultPropertyAsync("""
            partial struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """);
    }

    public override async Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration_MissingPartialModifier()
    {
        await VerifyDefaultPropertyAsync("""
            struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """);
    }

    public override async Task InsertSnippetInInterfaceTest()
    {
        await VerifyDefaultPropertyAsync("""
            interface MyInterface
            {
                $$
            }
            """);
    }
}
