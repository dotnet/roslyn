// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    internal struct OptionKey : IEquatable<OptionKey>
    {
        public IOption Option { get; }

        public OptionKey(IOption option)
        {
            this.Option = option ?? throw new ArgumentNullException(nameof(option));
        }

        public override bool Equals(object obj)
        {
            return obj is OptionKey key &&
                   Equals(key);
        }

        public bool Equals(OptionKey other)
        {
            return Option == other.Option;
        }

        public override int GetHashCode()
        {
            return Option.GetHashCode();
        }

        public override string ToString()
        {
            if (Option is null)
            {
                return "";
            }

            return Option.ToString();
        }

        public static bool operator ==(OptionKey left, OptionKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionKey left, OptionKey right)
        {
            return !left.Equals(right);
        }
    }
}
