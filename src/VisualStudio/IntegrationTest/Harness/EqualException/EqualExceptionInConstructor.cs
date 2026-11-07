// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace EqualExceptionLegacy
{
    using System;
    using Xunit;

    public class EqualExceptionInConstructor
    {
        public EqualExceptionInConstructor()
        {
            throw new InvalidOperationException("Unexpected exception");
        }

        [IdeFact]
        public void EqualsSucceeds()
        {
            Assert.Equal(0, 0);
        }
    }
}
