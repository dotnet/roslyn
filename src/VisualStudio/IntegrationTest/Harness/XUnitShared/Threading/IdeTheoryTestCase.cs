// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.v3;

    public sealed class IdeTheoryTestCase : XunitDelayEnumeratedTheoryTestCase, IIdeTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes")]
        public IdeTheoryTestCase()
        {
        }

        public IdeTheoryTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            bool skipTestWithoutData,
            VisualStudioInstanceKey visualStudioInstanceKey,
            Type[]? skipExceptions = null,
            string? skipReason = null,
            Type? skipType = null,
            string? skipUnless = null,
            string? skipWhen = null,
            Dictionary<string, HashSet<string>>? traits = null,
            string? sourceFilePath = null,
            int? sourceLineNumber = null,
            int? timeout = null)
            : base(
                testMethod,
                IdeTestCaseBase.GetDisplayName(testCaseDisplayName, visualStudioInstanceKey, includeRootSuffix: false),
                IdeTestCaseBase.GetUniqueID(uniqueID, visualStudioInstanceKey),
                @explicit,
                skipTestWithoutData,
                skipExceptions,
                skipReason,
                skipType,
                skipUnless,
                skipWhen,
                traits,
                sourceFilePath,
                sourceLineNumber,
                timeout)
        {
            VisualStudioInstanceKey = visualStudioInstanceKey;

            if (!IdeTestCaseBase.IsInstalled(visualStudioInstanceKey.Version))
            {
                SkipReason = $"{visualStudioInstanceKey.Version} is not installed";
            }
        }

        public VisualStudioInstanceKey VisualStudioInstanceKey { get; private set; } = VisualStudioInstanceKey.Unspecified;

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
    }
}
