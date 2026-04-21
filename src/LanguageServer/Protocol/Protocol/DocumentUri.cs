// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Datatype used to hold URI strings for LSP message serialization.  For details on how URIs are communicated in LSP,
/// see https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#uri
/// </summary>
/// <remarks>
/// The underlying parsed representation is <see cref="Protocol.ParsedUri"/>, which implements vscode-uri compatible
/// parsing without depending on System.Uri.
/// </remarks>
internal sealed record class DocumentUri(string UriString)
{
    private Optional<ParsedUri?> _parsedUri;

    [Obsolete("Use the constructor taking ParsedDocumentUri instead.")]
    public DocumentUri(Uri parsedUri) : this(parsedUri.AbsoluteUri)
    {
    }

    public DocumentUri(ParsedUri parsedDocumentUri) : this(parsedDocumentUri.ToString())
        => _parsedUri = parsedDocumentUri;

    /// <summary>
    /// Gets the parsed URI using vscode-uri compatible parsing. Lazily parsed from
    /// <see cref="UriString"/> on first access if not provided at construction time.
    /// 
    /// May be null if the URI string is not parseable.
    /// </summary>
    public ParsedUri? ParsedDocumentUri
    {
        get
        {
            _parsedUri = _parsedUri.HasValue ? _parsedUri : ParseUri(UriString);
            return _parsedUri.Value;
        }
    }

    private static ParsedUri? ParseUri(string uriString)
    {
        try
        {
            return Protocol.ParsedUri.Parse(uriString);
        }
        catch (UriFormatException)
        {
            // This is not a URI that we can parse.
            return null;
        }
    }

    /// <summary>
    /// Gets the parsed <see cref="System.Uri"/> for the URI string.
    /// </summary>
    /// <returns>
    /// Null if the URI string is not parse-able with <see cref="System.Uri"/>.
    /// </returns>
    /// <remarks>
    /// A new <see cref="System.Uri"/> instance is created from <see cref="ParsedDocumentUri"/> on each access.
    /// </remarks>
    [Obsolete("Use ParsedDocUri instead.")]
    public Uri? ParsedUri
    {
        get
        {
            var parsedDocumentUri = ParsedDocumentUri;
            if (parsedDocumentUri is null)
            {
                return null;
            }

            try
            {
                // We purposefully use the ToString(skipEncoding: true) overload here to avoid issues when the UriString contains
                // an encoded drive letter part (e.g. `file:///c%3A/`) which breaks System.Uri parsing.
                return new Uri(parsedDocumentUri.Value.ToString(skipEncoding: true));
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

        var thisParsed = ParsedDocumentUri;
        var otherParsed = otherUri.ParsedDocumentUri;

        // Bail if we cannot parse either of the URIs.  We already determined the URI strings are not equal and we need
        // to be able to parse the URIs to do deeper equivalency checks.
        if (thisParsed is null || otherParsed is null)
            return false;

        return thisParsed.Value.Equals(otherParsed.Value);
    }

    public override int GetHashCode()
    {
        var parsed = ParsedDocumentUri;
        return parsed is null ? UriString.GetHashCode() : parsed.Value.GetHashCode();
    }
}
