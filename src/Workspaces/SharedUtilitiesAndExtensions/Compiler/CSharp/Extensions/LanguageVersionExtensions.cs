// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Shared.Extensions;

internal static class LanguageVersionExtensions
{
    public static bool IsCSharp15OrAbove(this LanguageVersion languageVersion)
        => languageVersion >= LanguageVersion.Preview;

#if ROSLYN_4_12_OR_LOWER
    public static bool IsCSharp14OrAbove(this LanguageVersion languageVersion)
        => (int)languageVersion >= 1400;
#else
    public static bool IsCSharp14OrAbove(this LanguageVersion languageVersion)
        => languageVersion >= LanguageVersion.CSharp14;
#endif

    public static bool IsCSharp13OrAbove(this LanguageVersion languageVersion)
        => languageVersion >= LanguageVersion.CSharp13;

    public static bool IsCSharp12OrAbove(this LanguageVersion languageVersion)
        => languageVersion >= LanguageVersion.CSharp12;

    public static bool IsCSharp11OrAbove(this LanguageVersion languageVersion)
        => languageVersion >= LanguageVersion.CSharp11;

    public static bool HasConstantInterpolatedStrings(this LanguageVersion languageVersion)
        => languageVersion >= LanguageVersion.CSharp10;

    public static bool SupportsCollectionExpressions(this LanguageVersion languageVersion)
        => languageVersion.IsCSharp12OrAbove();

    public static bool SupportsPrimaryConstructors(this LanguageVersion languageVersion)
        => languageVersion.IsCSharp12OrAbove();

    public static bool SupportsExtensions(this LanguageVersion languageVersion)
        => languageVersion.IsCSharp14OrAbove();

    /// <remarks>
    /// Corresponds to Microsoft.CodeAnalysis.CSharp.LanguageVersionFacts.CSharpNext.
    /// </remarks>
    internal const LanguageVersion CSharpNext = LanguageVersion.Preview;
}
