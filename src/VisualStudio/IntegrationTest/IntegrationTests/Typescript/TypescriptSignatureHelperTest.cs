// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Typescript
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class TypescriptSignatureHelperTest : AbstractEditorTest
    {
        public TypescriptSignatureHelperTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(TypescriptCompletionTest))
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "File1.ts");
            VisualStudio.SolutionExplorer.OpenFile(project, "File1.ts");

            // Wait for current file to become part of the typescript project
            // and not miscellaneous files. Otherwise signature helper will not work.
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        /// <remarks>
        /// For <see cref="TypescriptCommonTest"/> a typescript file is created
        /// inside of a CSharp project. Thus the project's language name is CSharp.
        /// </remarks>
        protected override string LanguageName => LanguageNames.CSharp;

        [Fact]
        public void ShowSignature()
        {
            VisualStudio.Editor.SetText(@"var v = 0;
v.toExponential(");

            VisualStudio.Editor.InvokeSignatureHelp();
            VisualStudio.Editor.Verify.CurrentSignature(
                "toExponential([fractionDigits?: number]): string\r\n" +
                "Returns a string containing a number represented in exponential notation.");
        }
    }
}
