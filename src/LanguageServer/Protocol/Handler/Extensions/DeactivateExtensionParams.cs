// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

/// <summary>
/// Parameters for the server/_vs_deactivateExtension request.
/// </summary>
/// <param name="AssemblyFilePath">Full path to the assembly that contains the message handlers to unregister.</param>
internal readonly record struct DeactivateExtensionParams(
    [property: JsonPropertyName("assemblyFilePath")] string AssemblyFilePath);
