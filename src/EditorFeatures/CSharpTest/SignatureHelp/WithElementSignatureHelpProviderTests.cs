// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class WithElementSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(WithElementSignatureHelpProvider);

    [Theory]
    [InlineData("IList<int>")]
    [InlineData("ICollection<int>")]
    public async Task TestMutableInterfaces(string type)
    {
        var markup = $$"""
            using System.Collections.Generic;

            class C
            {
                void Goo()
                {
                    {{type}} list = [with($$)];
                }
            }
            """;

        await TestAsync(markup, [new("List<int>(int capacity)", string.Empty, null, currentParameterIndex: 0)]);
    }

    [Theory]
    [InlineData("IReadOnlyList<int>")]
    [InlineData("IReadOnlyCollection<int>")]
    [InlineData("IEnumerable<int>")]
    [InlineData("IEnumerable")]
    public async Task TestReadOnlyInterfaces(string type)
    {
        var markup = $$"""
            using System;
            using System.Collections.Generic;

            class C
            {
                void Goo()
                {
                    {{type}} list = [with($$)];
                }
            }
            """;

        await TestAsync(markup, []);
    }
}
