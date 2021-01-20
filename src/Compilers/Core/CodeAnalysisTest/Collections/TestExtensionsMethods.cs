// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    internal static partial class TestExtensionsMethods
    {
        private static readonly double s_GoldenRatio = (1 + Math.Sqrt(5)) / 2;

        internal static void ValidateDefaultThisBehavior(Action a)
        {
            Assert.Throws<NullReferenceException>(a);
        }
    }
}
