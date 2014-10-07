using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Services.Formatting;
using Roslyn.Services.Shared.Extensions;

namespace Roslyn.Services
{
    public static partial class DocumentExtensions
    {
        public static IEnumerable<IFormattingRule> GetDefaultFormattingRules(this Document document)
        {
            return document.GetLanguageService<IFormattingService>().GetDefaultFormattingRules();
        }

        public static Document Format(this Document document, IEnumerable<IFormattingRule> formattingRules = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.WithSyntaxRoot(document.GetSyntaxRoot(cancellationToken).Format(document.GetFormattingOptions(), formattingRules, cancellationToken).GetFormattedRoot(cancellationToken));
        }

        public static Document Format(this Document document, TextSpan textSpan, IEnumerable<IFormattingRule> formattingRules = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.WithSyntaxRoot(document.GetSyntaxRoot(cancellationToken).Format(textSpan, document.GetFormattingOptions(), formattingRules, cancellationToken).GetFormattedRoot(cancellationToken));
        }

        public static Document Format(this Document document, IEnumerable<TextSpan> spans, IEnumerable<IFormattingRule> formattingRules = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.WithSyntaxRoot(document.GetSyntaxRoot(cancellationToken).Format(spans, document.GetFormattingOptions(), formattingRules, cancellationToken).GetFormattedRoot(cancellationToken));
        }

        public static Document Format(this Document document, SyntaxAnnotation annotation, IEnumerable<IFormattingRule> formattingRules = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.WithSyntaxRoot(document.GetSyntaxRoot(cancellationToken).Format(annotation, document.GetFormattingOptions(), formattingRules, cancellationToken).GetFormattedRoot(cancellationToken));
        }
    }
}