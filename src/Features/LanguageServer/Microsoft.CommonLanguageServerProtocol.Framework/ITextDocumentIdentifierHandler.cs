// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CommonLanguageServerProtocol.Framework;

public interface ITextDocumentIdentifierHandler<TRequest, TTextDocumentIdentifier> : ITextDocumentIdentifierHandler
{
    /// <summary>
    /// Gets the identifier of the document from the request, if the request provides one.
    /// </summary>
    TTextDocumentIdentifier GetTextDocumentIdentifier(TRequest request);
}

public interface ITextDocumentIdentifierHandler
{
}
