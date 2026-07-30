// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

/// <summary>
/// Struct representing a text document content change event.
/// May contain either a <see cref="TextDocumentContentChangePartial"/> (ranged change) or
/// a <see cref="TextDocumentContentChangeWholeDocument"/> (full-document replacement).
/// <para>
/// Mirrors the LSP 3.18 spec's TypeScript union type:
/// <c>export type TextDocumentContentChangeEvent = TextDocumentContentChangePartial | TextDocumentContentChangeWholeDocument;</c>
/// </para>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#textDocumentContentChangeEvent">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
[JsonConverter(typeof(SumConverter))]
internal readonly struct TextDocumentContentChangeEvent : ISumType, IEquatable<TextDocumentContentChangeEvent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextDocumentContentChangeEvent"/> struct containing a <see cref="TextDocumentContentChangePartial"/>.
    /// </summary>
    public TextDocumentContentChangeEvent(TextDocumentContentChangePartial val)
    {
        this.Value = val;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextDocumentContentChangeEvent"/> struct containing a <see cref="TextDocumentContentChangeWholeDocument"/>.
    /// </summary>
    public TextDocumentContentChangeEvent(TextDocumentContentChangeWholeDocument val)
    {
        this.Value = val;
    }

    /// <inheritdoc/>
    public object? Value { get; }

    /// <summary>
    /// Implicitly wraps a <see cref="TextDocumentContentChangePartial"/> in a <see cref="TextDocumentContentChangeEvent"/>.
    /// </summary>
    public static implicit operator TextDocumentContentChangeEvent(TextDocumentContentChangePartial val) => new(val);

    /// <summary>
    /// Implicitly wraps a <see cref="TextDocumentContentChangeWholeDocument"/> in a <see cref="TextDocumentContentChangeEvent"/>.
    /// </summary>
    public static implicit operator TextDocumentContentChangeEvent(TextDocumentContentChangeWholeDocument val) => new(val);

    /// <summary>
    /// Tries to get the value as <see cref="TextDocumentContentChangePartial"/>.
    /// </summary>
    public bool TryGetFirst([MaybeNullWhen(false)] out TextDocumentContentChangePartial value)
    {
        if (this.Value is TextDocumentContentChangePartial tVal)
        {
            value = tVal;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Tries to get the value as <see cref="TextDocumentContentChangeWholeDocument"/>.
    /// </summary>
    public bool TryGetSecond([MaybeNullWhen(false)] out TextDocumentContentChangeWholeDocument value)
    {
        if (this.Value is TextDocumentContentChangeWholeDocument tVal)
        {
            value = tVal;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is TextDocumentContentChangeEvent other && this.Equals(other);

    /// <inheritdoc/>
    public bool Equals(TextDocumentContentChangeEvent other)
        => EqualityComparer<object?>.Default.Equals(this.Value, other.Value);

    /// <inheritdoc/>
    public override int GetHashCode()
        => -1937169414 + EqualityComparer<object?>.Default.GetHashCode(this.Value);

    public static bool operator ==(TextDocumentContentChangeEvent left, TextDocumentContentChangeEvent right)
        => left.Equals(right);

    public static bool operator !=(TextDocumentContentChangeEvent left, TextDocumentContentChangeEvent right)
        => !(left == right);
}
