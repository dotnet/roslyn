// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace EqualExceptionLegacy
{
    using Xunit;

    public class EqualExceptionInBeforeAfterTest
    {
        [IdeFact]
        [ExceptionBeforeTest]
        public void FailBeforeTest()
        {
            Assert.Equal(0, 0);
        }

        [IdeFact]
        [ExceptionAfterTest]
        public void FailAfterTest()
        {
            Assert.Equal(0, 0);
        }
    }
}
