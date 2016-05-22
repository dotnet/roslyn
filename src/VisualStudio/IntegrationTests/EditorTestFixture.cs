// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.VisualStudio.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class EditorTestFixture : IDisposable
    {
        private readonly VisualStudioInstanceContext _visualStudio;
        private readonly Workspace _workspace;
        private readonly Solution _solution;
        private readonly Project _project;
        protected readonly EditorWindow EditorWindow;

        protected EditorTestFixture(VisualStudioInstanceFactory instanceFactory, string solutionName)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            _solution = _visualStudio.Instance.SolutionExplorer.CreateSolution(solutionName);
            _project = _solution.AddProject("TestProj", ProjectTemplate.ClassLibrary, ProjectLanguage.CSharp);

            _workspace = _visualStudio.Instance.Workspace;
            _workspace.UseSuggestionMode = false;

            EditorWindow = _visualStudio.Instance.EditorWindow;
        }

        public void Dispose()
        {
            _visualStudio.Dispose();
        }

        public void WaitForWorkspace()
        {
            _workspace.WaitForAsyncOperations("Workspace");
        }

        public void WaitForAllAsyncOperations()
        {
            _workspace.WaitForAllAsyncOperations();
        }
    }
}
