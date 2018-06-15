// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Roslyn.Test.Utilities.WpfFactDiscoverer", "Roslyn.Services.Test.Utilities")]
    public class WpfFactAttribute : FactAttribute
    {
    }
}
