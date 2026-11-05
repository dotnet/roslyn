// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

[Union, JsonConverter(typeof(UnionConverter))]
internal sealed class RazorDefinitionResult : IUnion
{
    public RazorDefinitionResult(Location value) => Value = value;
    public RazorDefinitionResult(Location[] value) => Value = value;
    public RazorDefinitionResult(DocumentLink[] value) => Value = value;
    public object Value { get; }
}
