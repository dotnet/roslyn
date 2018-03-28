// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices
{
    [ExportContentTypeLanguageService(ContentTypeNames.CSharpContentType, LanguageNames.CSharp), Shared]
    internal class CSharpContentTypeLanguageService : IContentTypeLanguageService
    {
        private readonly IContentTypeRegistryService _contentTypeRegistry;

        [ImportingConstructor]
        public CSharpContentTypeLanguageService(IContentTypeRegistryService contentTypeRegistry)
        {
            _contentTypeRegistry = contentTypeRegistry;
        }

        public IContentType GetDefaultContentType()
        {
            return _contentTypeRegistry.GetContentType(ContentTypeNames.CSharpContentType);
        }
    }
}
