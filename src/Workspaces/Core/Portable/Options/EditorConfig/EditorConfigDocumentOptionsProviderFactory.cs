// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorLogger;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options.EditorConfig
{
    internal static class EditorConfigDocumentOptionsProviderFactory
    {
        public static IDocumentOptionsProvider Create(Workspace workspace)
        {
            return new EditorConfigDocumentOptionsProvider(workspace.Services.GetService<IErrorLoggerService>());
        }

        private const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        public static readonly Option<bool> UseLegacyEditorConfigSupport =
            new Option<bool>(nameof(EditorConfigDocumentOptionsProviderFactory), nameof(UseLegacyEditorConfigSupport), defaultValue: false,
                storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "UseLegacySupport"));

        public static bool ShouldUseNativeEditorConfigSupport(Workspace workspace)
        {
            return !workspace.Options.GetOption(UseLegacyEditorConfigSupport);
        }

        private class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
        {
            private readonly IErrorLoggerService? _errorLogger;

            public EditorConfigDocumentOptionsProvider(IErrorLoggerService? errorLogger)
            {
                _errorLogger = errorLogger;
            }

            public async Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                if (!ShouldUseNativeEditorConfigSupport(document.Project.Solution.Workspace))
                {
                    // Simply disable if the feature isn't on
                    return null;
                }

                var options = await document.GetAnalyzerOptionsAsync(cancellationToken).ConfigureAwait(false);

                return new DocumentOptions(options, _errorLogger);
            }

            private class DocumentOptions : IDocumentOptions
            {
                private readonly ImmutableDictionary<string, string> _options;
                private readonly IErrorLoggerService? _errorLogger;

                public DocumentOptions(ImmutableDictionary<string, string> options, IErrorLoggerService? errorLogger)
                {
                    _options = options;
                    _errorLogger = errorLogger;
                }

                public bool TryGetDocumentOption(OptionKey option, out object? value)
                {
                    var editorConfigPersistence = option.Option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault();
                    if (editorConfigPersistence == null)
                    {
                        value = null;
                        return false;
                    }

                    try
                    {
                        return editorConfigPersistence.TryGetOption(_options.AsNullable(), option.Option.Type, out value);
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
