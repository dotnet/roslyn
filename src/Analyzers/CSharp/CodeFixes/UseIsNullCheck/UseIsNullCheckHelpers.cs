// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    internal static class UseIsNullCheckHelpers
    {
        public static string GetTitle(bool negated, ParseOptions options)
        {
            if (negated)
            {
                return SupportsIsNotPattern(options)
                    ? CSharpAnalyzersResources.Use_is_not_null_check
                    : CSharpAnalyzersResources.Use_is_object_check;
            }
            else
            {
                return CSharpAnalyzersResources.Use_is_null_check;
            }
        }

        public static bool SupportsIsNotPattern(ParseOptions options)
            => options.LanguageVersion() >= LanguageVersion.CSharp9;
    }
}
