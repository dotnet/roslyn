// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class MiscTests
    {
        /// <summary>
        /// Sanity check to help ensure our code base was compiled without overflow checking.
        /// </summary>
        [Fact]
        public void OverflowCheck()
        {
            int max = int.MaxValue;
            int x = max + max;
            Assert.Equal(-2, x);
            int y = 0 - int.MaxValue;
            Assert.Equal(-2147483647, y);
        }
    }
}
