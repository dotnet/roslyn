// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace EqualExceptionLegacy
{
    using System;
    using System.Reflection;
    using Xunit;
    using Xunit.Sdk;
    using Xunit.v3;

    public class ExceptionAfterTestAttribute : BeforeAfterTestAttribute
    {
        public override void After(MethodInfo methodUnderTest, IXunitTest test)
        {
            throw new InvalidOperationException("Unexpected exception before test");
        }
    }
}
