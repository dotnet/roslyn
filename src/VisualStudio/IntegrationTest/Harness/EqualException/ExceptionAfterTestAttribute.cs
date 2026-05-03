// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace EqualExceptionLegacy
{
    using System;
    using System.Reflection;
    using Xunit;
    using Xunit.Sdk;

    public class ExceptionAfterTestAttribute : BeforeAfterTestAttribute
    {
        public override void After(MethodInfo methodUnderTest)
        {
            throw new InvalidOperationException("Unexpected exception before test");
        }
    }
}
