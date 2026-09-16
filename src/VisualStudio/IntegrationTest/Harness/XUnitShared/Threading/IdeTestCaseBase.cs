// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.v3;

    internal interface IIdeTestCase
    {
        VisualStudioInstanceKey VisualStudioInstanceKey { get; }
    }

    public abstract class IdeTestCaseBase : XunitTestCase, IIdeTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes")]
        protected IdeTestCaseBase()
        {
        }

        protected IdeTestCaseBase(
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
            VisualStudioInstanceKey visualStudioInstanceKey,
            string? testLabel = null,
            bool disableParallelization = false,
            bool includeRootSuffixInDisplayName = false,
            bool includeRootSuffixInUniqueID = true)
            : base(
                testMethod,
                GetDisplayName(testCaseDisplayName, visualStudioInstanceKey, includeRootSuffixInDisplayName),
                GetUniqueID(uniqueID, visualStudioInstanceKey, includeRootSuffixInUniqueID),
                @explicit,
                testLabel,
                disableParallelization,
                skipExceptions,
                skipReason,
                skipType,
                skipUnless,
                skipWhen,
                traits,
                testMethodArguments,
                sourceFilePath,
                sourceLineNumber,
                timeout)
        {
            VisualStudioInstanceKey = visualStudioInstanceKey;

            if (!IsInstalled(visualStudioInstanceKey.Version))
            {
                SkipReason = $"{visualStudioInstanceKey.Version} is not installed";
            }
        }

        public VisualStudioInstanceKey VisualStudioInstanceKey { get; private set; } = VisualStudioInstanceKey.Unspecified;

        internal static string GetDisplayName(string displayName, VisualStudioInstanceKey visualStudioInstanceKey, bool includeRootSuffix)
        {
            if (!includeRootSuffix || string.IsNullOrEmpty(visualStudioInstanceKey.RootSuffix))
            {
                return $"{displayName} ({visualStudioInstanceKey.Version})";
            }

            return $"{displayName} ({visualStudioInstanceKey.Version}, {visualStudioInstanceKey.RootSuffix})";
        }

        internal static string GetUniqueID(string uniqueID, VisualStudioInstanceKey visualStudioInstanceKey, bool includeRootSuffix = true)
        {
            if (!includeRootSuffix || string.IsNullOrEmpty(visualStudioInstanceKey.RootSuffix))
            {
                return $"{uniqueID}_{visualStudioInstanceKey.Version}";
            }

            return $"{uniqueID}_{visualStudioInstanceKey.RootSuffix}_{visualStudioInstanceKey.Version}";
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
