// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    public sealed class IdeTestCaseRunner : XunitTestCaseRunner
    {
        public IdeTestCaseRunner(
            WpfTestSharedData sharedData,
            VisualStudioVersion visualStudioVersion,
            IXunitTestCase testCase,
            string displayName,
            string skipReason,
            object[] constructorArguments,
            object[] testMethodArguments,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
        {
            SharedData = sharedData;
            VisualStudioVersion = visualStudioVersion;
        }

        public WpfTestSharedData SharedData
        {
            get;
        }

        public VisualStudioVersion VisualStudioVersion
        {
            get;
        }

        protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            if (Process.GetCurrentProcess().ProcessName == "devenv")
            {
                // We are already running inside Visual Studio
                // TODO: Verify version under test
                return new InProcessIdeTestRunner(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource);
            }
            else
            {
                throw new NotSupportedException($"{nameof(IdeFactAttribute)} can only be used with the {nameof(IdeTestFramework)} test framework");
            }
        }
    }
}
