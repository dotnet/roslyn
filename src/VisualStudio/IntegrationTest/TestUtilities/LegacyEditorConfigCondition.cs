// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public sealed class LegacyEditorConfigCondition : ExecutionCondition
    {
        // Skip legacy .editorconfig tests until our infrastructure is ready to test the
        // compiler support. Waiting for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/839836
        public override bool ShouldSkip => true;

        public override string SkipReason => "Test is only supported with our legacy .editorconfig support";
    }
}
