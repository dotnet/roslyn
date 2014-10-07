using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A workspace that is used for Solutions created via Solution.Create
    /// </summary>
    internal class StandaloneWorkspace : Workspace
    {
        public StandaloneWorkspace(IWorkspaceServiceProvider workspaceServices, SolutionInfo solutionInfo)
            : base(workspaceServices)
        {
            SetCurrentSolution(this.CreateSolution(solutionInfo));
        }

        public StandaloneWorkspace(IWorkspaceServiceProvider workspaceServices, SolutionId id)
            : this(workspaceServices, SolutionInfo.Create(id, VersionStamp.Create()))
        {
        }
    }
}
