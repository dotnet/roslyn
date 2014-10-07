using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Services.CaseCorrection;
using Roslyn.Services.CodeCleanup;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    public static partial class DocumentExtensions
    {
        /// <summary>
        /// Clean up the whole document.
        /// Optionally you can provide your own options and code cleaners. otherwise, default will be used.
        /// </summary>
        public static Document Cleanup(this Document document, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CodeCleaner.Cleanup(document, providers, cancellationToken);
        }

        /// <summary>
        /// Clean up the document marked with the provided annotation.
        /// Optionally you can provide your own options and code cleaners. otherwise, default will be used.
        /// </summary>
        public static Document Cleanup(this Document document, SyntaxAnnotation annotation, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CodeCleaner.Cleanup(document, annotation, providers, cancellationToken);
        }

        /// <summary>
        /// Clean up the provided span in the document
        /// Optionally you can provide your own options and code cleaners. otherwise, default will be used.
        /// </summary>
        public static Document Cleanup(this Document document, TextSpan textSpan, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CodeCleaner.Cleanup(document, textSpan, providers, cancellationToken);
        }

        /// <summary>
        /// Clean up the provided spans in the document
        /// Optionally you can provide your own options and code cleaners. otherwise, default will be used.
        /// </summary>
        public static Document Cleanup(this Document document, IEnumerable<TextSpan> spans, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CodeCleaner.Cleanup(document, spans, providers, cancellationToken);
        }
    }
}
