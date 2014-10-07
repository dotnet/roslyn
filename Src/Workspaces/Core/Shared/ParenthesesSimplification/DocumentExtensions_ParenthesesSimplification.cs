#if false
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Services.Shared.ParenthesesSimplification;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    public static partial class DocumentExtensions
    {
        public static IDocument SimplifyParentheses(this IDocument document, SyntaxAnnotation annotation, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetLanguageService<IParenthesesSimplificationService>().SimplifyParentheses(document, annotation, cancellationToken);
        }
    }
}
#endif