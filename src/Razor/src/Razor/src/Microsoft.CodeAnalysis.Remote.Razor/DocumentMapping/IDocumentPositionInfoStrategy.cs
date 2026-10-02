// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal interface IDocumentPositionInfoStrategy
{
    DocumentPositionInfo GetPositionInfo(IDocumentMappingService mappingService, RazorCodeDocument codeDocument, int hostDocumentIndex);
}
