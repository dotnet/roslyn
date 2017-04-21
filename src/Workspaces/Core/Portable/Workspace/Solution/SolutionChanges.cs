// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public struct SolutionChanges
    {
        private readonly Solution _newSolution;
        private readonly Solution _oldSolution;

        internal SolutionChanges(Solution newSolution, Solution oldSolution)
        {
            _newSolution = newSolution;
            _oldSolution = oldSolution;
        }

        public IEnumerable<Project> GetAddedProjects()
        {
            foreach (var id in _newSolution.ProjectIds)
            {
                if (!_oldSolution.ContainsProject(id))
                {
                    yield return _newSolution.GetProject(id);
                }
            }
        }

        public IEnumerable<ProjectChanges> GetProjectChanges()
        {
            var old = _oldSolution;

            // if the project states are different then there is a change.
            foreach (var id in _newSolution.ProjectIds)
            {
                var newState = _newSolution.GetProjectState(id);
                var oldState = old.GetProjectState(id);
                if (oldState != null && newState != null && newState != oldState)
                {
                    yield return _newSolution.GetProject(id).GetChanges(_oldSolution.GetProject(id));
                }
            }
        }

        public IEnumerable<Project> GetRemovedProjects()
        {
            foreach (var id in _oldSolution.ProjectIds)
            {
                if (!_newSolution.ContainsProject(id))
                {
                    yield return _oldSolution.GetProject(id);
                }
            }
        }
    }
}
