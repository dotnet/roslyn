// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal interface ITextDocumentIdentifierHandler<TRequest, TTextDocumentIdentifier> : ITextDocumentIdentifierHandler
{
    /// <summary>
    /// Gets the identifier of the document from the request, if the request provides one.
    /// </summary>
    TTextDocumentIdentifier GetTextDocumentIdentifier(TRequest request);
}

internal interface ITextDocumentIdentifierHandler
{
}
