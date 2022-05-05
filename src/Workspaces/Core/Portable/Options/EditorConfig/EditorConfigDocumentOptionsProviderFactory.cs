// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options.EditorConfig
{
    internal static class EditorConfigDocumentOptionsProviderFactory
    {
        public static IDocumentOptionsProvider Create()
            => new EditorConfigDocumentOptionsProvider();

        private sealed class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
        {
            public async Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                var data = await document.GetAnalyzerOptionsAsync(cancellationToken).ConfigureAwait(false);
                return new DocumentOptions(data?.AnalyzerConfigOptions);
            }

            private sealed class DocumentOptions : IDocumentOptions
            {
                private readonly StructuredAnalyzerConfigOptions? _options;

                public DocumentOptions(StructuredAnalyzerConfigOptions? options)
                    => _options = options;

                public bool TryGetDocumentOption(OptionKey option, out object? value)
                {
                    if (_options == null)
                    {
                        value = null;
                        return false;
                    }

                    var editorConfigPersistence = (IEditorConfigStorageLocation?)option.Option.StorageLocations.SingleOrDefault(static location => location is IEditorConfigStorageLocation);
                    if (editorConfigPersistence == null)
                    {
                        value = null;
                        return false;
                    }

                    try
                    {
                        return editorConfigPersistence.TryGetOption(_options, option.Option.Type, out value);
                    }
                    catch (Exception e) when (FatalError.ReportAndCatch(e))
                    {
                        value = null;
                        return false;
                    }
                }
            }
        }
    }
}
