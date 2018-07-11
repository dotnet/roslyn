// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGoToImplementation : AbstractIdeEditorTest
    {
        public BasicGoToImplementation()
            : base(nameof(BasicGoToImplementation))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public async Task SimpleGoToImplementationAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileImplementation.vb");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileImplementation.vb");
            await VisualStudio.Editor.SetTextAsync(
@"Class Implementation
  Implements IGoo
End Class");
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileInterface.vb");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileInterface.vb");
            await VisualStudio.Editor.SetTextAsync(
@"Interface IGoo 
End Interface");
            await VisualStudio.Editor.PlaceCaretAsync("Interface IGoo");
            await VisualStudio.Editor.GoToImplementationAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"Class Implementation$$", assertCaretPosition: true);
            Assert.False(await VisualStudio.Shell.IsActiveTabProvisionalAsync());
        }
    }
}
