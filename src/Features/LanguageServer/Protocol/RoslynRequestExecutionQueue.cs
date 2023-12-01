// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal sealed class RoslynRequestExecutionQueue : RequestExecutionQueue<RequestContext>
    {
        private readonly ConcurrentDictionary<RequestHandlerMetadata, IMethodHandler> _handlerCache = new();
        private readonly IInitializeManager _initializeManager;
        private readonly LspWorkspaceManager _lspWorkspaceManager;

        /// <summary>
        /// Serial access is guaranteed by the queue.
        /// </summary>
        private CultureInfo? _cultureInfo;

        public RoslynRequestExecutionQueue(AbstractLanguageServer<RequestContext> languageServer, ILspLogger logger, AbstractHandlerProvider handlerProvider)
            : base(languageServer, logger, handlerProvider)
        {
            _initializeManager = languageServer.GetLspServices().GetRequiredService<IInitializeManager>();
            _lspWorkspaceManager = languageServer.GetLspServices().GetRequiredService<LspWorkspaceManager>();
        }

        public override Task WrapStartRequestTaskAsync(Task nonMutatingRequestTask, bool rethrowExceptions)
        {
            // Update the locale for this request to the desired LSP locale.
            CultureInfo.CurrentUICulture = GetCultureForRequest();
            if (rethrowExceptions)
            {
                return nonMutatingRequestTask;
            }
            else
            {
                return nonMutatingRequestTask.ReportNonFatalErrorAsync();
            }
        }

        /// <inheritdoc/>
        protected override IMethodHandler GetHandlerForRequest(IQueueItem<RequestContext> work)
        {
            var defaultHandlerMetadata = new RequestHandlerMetadata(work.MethodName, work.RequestType, work.ResponseType, LanguageServerConstants.DefaultLanguageName);
            var defaultHandler = _handlerCache.GetOrAdd(defaultHandlerMetadata, metadata => _handlerProvider.GetMethodHandler(metadata.MethodName, metadata.RequestType, metadata.ResponseType, metadata.Language));
            var identifier = RoslynRequestExecutionQueue.GetTextDocumentIdentifier(work, defaultHandler);
            if (identifier is null)
            {
                return defaultHandler;
            }

            var language = _lspWorkspaceManager.GetLanguageForUri(identifier.Uri);
            if (language is null || language == LanguageServerConstants.DefaultLanguageName)
            {
                return defaultHandler;
            }

            var handlerMetadata = new RequestHandlerMetadata(work.MethodName, work.RequestType, work.ResponseType, language);
            var handler = _handlerCache.GetOrAdd(handlerMetadata, metadata => _handlerProvider.GetMethodHandler(metadata.MethodName, metadata.RequestType, metadata.ResponseType, metadata.Language));

            return handler;
        }

        private static TextDocumentIdentifier? GetTextDocumentIdentifier(IQueueItem<RequestContext> work, IMethodHandler handler)
        {
            var textIdentifier = work.GetTextDocumentIdentifier<TextDocumentIdentifier>(handler);
            if (textIdentifier != null)
            {
                return textIdentifier;
            }

            var nullIdentifier = work.GetTextDocumentIdentifier<TextDocumentIdentifier?>(handler);
            if (nullIdentifier != null)
            {
                return nullIdentifier;
            }

            var uri = work.GetTextDocumentIdentifier<Uri?>(handler);
            if (uri != null)
            {
                return new TextDocumentIdentifier
                {
                    Uri = uri,
                };
            }

            return null;
        }

        /// <summary>
        /// Serial access is guaranteed by the queue.
        /// </summary>
        private CultureInfo GetCultureForRequest()
        {
            if (_cultureInfo != null)
            {
                return _cultureInfo;
            }

            var initializeParams = _initializeManager.TryGetInitializeParams();
            if (initializeParams == null)
            {
                // Initialize has not been called yet, no culture to set.
                // Don't update the _cultureInfo since we don't know what it should be.
                return CultureInfo.CurrentUICulture;
            }

            var locale = initializeParams.Locale;
            if (string.IsNullOrWhiteSpace(locale))
            {
                // The client did not provide a culture, use the OS configured value
                // and remember that so we can short-circuit from now on.
                _cultureInfo = CultureInfo.CurrentUICulture;
                return _cultureInfo;
            }

            try
            {
                // Parse the LSP locale into a culture and remember it for future requests.
                _cultureInfo = CultureInfo.CreateSpecificCulture(locale);
                return _cultureInfo;
            }
            catch (CultureNotFoundException)
            {
                // We couldn't parse the culture, log a warning and fallback to the OS configured value.
                // Also remember the fallback so we don't warn on every request.
                _logger.LogWarning($"Culture {locale} was not found, falling back to OS culture");
                _cultureInfo = CultureInfo.CurrentUICulture;
                return _cultureInfo;
            }
        }
    }
}
