// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.ComponentModel;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.Sdk;

    public sealed class IdeSkippedDataRowTestCase : XunitSkippedDataRowTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
        public IdeSkippedDataRowTestCase()
        {
        }

        public IdeSkippedDataRowTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, string skipReason, object?[]? testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, skipReason, testMethodArguments)
        {
            VisualStudioInstanceKey = visualStudioInstanceKey;
        }

        public VisualStudioInstanceKey VisualStudioInstanceKey
        {
            get;
            private set;
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
        {
            var baseName = base.GetDisplayName(factAttribute, displayName);
            return $"{baseName} ({VisualStudioInstanceKey.Version})";
        }

        protected override string GetUniqueID()
        {
            return $"{base.GetUniqueID()}_{VisualStudioInstanceKey.Version}";
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(VisualStudioInstanceKey), VisualStudioInstanceKey.SerializeToString());
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            VisualStudioInstanceKey = VisualStudioInstanceKey.DeserializeFromString(data.GetValue<string>(nameof(VisualStudioInstanceKey)));
            base.Deserialize(data);
        }
    }
}
