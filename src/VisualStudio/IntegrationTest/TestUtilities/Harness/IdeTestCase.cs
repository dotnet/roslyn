﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
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

        public IdeTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, VisualStudioVersion visualStudioVersion, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
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
            base.Deserialize(data);
            VisualStudioVersion = (VisualStudioVersion)data.GetValue<int>(nameof(VisualStudioVersion));
            SkipReason = data.GetValue<string>(nameof(SkipReason));
            SharedData = WpfTestSharedData.Instance;
        }

        internal static bool IsInstalled(VisualStudioVersion visualStudioVersion)
        {
            string dteKey;

            switch (visualStudioVersion)
            {
                case VisualStudioVersion.VS2017:
                    dteKey = "VisualStudio.DTE.15.0";
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
