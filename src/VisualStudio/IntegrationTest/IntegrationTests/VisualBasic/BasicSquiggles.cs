// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicSquiggles : AbstractEditorTest
    {
        public BasicSquiggles(VisualStudioInstanceFactory instanceFactory)
            :base(instanceFactory, nameof(BasicSquiggles), WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public void VerifySyntaxErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"Class A
      Sub S()
        Dim x = 1 +
      End Sub
End Class");
            VisualStudio.Editor.Verify.ErrorTags("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\r'[43-44]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public void VerifySemanticErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"Class A
      Sub S(b as Bar)
      End Sub
End Class");
            VisualStudio.Editor.Verify.ErrorTags("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[26-29]");
        }
    }
}
