// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal static class SolutionExtensions
{
    public static ImmutableArray<TextDocument> GetTextDocuments(this Solution solution, Uri documentUri)
        => LanguageServer.Extensions.GetTextDocuments(solution, new(documentUri));

    public static ImmutableArray<DocumentId> GetDocumentIds(this Solution solution, Uri documentUri)
        => LanguageServer.Extensions.GetDocumentIds(solution, new(documentUri));

    public static int GetWorkspaceVersion(this Solution solution)
        => solution.SolutionStateContentVersion;
}
