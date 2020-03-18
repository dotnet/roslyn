// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal partial class PerLanguageOption2<T>
    {
        public static explicit operator PerLanguageOption<T>(PerLanguageOption2<T> option)
        {
            RoslynDebug.Assert(option != null);

            return new PerLanguageOption<T>(option.OptionDefinition, option.StorageLocations.As<OptionStorageLocation>());
        }
    }
}
