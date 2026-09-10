// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Xunit.Harness;
    using Xunit.v3;

    public sealed class IdeTestCase : IdeTestCaseBase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes")]
        public IdeTestCase()
        {
        }

        public IdeTestCase(
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
            bool includeRootSuffixInUniqueID = true)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout, visualStudioInstanceKey, testLabel, disableParallelization, includeRootSuffixInUniqueID: includeRootSuffixInUniqueID)
        {
        }
    }
}
