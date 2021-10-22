﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices
{
    [ExportContentTypeLanguageService(ContentTypeNames.CSharpContentType, LanguageNames.CSharp), Shared]
    internal class CSharpContentTypeLanguageService : IContentTypeLanguageService
    {
        private readonly IContentTypeRegistryService _contentTypeRegistry;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpContentTypeLanguageService(IContentTypeRegistryService contentTypeRegistry)
            => _contentTypeRegistry = contentTypeRegistry;

        public IContentType GetDefaultContentType()
            => _contentTypeRegistry.GetContentType(ContentTypeNames.CSharpContentType);
    }
}
