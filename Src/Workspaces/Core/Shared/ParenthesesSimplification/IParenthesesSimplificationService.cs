#if false
using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers;

namespace Roslyn.Services.Shared.ParenthesesSimplification
{
    internal interface IParenthesesSimplificationService : ILanguageService
    {
        IDocument SimplifyParentheses(IDocument document, SyntaxAnnotation annotation, CancellationToken cancellationToken);
    }
}
#endif