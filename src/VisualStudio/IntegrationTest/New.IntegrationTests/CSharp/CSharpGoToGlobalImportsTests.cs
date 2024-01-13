// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpGoToGlobalImportsTests : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToGlobalImportsTests() : base(solutionName: nameof(CSharpGoToGlobalImportsTests), projectTemplate: WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [IdeFact]
        public async Task TestGlobalImports()
        {
            // Make sure no glyph is in the margin at first.
            await TestServices.InheritanceMargin.DisableOptionsAsync(LanguageName, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(
                ProjectName, "Class1.cs", HangMitigatingCancellationToken);

            await TestServices.InheritanceMargin.EnableOptionsAndEnsureGlyphsAppearAsync(LanguageName, 1, HangMitigatingCancellationToken);
            await TestServices.InheritanceMargin.ClickTheGlyphOnLine(1, HangMitigatingCancellationToken);

            // Move focus to menu item 'System'
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            // Navigate to 'System'
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.InheritanceMargin], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"global using global::System;$$", assertCaretPosition: true);

            var document = await TestServices.Editor.GetActiveDocumentAsync(HangMitigatingCancellationToken);
            RoslynDebug.AssertNotNull(document);
            Assert.NotEqual(WorkspaceKind.MetadataAsSource, document.Project.Solution.WorkspaceKind);
        }
    }
}
