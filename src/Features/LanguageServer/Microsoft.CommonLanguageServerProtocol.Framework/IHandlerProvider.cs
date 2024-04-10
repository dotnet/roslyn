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
#if BINARY_COMPAT // TODO - Remove with https://github.com/dotnet/roslyn/issues/72251
public interface IHandlerProvider
#else
internal interface IHandlerProvider
#endif
{
    ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods();

    IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType);
}
