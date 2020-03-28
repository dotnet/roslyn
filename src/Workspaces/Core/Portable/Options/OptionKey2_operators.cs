// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Options
{
    internal readonly partial struct OptionKey2
    {
        public static explicit operator OptionKey(OptionKey2 optionKey)
            => new OptionKey(optionKey.Option, optionKey.Language);
    }
}
