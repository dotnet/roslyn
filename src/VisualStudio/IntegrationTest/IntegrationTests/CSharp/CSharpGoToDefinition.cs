// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGoToDefinition : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToDefinition()
            : base(nameof(CSharpGoToDefinition))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public async Task GoToClassDeclarationAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileDef.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileDef.cs");
            await VisualStudio.Editor.SetTextAsync(
@"class SomeClass
{
}");
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName ,"FileConsumer.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileConsumer.cs");
            await VisualStudio.Editor.SetTextAsync(
@"class SomeOtherClass
{
    SomeClass sc;
}");
            await VisualStudio.Editor.PlaceCaretAsync("SomeClass");
            await VisualStudio.Editor.GoToDefinitionAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"class SomeClass$$", assertCaretPosition: true);
            Assert.False(await VisualStudio.Shell.IsActiveTabProvisionalAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public async Task GoToDefinitionOpensProvisionalTabIfDocumentNotAlreadyOpenAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileDef.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileDef.cs");
            await VisualStudio.Editor.SetTextAsync(
@"class SomeClass
{
}
");
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "FileDef.cs", saveFile: true);
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileConsumer.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileConsumer.cs");
            await VisualStudio.Editor.SetTextAsync(
@"class SomeOtherClass
{
    SomeClass sc;
}");
            await VisualStudio.Editor.PlaceCaretAsync("SomeClass");
            await VisualStudio.Editor.GoToDefinitionAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"class SomeClass$$", assertCaretPosition: true);
            Assert.True(await VisualStudio.Shell.IsActiveTabProvisionalAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public async Task GoToDefinitionWithMultipleResultsAsync()
        {
            await SetUpEditorAsync(
@"partial class /*Marker*/ $$PartialClass { }

partial class PartialClass { int i = 0; }");

            await VisualStudio.Editor.GoToDefinitionAsync();

            const string programReferencesCaption = "'PartialClass' declarations";
            var results = await VisualStudio.FindReferencesWindow.GetContentsAsync(programReferencesCaption);

            var activeWindowCaption = await VisualStudio.Shell.GetActiveWindowCaptionAsync();
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
