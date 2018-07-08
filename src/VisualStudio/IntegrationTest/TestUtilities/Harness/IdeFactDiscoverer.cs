// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
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
                        yield return new IdeTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, supportedVersion, factAttribute.GetNamedArgument<string>(nameof(IdeFactAttribute.Isolate)));
                    }
                }
                else
                {
                    yield return new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, "[Fact] methods are not allowed to be generic.");
                }
            }
            else
            {
                yield return new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, "[Fact] methods are not allowed to have parameters. Did you mean to use [Theory]?");
            }
        }

        private IEnumerable<VisualStudioVersion> GetSupportedVersions(IAttributeInfo theoryAttribute)
        {
            yield return VisualStudioVersion.VS2017;
        }
    }
}
