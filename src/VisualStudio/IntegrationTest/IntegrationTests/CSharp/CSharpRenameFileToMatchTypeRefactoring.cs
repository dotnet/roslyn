// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public CSharpRenameFileToMatchTypeRefactoring(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpGenerateFromUsage))
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
            VisualStudio.SolutionExplorer.Verify.FileContents(project, "MismatchedClassName.cs", @"class MismatchedClassName { }");
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
            VisualStudio.SolutionExplorer.Verify.FileContents(project, "MismatchedClassName.cs", @"public class MismatchedClassName { }");

            // Undo just undoes text changes. The file rename is not undone, and the changes are not saved after the undo
            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.CurrentLineText("class MismatchedClassName { }");
            VisualStudio.SolutionExplorer.Verify.FileContents(project, "MismatchedClassName.cs", @"public class MismatchedClassName { }");
        }
    }
}
