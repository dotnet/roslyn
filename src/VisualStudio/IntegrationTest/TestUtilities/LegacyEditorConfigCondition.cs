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
        public override bool ShouldSkip => EditorConfigDocumentOptionsProviderFactory.ShouldUseNativeEditorConfigSupport;

        public override string SkipReason => "Test is only supported with our legacy .editorconfig support";
    }
}
