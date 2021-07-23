// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Win32;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public sealed class IdeTestCase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
        public IdeTestCase()
        {
        }

        public IdeTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, VisualStudioVersion visualStudioVersion, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
            SharedData = WpfTestSharedData.Instance;
            VisualStudioVersion = visualStudioVersion;

            if (!IsInstalled(visualStudioVersion))
            {
                SkipReason = $"{visualStudioVersion} is not installed";
            }
        }

        public VisualStudioVersion VisualStudioVersion
        {
            get;
            private set;
        }

        public new TestMethodDisplay DefaultMethodDisplay => base.DefaultMethodDisplay;

        public new TestMethodDisplayOptions DefaultMethodDisplayOptions => base.DefaultMethodDisplayOptions;

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
            data.AddValue(nameof(SkipReason), SkipReason);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            VisualStudioVersion = (VisualStudioVersion)data.GetValue<int>(nameof(VisualStudioVersion));
            base.Deserialize(data);
            SkipReason = data.GetValue<string>(nameof(SkipReason));
            SharedData = WpfTestSharedData.Instance;
        }

        internal static bool IsInstalled(VisualStudioVersion visualStudioVersion)
        {
            string dteKey;

            switch (visualStudioVersion)
            {
            case VisualStudioVersion.VS2012:
                dteKey = "VisualStudio.DTE.11.0";
                break;

            case VisualStudioVersion.VS2013:
                dteKey = "VisualStudio.DTE.12.0";
                break;

            case VisualStudioVersion.VS2015:
                dteKey = "VisualStudio.DTE.14.0";
                break;

            case VisualStudioVersion.VS2017:
                dteKey = "VisualStudio.DTE.15.0";
                break;

            case VisualStudioVersion.VS2019:
                dteKey = "VisualStudio.DTE.16.0";
                break;

            case VisualStudioVersion.VS2022:
                dteKey = "VisualStudio.DTE.17.0";
                break;

            default:
                throw new ArgumentException();
            }

            using (var key = Registry.ClassesRoot.OpenSubKey(dteKey))
            {
                return key != null;
            }
        }
    }
}
