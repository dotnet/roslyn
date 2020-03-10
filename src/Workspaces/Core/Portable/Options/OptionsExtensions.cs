// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class OptionsExtensions
    {
        public static Option<CodeStyleOption<T>> ToPublicOption<T>(this Option2<CodeStyleOption2<T>> option)
        {
            RoslynDebug.Assert(option != null);

            return new Option<CodeStyleOption<T>>(option.Feature, option.Group, option.Name,
                option.DefaultValue, option.StorageLocations.As<OptionStorageLocation>());
        }

        public static PerLanguageOption<CodeStyleOption<T>> ToPublicOption<T>(this PerLanguageOption2<CodeStyleOption2<T>> option)
        {
            RoslynDebug.Assert(option != null);

            return new PerLanguageOption<CodeStyleOption<T>>(option.Feature, option.Group, option.Name,
                option.DefaultValue, option.StorageLocations.As<OptionStorageLocation>());
        }
    }
}
