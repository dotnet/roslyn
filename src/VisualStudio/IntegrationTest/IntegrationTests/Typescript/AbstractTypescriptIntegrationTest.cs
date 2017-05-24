// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Typescript
{
    public abstract class AbstractTypescriptIntegrationTest : AbstractEditorTest
    {
        public AbstractTypescriptIntegrationTest(VisualStudioInstanceFactory instanceFactory, string solutionName)
            : base(instanceFactory, solutionName)
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Lib.ts", contents:
@"export function foo(): string {
    return ""Hello World""
}");
            VisualStudio.SolutionExplorer.AddFile(project, "File1.ts");
            VisualStudio.SolutionExplorer.OpenFile(project, "File1.ts");

            // Wait for the workspace to finish loading the typescript project.
            // Otherwise the file is part of miscellaneous files and Roslyn features will not work.
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        /// <remarks>
        /// For <see cref="TypescriptCommonTest"/> a typescript file is created
        /// inside of a CSharp project. Thus the project's language name is CSharp.
        /// </remarks>
        protected override string LanguageName => LanguageNames.CSharp;
    }
}
