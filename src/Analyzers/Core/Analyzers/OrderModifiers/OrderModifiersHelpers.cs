// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.CodeAnalysis.OrderModifiers
{
    internal abstract class AbstractOrderModifiersHelpers
    {
        private static readonly char[] s_comma = [','];

        /// <remarks>
        /// Reference type so we can read/write atomically.
        /// </remarks>
        private Tuple<string, Dictionary<int, int>>? _lastParsed;

        protected abstract int GetKeywordKind(string trimmed);

        public static bool IsOrdered(Dictionary<int, int> preferredOrder, SyntaxTokenList modifiers)
        {
            if (modifiers.Count >= 2)
            {
                var lastOrder = int.MinValue;
                foreach (var modifier in modifiers)
                {
                    var currentOrder = preferredOrder.TryGetValue(modifier.RawKind, out var value) ? value : int.MaxValue;
                    if (currentOrder < lastOrder)
                    {
                        return false;
                    }

                    lastOrder = currentOrder;
                }
            }

            return true;
        }

        public bool TryGetOrComputePreferredOrder(string value, [NotNullWhen(true)] out Dictionary<int, int>? preferredOrder)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                preferredOrder = null;
                return false;
            }

            var lastParsed = Volatile.Read(ref _lastParsed);
            if (lastParsed?.Item1 != value)
            {
                if (!TryParse(value, out var parsed))
                {
                    preferredOrder = null;
                    return false;
                }

                lastParsed = Tuple.Create(value, parsed);
                Volatile.Write(ref _lastParsed, lastParsed);
            }

            preferredOrder = lastParsed.Item2;
            return true;
        }

        protected virtual bool TryParse(string value, [NotNullWhen(true)] out Dictionary<int, int>? parsed)
        {
            var result = new Dictionary<int, int>();

            var index = 0;
            foreach (var piece in value.Split(s_comma, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = piece.Trim();
                var kind = GetKeywordKind(trimmed);

                if (kind == 0)
                {
                    parsed = null;
                    return false;
                }

                result[kind] = index;
                index++;
            }

            if (result.Count == 0)
            {
                parsed = null;
                return false;
            }

            parsed = result;
            return true;
        }
    }
}
