namespace Roslyn.Services.Host.UnitTests
{
    public class TestSolutionFactory : ISolutionFactoryService
    {
        public ISolution CreateSolution(SolutionId id)
        {
            return Solution.Create(id);
        }

        public ISolution CreateSolution(ISolutionInfo info)
        {
            return Solution.Create(info);
        }
    }
}
