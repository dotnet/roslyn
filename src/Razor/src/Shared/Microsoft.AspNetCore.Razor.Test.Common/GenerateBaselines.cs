// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Uncomment to easily generate baselines for tests
//#define GENERATE_BASELINES

using Xunit;

public class GenerateBaselines
{
#if GENERATE_BASELINES
    internal static readonly bool ShouldGenerate = true;
#else
    internal static readonly bool ShouldGenerate = false;
#endif

    // This is to prevent you from accidentally checking in with GenerateBaselines = true
    [Fact]
    public void GenerateBaselinesMustBeFalse()
    {
        Assert.False(ShouldGenerate, "GenerateBaselines should be set back to false before you check in!");
    }
}
