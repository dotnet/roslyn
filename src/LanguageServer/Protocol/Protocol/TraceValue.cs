// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A TraceValue represents the level of verbosity with which the server systematically reports its
/// execution trace using <see cref="Methods.LogTraceName"/>. The initial trace value is set by the
/// client at initialization and can be modified later using the <see cref="Methods.SetTraceName"/> notification.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#traceValue">Language Server Protocol specification</see> for additional information.
/// </summary>
[JsonConverter(typeof(StringEnumConverter<TraceValue>))]
[TypeConverter(typeof(StringEnumConverter<TraceValue>.TypeConverter))]
internal readonly record struct TraceValue(string Value) : IStringEnum
{
    public static readonly TraceValue Off = new("off");
    public static readonly TraceValue Messages = new("messages");
    public static readonly TraceValue Verbose = new("verbose");
}
