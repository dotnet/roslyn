// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Top level type for LSP request handler.
/// </summary>
public interface IMethodHandler
{
    /// <summary>
    /// Whether or not the solution state on the server is modified
    /// as a part of handling this request.
    /// </summary>
    bool MutatesSolutionState { get; }
}
