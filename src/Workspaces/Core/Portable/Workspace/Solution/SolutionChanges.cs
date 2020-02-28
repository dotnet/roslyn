// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
                    yield return _newSolution.GetRequiredProject(id);
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
                    yield return _newSolution.GetRequiredProject(id).GetChanges(_oldSolution.GetRequiredProject(id));
                }
            }
        }

        public IEnumerable<Project> GetRemovedProjects()
        {
            foreach (var id in _oldSolution.ProjectIds)
            {
                if (!_newSolution.ContainsProject(id))
                {
                    yield return _oldSolution.GetRequiredProject(id);
                }
            }
        }
    }
}
