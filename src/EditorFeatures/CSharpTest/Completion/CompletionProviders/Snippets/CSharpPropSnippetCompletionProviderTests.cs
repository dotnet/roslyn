// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets;

public class CSharpPropSnippetCompletionProviderTests : AbstractCSharpAutoPropertyCompletionProviderTests
{
    protected override string ItemToCommit => "prop";

    protected override string GetDefaultPropertyBlockText()
        => "{ get; set; }";

    public override async Task InsertSnippetInReadonlyStruct()
    {
        // Ensure we don't generate redundant `set` accessor when executed in readonly struct
        await VerifyPropertyAsync("""
            readonly struct MyStruct
            {
                $$
            }
            """, "public int MyProperty { get; }");
    }

    public override async Task InsertSnippetInReadonlyStruct_ReadonlyModifierInOtherPartialDeclaration()
    {
        // Ensure we don't generate redundant `set` accessor when executed in readonly struct
        await VerifyPropertyAsync("""
            partial struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """, "public int MyProperty { get; }");
    }

    public override async Task InsertSnippetInReadonlyStruct_ReadonlyModifierInOtherPartialDeclaration_MissingPartialModifier()
    {
        // Even though there is no `partial` modifier on the first declaration
        // compiler still treats the whole type as partial since it is more likely that
        // the user's intent was to have a partial type and they just forgot the modifier.
        // Thus we still recognize that as `readonly` context and don't generate a setter
        await VerifyPropertyAsync("""
            struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """, "public int MyProperty { get; }");
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
