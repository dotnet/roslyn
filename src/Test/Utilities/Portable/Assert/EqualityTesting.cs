// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Helpers for testing equality APIs. 
    /// Gives us more control than calling Assert.Equals.
    /// </summary>
    public static class EqualityTesting
    {
        public static void AssertEqual<T>(IEquatable<T> x, IEquatable<T> y)
        {
            Assert.True(x.Equals(y));
            Assert.True(((object)x).Equals(y));
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
        }

        public static void AssertNotEqual<T>(IEquatable<T> x, IEquatable<T> y)
        {
            Assert.False(x.Equals(y));
            Assert.False(((object)x).Equals(y));
        }
    }
}
