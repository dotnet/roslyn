// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal partial class CodeStyleOption2<T>
    {
        public static implicit operator CodeStyleOption<T>(CodeStyleOption2<T> option)
        {
            if (option == null)
            {
                return null;
            }

            return new CodeStyleOption<T>(option.Value, (NotificationOption)option.Notification);
        }

        public static implicit operator CodeStyleOption2<T>(CodeStyleOption<T> option)
        {
            if (option == null)
            {
                return null;
            }

            return new CodeStyleOption2<T>(option.Value, (NotificationOption2)option.Notification);
        }
    }
}
