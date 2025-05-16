// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

/// <summary>
/// Parameters for the server/_vs_activateExtension request.
/// </summary>
/// <param name="AssemblyFilePath">Full path to the assembly that contains the message handlers to register.</param>
internal readonly record struct ActivateExtensionParams(
    [property: JsonPropertyName("assemblyFilePath")] string AssemblyFilePath);
