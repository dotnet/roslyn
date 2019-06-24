// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Experiments;

namespace Microsoft.CodeAnalysis.Options.EditorConfig
{
    [Export(typeof(IDocumentOptionsProviderFactory)), Shared]
    internal sealed class EditorConfigDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
    {
        [ImportingConstructor]
        public EditorConfigDocumentOptionsProviderFactory()
        {
        }

        public IDocumentOptionsProvider TryCreate(Workspace workspace)
        {
            if (!ShouldUseNativeEditorConfigSupport(workspace))
            {
                // Simply disable if the feature isn't on
                return null;
            }

            return new EditorConfigDocumentOptionsProvider(workspace.Services.GetService<IErrorLoggerService>());
        }

        public static bool ShouldUseNativeEditorConfigSupport(Workspace workspace)
        {
            var experimentationService = workspace.Services.GetService<IExperimentationService>();
            return experimentationService.IsExperimentEnabled(WellKnownExperimentNames.NativeEditorConfigSupport);
        }

        private class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
        {
            private readonly IErrorLoggerService _errorLogger;

            public EditorConfigDocumentOptionsProvider(IErrorLoggerService errorLogger)
            {
                _errorLogger = errorLogger;
            }

            public async Task<IDocumentOptions> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                var options = await document.GetAnalyzerOptionsAsync(cancellationToken).ConfigureAwait(false);

                return new DocumentOptions(options, _errorLogger);
            }

            private class DocumentOptions : IDocumentOptions
            {
                private readonly ImmutableDictionary<string, string> _options;
                private readonly IErrorLoggerService _errorLogger;

                public DocumentOptions(ImmutableDictionary<string, string> options, IErrorLoggerService errorLogger)
                {
                    _options = options;
                    _errorLogger = errorLogger;
                }

                public bool TryGetDocumentOption(OptionKey option, out object value)
                {
                    var editorConfigPersistence = option.Option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault();
                    if (editorConfigPersistence == null)
                    {
                        value = null;
                        return false;
                    }

                    try
                    {
                        return editorConfigPersistence.TryGetOption(_options, option.Option.Type, out value);
                    }
                    catch (Exception ex)
                    {
                        _errorLogger?.LogException(this, ex);
                        value = null;
                        return false;
                    }
                }
            }
        }
    }
}
