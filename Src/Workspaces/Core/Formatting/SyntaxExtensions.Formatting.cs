using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Formatting;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    public static partial class SyntaxExtensions
    {
        public static IFormattingResult Format(this CommonSyntaxNode node, FormattingOptions options, IEnumerable<IFormattingRule> formattingRules = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Formatter.Format(node, options, formattingRules, cancellationToken);
        }

        public static IFormattingResult Format(this CommonSyntaxNode root, TextSpan textSpan, FormattingOptions options, IEnumerable<IFormattingRule> formattingRules = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Formatter.Format(root, textSpan, options, formattingRules, cancellationToken);
        }

        public static IFormattingResult Format(this CommonSyntaxNode root, IEnumerable<TextSpan> spans, FormattingOptions options, IEnumerable<IFormattingRule> formattingRules = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Formatter.Format(root, spans, options, formattingRules, cancellationToken);
        }

        public static IFormattingResult Format(this CommonSyntaxNode root, SyntaxAnnotation annotation, FormattingOptions options, IEnumerable<IFormattingRule> formattingRules = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Formatter.Format(root, annotation, options, formattingRules, cancellationToken);
        }
    }
}