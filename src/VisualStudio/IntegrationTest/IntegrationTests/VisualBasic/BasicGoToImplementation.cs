// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

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
            var project = new ProjectUtils.Project(ProjectName);
            this.AddFile("FileImplementation.vb", project);
            this.OpenFile("FileImplementation.vb", project);
            Editor.SetText(
@"Class Implementation
  Implements IFoo
End Class");
            this.AddFile("FileInterface.vb", project);
            this.OpenFile("FileInterface.vb", project);
            Editor.SetText(
@"Interface IFoo 
End Interface");
            this.PlaceCaret("Interface IFoo");
            Editor.GoToImplementation();
            this.VerifyTextContains(@"Class Implementation$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }
    }
}
