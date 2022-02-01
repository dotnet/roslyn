// Copyright (c) COMPANY-PLACEHOLDER. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Library;
using Xunit;

public class CalculatorTests
{
    public CalculatorTests()
    {
    }

    [Fact]
    public void AddOrSubtract()
    {
        // This tests aggregation of code coverage across test runs.
#if NETCOREAPP3_1
        Assert.Equal(3, Calculator.Add(1, 2));
#else
        Assert.Equal(-1, Calculator.Subtract(1, 2));
#endif
    }
}
