// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToDefinition(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpGoToDefinition))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToClassDeclaration()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.AddFile("FileDef.cs", project);
            this.OpenFile("FileDef.cs", project);
            Editor.SetText(
@"class SomeClass
{
}");
            this.AddFile("FileConsumer.cs", project);
            this.OpenFile("FileConsumer.cs", project);
            Editor.SetText(
@"class SomeOtherClass
{
    SomeClass sc;
}");
            this.PlaceCaret("SomeClass");
            Editor.GoToDefinition();
            this.VerifyTextContains(@"class SomeClass$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToDefinitionOpensProvisionalTabIfDocumentNotAlreadyOpen()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.AddFile("FileDef.cs", project);
            this.OpenFile("FileDef.cs", project);
            Editor.SetText(
@"class SomeClass
{
}");
            this.CloseFile("FileDef.cs", project);
            this.AddFile("FileConsumer.cs", project);
            this.OpenFile("FileConsumer.cs", project);
            Editor.SetText(
@"class SomeOtherClass
{
    SomeClass sc;
}");
            this.PlaceCaret("SomeClass");
            Editor.GoToDefinition();
            this.VerifyTextContains(@"class SomeClass$$", assertCaretPosition: true);
            Assert.True(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToDefinitionWithMultipleResults()
        {
            SetUpEditor(
@"partial class /*Marker*/ $$PartialClass { }

partial class PartialClass { int i = 0; }");

            Editor.GoToDefinition();

            const string programReferencesCaption = "'PartialClass' declarations";
            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(programReferencesCaption);

            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
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