// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGoToImplementation : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGoToImplementation(VisualStudioInstanceFactory instanceFactory)
                    : base(instanceFactory, nameof(BasicGoToImplementation))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void SimpleGoToImplementation()
        {
            AddFile("FileImplementation.vb");
            OpenFile(ProjectName, "FileImplementation.vb");
            Editor.SetText(
@"Class Implementation
  Implements IFoo
End Class");
            AddFile("FileInterface.vb");
            OpenFile(ProjectName, "FileInterface.vb");
            Editor.SetText(
@"Interface IFoo 
End Interface");
            PlaceCaret("Interface IFoo");
            Editor.GoToImplementation();
            VerifyTextContains(@"Class Implementation$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }
    }
}
