// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private readonly struct ComparisonOptions(bool ignoreCase, bool ignoreAssemblyKeys)
        {
            [Flags]
            private enum Option : byte
            {
                None = 0x0,
                IgnoreCase = 0x1,
                IgnoreAssemblyKeys = 0x2,
            }

            private readonly Option _flags =
                    BoolToOption(ignoreCase, Option.IgnoreCase) |
                    BoolToOption(ignoreAssemblyKeys, Option.IgnoreAssemblyKeys);

            public bool IgnoreCase => (_flags & Option.IgnoreCase) == Option.IgnoreCase;

            public bool IgnoreAssemblyKey => (_flags & Option.IgnoreAssemblyKeys) == Option.IgnoreAssemblyKeys;

            public byte FlagsValue => (byte)_flags;

            private static Option BoolToOption(bool value, Option option)
                => value ? option : Option.None;
        }
    }
}
