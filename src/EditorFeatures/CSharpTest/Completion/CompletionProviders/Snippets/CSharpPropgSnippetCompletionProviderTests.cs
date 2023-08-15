// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class CSharpPropgSnippetCompletionProviderTests : AbstractCSharpAutoPropertyCompletionProviderTests
    {
        protected override string ItemToCommit => "propg";

        protected override string GetDefaultPropertyBlockText()
            => "{ get; private set; }";

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

        public override async Task InsertSnippetInInterface()
        {
            // Ensure we don't generate redundant `set` accessor when executed in interface
            await VerifyPropertyAsync("""
                interface MyInterface
                {
                    $$
                }
                """, "public int MyProperty { get; }");
        }
    }
}
