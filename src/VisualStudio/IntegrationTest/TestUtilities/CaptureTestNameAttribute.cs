// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Captures the name of the test currently being run by xUnit.
    /// This should only be applied to test methods or classes that are guaranteed
    /// to run serially, not in parallel, as it assumes tests are run one at a time.
    /// </summary>
    public class CaptureTestNameAttribute : BeforeAfterTestAttribute
    {
        /// <summary>
        /// The name of the currently running test, or null if no test is running.
        /// The format is test_class_name.method_name.
        /// </summary>
        public static string CurrentName { get; set; }

        public override void Before(MethodInfo methodUnderTest)
        {
            CurrentName = methodUnderTest.DeclaringType.Name + "." + methodUnderTest.Name;
        }

        public override void After(MethodInfo methodUnderTest)
        {
            CurrentName = null;
        }
    }
}
