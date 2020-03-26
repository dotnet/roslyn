// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Options
{
    internal partial class Option2<T>
    {
        [return: NotNullIfNotNull("option")]
        public static explicit operator Option<T>?(Option2<T>? option)
        {
            if (option is null)
            {
                return null;
            }

            return new Option<T>(option.OptionDefinition, option.StorageLocations.As<OptionStorageLocation>());
        }
    }
}
