// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class DiffUtilTests
    {
        [Fact]
        public void TestDiffTester()
        {
            // Make sure the diff tester is working!
            Assert.Equal(@"    A
++> 1
    B
    C
--> D
    E
++> 2
--> F",
DiffUtil.DiffReport(
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
2"));
        }
    }
}
