// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    public sealed class IdeTestCase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes")]
        public IdeTestCase()
        {
        }

        public IdeTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, VisualStudioVersion visualStudioVersion, string isolatedInstanceMessage, object[] testMethodArguments = null, string skipReason = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
            SharedData = WpfTestSharedData.Instance;
            VisualStudioVersion = visualStudioVersion;
            IsolatedInstanceMessage = isolatedInstanceMessage;
            if (!string.IsNullOrEmpty(skipReason))
            {
                SkipReason = skipReason;
            }
        }

        public VisualStudioVersion VisualStudioVersion
        {
            get;
            private set;
        }

        public string IsolatedInstanceMessage
        {
            get;
            private set;
        }

        public new TestMethodDisplay DefaultMethodDisplay => base.DefaultMethodDisplay;

        public WpfTestSharedData SharedData
        {
            get;
            private set;
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
        {
            var baseName = base.GetDisplayName(factAttribute, displayName);
            return $"{baseName} ({VisualStudioVersion})";
        }

        protected override string GetUniqueID()
        {
            return $"{base.GetUniqueID()}_{VisualStudioVersion}";
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            TestCaseRunner<IXunitTestCase> runner;
            if (!string.IsNullOrEmpty(SkipReason))
            {
                // Use XunitTestCaseRunner so the skip gets reported without trying to open VS
                runner = new XunitTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource);
            }
            else
            {
                runner = new IdeTestCaseRunner(SharedData, VisualStudioVersion, this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource);
            }

            return runner.RunAsync();
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(VisualStudioVersion), (int)VisualStudioVersion);
            data.AddValue(nameof(IsolatedInstanceMessage), IsolatedInstanceMessage, typeof(string));
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            VisualStudioVersion = (VisualStudioVersion)data.GetValue<int>(nameof(VisualStudioVersion));
            IsolatedInstanceMessage = data.GetValue<string>(nameof(IsolatedInstanceMessage));
            SharedData = WpfTestSharedData.Instance;
        }
    }
}
