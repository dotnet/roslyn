// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Parameters for the roslyn/customMessageLoad request.
/// </summary>
/// <param name="AssemblyFolderPath">Full path to the assembly that contains the message handlers to load.</param>
/// <param name="AssemblyFileName">File name of the assembly that contains the message handlers to load.</param>
internal readonly record struct CustomMessageLoadParams(
    [property: JsonPropertyName("assemblyFolderPath")] string AssemblyFolderPath,
    [property: JsonPropertyName("assemblyFileName")] string AssemblyFileName);
