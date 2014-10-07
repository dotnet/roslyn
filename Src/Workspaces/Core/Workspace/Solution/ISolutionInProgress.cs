using Roslyn.Services;

namespace Roslyn.Services
{
    internal interface ISolutionInProgress
    {
        ISolution GetInProgressSolution();
    }
}