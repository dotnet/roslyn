// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Shell.TableControl;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.GoToDefinition)]
    public class CSharpGoToDefinitionNet60 : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToDefinitionNet60()
            : base(nameof(CSharpGoToDefinition), WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [IdeFact]
        public async Task ConsoleWriteLine()
        {
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);

            globalOptions.SetGlobalOption(new OptionKey(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, language: LanguageName), false);

            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "C.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "C.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync("""
                using System;

                class C
                {
                    public void M()
                    {
                        Console.WriteLine("hello");
                    }
                }
                """, HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("WriteLine", charsOffset: -1, HangMitigatingCancellationToken);

            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("Console [SourceLink] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        }
    }
}
