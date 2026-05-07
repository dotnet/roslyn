// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

// Note: RazorSDK is aware of version monikers such as "latest", and "experimental". Update it if we introduce new monikers.
[DebuggerDisplay("{" + nameof(DebuggerToString) + "(),nq}")]
public sealed record RazorLanguageVersion : IComparable<RazorLanguageVersion>
{
    // Note: When adding a new version, be sure to update Latest and BuildKnownVersion() below!
    // Also update RazorLanguageVersionTest (add new case and update TryParseLatest).
    public static readonly RazorLanguageVersion Version_1_0 = new(1, 0);
    public static readonly RazorLanguageVersion Version_1_1 = new(1, 1);
    public static readonly RazorLanguageVersion Version_2_0 = new(2, 0);
    public static readonly RazorLanguageVersion Version_2_1 = new(2, 1);
    public static readonly RazorLanguageVersion Version_3_0 = new(3, 0);
    public static readonly RazorLanguageVersion Version_5_0 = new(5, 0);
    public static readonly RazorLanguageVersion Version_6_0 = new(6, 0);
    public static readonly RazorLanguageVersion Version_7_0 = new(7, 0);
    public static readonly RazorLanguageVersion Version_8_0 = new(8, 0);
    public static readonly RazorLanguageVersion Version_9_0 = new(9, 0);
    public static readonly RazorLanguageVersion Version_10_0 = new(10, 0); // Didn't ship anywhere
    public static readonly RazorLanguageVersion Version_11_0 = new(11, 0);
    public static readonly RazorLanguageVersion Latest = Version_9_0;
    public static readonly RazorLanguageVersion Preview = Version_11_0;
    public static readonly RazorLanguageVersion Experimental = new(1337, 1337);

    private static readonly FrozenDictionary<string, RazorLanguageVersion> s_knownVersions = BuildKnownVersions();

    public int Major { get; }
    public int Minor { get; }

    private RazorLanguageVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    private static FrozenDictionary<string, RazorLanguageVersion> BuildKnownVersions()
    {
        var map = new Dictionary<string, RazorLanguageVersion>(StringComparer.OrdinalIgnoreCase)
        {
            ["1.0"] = Version_1_0,
            ["1.1"] = Version_1_1,
            ["2.0"] = Version_2_0,
            ["2.1"] = Version_2_1,
            ["3.0"] = Version_3_0,
            ["5.0"] = Version_5_0,
            ["6.0"] = Version_6_0,
            ["7.0"] = Version_7_0,
            ["8.0"] = Version_8_0,
            ["9.0"] = Version_9_0,
            ["10.0"] = Version_10_0,
            ["11.0"] = Version_11_0,
            ["latest"] = Latest,
            ["preview"] = Preview,
            ["experimental"] = Experimental,
        };

        return map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryParse(string languageVersion, [NotNullWhen(true)] out RazorLanguageVersion? version)
    {
        if (languageVersion == null)
        {
            throw new ArgumentNullException(nameof(languageVersion));
        }

        return s_knownVersions.TryGetValue(languageVersion, out version);
    }

    public static RazorLanguageVersion Parse(string languageVersion)
    {
        if (TryParse(languageVersion, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException(
            Resources.FormatRazorLanguageVersion_InvalidVersion(languageVersion),
            nameof(languageVersion));
    }

    public int CompareTo(RazorLanguageVersion? other)
    {
        if (other == null)
        {
            return 1;
        }

        var result = Major.CompareTo(other.Major);
        if (result != 0)
        {
            return result;
        }

        return Minor.CompareTo(other.Minor);
    }

    public override string ToString() => $"{Major}.{Minor}";

    private string DebuggerToString() => $"Razor '{Major}.{Minor}'";

    public static bool operator <(RazorLanguageVersion x, RazorLanguageVersion y) => x.CompareTo(y) < 0;
    public static bool operator <=(RazorLanguageVersion x, RazorLanguageVersion y) => x.CompareTo(y) <= 0;
    public static bool operator >(RazorLanguageVersion x, RazorLanguageVersion y) => x.CompareTo(y) > 0;
    public static bool operator >=(RazorLanguageVersion x, RazorLanguageVersion y) => x.CompareTo(y) >= 0;

    /// <summary>
    /// Gets the default warning level for this language version.
    /// The warning level corresponds to the major version number
    /// (e.g., <see cref="Version_11_0"/> → <c>11</c>).
    /// </summary>
    public int GetDefaultWarningLevel() => Major;
}
