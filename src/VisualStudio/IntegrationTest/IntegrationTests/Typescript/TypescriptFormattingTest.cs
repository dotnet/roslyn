// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Typescript
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class TypescriptFormattingTest : AbstractEditorTest
    {
        public TypescriptFormattingTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(TypescriptCompletionTest))
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "File1.ts");
            VisualStudio.SolutionExplorer.OpenFile(project, "File1.ts");

            // Wait for current file to become part of the typescript project
            // and not miscellaneous files. Otherwise intellisense will not work.
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        /// <remarks>
        /// For <see cref="TypescriptCommonTest"/> a typescript file is created
        /// inside of a CSharp project. Thus the project's language name is CSharp.
        /// </remarks>
        protected override string LanguageName => LanguageNames.CSharp;

        [Fact]
        public void FormatAssignment()
        {
            VisualStudio.Editor.SetText("var   v =   0;");

            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains("var v = 0;");
        }
    }
}
