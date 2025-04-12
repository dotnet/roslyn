// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Linq;
using System.Text.Json.Serialization;
using Roslyn.Utilities;

/// <summary>
/// Class which represents a source code diagnostic message.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#diagnostic">Language Server Protocol specification</see> for additional information.
/// </summary>
internal class Diagnostic : IEquatable<Diagnostic>
{
    /// <summary>
    /// Gets or sets the source code range.
    /// </summary>
    [JsonPropertyName("range")]
    public Range Range
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the diagnostic severity.
    /// </summary>
    [JsonPropertyName("severity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiagnosticSeverity? Severity
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the diagnostic's code, which usually appear in the user interface.
    /// </summary>
    /// <remarks>
    /// The value can be an <see cref="int"/>, <see cref="string"/>.
    /// </remarks>
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SumType<int, string>? Code
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets an optional value that describes the error code.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("codeDescription")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodeDescription? CodeDescription
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a human-readable string describing the source of this
    /// diagnostic, e.g. 'typescript' or 'super lint'. It usually appears in the user interface.
    /// </summary>
    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the diagnostic's message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message
    {
        get;
        set;
    }

    /// <summary>
    /// Additional metadata about the diagnostic.
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiagnosticTag[]? Tags
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the diagnostic related information
    /// </summary>
    [JsonPropertyName("relatedInformation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiagnosticRelatedInformation[]? RelatedInformation
    {
        get;
        set;
    }

    /// <summary>
    /// Data that is preserved for a <c>textDocument/codeAction</c> request
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }

    public static bool operator ==(Diagnostic? value1, Diagnostic? value2)
    {
        if (ReferenceEquals(value1, value2))
        {
            return true;
        }

        // Is null?
        if (ReferenceEquals(null, value2))
        {
            return false;
        }

        return value1?.Equals(value2) ?? false;
    }

    public static bool operator !=(Diagnostic? value1, Diagnostic? value2)
    {
        return !(value1 == value2);
    }

    /// <inheritdoc/>
    public bool Equals(Diagnostic other)
    {
        return other is not null
            && this.Range == other.Range
            && this.Severity == other.Severity
            && object.Equals(this.Code, other.Code)
            && this.CodeDescription == other.CodeDescription
            && string.Equals(this.Source, other.Source, StringComparison.Ordinal)
            && string.Equals(this.Message, other.Message, StringComparison.Ordinal)
            && (this.Tags == null
                    ? other.Tags == null
                    : this.Tags.Equals(other.Tags) || this.Tags.SequenceEqual(other.Tags))
            && (this.Data is null
                    ? other.Data is null
                    : this.Data.Equals(other.Data));
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is Diagnostic other)
        {
            return this.Equals(other);
        }
        else
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override int GetHashCode() =>
#if NET
        HashCode.Combine(Range, Severity, Code, Source, Message, Hash.CombineValues(Tags), CodeDescription, Data);
#else
        Hash.Combine(Range,
        Hash.Combine((int)(Severity ?? 0),
        Hash.Combine(Code?.GetHashCode() ?? 0,
        Hash.Combine(Source,
        Hash.Combine(Message,
        Hash.Combine(Hash.CombineValues(Tags),
        Hash.Combine(CodeDescription?.GetHashCode() ?? 0, Data?.GetHashCode() ?? 0)))))));
#endif
}
