using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services
{
    public static partial class SyntaxExtensions
    {
        public static Document CaseCorrect(this CommonSyntaxTree syntaxTree, Solution solution, CancellationToken cancellationToken = default(CancellationToken))
        {
            return solution.GetDocument(syntaxTree).CaseCorrect(cancellationToken);
        }

        public static Document CaseCorrect(this CommonSyntaxTree syntaxTree, Solution solution, SyntaxAnnotation annotation, CancellationToken cancellationToken = default(CancellationToken))
        {
            return solution.GetDocument(syntaxTree).CaseCorrect(annotation, cancellationToken);
        }

        public static Document CaseCorrect(this CommonSyntaxTree syntaxTree, Solution solution, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return solution.GetDocument(syntaxTree).CaseCorrect(span, cancellationToken);
        }

        public static Document CaseCorrect(this CommonSyntaxTree syntaxTree, Solution solution, IEnumerable<TextSpan> spans, CancellationToken cancellationToken = default(CancellationToken))
        {
            return solution.GetDocument(syntaxTree).CaseCorrect(spans, cancellationToken);
        }
    }
}