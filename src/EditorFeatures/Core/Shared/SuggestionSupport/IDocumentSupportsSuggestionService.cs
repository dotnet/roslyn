// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Shared.SuggestionSupport
{
    internal interface IDocumentSupportsSuggestionService : IWorkspaceService
    {
        bool SupportsCodeFixes(Document document);
        bool SupportsRefactorings(Document document);
        bool SupportsRename(Document document);
        bool SupportsNavigationToAnyPosition(Document document);
    }
}
