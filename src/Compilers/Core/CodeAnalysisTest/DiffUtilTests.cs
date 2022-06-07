// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class DiffUtilTests
    {
        [Fact]
        public void TestDiffTester()
        {
            var expected = DiffUtil.DiffReport(
@"A
B
C
D
E
F",
@"A
1
B
C
E
2");
            // Make sure the diff tester is working!
            Assert.Equal(@"    A
++> 1
    B
    C
--> D
    E
++> 2
--> F", expected);
        }
    }
}
