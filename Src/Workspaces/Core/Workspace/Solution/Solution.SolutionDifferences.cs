using System.Collections.Generic;
using System.Linq;

namespace Roslyn.Services
{
    public partial class Solution
    {
        private class SolutionDifferences : ISolutionDifferences
        {
            private readonly Solution newSolution;
            private readonly ISolution oldSolution;

            internal SolutionDifferences(Solution newSolution, ISolution oldSolution)
            {
                this.newSolution = newSolution;
                this.oldSolution = oldSolution;
            }

            public IEnumerable<ProjectId> GetAddedProjects()
            {
                foreach (var id in this.newSolution.ProjectIds)
                {
                    if (!this.oldSolution.ContainsProject(id))
                    {
                        yield return id;
                    }
                }
            }

            public IEnumerable<ProjectId> GetChangedProjects()
            {
                return GetChangedProjects(onlyReferences: false);
            }

            public IEnumerable<IProjectDifferences> GetProjectDifferences()
            {
                return GetChangedProjects().Select(pid => newSolution.GetProject(pid).GetDifferences(oldSolution.GetProject(pid)));
            }

            public IEnumerable<ProjectId> GetProjectsWithChangedReferences()
            {
                return GetChangedProjects(onlyReferences: true);
            }

            public IEnumerable<ProjectId> GetRemovedProjects()
            {
                foreach (var id in this.oldSolution.ProjectIds)
                {
                    if (!this.newSolution.ContainsProject(id))
                    {
                        yield return id;
                    }
                }
            }

            private IEnumerable<ProjectId> GetChangedProjects(bool onlyReferences)
            {
                var old = oldSolution as Solution;
                if (old != null)
                {
                    // if the project states are different then there is a change.
                    foreach (var id in this.newSolution.ProjectIds)
                    {
                        var newState = this.newSolution.GetProjectState(id);
                        var oldState = old.GetProjectState(id);
                        if (oldState != null && newState != oldState)
                        {
                            if (onlyReferences)
                            {
                                var oldRefs = oldState.ProjectReferences;
                                var newRefs = newState.ProjectReferences;

                                // Note: this uses reference equality, but that's okay, since the
                                // state cache the project reference result across other changes;
                                if (oldRefs != newRefs)
                                {
                                    yield return id;
                                }
                            }
                            else
                            {
                                yield return id;
                            }
                        }
                    }
                }
                else
                {
                    // all are changed if the solution is now a 'Solution'
                    foreach (var id in this.oldSolution.ProjectIds)
                    {
                        if (this.newSolution.ContainsProject(id))
                        {
                            yield return id;
                        }
                    }
                }
            }
        }
    }
}
