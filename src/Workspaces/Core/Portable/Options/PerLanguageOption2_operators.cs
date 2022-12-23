// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Options
{
    internal partial class PerLanguageOption2<T>
    {
        [return: NotNullIfNotNull(nameof(option))]
        public static explicit operator PerLanguageOption<T>?(PerLanguageOption2<T>? option)
        {
            if (option is null)
            {
                return null;
            }

            return new PerLanguageOption<T>(option.OptionDefinition, ((IOption2)option).StorageLocations);
        }
    }
}
