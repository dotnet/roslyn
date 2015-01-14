// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
