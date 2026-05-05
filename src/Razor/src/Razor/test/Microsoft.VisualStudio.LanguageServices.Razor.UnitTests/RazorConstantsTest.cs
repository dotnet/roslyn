// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.VisualStudio.Razor;
using Xunit;
using CohostConstants = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Constants;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test;

public class RazorConstantsTest
{
    [Fact]
    public void MatchRoslynEA()
    {
        Assert.Equal(RazorConstants.RazorLSPContentTypeName, Constants.RazorLanguageName);
        Assert.Equal(RazorConstants.RazorLSPContentTypeName, CohostConstants.RazorLanguageName);
        Assert.Equal(RazorConstants.RazorCohostingUIContext, CohostConstants.RazorCohostingUIContext.ToString());
    }
}
