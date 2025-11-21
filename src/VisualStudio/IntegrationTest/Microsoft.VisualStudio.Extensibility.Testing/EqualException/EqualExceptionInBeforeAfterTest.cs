// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

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
