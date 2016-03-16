// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using DteProject = EnvDTE.Project;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class Project
    {
        private DteProject _dteProject;
        private Solution _solution;
        private ProjectLanguage _language;

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
