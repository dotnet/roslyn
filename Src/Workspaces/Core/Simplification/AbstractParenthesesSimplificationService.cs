using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Simplification
{
    internal abstract class AbstractParenthesesSimplificationService
    {
        public abstract IDocument SimplifyParentheses(IDocument document, CancellationToken cancellationToken);
    }
}
