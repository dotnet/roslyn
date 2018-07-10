// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public abstract class BasicSquigglesCommon : AbstractIdeEditorTest
    {
        public BasicSquigglesCommon(string projectTemplate)
            : base(nameof(BasicSquigglesCommon), projectTemplate)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        public virtual async Task VerifySyntaxErrorSquigglesAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Class A
      Sub S()
        Dim x = 1 +
      End Sub
End Class");
            await VisualStudio.Editor.Verify.ErrorTagsAsync("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\r'[43-44]");
        }

        public virtual async Task VerifySemanticErrorSquigglesAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Class A
      Sub S(b as Bar)
      End Sub
End Class");
            await VisualStudio.Editor.Verify.ErrorTagsAsync("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[26-29]");
        }
    }
}
