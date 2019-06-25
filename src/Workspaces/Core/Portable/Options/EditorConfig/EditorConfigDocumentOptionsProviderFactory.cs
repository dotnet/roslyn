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
            return new EditorConfigDocumentOptionsProvider(
                workspace.Services.GetService<IErrorLoggerService>(),
                workspace.Services.GetRequiredService<IExperimentationServiceFactory>());
        }

        public static bool ShouldUseNativeEditorConfigSupport(IExperimentationService experimentationService)
        {
            return experimentationService.IsExperimentEnabled(WellKnownExperimentNames.NativeEditorConfigSupport);
        }

        private class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
        {
            private readonly IErrorLoggerService _errorLogger;
            private readonly IExperimentationServiceFactory _experimentationServiceFactory;
            private bool? _enabled;

            public EditorConfigDocumentOptionsProvider(IErrorLoggerService errorLogger, IExperimentationServiceFactory experimentationServiceFactory)
            {
                _errorLogger = errorLogger;
                _experimentationServiceFactory = experimentationServiceFactory;
            }

            public async Task<IDocumentOptions> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                if (_enabled is null)
                {
                    var experimentationService = await _experimentationServiceFactory.GetExperimentationServiceAsync(cancellationToken);
                    _enabled = ShouldUseNativeEditorConfigSupport(experimentationService);
                }

                if (_enabled != true)
                {
                    return null;
                }

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
