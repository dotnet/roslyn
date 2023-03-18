// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: CodeStyleOption<T>

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal sealed partial class CodeStyleOption2<T>
    {
        [return: NotNullIfNotNull(nameof(option))]
        public static explicit operator CodeStyleOption<T>?(CodeStyleOption2<T>? option)
        {
            if (option == null)
            {
                return null;
            }

            return new CodeStyleOption<T>(option.Value, (NotificationOption)option.Notification);
        }

        [return: NotNullIfNotNull(nameof(option))]
        public static explicit operator CodeStyleOption2<T>?(CodeStyleOption<T>? option)
            => option?.UnderlyingOption;
    }
}
