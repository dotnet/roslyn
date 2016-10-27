// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private struct ComparisonOptions
        {
            [Flags]
            private enum Option : byte
            {
                None = 0x0,
                IgnoreCase = 0x1,
                IgnoreAssemblyKeys = 0x2,
            }

            private readonly Option _flags;

            public ComparisonOptions(bool ignoreCase, bool ignoreAssemblyKeys)
            {
                _flags =
                    BoolToOption(ignoreCase, Option.IgnoreCase) |
                    BoolToOption(ignoreAssemblyKeys, Option.IgnoreAssemblyKeys);
            }

            public bool IgnoreCase => (_flags & Option.IgnoreCase) == Option.IgnoreCase;

            public bool IgnoreAssemblyKey => (_flags & Option.IgnoreAssemblyKeys) == Option.IgnoreAssemblyKeys;

            public byte FlagsValue => (byte)_flags;

            private static Option BoolToOption(bool value, Option option)
            {
                return value ? option : Option.None;
            }
        }
    }
}