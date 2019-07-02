using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options.EditorConfig;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public sealed class LegacyEditorConfigCondition : ExecutionCondition
    {
        // Run legacy .editorconfig tests until our infrastructure is ready to test the
        // compiler support. Waiting for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/839836
        public override bool ShouldSkip => false;

        public override string SkipReason => "Test is only supported with our legacy .editorconfig support";
    }
}
