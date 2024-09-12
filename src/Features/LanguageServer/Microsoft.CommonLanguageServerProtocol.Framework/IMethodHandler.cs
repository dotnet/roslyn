// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Top level type for LSP request handler.
/// </summary>
#if BINARY_COMPAT // TODO - Remove with https://github.com/dotnet/roslyn/issues/72251
public interface IMethodHandler
#else
internal interface IMethodHandler
#endif
{
    /// <summary>
    /// Whether or not the solution state on the server is modified as a part of handling this request.
    /// This may affect queuing behavior (IE mutating requests are run in serial rather than parallel) depending on the <see cref="IRequestExecutionQueue{TRequestContext}"/> implementation.
    /// </summary>
    bool MutatesSolutionState { get; }
}
