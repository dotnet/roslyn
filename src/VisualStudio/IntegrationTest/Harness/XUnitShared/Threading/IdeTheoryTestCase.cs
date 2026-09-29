// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.ComponentModel;
    using Xunit.Harness;
    using Xunit.v3;

    public sealed class IdeTheoryTestCase : IdeTestCaseBase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
        public IdeTheoryTestCase()
        {
        }

        public IdeTheoryTestCase(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, object?[]? testMethodArguments = null)
            : base(testMethod, visualStudioInstanceKey, includeRootSuffixInDisplayName: false, testMethodArguments)
        {
        }
    }
}
