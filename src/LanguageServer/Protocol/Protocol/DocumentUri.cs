// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

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

        // Compare using vscode-uri parsed components. ParsedDocumentUri.ToString() produces a deterministic
        // encoded form, so comparing the struct components directly is equivalent (and avoids an allocation).
        return this.ParsedDocUri.Equals(otherUri.ParsedDocUri);
    }

    public override int GetHashCode()
    {
        // Use the deterministic encoded form from ParsedDocumentUri for hashing. Hash case-insensitively
        // to handle scheme/authority case differences (vscode-uri lowercases authority but preserves scheme case).
        return StringComparer.OrdinalIgnoreCase.GetHashCode(this.ParsedDocUri.ToString());
    }
}
