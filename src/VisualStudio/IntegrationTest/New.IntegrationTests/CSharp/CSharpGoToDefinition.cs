// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Shell.TableControl;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.GoToDefinition)]
    public class CSharpGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToDefinition()
            : base(nameof(CSharpGoToDefinition))
        {
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task GoToClassDeclaration()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileDef.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileDef.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class SomeClass
{
}", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileConsumer.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileConsumer.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class SomeOtherClass
{
    SomeClass sc;
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("SomeClass", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            var dirtyModifier = await TestServices.Editor.GetDirtyIndicatorAsync(HangMitigatingCancellationToken);
            Assert.Equal($"FileDef.cs{dirtyModifier}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"class SomeClass$$", assertCaretPosition: true, HangMitigatingCancellationToken);
            Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task GoToDefinitionOpensProvisionalTabIfDocumentNotAlreadyOpen()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileDef.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileDef.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class SomeClass
{
}
", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.CloseCodeFileAsync(project, "FileDef.cs", saveFile: true, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileConsumer.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileConsumer.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class SomeOtherClass
{
    SomeClass sc;
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("SomeClass", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("FileDef.cs", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"class SomeClass$$", assertCaretPosition: true, HangMitigatingCancellationToken);
            Assert.True(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task GoToDefinitionWithMultipleResults()
        {
            await SetUpEditorAsync(
@"partial class /*Marker*/ $$PartialClass { }

partial class PartialClass { int i = 0; }", HangMitigatingCancellationToken);

            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("'PartialClass' declarations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

            Assert.Collection(
                results,
                new Action<ITableEntryHandle2>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "partial class /*Marker*/ PartialClass { }", actual: reference.GetText());
                        Assert.Equal(expected: 0, actual: reference.GetLine());
                        Assert.Equal(expected: 25, actual: reference.GetColumn());
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "partial class PartialClass { int i = 0; }", actual: reference.GetText());
                        Assert.Equal(expected: 2, actual: reference.GetLine());
                        Assert.Equal(expected: 14, actual: reference.GetColumn());
                    }
                });
        }
    }
}
