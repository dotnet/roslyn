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
/// While .NET has a type represent URIs (System.Uri), we do not want to use this type directly in serialization and
/// deserialization.  System.Uri does full parsing and validation on the URI upfront, so any issues in the uri format
/// will cause deserialization to fail and bypass any of our error recovery.
/// 
/// Compounding this problem, System.Uri will fail to parse various RFC spec valid URIs. In order to gracefully handle
/// these issues, we defer the parsing of the URI until someone actually asks for it (and can handle the failure).
/// </remarks>
internal sealed record class DocumentUri(string UriString)
{
    private Optional<Uri> _parsedUri;
    private ParsedDocumentUri? _parsedDocumentUri;

    [Obsolete("Use the constructor taking ParsedDocumentUri instead.")]
    public DocumentUri(Uri parsedUri) : this(parsedUri.AbsoluteUri)
        => _parsedUri = parsedUri;

    public DocumentUri(ParsedDocumentUri parsedDocumentUri) : this(parsedDocumentUri.ToString())
        => _parsedDocumentUri = parsedDocumentUri;

    /// <summary>
    /// Gets the parsed URI using vscode-uri compatible parsing.
    /// </summary>
    public ParsedDocumentUri? ParsedDocUri => _parsedDocumentUri;

    /// <summary>
    /// Gets the parsed System.Uri for the URI string.
    /// </summary>
    /// <returns>
    /// Null if the URI string is not parse-able with System.Uri.
    /// </returns>
    /// <remarks>
    /// Invalid RFC spec URI strings are not parse-able as so will return null here. However, System.Uri can also fail
    /// to parse certain valid RFC spec URI strings.
    /// 
    /// For example, any URI containing a 'sub-delims' character in the host name is a valid RFC spec URI, but will fail
    /// with System.Uri
    /// </remarks>
    [Obsolete("Use ParsedDocUri instead.")]
    public Uri? ParsedUri
    {
        get
        {
            _parsedUri = _parsedUri.HasValue ? _parsedUri : ParseUri(UriString);
            return _parsedUri.Value;
        }
    }

    private static Uri? ParseUri(string uriString)
    {
        try
        {
            return new Uri(uriString);
        }
        catch (UriFormatException)
        {
            // This is not a URI that System.Uri can handle.
            return null;
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

        // If both sides have ParsedDocumentUri, compare using vscode-uri semantics.
        // ParsedDocumentUri.ToString() produces a deterministic encoded form, so comparing the struct
        // components directly is equivalent (and avoids an allocation).
        if (this._parsedDocumentUri is { } thisParsed && otherUri._parsedDocumentUri is { } otherParsed)
            return thisParsed.Equals(otherParsed);

        // If both sides have System.Uri, use existing comparison logic.
        if (this._parsedDocumentUri is null && otherUri._parsedDocumentUri is null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var thisSystemUri = this.ParsedUri;
            var otherSystemUri = otherUri.ParsedUri;
#pragma warning restore CS0618

            // Bail if we cannot parse either of the URIs.  We already determined the URI strings are not equal and we
            // need to be able to parse the URIs to do deeper equivalency checks.
            if (thisSystemUri is null || otherSystemUri is null)
                return false;

            // Compare AbsoluteUri properties (always percent encoded) to handle the case where one URI string is
            // encoded and the other isn't. Fall back to Uri.Equals for scheme-specific comparison rules.
            return (thisSystemUri.IsAbsoluteUri && otherSystemUri.IsAbsoluteUri && thisSystemUri.AbsoluteUri == otherSystemUri.AbsoluteUri) ||
                Equals(thisSystemUri, otherSystemUri);
        }

        // Mixed case (one has ParsedDocumentUri, other has System.Uri). These use different encoding strategies so we
        // cannot safely compare their canonical forms. The UriString fast path above already handles the common case.
        return false;
    }

    public override int GetHashCode()
    {
        // If this DocumentUri was constructed with a ParsedDocumentUri, use its ToString() for hashing.
        // ParsedDocumentUri.ToString() equals UriString (set in the constructor), and we hash case-insensitively
        // to match the existing behavior pattern for URI comparison.
        if (this._parsedDocumentUri is { } pdu)
            return StringComparer.OrdinalIgnoreCase.GetHashCode(pdu.ToString());

        // Existing System.Uri-based hashing behavior.
#pragma warning disable CS0618 // Type or member is obsolete
        // We can't do anything better than the uri string hash code if we cannot parse the URI.
        if (this.ParsedUri is null)
            return this.UriString.GetHashCode();

        // Since the Uri type does not consider an encoded Uri equal to an unencoded Uri, we need to handle this
        // ourselves. The AbsoluteUri property is always encoded, so we can use this to compare the URIs (see Equals
        // above).
        //
        // However, depending on the kind of URI, case sensitivity in AbsoluteUri should be ignored. Uri.GetHashCode
        // normally handles this internally, but the parameters it uses to determine which comparison to use are not
        // exposed.
        //
        // Instead, we will always create the hash code ignoring case, and will rely on the Equals implementation to
        // handle collisions (between two Uris with different casing).  This should be very rare in practice. Collisions
        // can happen for non UNC URIs (e.g. `git:/blah` vs `git:/Blah`).
        return this.ParsedUri.IsAbsoluteUri
            ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.ParsedUri.AbsoluteUri)
            : this.ParsedUri.GetHashCode();
#pragma warning restore CS0618
    }
}
