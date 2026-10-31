// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

// For source generated documents, we'll produce a URI specifically for LSP that has a scheme the client can register for; the "host" portion will
// just be the project ID of the document, and the path will be the hint text for the document. This recognizes that VS Code shows just the local
// path portion in the UI and thus embedding more into the path will appear and we don't want that. The rest of the stuff to serialize, namely the DocumentId, and any information
// for the SourceGeneratedDocumentIdentity are put in as query string arguments
//
// For example, the URI can look like:
//
// roslyn-source-generated://e7d5bcfa-e345-4029-9d12-3edcd0fb0f6b/Generated.cs?documentId%3D8e4c0b71-4044-4247-bdd0-04af4c9e1677%26assemblyName%3DGenerator...
//
// where the first GUID is the project ID, the second GUID is the document ID.
internal static class SourceGeneratedDocumentUri
{
    public const string Scheme = "roslyn-source-generated";
    private const string GuidFormat = "D";
    private const string DocumentIdParam = "documentId";
    private const string HintNameParam = "hintName";
    private const string AssemblyNameParam = "assemblyName";
    private const string AssemblyVersionParam = "assemblyVersion";
    private const string AssemblyPathParam = "assemblyPath";
    private const string TypeNameParam = "typeName";

    public static DocumentUri Create(SourceGeneratedDocumentIdentity identity)
    {
        var projectId = identity.DocumentId.ProjectId.Id.ToString(GuidFormat);
        var documentId = identity.DocumentId.Id.ToString(GuidFormat);
        var hintName = EscapeDataString(identity.HintName);
        var assemblyName = EscapeDataString(identity.Generator.AssemblyName);
        var assemblyVersion = EscapeDataString(identity.Generator.AssemblyVersion.ToString());
        var typeName = EscapeDataString(identity.Generator.TypeName);

        // Parameter escaping is separate from URI component escaping, so a client can parse and
        // serialize the URI without turning escaped characters in values into query delimiters.
        var query = $"{DocumentIdParam}={documentId}&{HintNameParam}={hintName}&{AssemblyNameParam}={assemblyName}&{AssemblyVersionParam}={assemblyVersion}&{TypeNameParam}={typeName}";

        // If we have a path (which is technically optional) also append it
        if (identity.Generator.AssemblyPath != null)
            query += $"&{AssemblyPathParam}={EscapeDataString(identity.Generator.AssemblyPath)}";

        return new DocumentUri(ParsedUri.From(Scheme, authority: projectId, path: "/" + identity.HintName, query: query));
    }

    private static string EscapeDataString(string value)
    {
#pragma warning disable RS0030 // Do not use banned APIs - EscapeDataString just escapes a string to be compatible with a URI and is safe to use.
        return Uri.EscapeDataString(value);
#pragma warning restore RS0030 // Do not use banned APIs
    }

    public static SourceGeneratedDocumentIdentity? DeserializeIdentity(Solution solution, ParsedUri documentUri)
    {
        // This is a generated document, so the "host" portion is just the GUID of the project ID; we'll parse that into an ID and then
        // look up the project in the Solution. This relies on the fact that technically the only part of the ID that matters for equality
        // is the GUID; looking up the project again means we can then recover the ProjectId with the debug name, so anybody looking at a crash
        // dump sees a "normal" ID. It also means if the project is gone we can trivially say there are no usable IDs anymore.
        var projectIdGuidOnly = ProjectId.CreateFromSerialized(Guid.ParseExact(documentUri.Authority, GuidFormat));
        var projectId = solution.GetProject(projectIdGuidOnly)?.Id;

        if (projectId == null)
            return null;

        var queryString = documentUri.Query;
        var query = System.Web.HttpUtility.ParseQueryString(queryString);
        var documentIdGuid = Guid.ParseExact(GetRequiredQueryValue(DocumentIdParam, query, queryString), GuidFormat);
        var hintName = GetRequiredQueryValue(HintNameParam, query, queryString);
        var assemblyName = GetRequiredQueryValue(AssemblyNameParam, query, queryString);
        // this one is actually OK if it's null, since it's optional
        var assemblyPath = query[AssemblyPathParam];
        var assemblyVersion = Version.Parse(GetRequiredQueryValue(AssemblyVersionParam, query, queryString));
        var typeName = GetRequiredQueryValue(TypeNameParam, query, queryString);

        var documentId = DocumentId.CreateFromSerialized(projectId, documentIdGuid, isSourceGenerated: true, hintName);

        return new SourceGeneratedDocumentIdentity(
            documentId,
            hintName,
            new SourceGeneratorIdentity(
                assemblyName,
                assemblyPath,
                assemblyVersion,
                typeName),
            hintName);

        static string GetRequiredQueryValue(string keyName, NameValueCollection nameValueCollection, string query)
        {
            var value = nameValueCollection[keyName];
            Contract.ThrowIfNull(value, $"Could not get {keyName} from {query}");
            return value;
        }
    }
}
