// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class DebugDocumentsBuilder
    {
        // This is a map from the document "name" to the document.
        // Document "name" is typically a file path like "C:\Abc\Def.cs". However, that is not guaranteed.
        // For compatibility reasons the names are treated as case-sensitive in C# and case-insensitive in VB.
        // Neither language trims the names, so they are both sensitive to the leading and trailing whitespaces.
        // NOTE: We are not considering how filesystem or debuggers do the comparisons, but how native implementations did.
        // Deviating from that may result in unexpected warnings or different behavior (possibly without warnings).
        private readonly ConcurrentDictionary<string, Cci.DebugSourceDocument> _debugDocuments;
        private readonly ConcurrentCache<(string, string), string> _normalizedPathsCache;
        private readonly SourceReferenceResolver _resolverOpt;

        public DebugDocumentsBuilder(SourceReferenceResolver resolverOpt, bool isDocumentNameCaseSensitive)
        {
            _resolverOpt = resolverOpt;

            _debugDocuments = new ConcurrentDictionary<string, Cci.DebugSourceDocument>(
                    isDocumentNameCaseSensitive ?
                    StringComparer.Ordinal :
                    StringComparer.OrdinalIgnoreCase);

            _normalizedPathsCache = new ConcurrentCache<(string, string), string>(16);
        }

        internal int DebugDocumentCount => _debugDocuments.Count;

        internal void AddDebugDocument(Cci.DebugSourceDocument document)
        {
            _debugDocuments.Add(document.Location, document);
        }

        internal IReadOnlyDictionary<string, Cci.DebugSourceDocument> DebugDocuments
            => _debugDocuments;

        internal Cci.DebugSourceDocument TryGetDebugDocument(string path, string basePath)
        {
            return TryGetDebugDocumentForNormalizedPath(NormalizeDebugDocumentPath(path, basePath));
        }

        internal Cci.DebugSourceDocument TryGetDebugDocumentForNormalizedPath(string normalizedPath)
        {
            Cci.DebugSourceDocument document;
            _debugDocuments.TryGetValue(normalizedPath, out document);
            return document;
        }

        internal Cci.DebugSourceDocument GetOrAddDebugDocument(string path, string basePath, Func<string, Cci.DebugSourceDocument> factory)
        {
            return _debugDocuments.GetOrAdd(NormalizeDebugDocumentPath(path, basePath), factory);
        }

        internal string NormalizeDebugDocumentPath(string path, string basePath)
        {
            if (_resolverOpt == null)
            {
                return path;
            }

            var key = (path, basePath);
            string normalizedPath;
            if (!_normalizedPathsCache.TryGetValue(key, out normalizedPath))
            {
                normalizedPath = _resolverOpt.NormalizePath(path, basePath) ?? path;
                _normalizedPathsCache.TryAdd(key, normalizedPath);
            }

            return normalizedPath;
        }
    }
}
