// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal interface IResolveDataService
{
    object ToResolveData(XamlRequestContext context, object data, LSP.TextDocumentIdentifier document);
    (object? data, LSP.TextDocumentIdentifier? document) FromResolveData(XamlRequestContext context, object? resolveData);
}
