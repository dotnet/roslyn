// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.EndConstructGeneration
{
    internal static class EndConstructGenerationOptions
    {
        public static readonly PerLanguageOption2<bool> EndConstruct = new("visual_basic_end_construct_generation_options_end_construct", defaultValue: true);
    }
}
