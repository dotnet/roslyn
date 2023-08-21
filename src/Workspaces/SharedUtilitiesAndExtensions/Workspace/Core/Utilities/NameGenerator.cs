// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class NameGenerator
    {
        /// <summary>
        /// Transforms baseName into a name that does not conflict with any name in 'reservedNames'
        /// </summary>
        public static string EnsureUniqueness(
            string baseName,
            IEnumerable<string> reservedNames,
            bool isCaseSensitive = true)
        {
            using var namesDisposer = ArrayBuilder<string>.GetInstance(out var names);
            using var isFixedDisposer = ArrayBuilder<bool>.GetInstance(out var isFixed);
            using var nameSetDisposer = PooledHashSet<string>.GetInstance(out var nameSet);

            names.Add(baseName);
            isFixed.Add(false);

            foreach (var reservedName in reservedNames)
            {
                if (nameSet.Add(reservedName))
                {
                    names.Add(reservedName);
                    isFixed.Add(true);
                }
            }

            EnsureUniquenessInPlace(names, isFixed, isCaseSensitive: isCaseSensitive);
            return names.First();
        }

        public static ImmutableArray<string> EnsureUniqueness(
            ImmutableArray<string> names,
            Func<string, bool>? canUse = null,
            bool isCaseSensitive = true)
        {
            using var isFixedDisposer = ArrayBuilder<bool>.GetInstance(names.Length, fillWithValue: false, out var isFixed);

            var result = ArrayBuilder<string>.GetInstance(names.Length);
            result.AddRange(names);
            EnsureUniquenessInPlace(result, isFixed, canUse, isCaseSensitive);
            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Ensures that any 'names' is unique and does not collide with any other name.  Names that
        /// are marked as IsFixed can not be touched.  This does mean that if there are two names
        /// that are the same, and both are fixed that you will end up with non-unique names at the
        /// end.
        /// </summary>
        public static ImmutableArray<string> EnsureUniqueness(
            ImmutableArray<string> names,
            ImmutableArray<bool> isFixed,
            Func<string, bool>? canUse = null,
            bool isCaseSensitive = true)
        {
            using var _1 = ArrayBuilder<bool>.GetInstance(names.Length, out var isFixedBuilder);
            using var _2 = ArrayBuilder<string>.GetInstance(names.Length, out var result);

            isFixedBuilder.AddRange(isFixed);
            result.AddRange(names);

            EnsureUniquenessInPlace(result, isFixedBuilder, canUse, isCaseSensitive);

            return result.ToImmutableAndClear();
        }

        /// <summary>
        /// Updates the names in <paramref name="names"/> to be unique.  A name at a particular
        /// index <c>i</c> will not be touched if <c>isFixed[i]</c> is <see langword="true"/>. All
        /// other names will not collide with any other in <paramref name="names"/> and will all
        /// return <see langword="true"/> for <c>canUse(name)</c>.
        /// </summary>
        public static void EnsureUniquenessInPlace(
            ArrayBuilder<string> names,
            ArrayBuilder<bool> isFixed,
            Func<string, bool>? canUse = null,
            bool isCaseSensitive = true)
        {
            canUse ??= Functions<string>.True;

            using var _ = ArrayBuilder<int>.GetInstance(out var collisionIndices);

            // Don't enumerate as we will be modifying the collection in place.
            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i];
                FillCollisionIndices(names, name, isCaseSensitive, collisionIndices);

                if (canUse(name) && collisionIndices.Count < 2)
                {
                    // no problems with this parameter name, move onto the next one.
                    continue;
                }

                HandleCollisions(names, isFixed, name, canUse, isCaseSensitive, collisionIndices);
            }
        }

        private static void HandleCollisions(
            ArrayBuilder<string> names,
            ArrayBuilder<bool> isFixed,
            string name,
            Func<string, bool> canUse,
            bool isCaseSensitive,
            ArrayBuilder<int> collisionIndices)
        {
            var suffix = 1;
            var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            for (var i = 0; i < collisionIndices.Count; i++)
            {
                var collisionIndex = collisionIndices[i];
                if (isFixed[collisionIndex])
                {
                    // can't do anything about this name.
                    continue;
                }

                while (true)
                {
                    var newName = name + suffix++;
                    if (!names.Contains(newName, comparer) && canUse(newName))
                    {
                        // Found a name that doesn't conflict with anything else.
                        names[collisionIndex] = newName;
                        break;
                    }
                }
            }
        }

        private static void FillCollisionIndices(
            ArrayBuilder<string> names,
            string name,
            bool isCaseSensitive,
            ArrayBuilder<int> collisionIndices)
        {
            collisionIndices.Clear();

            var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            for (int i = 0, n = names.Count; i < n; i++)
            {
                if (comparer.Equals(names[i], name))
                {
                    collisionIndices.Add(i);
                }
            }
        }

        public static string GenerateUniqueName(string baseName, Func<string, bool> canUse)
            => GenerateUniqueName(baseName, string.Empty, canUse);

        public static string GenerateUniqueName(string baseName, ISet<string> names, StringComparer comparer)
            => GenerateUniqueName(baseName, x => !names.Contains(x, comparer));

        public static string GenerateUniqueName(string baseName, string extension, Func<string, bool> canUse)
        {
            if (!string.IsNullOrEmpty(extension) && extension[0] != '.')
            {
                extension = "." + extension;
            }

            var name = baseName + extension;
            var index = 1;

            // Check for collisions
            while (!canUse(name))
            {
                name = baseName + index + extension;
                index++;
            }

            return name;
        }

        public static string GenerateUniqueName(IEnumerable<string> baseNames, Func<string, bool> canUse)
        {
            int? index = null;

            while (true)
            {
                foreach (var name in baseNames)
                {
                    var modifiedName = name + index;

                    if (canUse(modifiedName))
                        return modifiedName;
                }

                index = index is null ? 1 : index + 1;
            }
        }
    }
}
