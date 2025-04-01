// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// Value representing the language server trace setting.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#traceValue">Language Server Protocol specification</see> for additional information.
/// </summary>
[JsonConverter(typeof(StringEnumConverter<TraceSetting>))]
[TypeConverter(typeof(StringEnumConverter<TraceSetting>.TypeConverter))]
internal readonly record struct TraceSetting(string Value) : IStringEnum
{
    /// <summary>
    /// Setting for 'off'.
    /// </summary>
    public static readonly TraceSetting Off = new("off");

    /// <summary>
    /// Setting for 'messages'.
    /// </summary>
    public static readonly TraceSetting Messages = new("messages");

    /// <summary>
    /// Setting for 'verbose'.
    /// </summary>
    public static readonly TraceSetting Verbose = new("verbose");
}
