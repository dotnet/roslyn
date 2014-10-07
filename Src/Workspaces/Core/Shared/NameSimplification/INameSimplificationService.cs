#if false
using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers;

namespace Roslyn.Services.Shared.NameSimplification
{
    internal interface INameSimplificationService : ILanguageService
    {
        IDocument SimplifyNames(IDocument document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken);
    }
}
#endif