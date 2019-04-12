// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [ExportContentTypeLanguageService(FSharp.Editor.ContentTypeNames.FSharpContentType, LanguageNames.FSharp), Shared]
    internal class FSharpContentTypeLanguageService : IContentTypeLanguageService
    {
        private readonly IContentTypeRegistryService _contentTypeRegistry;

        [ImportingConstructor]
        public FSharpContentTypeLanguageService(IContentTypeRegistryService contentTypeRegistry)
        {
            _contentTypeRegistry = contentTypeRegistry;
        }

        public IContentType GetDefaultContentType()
        {
            return _contentTypeRegistry.GetContentType(FSharp.Editor.ContentTypeNames.FSharpContentType);
        }
    }
}
