// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices;

[ExportContentTypeLanguageService(ContentTypeNames.CSharpContentType, LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpContentTypeLanguageService(IContentTypeRegistryService contentTypeRegistry) : IContentTypeLanguageService
{
    private readonly IContentTypeRegistryService _contentTypeRegistry = contentTypeRegistry;

    public IContentType GetDefaultContentType()
        => _contentTypeRegistry.GetContentType(ContentTypeNames.CSharpContentType);
}
