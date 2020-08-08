// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void SimpleGoToImplementation()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileImplementation.vb");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileImplementation.vb");
            VisualStudio.Editor.SetText(
@"Class Implementation
  Implements IGoo
End Class");
            VisualStudio.SolutionExplorer.AddFile(project, "FileInterface.vb");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileInterface.vb");
            VisualStudio.Editor.SetText(
@"Interface IGoo 
End Interface");
            VisualStudio.Editor.PlaceCaret("Interface IGoo");
            VisualStudio.Editor.GoToImplementation();
            VisualStudio.Editor.Verify.TextContains(@"Class Implementation$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Shell.IsActiveTabProvisional());
        }
    }
}
