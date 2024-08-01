// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Immutable;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Manages handler discovery and distribution.
/// </summary>
internal abstract class AbstractHandlerProvider
{
    /// <summary>
    /// Gets the <see cref="RequestHandlerMetadata"/>s for all registered methods.
    /// </summary>
    public abstract ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods();

    /// <summary>
    /// Gets the <see cref="IMethodHandler"/>s for a particular request.
    /// </summary>
    /// <param name="method">The method name for the request.</param>
    /// <param name="requestTypeRef">A <see cref="TypeRef"/> representing the request type for this method.</param>
    /// <param name="responseTypeRef">A <see cref="TypeRef"/> representing the response type for this method.</param>
    /// <param name="language">The language for the request.</param>
    /// <returns>The handler for the request.</returns>
    /// <remarks>
    /// If the handler for the given language is not found, an exception is thrown.
    /// Callers are expected to only request handlers for methods and languages that exist.
    /// </remarks>
    public abstract IMethodHandler GetMethodHandler(string method, TypeRef? requestTypeRef, TypeRef? responseTypeRef, string language);
}
