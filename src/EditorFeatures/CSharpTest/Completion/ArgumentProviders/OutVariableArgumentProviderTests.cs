// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.ArgumentProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class OutVariableArgumentProviderTests : AbstractCSharpArgumentProviderTests
    {
        internal override Type GetArgumentProviderType()
            => typeof(OutVariableArgumentProvider);

        [Theory]
        [InlineData("")]
        [InlineData("ref")]
        [InlineData("in")]
        public async Task TestUnsupportedModifiers(string modifier)
        {
            var markup = $@"
class C
{{
    void Method()
    {{
        TryParse($$)
    }}

    bool TryParse({modifier} int value) => throw null;
}}
";

            await VerifyDefaultValueAsync(markup, expectedDefaultValue: null);
        }

        [Fact]
        public async Task TestDeclareVariable()
        {
            var markup = $@"
class C
{{
    void Method()
    {{
        int.TryParse(""x"", $$)
    }}
}}
";

            await VerifyDefaultValueAsync(markup, "out var result");
            await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
        }

        [Theory]
        [InlineData("string")]
        [InlineData("bool")]
        [InlineData("int?")]
        public async Task TestDeclareVariableEscapedIdentifier(string type)
        {
            var markup = $@"
class C
{{
    void Method()
    {{
        this.Target($$);
    }}

    void Target(out {type} @void)
    {{
        @void = default;
    }}
}}
";

            await VerifyDefaultValueAsync(markup, "out var @void");
            await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
        }
    }
}
