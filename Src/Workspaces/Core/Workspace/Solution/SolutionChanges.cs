// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public struct SolutionChanges
    {
        private readonly Solution newSolution;
        private readonly Solution oldSolution;

        internal SolutionChanges(Solution newSolution, Solution oldSolution)
        {
            this.newSolution = newSolution;
            this.oldSolution = oldSolution;
        }

        public IEnumerable<Project> GetAddedProjects()
        {
            foreach (var id in this.newSolution.ProjectIds)
            {
                if (!this.oldSolution.ContainsProject(id))
                {
                    yield return this.newSolution.GetProject(id);
                }
            }
        }

        public IEnumerable<ProjectChanges> GetProjectChanges()
        {
            var old = this.oldSolution;

            // if the project states are different then there is a change.
            foreach (var id in this.newSolution.ProjectIds)
            {
                var newState = this.newSolution.GetProjectState(id);
                var oldState = old.GetProjectState(id);
                if (oldState != null && newState != null && newState != oldState)
                {
                    yield return newSolution.GetProject(id).GetChanges(oldSolution.GetProject(id));
                }
            }
        }

        public IEnumerable<Project> GetRemovedProjects()
        {
            foreach (var id in this.oldSolution.ProjectIds)
            {
                if (!this.newSolution.ContainsProject(id))
                {
                    yield return this.oldSolution.GetProject(id);
                }
            }
        }
    }
}
