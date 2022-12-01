// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal static class FindUsagesOptionsStorage
    {
        public static FindUsagesOptions GetFindUsagesOptions(this IGlobalOptionService globalOptions, string language)
            => new(ClassificationOptions: globalOptions.GetClassificationOptions(language));
    }
}
