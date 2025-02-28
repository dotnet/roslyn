// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Parameters for the <see cref="CustomMessageUnloadHandler"/> request.
/// </summary>
/// <param name="assemblyPath">Full path to the assembly that contains the message handler to unload.</param>
internal readonly struct CustomMessageUnloadParams(string assemblyPath)
{
    /// <summary>
    /// Gets the full path to the assembly that contains the message handler to unload.
    /// </summary>
    [JsonPropertyName("assemblyPath")]
    public string AssemblyPath { get; } = Requires.NotNull(assemblyPath);
}
