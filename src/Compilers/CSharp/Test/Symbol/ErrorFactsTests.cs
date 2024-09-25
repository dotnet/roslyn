// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols;

public class ErrorFactsTests
{
    [Fact]
    public void ToStringHelpers()
    {
        foreach (ErrorCode errorCode in Enum.GetValues(typeof(ErrorCode)))
        {
            Assert.NotEmpty(ErrorFacts.ToString(errorCode));
            Assert.NotEmpty(ErrorFacts.ToStringWithDescription(errorCode));
            Assert.NotEmpty(ErrorFacts.ToStringWithTitle(errorCode));
        }
    }

    [Fact]
    public void ToStringHelpersBadValues()
    {
        var invalid = (ErrorCode)int.MinValue;
        Assert.Throws<InvalidOperationException>(() => ErrorFacts.ToString(invalid));
        Assert.Throws<InvalidOperationException>(() => ErrorFacts.ToStringWithDescription(invalid));
        Assert.Throws<InvalidOperationException>(() => ErrorFacts.ToStringWithTitle(invalid));
    }
}
