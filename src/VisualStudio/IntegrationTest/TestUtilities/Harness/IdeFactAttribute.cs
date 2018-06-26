// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Microsoft.VisualStudio.IntegrationTest.Utilities.Harness.IdeFactDiscoverer", "Microsoft.VisualStudio.IntegrationTest.Utilities")]
    public class IdeFactAttribute : FactAttribute
    {
    }
}
