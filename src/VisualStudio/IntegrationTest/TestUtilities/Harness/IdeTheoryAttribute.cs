// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Microsoft.VisualStudio.IntegrationTest.Utilities.Harness.IdeTheoryDiscoverer", "Microsoft.VisualStudio.IntegrationTest.Utilities")]
    public class IdeTheoryAttribute : TheoryAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating that an IDE test case should run in a separate instance of Visual Studio
        /// from other tests.
        /// </summary>
        /// <remarks>
        /// <para>This property incurs substantial performance overhead, and should only be used in cases where the test
        /// cannot be written in a way that avoids it.</para>
        /// </remarks>
        public string Isolate
        {
            get;
            set;
        }
    }
}
