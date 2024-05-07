// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
    public class CSharpRenameFileToMatchTypeRefactoring : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpRenameFileToMatchTypeRefactoring()
            : base(nameof(CSharpRenameFileToMatchTypeRefactoring))
        {
        }

        [IdeFact]
        public async Task RenameFileToMatchType_ExistingCode()
        {
            var project = ProjectName;

            await SetUpEditorAsync(@"class $$MismatchedClassName { }", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Rename file to MismatchedClassName.cs", applyFix: true, cancellationToken: HangMitigatingCancellationToken);

            // Ensure the file is still open in the editor, and that the file name change was made & saved
            await TestServices.EditorVerifier.TextContainsAsync("class MismatchedClassName { }", cancellationToken: HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(@"class MismatchedClassName { }", await TestServices.SolutionExplorer.GetFileContentsAsync(project, "MismatchedClassName.cs", HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task RenameFileToMatchType_InSubfolder()
        {
            var project = ProjectName;

            await TestServices.SolutionExplorer.AddFileAsync(project, @"folder1\folder2\test.cs", open: true, cancellationToken: HangMitigatingCancellationToken);

            await SetUpEditorAsync(@"class $$MismatchedClassName { }", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Rename file to MismatchedClassName.cs", applyFix: true, cancellationToken: HangMitigatingCancellationToken);

            // Ensure the file is still open in the editor, and that the file name change was made & saved
            await TestServices.EditorVerifier.TextContainsAsync("class MismatchedClassName { }", cancellationToken: HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(@"class MismatchedClassName { }", await TestServices.SolutionExplorer.GetFileContentsAsync(project, @"folder1\folder2\MismatchedClassName.cs", HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task RenameFileToMatchType_UndoStackPreserved()
        {
            var project = ProjectName;

            await SetUpEditorAsync(@"$$class MismatchedClassName { }", HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("public ", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Rename file to MismatchedClassName.cs", applyFix: true, cancellationToken: HangMitigatingCancellationToken);

            // Ensure the file is still open in the editor, and that the file name change was made & saved
            await TestServices.EditorVerifier.CurrentLineTextAsync("public class MismatchedClassName { }", cancellationToken: HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(@"public class MismatchedClassName { }", await TestServices.SolutionExplorer.GetFileContentsAsync(project, "MismatchedClassName.cs", HangMitigatingCancellationToken));

            // The first undo is for the file rename.
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("public class MismatchedClassName { }", cancellationToken: HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(@"public class MismatchedClassName { }", await TestServices.SolutionExplorer.GetFileContentsAsync(project, "Class1.cs", HangMitigatingCancellationToken));

            // The second undo is for the text changes.
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("class MismatchedClassName { }", cancellationToken: HangMitigatingCancellationToken);

            // Redo the text changes
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Redo, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("public class MismatchedClassName { }", cancellationToken: HangMitigatingCancellationToken);

            // Redo the file rename
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Redo, HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(@"public class MismatchedClassName { }", await TestServices.SolutionExplorer.GetFileContentsAsync(project, "MismatchedClassName.cs", HangMitigatingCancellationToken));
        }
    }
}
