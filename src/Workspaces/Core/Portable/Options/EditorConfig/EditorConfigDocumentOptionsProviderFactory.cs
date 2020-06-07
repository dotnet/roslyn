﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Options.EditorConfig
{
    internal static class EditorConfigDocumentOptionsProviderFactory
    {
        public static IDocumentOptionsProvider Create()
            => new EditorConfigDocumentOptionsProvider();

        private const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        public static readonly Option2<bool> UseLegacyEditorConfigSupport =
            new Option2<bool>(nameof(EditorConfigDocumentOptionsProviderFactory), nameof(UseLegacyEditorConfigSupport), defaultValue: false,
                storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "UseLegacySupport16.7"));

        public static bool ShouldUseNativeEditorConfigSupport(Workspace workspace)
            => !workspace.Options.GetOption(UseLegacyEditorConfigSupport);

        private sealed class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
        {
            public async Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                if (!ShouldUseNativeEditorConfigSupport(document.Project.Solution.Workspace))
                {
                    // Simply disable if the feature isn't on
                    return null;
                }

                var options = await document.GetAnalyzerOptionsAsync(cancellationToken).ConfigureAwait(false);

                return new DocumentOptions(options);
            }

            private sealed class DocumentOptions : IDocumentOptions
            {
                private readonly ImmutableDictionary<string, string> _options;
                public DocumentOptions(ImmutableDictionary<string, string> options)
                    => _options = options;

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
                    catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                    {
                        value = null;
                        return false;
                    }
                }
            }
        }
    }
}
