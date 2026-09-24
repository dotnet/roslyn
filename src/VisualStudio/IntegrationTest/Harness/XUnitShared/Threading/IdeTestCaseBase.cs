// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.v3;

    public abstract class IdeTestCaseBase : XunitTestCase, ISelfExecutingXunitTestCase
    {
        protected IdeTestCaseBase(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, bool includeRootSuffixInDisplayName, object?[]? testMethodArguments = null)
            : base(testMethod, GetDisplayName(testMethod, visualStudioInstanceKey, includeRootSuffixInDisplayName), GetUniqueID(testMethod, visualStudioInstanceKey), @explicit: false, testMethodArguments: testMethodArguments)
        {
            SharedData = WpfTestSharedData.Instance;
            VisualStudioInstanceKey = visualStudioInstanceKey;

            if (!IsInstalled(visualStudioInstanceKey.Version))
            {
                SkipReason = $"{visualStudioInstanceKey.Version} is not installed";
            }
        }

        public VisualStudioInstanceKey VisualStudioInstanceKey
        {
            get;
            private set;
        }

        public WpfTestSharedData SharedData
        {
            get;
            private set;
        }

        private static string GetDisplayName(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, bool includeRootSuffixInDisplayName)
        {
            if (!includeRootSuffixInDisplayName || string.IsNullOrEmpty(visualStudioInstanceKey.RootSuffix))
            {
                return $"{testMethod.MethodName} ({visualStudioInstanceKey.Version})";
            }
            else
            {
                return $"{testMethod.MethodName} ({visualStudioInstanceKey.Version}, {visualStudioInstanceKey.RootSuffix})";
            }
        }

        private static string GetUniqueID(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey)
        {
            if (string.IsNullOrEmpty(visualStudioInstanceKey.RootSuffix))
            {
                return $"{testMethod.UniqueID}_{visualStudioInstanceKey.Version}";
            }
            else
            {
                return $"{testMethod.UniqueID}_{visualStudioInstanceKey.RootSuffix}_{visualStudioInstanceKey.Version}";
            }
        }

        protected override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(VisualStudioInstanceKey), VisualStudioInstanceKey.SerializeToString());
            data.AddValue(nameof(SkipReason), SkipReason);
        }

        protected override void Deserialize(IXunitSerializationInfo data)
        {
            VisualStudioInstanceKey = VisualStudioInstanceKey.DeserializeFromString(data.GetValue<string>(nameof(VisualStudioInstanceKey))!);
            base.Deserialize(data);
            SkipReason = data.GetValue<string>(nameof(SkipReason));
            SharedData = WpfTestSharedData.Instance;
        }

        public async ValueTask<RunSummary> Run(
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            object?[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            ParallelMode parallelMode,
            ExecutionScheduler scheduler,
            FixtureMappingManager methodFixtureMappings)
        {
            // NOTE: Unlike the xUnit v2 implementation, this does not currently check WpfTestSharedData.Exception to
            // report a prior harness failure, nor does it verify the current process is "devenv". This test case is
            // only ever executed in-process inside Visual Studio (via InProcessIdeTestAssemblyRunner), so the process
            // check is implied by the architecture. Reporting a prior harness failure via ErrorReportingIdeTestRunner
            // is a known gap in this initial xUnit v3 port.
            return await InProcessIdeTestCaseRunner.Instance.Run(this, await CreateTests(), messageBus, aggregator, cancellationTokenSource, parallelMode, scheduler, TestCaseDisplayName, SkipReason, explicitOption, constructorArguments, methodFixtureMappings);
        }

        internal static bool IsInstalled(VisualStudioVersion visualStudioVersion)
        {
            int majorVersion;

            switch (visualStudioVersion)
            {
                case VisualStudioVersion.VS2012:
                    majorVersion = 11;
                    break;

                case VisualStudioVersion.VS2013:
                    majorVersion = 12;
                    break;

                case VisualStudioVersion.VS2015:
                    majorVersion = 14;
                    break;

                case VisualStudioVersion.VS2017:
                    majorVersion = 15;
                    break;

                case VisualStudioVersion.VS2019:
                    majorVersion = 16;
                    break;

                case VisualStudioVersion.VS2022:
                    majorVersion = 17;
                    break;

                case VisualStudioVersion.VS18:
                    majorVersion = 18;
                    break;

                default:
                    throw new ArgumentException();
            }

            var instances = VisualStudioInstanceFactory.EnumerateVisualStudioInstances();
            return instances.Any(i => i.Item2.Major == majorVersion);
        }
    }
}
