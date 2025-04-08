// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Strongly typed object used to specify a LSP requests's parameter and return types.
/// </summary>
/// <typeparam name="TIn">The parameter type.</typeparam>
/// <typeparam name="TOut">The return type.</typeparam>
internal sealed class LspRequest<TIn, TOut>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LspRequest{TIn, TOut}"/> class.
    /// </summary>
    /// <param name="name">The name of the JSON-RPC request.</param>
    public LspRequest(string name)
    {
        this.Name = name;
    }

    /// <summary>
    /// Gets the name of the JSON-RPC request.
    /// </summary>
    public string Name { get; }
}
