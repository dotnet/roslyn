// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentPresentation;

internal interface IPresentationParams
{
    TextDocumentIdentifier TextDocument { get; set; }
    LspRange Range { get; set; }
}

internal interface IRazorPresentationParams : IPresentationParams
{
    int HostDocumentVersion { get; set; }
    RazorLanguageKind Kind { get; set; }
}
