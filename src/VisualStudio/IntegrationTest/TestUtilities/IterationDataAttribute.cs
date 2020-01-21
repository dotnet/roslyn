// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// xUnit data attribute that allows looping tests. The following example shows a test which will run 50 times.
    /// <code>
    /// [WpfTheory, IterationData(50)]
    /// public void IteratingTest(int iteration)
    /// {
    /// }
    /// </code>
    /// </summary>
    public sealed class IterationDataAttribute : DataAttribute
    {
        public IterationDataAttribute(int iterations = 100)
        {
            Iterations = iterations;
        }

        public int Iterations { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            for (var i = 0; i < Iterations; i++)
            {
                yield return new object[] { i };
            }
        }
    }
}
