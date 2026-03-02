// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Insertion;

internal readonly record struct BuildVersion(int Build, int Revision) : IComparable<BuildVersion>
{
    public override string ToString() => Build + "." + Revision;

    public int CompareTo(BuildVersion other)
    {
        var result = Build.CompareTo(other.Build);
        return result == 0 ? Revision.CompareTo(other.Revision) : result;
    }

    public static bool operator <(BuildVersion x, BuildVersion y) => x.CompareTo(y) < 0;
    public static bool operator >(BuildVersion x, BuildVersion y) => x.CompareTo(y) > 0;
    public static bool operator <=(BuildVersion x, BuildVersion y) => x.CompareTo(y) <= 0;
    public static bool operator >=(BuildVersion x, BuildVersion y) => x.CompareTo(y) >= 0;

    internal static BuildVersion FromTfsBuildNumber(string buildNumber, string roslynBuildName)
    {
        return FromString(buildNumber.Replace(roslynBuildName, string.Empty).Replace("_", string.Empty));
    }

    internal static BuildVersion FromString(string str)
    {
        string[] parts;
        if (str.Contains('.'))
        {
            parts = str.Split('.');
        }
        else if (str.Contains('-'))
        {
            parts = str.Split('-');
        }
        else
        {
            throw new FormatException($"BuildVersion should be in the form of 12345678.9 or 12345678-9");
        }

        return new BuildVersion(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public static explicit operator BuildVersion(Version version) => new(version.Build, version.Revision);
}
