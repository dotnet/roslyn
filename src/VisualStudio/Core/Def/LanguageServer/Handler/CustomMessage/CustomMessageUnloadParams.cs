// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Parameters for the <see cref="CustomMessageUnloadHandler"/> request.
/// </summary>
/// <param name="AssemblyFolderPath">Full path to the folder that contains the message handler assemblies to unload.</param>
internal readonly record struct CustomMessageUnloadParams(
    [property: JsonPropertyName("assemblyFolderPath")] string AssemblyFolderPath);
