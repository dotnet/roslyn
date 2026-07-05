// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal abstract class ComponentIntermediateNodePassBase : IntermediateNodePassBase
{
    protected static bool IsComponentDocument(DocumentIntermediateNode document)
        => document.DocumentKind == ComponentDocumentClassifierPass.ComponentDocumentKind;
}
