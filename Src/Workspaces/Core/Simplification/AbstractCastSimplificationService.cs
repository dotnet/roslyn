using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Simplification
{
    internal abstract class AbstractCastSimplificationService
    {
        public abstract IDocument SimplifyCasts(IDocument document, CancellationToken cancellationToken);
    }
}
