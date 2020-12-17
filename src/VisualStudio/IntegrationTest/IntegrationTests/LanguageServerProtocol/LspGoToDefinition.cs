// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.LanguageServerProtocol
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class LspGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public LspGoToDefinition(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(LspGoToDefinition))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition), Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public void GoToDefinitionLSP()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileDef.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileDef.cs");
            VisualStudio.Editor.SetText(
@"class SomeClass
{
}
");
            VisualStudio.SolutionExplorer.CloseCodeFile(project, "FileDef.cs", saveFile: true);
            VisualStudio.SolutionExplorer.AddFile(project, "FileConsumer.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileConsumer.cs");
            VisualStudio.Editor.SetText(
@"class SomeOtherClass
{
    SomeClass sc;
}");
            VisualStudio.Editor.PlaceCaret("SomeClass");
            VisualStudio.Editor.GoToDefinition("FileDef.cs");
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
            VisualStudio.Editor.Verify.TextContains(@"class SomeClass$$", assertCaretPosition: true);
            Assert.True(VisualStudio.Shell.IsActiveTabProvisional());
        }
    }
}
