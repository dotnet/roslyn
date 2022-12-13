// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class CSharpPropgSnippetProviderTests : CSharpAutoPropertyCompletionProviderTests
    {
        protected override string ItemToCommit => "propg";

        protected override string GetDefaultPropertyText(string propertyName)
            => $"public int {propertyName} {{ get; private set; }}";

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotSetterInInterface()
        {
            await VerifyPropertyAsync("""
                interface MyInterface
                {
                    $$
                }
                """,
                "public int MyProperty { get; }");
        }
    }
}
