// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.v3;

    public abstract class IdeTestCaseBase : XunitTestCase, ISelfExecutingXunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
        protected IdeTestCaseBase()
        {
            SharedData = WpfTestSharedData.Instance;
        }

        protected IdeTestCaseBase(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, bool includeRootSuffixInDisplayName, object?[]? testMethodArguments = null)
            : base(testMethod, GetDisplayName(testMethod, visualStudioInstanceKey, includeRootSuffixInDisplayName, testMethodArguments), GetUniqueID(testMethod, visualStudioInstanceKey, testMethodArguments), @explicit: false, testMethodArguments: testMethodArguments)
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

        private static string GetDisplayName(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, bool includeRootSuffixInDisplayName, object?[]? testMethodArguments)
        {
            var methodDisplayName = testMethodArguments is null
                ? testMethod.MethodName
                : testMethod.GetDisplayName(testMethod.MethodName, label: null, testMethodArguments, methodGenericTypes: null);

            if (!includeRootSuffixInDisplayName || string.IsNullOrEmpty(visualStudioInstanceKey.RootSuffix))
            {
                return $"{methodDisplayName} ({visualStudioInstanceKey.Version})";
            }
            else
            {
                return $"{methodDisplayName} ({visualStudioInstanceKey.Version}, {visualStudioInstanceKey.RootSuffix})";
            }
        }

        private static string GetUniqueID(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, object?[]? testMethodArguments)
        {
            // Data rows of the same theory share a test method, so the arguments are included to keep their IDs distinct.
            var baseUniqueID = testMethodArguments is null
                ? testMethod.UniqueID
                : UniqueIDGenerator.ForTestCase(testMethod.UniqueID, (Type[]?)null, testMethodArguments);

            if (string.IsNullOrEmpty(visualStudioInstanceKey.RootSuffix))
            {
                return $"{baseUniqueID}_{visualStudioInstanceKey.Version}";
            }
            else
            {
                return $"{baseUniqueID}_{visualStudioInstanceKey.RootSuffix}_{visualStudioInstanceKey.Version}";
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
            if (!string.IsNullOrEmpty(SkipReason))
            {
                // Use XunitTestCaseRunner so the skip gets reported without trying to access Visual Studio
                return await XunitTestCaseRunner.Instance.Run(this, await CreateTests().ConfigureAwait(true), messageBus, aggregator, cancellationTokenSource, parallelMode, scheduler, TestCaseDisplayName, SkipReason, explicitOption, constructorArguments, methodFixtureMappings).ConfigureAwait(true);
            }

            // This runner does not inspect WpfTestSharedData.Exception or report harness failures through
            // ErrorReportingIdeTestRunner. Test cases that are not skipped are expected to run inside devenv, where
            // they are invoked by InProcessIdeTestAssemblyRunner.
            return await InProcessIdeTestCaseRunner.Instance.Run(this, await CreateTests().ConfigureAwait(true), messageBus, aggregator, cancellationTokenSource, parallelMode, scheduler, TestCaseDisplayName, SkipReason, explicitOption, constructorArguments, methodFixtureMappings).ConfigureAwait(true);
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
