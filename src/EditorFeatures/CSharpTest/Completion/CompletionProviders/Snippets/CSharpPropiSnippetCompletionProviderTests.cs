// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets;

public class CSharpPropiSnippetCompletionProviderTests : AbstractCSharpAutoPropertyCompletionProviderTests
{
    protected override string ItemToCommit => "propi";

    protected override string GetDefaultPropertyBlockText()
        => "{ get; init; }";

    public override async Task InsertSnippetInReadonlyStruct()
    {
        await VerifyDefaultPropertyAsync("""
            readonly struct MyStruct
            {
                $$
            }
            """);
    }

    public override async Task InsertSnippetInReadonlyStruct_ReadonlyModifierInOtherPartialDeclaration()
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

    public override async Task InsertSnippetInReadonlyStruct_ReadonlyModifierInOtherPartialDeclaration_MissingPartialModifier()
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

    public override async Task InsertSnippetInInterface()
    {
        await VerifyDefaultPropertyAsync("""
            interface MyInterface
            {
                $$
            }
            """);
    }
}
