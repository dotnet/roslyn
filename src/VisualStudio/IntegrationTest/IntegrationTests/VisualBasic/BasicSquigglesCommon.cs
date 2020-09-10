// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public abstract class BasicSquigglesCommon : AbstractEditorTest
    {
        public BasicSquigglesCommon(VisualStudioInstanceFactory instanceFactory, string projectTemplate)
            : base(instanceFactory, nameof(BasicSquigglesCommon), projectTemplate)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        public virtual void VerifySyntaxErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"Class A
      Shared Sub S()
        Dim x = 1 +
      End Sub
End Class");
            VisualStudio.Editor.Verify.ErrorTags("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\\r'[50-51]");
        }

        public virtual void VerifySemanticErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"Class A
      Shared Sub S(b as Bar)
        Console.WriteLine(b)
      End Sub
End Class");
            VisualStudio.Editor.Verify.ErrorTags("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[33-36]");
        }
    }
}
