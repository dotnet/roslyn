// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Interface to describe parameters for requests that support streaming results.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#partialResultParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <typeparam name="T">The type to be reported by <see cref="PartialResultToken"/>.</typeparam>
internal interface IPartialResultParams<T>
{
    /// <summary>
    /// An <see cref="IProgress{T}"/> instance that can be used to report partial results
    /// via the <c>$/progress</c> notification.
    /// </summary>
    // NOTE: these JSON attributes are not inherited, they are here as a reference for implementations
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<T>? PartialResultToken { get; set; }
}
