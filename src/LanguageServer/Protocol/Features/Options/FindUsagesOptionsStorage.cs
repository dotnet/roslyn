// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.FindUsages;

internal static class FindUsagesOptionsStorage
{
    extension(IGlobalOptionService globalOptions)
    {
        public FindUsagesOptions GetFindUsagesOptions(string language)
        => new() { ClassificationOptions = globalOptions.GetClassificationOptions(language) };
    }
}
