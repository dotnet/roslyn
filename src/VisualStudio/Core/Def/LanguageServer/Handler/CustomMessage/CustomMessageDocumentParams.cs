// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Parameters for the <see cref="CustomMessageDocumentHandler"/> request.
/// </summary>
/// <param name="AssemblyFolderPath">Full path to the folder that contains <paramref name="AssemblyFileName"/>.</param>
/// <param name="AssemblyFileName">File name of the assembly that contains the message handler.</param>
/// <param name="TypeFullName">Full name of the <see cref="Type"/> of the message handler.</param>
/// <param name="Message">Json message to be passed to a custom message handler.</param>
/// <param name="TextDocument">Text document the <paramref name="Message"/> refers to.</param>
internal readonly record struct CustomMessageDocumentParams(
    [property: JsonPropertyName("assemblyFolderPath")] string AssemblyFolderPath,
    [property: JsonPropertyName("assemblyFileName")] string AssemblyFileName,
    [property: JsonPropertyName("typeFullName")] string TypeFullName,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument);
