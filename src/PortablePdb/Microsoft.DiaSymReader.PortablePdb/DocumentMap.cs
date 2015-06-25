// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal sealed class DocumentMap
    {
        private readonly MetadataReader _reader;

        // { last part of document name -> one or many document handles that have the part in commmon }
        private readonly IReadOnlyDictionary<string, KeyValuePair<DocumentHandle, ImmutableArray<DocumentHandle>>> _map;

        public DocumentMap(MetadataReader reader)
        {
            _reader = reader;
            _map = GetDocumentsByFileName(reader).GroupBy(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<KeyValuePair<string, DocumentHandle>> GetDocumentsByFileName(MetadataReader reader)
        {
            foreach (var documentHandle in reader.Documents)
            {
                string fileName = GetFileName(reader, documentHandle);

                // invalid metadata: document doesn't have a name
                if (fileName == null)
                {
                    continue;
                }

                yield return new KeyValuePair<string, DocumentHandle>(fileName, documentHandle);
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

            KeyValuePair<DocumentHandle, ImmutableArray<DocumentHandle>> documents; 
            if (!_map.TryGetValue(fileName, out documents))
            {
                documentHandle = default(DocumentHandle);
                return false;
            }

            // SymReader first attempts to find the document by the full path, then by file name with extension.

            if (!documents.Key.IsNil)
            {
                // There is only one document with the specified file name.
                // SymReader returns the document regardless of whether the path matches the name.
                documentHandle = documents.Key;
                return true;
            }

            Debug.Assert(documents.Value.Length > 1);

            // We have multiple candidates with the same file name. Find the one whose name matches the specified full path.
            // If none does return the first one. It will be the one with the smallest handle, due to the multi-map construction implementation.

            foreach (DocumentHandle candidateHandle in documents.Value)
            {
                if (_reader.StringComparer.Equals(_reader.GetDocument(candidateHandle).Name, fullPath, ignoreCase: true))
                {
                    documentHandle = candidateHandle;
                    return true;
                }
            }

            documentHandle = documents.Value[0];
            return true;
        }
    }
}
