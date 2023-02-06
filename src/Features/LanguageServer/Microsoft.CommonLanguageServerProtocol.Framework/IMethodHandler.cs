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
    /// Specifies allowed concurrency when processing this method in the queue.
    /// </summary>
    RequestConcurrency Concurrency { get; }
}
