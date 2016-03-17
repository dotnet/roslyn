// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using DteProject = EnvDTE.Project;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>Provides a means of interacting with a Project that exists in the current solution.</summary>
    public class Project
    {
        private readonly DteProject _dteProject;
        private readonly Solution _solution;
        private readonly ProjectLanguage _language;

        internal Project(DteProject dteProject, Solution solution, ProjectLanguage language)
        {
            _dteProject = dteProject;
            _solution = solution;
            _language = language;
        }

        public DteProject DteProject
            => _dteProject;

        public ProjectLanguage Language
            => _language;

        public Solution Solution
            => _solution;
    }
}
