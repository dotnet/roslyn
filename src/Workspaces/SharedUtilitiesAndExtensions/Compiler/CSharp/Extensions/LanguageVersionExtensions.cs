// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Shared.Extensions
{
    internal static class LanguageVersionExtensions
    {
        public static bool IsCSharp9OrAbove(this LanguageVersion languageVersion)
            => languageVersion >= LanguageVersion.CSharp9;

        public static bool IsCSharp10OrAbove(this LanguageVersion languageVersion)
            => languageVersion >= LanguageVersion.CSharp10;

        public static bool HasConstantInterpolatedStrings(this LanguageVersion languageVersion)
            => languageVersion.IsCSharp10OrAbove();
    }
}
