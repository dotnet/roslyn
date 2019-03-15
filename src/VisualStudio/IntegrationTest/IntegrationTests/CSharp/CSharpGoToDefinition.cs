// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToDefinition(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpGoToDefinition))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToClassDeclaration()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileDef.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileDef.cs");
            VisualStudio.Editor.SetText(
@"class SomeClass
{
}");
            VisualStudio.SolutionExplorer.AddFile(project, "FileConsumer.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileConsumer.cs");
            VisualStudio.Editor.SetText(
@"class SomeOtherClass
{
    SomeClass sc;
}");
            VisualStudio.Editor.PlaceCaret("SomeClass");
            VisualStudio.Editor.GoToDefinition();
            VisualStudio.Editor.Verify.TextContains(@"class SomeClass$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Shell.IsActiveTabProvisional());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToDefinitionOpensProvisionalTabIfDocumentNotAlreadyOpen()
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
            VisualStudio.Editor.GoToDefinition();
            VisualStudio.Editor.Verify.TextContains(@"class SomeClass$$", assertCaretPosition: true);
            Assert.True(VisualStudio.Shell.IsActiveTabProvisional());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToDefinitionWithMultipleResults()
        {
            SetUpEditor(
@"partial class /*Marker*/ $$PartialClass { }

partial class PartialClass { int i = 0; }");

            VisualStudio.Editor.GoToDefinition();

            const string programReferencesCaption = "'PartialClass' declarations";
            var results = VisualStudio.FindReferencesWindow.GetContents(programReferencesCaption);

            var activeWindowCaption = VisualStudio.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: programReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "partial class /*Marker*/ PartialClass { }", actual: reference.Code);
                        Assert.Equal(expected: 0, actual: reference.Line);
                        Assert.Equal(expected: 25, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "partial class PartialClass { int i = 0; }", actual: reference.Code);
                        Assert.Equal(expected: 2, actual: reference.Line);
                        Assert.Equal(expected: 14, actual: reference.Column);
                    }
                });
        }
    }
}
