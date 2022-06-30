// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract class AbstractNewDocumentFormattingService : INewDocumentFormattingService
    {
        private readonly IEnumerable<Lazy<INewDocumentFormattingProvider, LanguageMetadata>> _providers;
        private IEnumerable<INewDocumentFormattingProvider>? _providerValues;

        protected abstract string Language { get; }

        protected AbstractNewDocumentFormattingService(IEnumerable<Lazy<INewDocumentFormattingProvider, LanguageMetadata>> providers)
        {
            _providers = providers;
        }

        private IEnumerable<INewDocumentFormattingProvider> GetProviders()
        {
            _providerValues ??= _providers.Where(p => p.Metadata.Language == Language).Select(p => p.Value);
            return _providerValues;
        }

        public async Task<Document> FormatNewDocumentAsync(Document document, Document? hintDocument, CodeCleanupOptions options, CancellationToken cancellationToken)
        {
            foreach (var provider in GetProviders())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // If a single formatter has a bug, we still want to keep trying the others.
                // Since they are unordered it would be inappropriate for them to depend on each
                // other, so this shouldn't cause problems.
                try
                {
                    // First we ask the provider to "format" the document. This could be formatting in terms
                    // of adjusting block scopes to file scopes etc., but it could also be more akin to fixers
                    // like adding access modifiers, or adding .ConfigureAwait() calls etc.
                    document = await provider.FormatNewDocumentAsync(document, hintDocument, options, cancellationToken).ConfigureAwait(false);

                    // Now that the above has changed the document, we use the code action engine to clean up the document
                    // before we call the next provider, otherwise they might not see things as they are meant to be.
                    // Because formatting providers often re-use code fix logic, they are often written assuming this will
                    // happen.
                    document = await CodeAction.CleanupDocumentAsync(document, options, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken, ErrorSeverity.General))
                {
                }
            }

            return document;
        }
    }
}
