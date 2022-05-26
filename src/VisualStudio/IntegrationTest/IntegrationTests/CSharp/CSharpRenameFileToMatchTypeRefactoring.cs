// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpRenameFileToMatchTypeRefactoring : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpRenameFileToMatchTypeRefactoring(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpGenerateFromUsage))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public void RenameFileToMatchType_ExistingCode()
        {
            var project = new ProjectUtils.Project(ProjectName);

            SetUpEditor(@"class $$MismatchedClassName { }");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Rename file to MismatchedClassName.cs", applyFix: true);

            // Ensure the file is still open in the editor, and that the file name change was made & saved
            VisualStudio.Editor.Verify.TextContains("class MismatchedClassName { }");
            AssertEx.EqualOrDiff(@"class MismatchedClassName { }", VisualStudio.SolutionExplorer.GetFileContents(project, "MismatchedClassName.cs"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public void RenameFileToMatchType_InSubfolder()
        {
            var project = new ProjectUtils.Project(ProjectName);

            VisualStudio.SolutionExplorer.AddFile(project, @"folder1\folder2\test.cs", open: true);

            SetUpEditor(@"class $$MismatchedClassName { }");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Rename file to MismatchedClassName.cs", applyFix: true);

            // Ensure the file is still open in the editor, and that the file name change was made & saved
            VisualStudio.Editor.Verify.TextContains("class MismatchedClassName { }");
            AssertEx.EqualOrDiff(@"class MismatchedClassName { }", VisualStudio.SolutionExplorer.GetFileContents(project, @"folder1\folder2\MismatchedClassName.cs"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public void RenameFileToMatchType_UndoStackPreserved()
        {
            var project = new ProjectUtils.Project(ProjectName);

            SetUpEditor(@"$$class MismatchedClassName { }");
            VisualStudio.Editor.SendKeys("public ");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Rename file to MismatchedClassName.cs", applyFix: true);

            // Ensure the file is still open in the editor, and that the file name change was made & saved
            VisualStudio.Editor.Verify.CurrentLineText("public class MismatchedClassName { }");
            AssertEx.EqualOrDiff(@"public class MismatchedClassName { }", VisualStudio.SolutionExplorer.GetFileContents(project, "MismatchedClassName.cs"));

            // The first undo is for the file rename.
            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.CurrentLineText("public class MismatchedClassName { }");
            AssertEx.EqualOrDiff(@"public class MismatchedClassName { }", VisualStudio.SolutionExplorer.GetFileContents(project, "Class1.cs"));

            // The second undo is for the text changes.
            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.CurrentLineText("class MismatchedClassName { }");

            // Redo the text changes
            VisualStudio.Editor.Redo();
            VisualStudio.Editor.Verify.CurrentLineText("public class MismatchedClassName { }");

            // Redo the file rename
            VisualStudio.Editor.Redo();
            AssertEx.EqualOrDiff(@"public class MismatchedClassName { }", VisualStudio.SolutionExplorer.GetFileContents(project, "MismatchedClassName.cs"));
        }
    }
}
