// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace EqualExceptionLegacy
{
    using System;
    using System.Reflection;
    using Xunit;
    using Xunit.Sdk;

    public class ExceptionBeforeTestAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            throw new InvalidOperationException("Unexpected exception before test");
        }
    }
}
