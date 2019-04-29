// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Marks a test that should only run when legacy completion is enabled.
    /// </summary>
    public sealed class LegacyCompletionCondition : ExecutionCondition
    {
        public static LegacyCompletionCondition Instance { get; } = new LegacyCompletionCondition();

        public override bool ShouldSkip => !string.Equals(Environment.GetEnvironmentVariable("ROSLYN_TEST_LEGACY_COMPLETION"), "true", StringComparison.OrdinalIgnoreCase);
        public override string SkipReason => "The test only runs when legacy completion is enabled.";
    }
}
