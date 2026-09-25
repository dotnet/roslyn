// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System.Collections.Immutable;
    using System.Runtime.CompilerServices;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.v3;

    public sealed class IdeInstanceTestCase : IdeTestCaseBase
    {
        /// <summary>
        /// Keep track of unique <see cref="IdeInstanceTestCase"/> instances returned for a given discovery pass. The
        /// <see cref="ITestFrameworkDiscoveryOptions"/> instance used for discovery is assumed to be a singleton
        /// instance used for one complete discovery pass. If this instance is used for subsequent discovery passes,
        /// the instance test cases might not show up in the discovery.
        /// </summary>
#if IDE_INSTANCE_TEST_CASE_SUPPORT
        private static readonly ConditionalWeakTable<ITestFrameworkDiscoveryOptions, StrongBox<ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>>> _instances = new();
#endif

        public IdeInstanceTestCase(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, object?[]? testMethodArguments = null)
            : base(testMethod, visualStudioInstanceKey, includeRootSuffixInDisplayName: true, testMethodArguments)
        {
        }

        public static IdeInstanceTestCase? TryCreateNewInstanceForFramework(ITestFrameworkDiscoveryOptions discoveryOptions, VisualStudioInstanceKey visualStudioInstanceKey)
        {
#if IDE_INSTANCE_TEST_CASE_SUPPORT
            var lazyInstances = _instances.GetValue(discoveryOptions, static _ => new StrongBox<ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>>(ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>.Empty));
            var candidateTestCase = new IdeInstanceTestCase(IdeFactDiscoverer.CreateVisualStudioTestMethod(), visualStudioInstanceKey);
            var testCase = ImmutableInterlocked.GetOrAdd(ref lazyInstances.Value, visualStudioInstanceKey, candidateTestCase);
            if (testCase != candidateTestCase)
            {
                // A different call to this method already returned the test case for this instance
                return null;
            }

            return candidateTestCase;
#else
            return null;
#endif
        }
    }
}
