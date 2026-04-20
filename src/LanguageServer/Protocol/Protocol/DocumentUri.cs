// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Datatype used to hold URI strings for LSP message serialization.  For details on how URIs are communicated in LSP,
/// see https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#uri
/// </summary>
/// <remarks>
/// The underlying parsed representation is <see cref="ParsedDocumentUri"/>, which implements vscode-uri compatible
/// parsing without depending on System.Uri.
/// </remarks>
internal sealed record class DocumentUri(string UriString)
{
    private ParsedDocumentUri? _parsedDocumentUri;

    [Obsolete("Use the constructor taking ParsedDocumentUri instead.")]
    public DocumentUri(Uri parsedUri) : this(parsedUri.AbsoluteUri)
    {
    }

    public DocumentUri(ParsedDocumentUri parsedDocumentUri) : this(parsedDocumentUri.ToString())
        => _parsedDocumentUri = parsedDocumentUri;

    /// <summary>
    /// Gets the parsed URI using vscode-uri compatible parsing. Always available — lazily parsed from
    /// <see cref="UriString"/> on first access if not provided at construction time.
    /// </summary>
    public ParsedDocumentUri ParsedDocUri
    {
        get
        {
            _parsedDocumentUri ??= ParsedDocumentUri.Parse(UriString);
            return _parsedDocumentUri.Value;
        }
    }

    /// <summary>
    /// Gets the parsed System.Uri for the URI string.
    /// </summary>
    /// <returns>
    /// Null if the URI string is not parse-able with System.Uri.
    /// </returns>
    /// <remarks>
    /// A new System.Uri instance is created from <see cref="UriString"/> on each access.
    /// Prefer <see cref="ParsedDocUri"/> for URI operations.
    /// </remarks>
    [Obsolete("Use ParsedDocUri instead.")]
    public Uri? ParsedUri
    {
        get
        {
            try
            {
                return new Uri(UriString);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }
    }

    public override string ToString() => UriString;

    public bool Equals(DocumentUri otherUri)
    {
        if (otherUri is null)
            return false;

        // 99% of the time the equivalent URIs will have equivalent URI strings, as the client is expected to be
        // consistent in how it sends the URIs to the server, either always encoded or always unencoded. See
        // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#uri
        if (this.UriString == otherUri.UriString)
            return true;

        // Compare using vscode-uri parsed components.
        var thisParsed = this.ParsedDocUri;
        var otherParsed = otherUri.ParsedDocUri;

        // Schemes are always case-insensitive per RFC 3986 Section 3.1.
        if (!string.Equals(thisParsed.Scheme, otherParsed.Scheme, StringComparison.OrdinalIgnoreCase))
            return false;

        // For file URIs with UNC paths or DOS drive letter paths, compare case-insensitively.
        // This matches System.Uri's behavior (IsUncOrDosPath flag). Unix-style file paths
        // (e.g., file:///usr/home) remain case-sensitive.
        var comparison = thisParsed.IsUncOrDosPath || otherParsed.IsUncOrDosPath
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(thisParsed.Authority, otherParsed.Authority, comparison)
            && string.Equals(thisParsed.Path, otherParsed.Path, comparison)
            && string.Equals(thisParsed.Query, otherParsed.Query, comparison)
            && string.Equals(thisParsed.Fragment, otherParsed.Fragment, comparison);
    }

    public override int GetHashCode()
    {
        var parsed = this.ParsedDocUri;

        // For file URIs with UNC/DOS paths, use case-insensitive hashing to match the case-insensitive
        // Equals behavior. For all other URIs, use case-sensitive hashing (except scheme, which is always
        // case-insensitive per RFC 3986).
        if (parsed.IsUncOrDosPath)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(parsed.ToString());
        }

        // Scheme is always case-insensitive, so hash it that way. Other components are case-sensitive.
        var schemeHash = StringComparer.OrdinalIgnoreCase.GetHashCode(parsed.Scheme ?? string.Empty);
#if NET
        return HashCode.Combine(schemeHash, parsed.Authority, parsed.Path, parsed.Query, parsed.Fragment);
#else
        return Hash.Combine(schemeHash,
            Hash.Combine(parsed.Authority?.GetHashCode() ?? 0,
            Hash.Combine(parsed.Path?.GetHashCode() ?? 0,
            Hash.Combine(parsed.Query?.GetHashCode() ?? 0, parsed.Fragment?.GetHashCode() ?? 0))));
#endif
    }
}
