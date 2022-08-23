// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An interface for handlers of methods which do not return a response and receive no parameters.
/// </summary>
/// <typeparam name="RequestContextType">The type of the RequestContext to be used by this handler.</typeparam>
public interface INotificationHandler<RequestContextType> : IMethodHandler
{
    Task HandleNotificationAsync(RequestContextType requestContext, CancellationToken cancellationToken);
}

/// <summary>
/// An interface for handlers of methods which do not return a response 
/// </summary>
/// <typeparam name="RequestType">The type of the Request parameter to be received.</typeparam>
/// <typeparam name="RequestContextType">The type of the RequestContext to be used by this handler.</typeparam>
public interface INotificationHandler<RequestType, RequestContextType> : IMethodHandler
{
    Task HandleNotificationAsync(RequestType request, RequestContextType requestContext, CancellationToken cancellationToken);
}

public interface ITextDocumentIdentifierHandler<RequestType>
{
    /// <summary>
    /// Gets the identifier of the document from the request, if the request provides one.
    /// </summary>
    /// <remarks>Despite a return type of <see cref="object"/>, you are advised to severly restrict variety of possible return values.
    /// It is left open here to allow for flexibility and variability in finding the TextDocumentIdentifier.
    /// For example, some Param types only have a URI instead of a "TextDocumentIdenfier" object, and others have custom TDI's, or choose to parse JSON.</remarks>
    object? GetTextDocumentIdentifier(RequestType request);
}
