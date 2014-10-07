using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Host
{
    [ExportWorkspaceServiceFactory(typeof(IRetainerFactory<CommonCompilation>), WorkspaceKind.Any)]
    internal class CompilationRetainerFactoryFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new CompilationRetainerFactory();
        }

        public class CompilationRetainerFactory : CostBasedRetainerFactory<CommonCompilation>
        {
            private const long MaxTotalCostForAllCompilations = 12;
            private const int MinimumCompilationsRetained = 2;

            public CompilationRetainerFactory()
                : base(ComputeCompilationCost, MaxTotalCostForAllCompilations, MinimumCompilationsRetained)
            {
            }

            private static long ComputeCompilationCost(CommonCompilation compilation)
            {
                return ComputeCompilationCost(compilation, new HashSet<CommonCompilation>());
            }

            private static long ComputeCompilationCost(CommonCompilation compilation, HashSet<CommonCompilation> seen)
            {
                // compute cost of compilation as 1 per compilation kept alive
                if (!seen.Contains(compilation))
                {
                    seen.Add(compilation);

                    long cost = 1;
                    foreach (var md in compilation.References)
                    {
                        var cr = md as ICompilationReference;
                        if (cr != null)
                        {
                            cost += ComputeCompilationCost(cr.Compilation, seen);

                            // no point in being more precise if we've already reached max cost.
                            if (cost >= MaxTotalCostForAllCompilations)
                            {
                                break;
                            }
                        }
                    }

                    return cost;
                }

                return 0;
            }
        }
    }
}