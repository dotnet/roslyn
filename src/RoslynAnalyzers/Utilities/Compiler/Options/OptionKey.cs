// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Analyzer.Utilities
{
    internal readonly struct OptionKey : IEquatable<OptionKey>
    {
        private static readonly ConcurrentDictionary<(string ruleId, string optionName), OptionKey> s_keys = new();
        private static int s_lastOrdinal;

        private readonly int _ordinal;

        private OptionKey(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _ordinal = Interlocked.Increment(ref s_lastOrdinal);
        }

        public string Name { get; }

        public static OptionKey GetOrCreate(string? ruleId, string optionName)
        {
            return s_keys.GetOrAdd(
                (ruleId ?? "", optionName),
                static pair => new OptionKey(pair.ruleId is not null ? $"{pair.ruleId}.{pair.optionName}" : pair.optionName));
        }

        public static bool operator ==(OptionKey left, OptionKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionKey left, OptionKey right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return obj is OptionKey other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            return _ordinal;
        }

        public bool Equals(OptionKey other)
        {
            return _ordinal == other._ordinal;
        }
    }
}
