// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class ConditionalFactAttributeTests
{
    [Fact]
    public void FactCapturesSourceLocation()
    {
        var attribute = new ConditionalFactAttribute(Is.Windows);

        Assert.EndsWith(nameof(ConditionalFactAttributeTests) + ".cs", attribute.SourceFilePath);
        Assert.True(attribute.SourceLineNumber > 0);
    }

    [Fact]
    public void TheoryCapturesSourceLocation()
    {
        var attribute = new ConditionalTheoryAttribute(Is.Windows);

        Assert.EndsWith(nameof(ConditionalFactAttributeTests) + ".cs", attribute.SourceFilePath);
        Assert.True(attribute.SourceLineNumber > 0);
    }
}
