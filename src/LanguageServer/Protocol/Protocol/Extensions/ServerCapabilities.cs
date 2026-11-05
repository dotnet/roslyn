// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

internal partial class WorkspaceClientCapabilities
{
    [JsonPropertyName("_vs_projectContext")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiagnosticWorkspaceClientCapabilities? ProjectContext { get; set; }
}

internal partial class VSProjectContextList
{
    [JsonPropertyName("_vs_key")]
    public string Key { get; set; } = null!;
}

internal readonly partial record struct VSInternalDiagnosticKind
{
    public static readonly VSInternalDiagnosticKind EditAndContinue = new("enc");
}

internal static partial class SemanticTokenTypes
{
    public static readonly System.Collections.Generic.IReadOnlyList<string> AllTypes =
    [
        Namespace, Type, Class, Enum, Interface, Struct, TypeParameter, Parameter,
        Variable, Property, EnumMember, Event, Function, Method, Macro, Keyword,
        Modifier, Comment, String, Number, Regexp, Operator, Decorator,
    ];
}
