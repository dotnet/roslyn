// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

internal class CustomMessageParams(string assemblyPath, string typeFullName, CustomMessage message)
{
    [JsonPropertyName("assemblyPath")]
    public string AssemblyPath { get; } = Requires.NotNull(assemblyPath);

    [JsonPropertyName("typeFullName")]
    public string TypeFullName { get; } = Requires.NotNull(typeFullName);

    [JsonPropertyName("message")]
    public CustomMessage Message { get; } = Requires.NotNull(message);
}
