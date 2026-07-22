// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Roslyn.Utilities;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A C# port of vscode-uri's URI class. Parses, encodes, decodes, and formats URIs
/// identically to vscode-uri.
///
/// <code>
///       foo://example.com:8042/over/there?name=ferret#nose
///       \_/   \______________/\_________/ \_________/ \__/
///        |           |            |            |        |
///     scheme     authority       path        query   fragment
///        |   _____________________|__
///       / \ /                        \
///       urn:example:animal:ferret:nose
/// </code>
/// </summary>
internal readonly struct ParsedUri : IEquatable<ParsedUri>
{
    private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private static readonly Regex s_uriRegex = new(@"^(([^:/?#]+?):)?(//([^/?#]*))?([^?#]*)(\?([^#]*))?(#(.*))?", RegexOptions.Compiled);
    private static readonly Regex s_schemePattern = new(@"^[A-Za-z_][A-Za-z0-9_+.\-]*$", RegexOptions.Compiled);
    private static readonly Regex s_singleSlashStart = new(@"^/", RegexOptions.Compiled);
    private static readonly Regex s_doubleSlashStart = new(@"^//", RegexOptions.Compiled);
    private static readonly Regex s_encodedAsHex = new(@"(%[0-9A-Za-z][0-9A-Za-z])+", RegexOptions.Compiled);

    /// <summary>
    /// The scheme component (e.g., "http", "file").
    /// </summary>
    public string Scheme { get; }

    /// <summary>
    /// The authority component (e.g., "www.example.com").
    /// </summary>
    public string Authority { get; }

    /// <summary>
    /// The path component (e.g., "/some/path").
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The query component (e.g., "name=ferret").
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// The fragment component (e.g., "nose").
    /// </summary>
    public string Fragment { get; }

    private readonly string? _formatted;

    /// <summary>
    /// The file system path derived from this URI. Computed eagerly at construction time.
    /// Handles UNC paths, normalizes windows drive letters to lower-case, and uses the
    /// platform specific path separator.
    /// </summary>
    public string FsPath { get; }

    private ParsedUri(string scheme, string authority, string path, string query, string fragment, string? formatted = null)
    {
        Scheme = scheme;
        Authority = authority;
        Path = path;
        Query = query;
        Fragment = fragment;
        _formatted = formatted;
        FsPath = UriToFsPath(scheme, authority, path, keepDriveLetterCasing: false);
    }

    /// <summary>
    /// Mirrors vscode-uri's constructor string code path: applies SchemeFix, ReferenceResolution,
    /// and ValidateUri. Used by Parse and File.
    /// </summary>
    private static ParsedUri CreateFromComponents(string scheme, string authority, string path, string query, string fragment, bool strict)
    {
        scheme = SchemeFix(scheme, strict);
        path = ReferenceResolution(scheme, path);
        var result = new ParsedUri(scheme, authority, path, query, fragment);
        ValidateUri(result, strict);
        return result;
    }

    /// <summary>
    /// Creates a new URI from a string, e.g. <c>http://www.example.com/some/path</c>,
    /// <c>file:///usr/home</c>, or <c>scheme:with/path</c>.
    /// </summary>
    public static ParsedUri Parse(string value, bool strict = false)
    {
        var match = s_uriRegex.Match(value);
        if (!match.Success)
        {
            throw new UriFormatException($"[UriError]: URI could not be parsed: \"{value}\"");
        }

        var scheme = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
        var authority = match.Groups[4].Success ? PercentDecode(match.Groups[4].Value) : string.Empty;
        var path = PercentDecode(match.Groups[5].Value);
        var query = match.Groups[7].Success ? PercentDecode(match.Groups[7].Value) : string.Empty;
        var fragment = match.Groups[9].Success ? PercentDecode(match.Groups[9].Value) : string.Empty;

        return CreateFromComponents(scheme, authority, path, query, fragment, strict);
    }

    /// <summary>
    /// Creates a new URI from a file system path, e.g. <c>c:\my\files</c>,
    /// <c>/usr/home</c>, or <c>\\server\share\some\path</c>.
    /// </summary>
    public static ParsedUri File(string path)
    {
        var authority = string.Empty;

        // normalize to fwd-slashes on windows,
        // on other systems bwd-slashes are valid
        // filename character, eg /f\oo/ba\r.txt
        if (s_isWindows)
        {
            path = path.Replace('\\', '/');
        }

        // check for authority as used in UNC shares
        // or use the path as given
        if (path.Length >= 2 && path[0] == '/' && path[1] == '/')
        {
            var idx = path.IndexOf('/', 2);
            if (idx == -1)
            {
                authority = path.Substring(2);
                path = "/";
            }
            else
            {
                authority = path.Substring(2, idx - 2);
                path = path.Substring(idx);
                if (path.Length == 0)
                {
                    path = "/";
                }
            }
        }

        return CreateFromComponents("file", authority, path, string.Empty, string.Empty, strict: false);
    }

    /// <summary>
    /// Returns true when this URI uses the <c>file</c> scheme.
    /// </summary>
    public bool IsFile => IsFileScheme(Scheme);

    /// <summary>
    /// Creates a string representation for this URI. Calling <see cref="Parse"/> with the
    /// result creates a URI that is equal to this URI.
    /// </summary>
    /// <param name="skipEncoding">Do not encode the result.</param>
    public string ToString(bool skipEncoding)
    {
        if (!skipEncoding && _formatted != null)
        {
            return _formatted;
        }

        return AsFormatted(this, skipEncoding);
    }

    /// <summary>
    /// Returns the encoded string representation (equivalent to <c>ToString(false)</c>).
    /// </summary>
    public override string ToString()
    {
        return _formatted ?? AsFormatted(this, skipEncoding: false);
    }

    #region Equality

    public bool Equals(ParsedUri other)
    {
        // Schemes are always case-insensitive per RFC 3986 Section 3.1.
        if (!string.Equals(Scheme, other.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // For file URIs with UNC paths or DOS drive letter paths, compare case-insensitively.
        // This matches System.Uri's behavior (IsUncOrDosPath flag). Unix-style file paths
        // (e.g., file:///usr/home) remain case-sensitive.
        var comparison = IsUncOrDosPath || other.IsUncOrDosPath
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(Authority, other.Authority, comparison)
            && string.Equals(Path, other.Path, comparison)
            && string.Equals(Query, other.Query, comparison)
            && string.Equals(Fragment, other.Fragment, comparison);
    }

    public override bool Equals(object? obj)
        => obj is ParsedUri other && Equals(other);

    public override int GetHashCode()
    {
        // Scheme is always case-insensitive. Other components are case-insensitive only for UNC/DOS paths.
        var componentComparer = IsUncOrDosPath ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var schemeHash = StringComparer.OrdinalIgnoreCase.GetHashCode(Scheme ?? string.Empty);
        var authorityHash = componentComparer.GetHashCode(Authority ?? string.Empty);
        var pathHash = componentComparer.GetHashCode(Path ?? string.Empty);
        var queryHash = componentComparer.GetHashCode(Query ?? string.Empty);
        var fragmentHash = componentComparer.GetHashCode(Fragment ?? string.Empty);

#if NET
        return HashCode.Combine(schemeHash, authorityHash, pathHash, queryHash, fragmentHash);
#else
        return Hash.Combine(schemeHash,
        Hash.Combine(authorityHash,
        Hash.Combine(pathHash,
        Hash.Combine(queryHash, fragmentHash))));
#endif
    }

    public static bool operator ==(ParsedUri left, ParsedUri right) => left.Equals(right);
    public static bool operator !=(ParsedUri left, ParsedUri right) => !left.Equals(right);

    #endregion

    #region Validation helpers

    private static void ValidateUri(ParsedUri uri, bool strict)
    {
        // scheme, must be set in strict mode
        if (uri.Scheme.Length == 0 && strict)
        {
            throw new UriFormatException(
                $"[UriError]: Scheme is missing: {{scheme: \"\", authority: \"{uri.Authority}\", path: \"{uri.Path}\", query: \"{uri.Query}\", fragment: \"{uri.Fragment}\"}}");
        }

        // scheme, https://tools.ietf.org/html/rfc3986#section-3.1
        if (uri.Scheme.Length > 0 && !s_schemePattern.IsMatch(uri.Scheme))
        {
            throw new UriFormatException("[UriError]: Scheme contains illegal characters.");
        }

        // path, http://tools.ietf.org/html/rfc3986#section-3.3
        if (uri.Path.Length > 0)
        {
            if (uri.Authority.Length > 0)
            {
                if (uri.Path[0] != '/')
                {
                    throw new UriFormatException(
                        "[UriError]: If a URI contains an authority component, then the path component must either be empty or begin with a slash (\"/\") character");
                }
            }
            else
            {
                if (uri.Path.Length >= 2 && uri.Path[0] == '/' && uri.Path[1] == '/')
                {
                    throw new UriFormatException(
                        "[UriError]: If a URI does not contain an authority component, then the path cannot begin with two slash characters (\"//\")");
                }
            }
        }
    }

    private static string SchemeFix(string scheme, bool strict)
    {
        if (scheme.Length == 0 && !strict)
        {
            return "file";
        }

        return scheme.ToLower();
    }

    /// <summary>
    /// Implements a bit of https://tools.ietf.org/html/rfc3986#section-5
    /// </summary>
    private static string ReferenceResolution(string scheme, string path)
    {
        switch (scheme)
        {
            case "https":
            case "http":
            case "file":
                if (path.Length == 0)
                {
                    path = "/";
                }
                else if (path[0] != '/')
                {
                    path = "/" + path;
                }

                break;
        }

        return path;
    }

    #endregion

    #region Encoding

    private delegate string Encoder(string uriComponent, bool isPath, bool isAuthority);

    /// <summary>
    /// Encode table for reserved characters: https://tools.ietf.org/html/rfc3986#section-2.2
    /// </summary>
    private static string? GetEncodeTableEntry(char ch)
    {
        return ch switch
        {
            // gen-delims
            ':' => "%3A",
            '/' => "%2F",
            '?' => "%3F",
            '#' => "%23",
            '[' => "%5B",
            ']' => "%5D",
            '@' => "%40",
            // sub-delims
            '!' => "%21",
            '$' => "%24",
            '&' => "%26",
            '\'' => "%27",
            '(' => "%28",
            ')' => "%29",
            '*' => "%2A",
            '+' => "%2B",
            ',' => "%2C",
            ';' => "%3B",
            '=' => "%3D",
            ' ' => "%20",
            _ => null,
        };
    }

    private static string EncodeURIComponentFast(string uriComponent, bool isPath, bool isAuthority)
    {
        StringBuilder? res = null;
        var nativeEncodeStart = -1;

        for (var pos = 0; pos < uriComponent.Length; pos++)
        {
            var code = uriComponent[pos];

            // unreserved characters: https://tools.ietf.org/html/rfc3986#section-2.3
            if ((code >= 'a' && code <= 'z')
                || (code >= 'A' && code <= 'Z')
                || (code >= '0' && code <= '9')
                || code == '-'
                || code == '.'
                || code == '_'
                || code == '~'
                || (isPath && code == '/')
                || (isAuthority && code == '[')
                || (isAuthority && code == ']')
                || (isAuthority && code == ':'))
            {
                // check if we are delaying native encode
                if (nativeEncodeStart != -1)
                {
                    res ??= new StringBuilder();
                    res.Append(PercentEncodeString(uriComponent.Substring(nativeEncodeStart, pos - nativeEncodeStart)));
                    nativeEncodeStart = -1;
                }

                // check if we write into a new string (by default we try to return the param)
                res?.Append(code);
            }
            else
            {
                // encoding needed, we need to allocate a new string
                if (res == null)
                {
                    res = new StringBuilder(uriComponent, 0, pos, uriComponent.Length * 2);
                }

                // check with default table first
                var escaped = GetEncodeTableEntry(code);
                if (escaped != null)
                {
                    // check if we are delaying native encode
                    if (nativeEncodeStart != -1)
                    {
                        res.Append(PercentEncodeString(uriComponent.Substring(nativeEncodeStart, pos - nativeEncodeStart)));
                        nativeEncodeStart = -1;
                    }

                    // append escaped variant to result
                    res.Append(escaped);
                }
                else if (nativeEncodeStart == -1)
                {
                    // use native encode only when needed
                    nativeEncodeStart = pos;
                }
            }
        }

        if (nativeEncodeStart != -1)
        {
            res ??= new StringBuilder();
            res.Append(PercentEncodeString(uriComponent.Substring(nativeEncodeStart)));
        }

        return res != null ? res.ToString() : uriComponent;
    }

    private static string EncodeURIComponentMinimal(string path, bool isPath, bool isAuthority)
    {
        StringBuilder? res = null;
        for (var pos = 0; pos < path.Length; pos++)
        {
            var code = path[pos];
            if (code == '#' || code == '?')
            {
                if (res == null)
                {
                    res = new StringBuilder(path, 0, pos, path.Length + 6);
                }

                res.Append(GetEncodeTableEntry(code));
            }
            else
            {
                res?.Append(code);
            }
        }

        return res != null ? res.ToString() : path;
    }

    /// <summary>
    /// Percent-encodes a string using UTF-8, equivalent to JavaScript's encodeURIComponent.
    /// </summary>
    private static string PercentEncodeString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var sb = new StringBuilder(bytes.Length * 3);
        foreach (var b in bytes)
        {
            sb.Append('%');
            sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
    }

    #endregion

    #region Decoding

    private static string PercentDecode(string str)
    {
        if (!s_encodedAsHex.IsMatch(str))
        {
            return str;
        }

        return s_encodedAsHex.Replace(str, match => DecodeURIComponentGraceful(match.Value));
    }

    private static string DecodeURIComponentGraceful(string str)
    {
        try
        {
            return DecodeURIComponent(str);
        }
        catch
        {
            if (str.Length > 3)
            {
                return str.Substring(0, 3) + DecodeURIComponentGraceful(str.Substring(3));
            }
            else
            {
                return str;
            }
        }
    }

    private static readonly Encoding s_strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Decodes a percent-encoded string using UTF-8, equivalent to JavaScript's decodeURIComponent.
    /// Throws on invalid UTF-8 sequences, matching JavaScript's behavior.
    /// </summary>
    private static string DecodeURIComponent(string str)
    {
        var bytes = new byte[str.Length / 3];
        var byteCount = 0;
        StringBuilder? result = null;

        for (var i = 0; i < str.Length;)
        {
            if (str[i] == '%' && i + 2 < str.Length)
            {
                var hi = HexToInt(str[i + 1]);
                var lo = HexToInt(str[i + 2]);
                if (hi >= 0 && lo >= 0)
                {
                    bytes[byteCount++] = (byte)((hi << 4) | lo);
                    i += 3;
                    continue;
                }
            }

            // Flush any accumulated bytes
            if (byteCount > 0)
            {
                result ??= new StringBuilder();
                result.Append(s_strictUtf8.GetString(bytes, 0, byteCount));
                byteCount = 0;
            }

            result ??= new StringBuilder();
            result.Append(str[i]);
            i++;
        }

        if (byteCount > 0)
        {
            result ??= new StringBuilder();
            result.Append(s_strictUtf8.GetString(bytes, 0, byteCount));
        }

        return result?.ToString() ?? str;
    }

    private static int HexToInt(char ch)
    {
        if (ch >= '0' && ch <= '9') return ch - '0';
        if (ch >= 'A' && ch <= 'F') return ch - 'A' + 10;
        if (ch >= 'a' && ch <= 'f') return ch - 'a' + 10;
        return -1;
    }

    #endregion

    #region Formatting

    /// <summary>
    /// Compute fsPath for the given URI components.
    /// </summary>
    private static string UriToFsPath(string scheme, string authority, string path, bool keepDriveLetterCasing)
    {
        string value;
        if (authority.Length > 0 && path.Length > 1 && IsFileScheme(scheme))
        {
            // unc path: file://shares/c$/far/boo
            value = "//" + authority + path;
        }
        else if (
            path.Length >= 3
            && path[0] == '/'
            && IsLetter(path[1])
            && path[2] == ':')
        {
            if (!keepDriveLetterCasing)
            {
                // windows drive letter: file:///c:/far/boo
                value = char.ToLowerInvariant(path[1]) + path.Substring(2);
            }
            else
            {
                value = path.Substring(1);
            }
        }
        else
        {
            // other path
            value = path;
        }

        if (s_isWindows)
        {
            value = value.Replace('/', '\\');
        }

        return value;
    }

    internal static bool IsLetter(char ch)
        => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');

    internal static bool IsFileScheme(string scheme)
        => string.Equals(scheme, "file", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if this is a file URI with a UNC host or DOS drive letter path.
    /// Matches the behavior of System.Uri's internal IsUncOrDosPath flag, which determines
    /// whether path comparison should be case-insensitive.
    /// </summary>
    internal bool IsUncOrDosPath
        => IsFile
        && (Authority.Length > 0 || (Path.Length >= 3 && Path[0] == '/' && IsLetter(Path[1]) && Path[2] == ':'));

    /// <summary>
    /// Create the external version of a URI.
    /// </summary>
    private static string AsFormatted(ParsedUri uri, bool skipEncoding)
    {
        Encoder encoder = !skipEncoding
            ? EncodeURIComponentFast
            : EncodeURIComponentMinimal;

        var res = new StringBuilder();
        var scheme = uri.Scheme;
        var authority = uri.Authority;
        var path = uri.Path;
        var query = uri.Query;
        var fragment = uri.Fragment;

        if (scheme.Length > 0)
        {
            res.Append(scheme);
            res.Append(':');
        }

        if (authority.Length > 0 || IsFileScheme(scheme))
        {
            res.Append("//");
        }

        if (authority.Length > 0)
        {
            var idx = authority.IndexOf('@');
            if (idx != -1)
            {
                // <user>@<auth>
                var userinfo = authority.Substring(0, idx);
                authority = authority.Substring(idx + 1);
                idx = userinfo.LastIndexOf(':');
                if (idx == -1)
                {
                    res.Append(encoder(userinfo, false, false));
                }
                else
                {
                    // <user>:<pass>@<auth>
                    res.Append(encoder(userinfo.Substring(0, idx), false, false));
                    res.Append(':');
                    res.Append(encoder(userinfo.Substring(idx + 1), false, true));
                }

                res.Append('@');
            }

            authority = authority.ToLowerInvariant();
            idx = authority.LastIndexOf(':');
            if (idx == -1)
            {
                res.Append(encoder(authority, false, true));
            }
            else
            {
                // <auth>:<port>
                res.Append(encoder(authority.Substring(0, idx), false, true));
                res.Append(authority.Substring(idx));
            }
        }

        if (path.Length > 0)
        {
            // lower-case windows drive letters in /C:/fff or C:/fff
            if (path.Length >= 3 && path[0] == '/' && path[2] == ':')
            {
                var code = path[1];
                if (code >= 'A' && code <= 'Z')
                {
                    path = "/" + (char)(code + 32) + ":" + path.Substring(3);
                }
            }
            else if (path.Length >= 2 && path[1] == ':')
            {
                var code = path[0];
                if (code >= 'A' && code <= 'Z')
                {
                    path = (char)(code + 32) + ":" + path.Substring(2);
                }
            }

            // encode the rest of the path
            res.Append(encoder(path, true, false));
        }

        if (query.Length > 0)
        {
            res.Append('?');
            res.Append(encoder(query, false, false));
        }

        if (fragment.Length > 0)
        {
            res.Append('#');
            if (!skipEncoding)
            {
                res.Append(EncodeURIComponentFast(fragment, false, false));
            }
            else
            {
                res.Append(fragment);
            }
        }

        return res.ToString();
    }

    #endregion
}
