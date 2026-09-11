// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Linq;
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
        private static readonly ConditionalWeakTable<ITestFrameworkDiscoveryOptions, StrongBox<ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>>> _instances = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes")]
        public IdeInstanceTestCase()
        {
        }

        public IdeInstanceTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            Type[]? skipExceptions,
            string? skipReason,
            Type? skipType,
            string? skipUnless,
            string? skipWhen,
            Dictionary<string, HashSet<string>>? traits,
            object?[]? testMethodArguments,
            string? sourceFilePath,
            int? sourceLineNumber,
            int? timeout,
            VisualStudioInstanceKey visualStudioInstanceKey)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout, visualStudioInstanceKey, includeRootSuffixInDisplayName: true)
        {
        }

        public static IdeInstanceTestCase? TryCreateNewInstanceForFramework(ITestFrameworkDiscoveryOptions discoveryOptions, VisualStudioInstanceKey visualStudioInstanceKey)
        {
            var lazyInstances = _instances.GetValue(discoveryOptions, static _ => new StrongBox<ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>>(ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>.Empty));
            var testMethod = IdeFactDiscoverer.CreateVisualStudioTestMethod();
            var factAttribute = testMethod.FactAttributes.Single();
            var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);
            var candidateTestCase = new IdeInstanceTestCase(
                details.ResolvedTestMethod,
                details.TestCaseDisplayName,
                details.UniqueID,
                details.Explicit,
                details.SkipExceptions,
                details.SkipReason,
                details.SkipType,
                details.SkipUnless,
                details.SkipWhen,
                TestIntrospectionHelper.GetTraits(testMethod, dataRow: null),
                testMethodArguments: null,
                details.SourceFilePath,
                details.SourceLineNumber,
                details.Timeout,
                visualStudioInstanceKey);
            var testCase = ImmutableInterlocked.GetOrAdd(ref lazyInstances.Value, visualStudioInstanceKey, candidateTestCase);
            if (testCase != candidateTestCase)
            {
                // A different call to this method already returned the test case for this instance
                return null;
            }

            return candidateTestCase;
        }
    }
}
