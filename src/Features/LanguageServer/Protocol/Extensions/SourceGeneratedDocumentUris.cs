// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    // For source generated documents, we'll produce a URI specifically for LSP that has a scheme the client can register for; the "host" portion will
    // just be the project ID of the document, then the path will be the GUID that is the document ID, with the rest of it being the generated file path
    // just so any display of the end of the URI (like a file tab) works well. For example, the URI can look like:
    //
    // roslyn-source-generated://E7D5BCFA-E345-4029-9D12-3EDCD0FB0F6B/9BBBAC41-CA37-4DB6-97D0-2997F7434680/Generated.cs
    //
    // where the first GUID is the project ID, the second GUID is the document ID.
    internal static class SourceGeneratedDocumentUris
    {
        public const string Scheme = "roslyn-source-generated";
        private const string GuidFormat = "D";

        public static Uri Create(SourceGeneratedDocumentIdentity identity)
        {
            // Ensure the hint path is converted to a URI-friendly format
            var hintPathParts = identity.HintName.Split('\\');
            var hintPathPortion = string.Join("/", hintPathParts.Select(Uri.EscapeDataString));

            return ProtocolConversions.CreateAbsoluteUri(
                Scheme + "://" +
                identity.DocumentId.ProjectId.Id.ToString(GuidFormat) + "/" +
                identity.DocumentId.Id.ToString(GuidFormat) + "/" +
                hintPathPortion);
        }

        public static DocumentId? DeserializeDocumentId(Solution solution, Uri documentUri)
        {
            // This is a generated document, so the "host" portion is just the GUID of the project ID; we'll parse that into an ID and then
            // look up the project in the Solution. This relies on the fact that technically the only part of the ID that matters for equality
            // is the GUID; looking up the project again means we can then recover the ProjectId with the debug name, so anybody looking at a crash
            // dump sees a "normal" ID. It also means if the project is gone we can trivially say there are no usable IDs anymore.
            var projectIdGuidOnly = ProjectId.CreateFromSerialized(Guid.ParseExact(documentUri.Host, GuidFormat));
            var projectId = solution.GetProject(projectIdGuidOnly)?.Id;

            if (projectId == null)
                return null;

            // The AbsolutePath will consist of a leading / to ignore, then the GUID that is the DocumentId, and then another slash, then the hint path
            var slashAfterId = documentUri.AbsolutePath.IndexOf('/', startIndex: 1);
            Contract.ThrowIfFalse(slashAfterId > 0, $"The URI '{documentUri}' is not formatted correctly.");

            var documentIdGuidSpan = documentUri.AbsolutePath.AsSpan()[1..slashAfterId];
            var documentIdGuid =
#if NET // netstandard2.0 doesn't have Parse methods that take Spans
                Guid.ParseExact(documentIdGuidSpan, GuidFormat);
#else
                Guid.ParseExact(documentIdGuidSpan.ToString(), GuidFormat);
#endif

            return DocumentId.CreateFromSerialized(projectId, documentIdGuid, isSourceGenerated: true, debugName: documentUri.AbsolutePath.Substring(slashAfterId + 1));
        }
    }
}
