// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.Sdk;

    public class IdeFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public IdeFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            if (!testMethod.Method.GetParameters().Any())
            {
                if (!testMethod.Method.IsGenericMethodDefinition)
                {
                    var testCases = new List<IXunitTestCase>();
                    foreach (var supportedVersion in GetSupportedVersions(factAttribute))
                    {
                        yield return new IdeTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, supportedVersion);
                    }
                }
                else
                {
                    yield return new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, "[IdeFact] methods are not allowed to be generic.");
                }
            }
            else
            {
                yield return new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, "[IdeFact] methods are not allowed to have parameters. Did you mean to use [IdeTheory]?");
            }
        }

        private IEnumerable<VisualStudioVersion> GetSupportedVersions(IAttributeInfo theoryAttribute)
        {
            var minVersion = theoryAttribute.GetNamedArgument<VisualStudioVersion>(nameof(IdeFactAttribute.MinVersion));
            minVersion = minVersion == VisualStudioVersion.Unspecified ? VisualStudioVersion.VS2012 : minVersion;

            var maxVersion = theoryAttribute.GetNamedArgument<VisualStudioVersion>(nameof(IdeFactAttribute.MaxVersion));
            maxVersion = maxVersion == VisualStudioVersion.Unspecified ? VisualStudioVersion.VS2022 : maxVersion;

            for (var version = minVersion; version <= maxVersion; version++)
            {
#if MERGED_PIA
                if (version >= VisualStudioVersion.VS2012 && version < VisualStudioVersion.VS2022)
                {
                    continue;
                }
#else
                if (version >= VisualStudioVersion.VS2022)
                {
                    continue;
                }
#endif

                yield return version;
            }
        }
    }
}
