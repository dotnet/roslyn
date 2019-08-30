// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Marks a test that should only run when async completion is enabled.
    /// </summary>
    public sealed class AsyncCompletionCondition : ExecutionCondition
    {
        public static AsyncCompletionCondition Instance { get; } = new AsyncCompletionCondition();

        public override bool ShouldSkip => !LegacyCompletionCondition.Instance.ShouldSkip;
        public override string SkipReason => "The test only runs when async completion is enabled.";
    }
}
