// Copyright (c) COMPANY-PLACEHOLDER. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Library;
using Xunit;
using Xunit.Abstractions;

public class CalculatorTests
{
    private readonly ITestOutputHelper logger;

    public CalculatorTests(ITestOutputHelper logger)
    {
        this.logger = logger;
    }

    [Fact]
    public void AddOrSubtract()
    {
        // This tests aggregation of code coverage across test runs.
#if NETCOREAPP2_1
        Assert.Equal(3, Calculator.Add(1, 2));
#else
        Assert.Equal(-1, Calculator.Subtract(1, 2));
#endif
    }
}
