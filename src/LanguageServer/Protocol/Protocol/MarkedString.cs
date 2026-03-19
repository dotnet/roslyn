// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// MarkedString can be used to render human readable text. It is either a
/// markdown string or a code-block that provides a language and a code snippet.
/// <para>
/// The language identifier is semantically equal to the optional language
/// identifier in fenced code blocks in GitHub issues.
/// </para>
/// <para>The pair of a language and a value is an equivalent to markdown:
/// <code>
/// ```${language}
/// ${value}
/// ```
/// </code>
/// </para>
/// <para>Note that markdown strings will be sanitized - that means html will be escaped.
/// </para>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#markedString">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class MarkedString
{
    // Code has to reference MarkedString in a SumType even if it's not using the class itself.
    // This means that if we deprecate the type itself, referencing code would have to suppress
    // deprecation warnings even if they are only using non-deprecated types. We work around
    // by deprecating the members instead of the type itself.
    const string DeprecationMessage = "The MarkedString class is deprecated. Use MarkupContent instead.";

    /// <summary>
    /// Gets or sets the language of the code stored in <see cref="Value" />.
    /// </summary>
    [JsonPropertyName("language")]
    [JsonRequired]
    [Obsolete(DeprecationMessage)]
    public string Language
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonRequired]
    [Obsolete(DeprecationMessage)]
    public string Value
    {
        get;
        set;
    }
}
