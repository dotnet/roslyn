// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic;

public class CohostSemanticTokensLegendServiceTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void RazorModifiers_MustStartAfterRoslyn()
    {
        var clientCapabilitiesService = new TestClientCapabilitiesService(new VSInternalClientCapabilities());
        var service = new CohostSemanticTokensLegendService(clientCapabilitiesService);

        var expected = Math.Pow(2, SemanticTokensSchema.TokenModifiers.Length);

        Assert.Equal(expected, service.TokenModifiers.RazorCodeModifier);
    }
}
