// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    // For source generated documents, we'll produce a URI specifically for LSP that has a scheme the client can register for; the "host" portion will
    // just be the project ID of the document, and the path will be the hint text for the document. This recognizes that VS Code shows just the local
    // path portion in the UI and thus embedding more into the path will appear and we don't want that. The rest of the stuff to serialize, namely the DocumentId, and any information
    // for the SourceGeneratedDocumentIdentity are put in as query string arguments
    //
    // For example, the URI can look like:
    //
    // roslyn-source-generated://E7D5BCFA-E345-4029-9D12-3EDCD0FB0F6B/Generated.cs?documentId=8E4C0B71-4044-4247-BDD0-04AF4C9E1677&assembly=Generator...
    //
    // where the first GUID is the project ID, the second GUID is the document ID.
    internal static class SourceGeneratedDocumentUri
    {
        public const string Scheme = "roslyn-source-generated";
        private const string GuidFormat = "D";

        public static Uri Create(SourceGeneratedDocumentIdentity identity)
        {
            // Ensure the hint path is converted to a URI-friendly format
            var hintPathParts = identity.HintName.Split('\\');
            var hintPathPortion = string.Join("/", hintPathParts.Select(Uri.EscapeDataString));

            var uri = Scheme + "://" +
                identity.DocumentId.ProjectId.Id.ToString(GuidFormat) + "/" +
                hintPathPortion +
                "?documentId=" + identity.DocumentId.Id.ToString(GuidFormat) +
                "&hintName=" + Uri.EscapeDataString(identity.HintName) +
                "&assemblyName=" + Uri.EscapeDataString(identity.Generator.AssemblyName) +
                "&assemblyVersion=" + Uri.EscapeDataString(identity.Generator.AssemblyVersion.ToString()) +
                "&typeName=" + Uri.EscapeDataString(identity.Generator.TypeName);

            // If we have a path (which is technically optional) also append it
            if (identity.Generator.AssemblyPath != null)
                uri += "&assemblyPath=" + Uri.EscapeDataString(identity.Generator.AssemblyPath);

            return ProtocolConversions.CreateAbsoluteUri(uri);
        }

        public static SourceGeneratedDocumentIdentity? DeserializeIdentity(Solution solution, Uri documentUri)
        {
            // This is a generated document, so the "host" portion is just the GUID of the project ID; we'll parse that into an ID and then
            // look up the project in the Solution. This relies on the fact that technically the only part of the ID that matters for equality
            // is the GUID; looking up the project again means we can then recover the ProjectId with the debug name, so anybody looking at a crash
            // dump sees a "normal" ID. It also means if the project is gone we can trivially say there are no usable IDs anymore.
            var projectIdGuidOnly = ProjectId.CreateFromSerialized(Guid.ParseExact(documentUri.Host, GuidFormat));
            var projectId = solution.GetProject(projectIdGuidOnly)?.Id;

            if (projectId == null)
                return null;

            Guid? documentIdGuid = null;
            string? hintName = null;
            string? assemblyName = null;
            string? assemblyPath = null; // this one is actually OK if it's null, since it's optional
            Version? assemblyVersion = null;
            string? typeName = null;

            // Parse the query string apart and grab everything from it
            foreach (var part in documentUri.Query.TrimStart('?').Split('&'))
            {
                var equals = part.IndexOf('=');
                Contract.ThrowIfTrue(equals <= 0);
#if NET
                var name = part.AsSpan()[0..equals];
#else
                var name = part.Substring(0, equals);
#endif
                var value = Uri.UnescapeDataString(part.Substring(equals + 1));

                if (name.Equals("documentId", StringComparison.Ordinal))
                    documentIdGuid = Guid.ParseExact(value, GuidFormat);
                else if (name.Equals("hintName", StringComparison.Ordinal))
                    hintName = value.ToString();
                else if (name.Equals("assemblyName", StringComparison.Ordinal))
                    assemblyName = value.ToString();
                else if (name.Equals("assemblyPath", StringComparison.Ordinal))
                    assemblyPath = value.ToString();
                else if (name.Equals("assemblyVersion", StringComparison.Ordinal))
                    assemblyVersion = Version.Parse(value);
                else if (name.Equals("typeName", StringComparison.Ordinal))
                    typeName = value.ToString();
            }

            Contract.ThrowIfNull(documentIdGuid, "Expected a URI with a documentId parameter.");
            Contract.ThrowIfNull(hintName, "Expected a URI with a hintName parameter.");
            Contract.ThrowIfNull(assemblyName, "Expected a URI with an assemblyName parameter.");
            Contract.ThrowIfNull(assemblyVersion, "Expected a URI with an assemblyVersion parameter.");
            Contract.ThrowIfNull(typeName, "Expected a URI with an typeName parameter.");

            var documentId = DocumentId.CreateFromSerialized(projectId, documentIdGuid.Value, isSourceGenerated: true, hintName);

            return new SourceGeneratedDocumentIdentity(
                documentId,
                hintName,
                new SourceGeneratorIdentity(
                    assemblyName,
                    assemblyPath,
                    assemblyVersion,
                    typeName),
                hintName);
        }
    }
}