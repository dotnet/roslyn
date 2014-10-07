namespace Roslyn.Services.Host
{
    /// <summary>
    /// A factory that creates empty solutions.
    /// </summary>
    public interface ISolutionFactoryService : IWorkspaceService
    {
        ISolution CreateSolution(SolutionId solutionId);
        ISolution CreateSolution(ISolutionInfo solutionInfo);
    }
}