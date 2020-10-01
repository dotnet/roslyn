// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class TextLineCollectionTests : TestBase
    {
        [Fact]
        public void Equality()
        {
            Assert.Throws<NotSupportedException>(() => default(TextLineCollection.Enumerator).Equals(default(TextLineCollection.Enumerator)));
            Assert.Throws<NotSupportedException>(() => default(TextLineCollection.Enumerator).GetHashCode());
        }
    }
}
