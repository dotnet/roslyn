// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

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
