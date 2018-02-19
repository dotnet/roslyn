﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public abstract class BasicSquigglesCommon : AbstractEditorTest
    {
        public BasicSquigglesCommon(VisualStudioInstanceFactory instanceFactory, string projectTemplate)
            :base(instanceFactory, nameof(BasicSquigglesCommon), projectTemplate)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        public virtual void VerifySyntaxErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"Class A
      Sub S()
        Dim x = 1 +
      End Sub
End Class");
            VisualStudio.Editor.Verify.ErrorTags("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\r'[43-44]");
        }

        public virtual void VerifySemanticErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"Class A
      Sub S(b as Bar)
      End Sub
End Class");
            VisualStudio.Editor.Verify.ErrorTags("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[26-29]");
        }
    }
}
