// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal sealed class DocumentMap
    {
        private struct DocumentNameAndHandle
        {
            public readonly DocumentHandle Handle;
            public readonly string FileName;

            public DocumentNameAndHandle(DocumentHandle handle, string fileName)
            {
                Handle = handle;
                FileName = fileName;
            }
        }

        private readonly MetadataReader _reader;

        // { last part of document name -> one or many document handles that have the part in common }
        private readonly IReadOnlyDictionary<string, KeyValuePair<DocumentNameAndHandle, ImmutableArray<DocumentNameAndHandle>>> _map;

        public DocumentMap(MetadataReader reader)
        {
            _reader = reader;

            // group ignoring case, we will match the case within the group
            _map = GetDocumentsByFileName(reader).GroupBy(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<KeyValuePair<string, DocumentNameAndHandle>> GetDocumentsByFileName(MetadataReader reader)
        {
            foreach (var documentHandle in reader.Documents)
            {
                string fileName = GetFileName(reader, documentHandle);

                // invalid metadata: document doesn't have a name
                if (fileName == null)
                {
                    continue;
                }

                yield return new KeyValuePair<string, DocumentNameAndHandle>(fileName, new DocumentNameAndHandle(documentHandle, fileName));
            }
        }

        private static string GetFileName(MetadataReader reader, DocumentHandle documentHandle)
        {
            var document = reader.GetDocument(documentHandle);

            if (document.Name.IsNil)
            {
                return null;
            }

            var nameReader = reader.GetBlobReader(document.Name);

            int separator = nameReader.ReadByte();
            if (!FileNameUtilities.IsDirectorySeparator((char)separator))
            {
                return FileNameUtilities.GetFileName(reader.GetString(document.Name));
            }

            // find the last part handle:
            BlobHandle partHandle = default(BlobHandle);
            while (nameReader.RemainingBytes > 0)
            {
                partHandle = nameReader.ReadBlobHandle();
            }

            if (partHandle.IsNil)
            {
                return string.Empty;
            }

            var partReader = reader.GetBlobReader(partHandle);
            var part = partReader.ReadUTF8(partReader.Length);
            if (part.IndexOf('\0') >= 0)
            {
                // bad metadata
                return null;
            }

            // it is valid to encode document name so that the parts contain directory separators:
            return FileNameUtilities.GetFileName(part);
        }

        internal bool TryGetDocument(string fullPath, out DocumentHandle documentHandle)
        {
            var fileName = FileNameUtilities.GetFileName(fullPath);

            KeyValuePair<DocumentNameAndHandle, ImmutableArray<DocumentNameAndHandle>> documents; 
            if (!_map.TryGetValue(fileName, out documents))
            {
                documentHandle = default(DocumentHandle);
                return false;
            }

            // SymReader first attempts to find the document by the full path, then by file name with extension.

            if (documents.Key.FileName != null)
            {
                // There is only one document with the specified file name.
                // SymReader returns the document regardless of whether the path matches the name.
                documentHandle = documents.Key.Handle;
                return true;
            }

            Debug.Assert(documents.Value.Length > 1);

            // We have multiple candidates with the same file name. Find the one whose name matches the specified full path.
            // If none does return the first one. It will be the one with the smallest handle, due to the multi-map construction implementation.

            // First try to find candidate whose full name is exactly matching.
            foreach (DocumentNameAndHandle candidate in documents.Value)
            {
                if (_reader.StringComparer.Equals(_reader.GetDocument(candidate.Handle).Name, fullPath, ignoreCase: false))
                {
                    documentHandle = candidate.Handle;
                    return true;
                }
            }

            // Then try to find candidate whose full name is matching ignoring case.
            foreach (DocumentNameAndHandle candidate in documents.Value)
            {
                if (_reader.StringComparer.Equals(_reader.GetDocument(candidate.Handle).Name, fullPath, ignoreCase: true))
                {
                    documentHandle = candidate.Handle;
                    return true;
                }
            }

            // Then try to find candidate whose file name is matching exactly.
            foreach (DocumentNameAndHandle candidate in documents.Value)
            {
                if (candidate.FileName == fileName)
                {
                    documentHandle = candidate.Handle;
                    return true;
                }
            }

            documentHandle = documents.Value[0].Handle;
            return true;
        }
    }
}