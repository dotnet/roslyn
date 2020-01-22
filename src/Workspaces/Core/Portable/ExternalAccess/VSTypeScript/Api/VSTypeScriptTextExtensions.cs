// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptTextExtensions
    {
        public static IEnumerable<Document> GetRelatedDocuments(this SourceTextContainer container)
            => TextExtensions.GetRelatedDocuments(container);

        public static Document? GetOpenDocumentInCurrentContextWithChanges(this SourceText text)
            => TextExtensions.GetOpenDocumentInCurrentContextWithChanges(text);
    }
}
