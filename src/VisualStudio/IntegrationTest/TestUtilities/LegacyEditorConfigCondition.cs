// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
