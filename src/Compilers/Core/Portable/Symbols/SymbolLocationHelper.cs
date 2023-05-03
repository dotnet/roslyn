// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Symbols;

internal static class SymbolLocationHelper
{
    public static class Empty
    {
        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for an empty list.
        /// </summary>
        public static int LocationsCount => 0;

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.GetCurrentLocation(int, int)"/> for an empty list.
        /// </summary>
        public static Location GetCurrentLocation(int slot, int index)
        {
            throw ExceptionUtilities.UnexpectedValue((slot, index));
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.MoveNextLocation(int, int)"/> for an empty list.
        /// </summary>
        /// <remarks>
        /// A slot of <c>-1</c> means start at the beginning.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                case 0:
                    return (false, 0, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for an empty list.
        /// </summary>
        /// <remarks>
        /// A slot of <see cref="int.MaxValue"/> means start from the end.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                case 0:
                    return (false, 0, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
    }

    public static class EmptyOrSingle
    {
        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for an empty or single-element list.
        /// </summary>
        public static int LocationsCount(Location? location) => location is not null ? 1 : 0;

        public static int LocationsCount(SyntaxNode? location) => location is not null ? 1 : 0;

        public static int LocationsCount(SyntaxReference? location) => location is not null ? 1 : 0;

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.GetCurrentLocation(int, int)"/> for an empty or single-element list.
        /// </summary>
        public static Location GetCurrentLocation(int slot, int index, Location? location)
        {
            return slot switch
            {
                0 when location is not null => location,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        public static Location GetCurrentLocation(int slot, int index, SyntaxNode? location)
        {
            return slot switch
            {
                0 when location is not null => location.GetLocation(),
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        public static Location GetCurrentLocation(int slot, int index, SyntaxReference? location)
        {
            return slot switch
            {
                0 when location is not null => location.GetLocation(),
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.MoveNextLocation(int, int)"/> for an empty or single-element list.
        /// </summary>
        /// <remarks>
        /// A slot of <c>-1</c> means start at the beginning.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex, Location? location)
        {
            switch (previousSlot)
            {
                case -1:
                    if (location != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex, SyntaxNode? location)
        {
            switch (previousSlot)
            {
                case -1:
                    if (location != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex, SyntaxReference? location)
        {
            switch (previousSlot)
            {
                case -1:
                    if (location != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for an empty or single-element list.
        /// </summary>
        /// <remarks>
        /// A slot of <see cref="int.MaxValue"/> means start from the end.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex, Location? location)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (location != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex, SyntaxNode? location)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (location != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex, SyntaxReference? location)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (location != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
    }

    public static class Single
    {
        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for a single element list.
        /// </summary>
        public static int LocationsCount => 1;

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.GetCurrentLocation(int, int)"/> for a single element list.
        /// </summary>
        public static Location GetCurrentLocation(int slot, int index, Location location)
        {
            return slot switch
            {
                0 => location,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        public static Location GetCurrentLocation(int slot, int index, SyntaxNode location)
        {
            return slot switch
            {
                0 => location.GetLocation(),
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        public static Location GetCurrentLocation(int slot, int index, SyntaxReference location)
        {
            return slot switch
            {
                0 => location.GetLocation(),
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        public static Location GetCurrentLocation(int slot, int index, SyntaxToken location)
        {
            return slot switch
            {
                0 => location.GetLocation(),
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        public static Location GetCurrentLocation(int slot, int index, SyntaxNodeOrToken location)
        {
            return slot switch
            {
                0 => location.GetLocation()!,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        public static Location GetCurrentLocation(int slot, int index, SyntaxTree tree, TextSpan span)
        {
            return slot switch
            {
                0 => tree.GetLocation(span),
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.MoveNextLocation(int, int)"/> for a single element list.
        /// </summary>
        /// <remarks>
        /// A slot of <c>-1</c> means start at the beginning.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    return (true, 0, 0);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for a single element list.
        /// </summary>
        /// <remarks>
        /// A slot of <see cref="int.MaxValue"/> means start from the end.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    return (true, 0, 0);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
    }

    public static class Many
    {
        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for a cached list.
        /// </summary>
        public static int LocationsCount(ImmutableArray<Location> locations) => locations.Length;

        public static int LocationsCount<T>(ImmutableArray<T> locations) => locations.Length;

        public static int LocationsCount<T>(ImmutableArray<T> locationsContainer, Func<T, int> containerCount) => locationsContainer.Sum(containerCount);

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.GetCurrentLocation(int, int)"/> for a cached list.
        /// </summary>
        public static Location GetCurrentLocation(int slot, int index, ImmutableArray<Location> locations)
        {
            if (unchecked((uint)slot) >= locations.Length)
                throw ExceptionUtilities.UnexpectedValue((slot, index));

            return locations[slot];
        }

        public static Location GetCurrentLocation<T>(int slot, int index, ImmutableArray<T> locations, Func<T, Location> selector)
        {
            if (unchecked((uint)slot) >= locations.Length)
                throw ExceptionUtilities.UnexpectedValue((slot, index));

            return selector(locations[slot]);
        }

        public static Location GetCurrentLocation<T>(int slot, int index, ImmutableArray<T> locationsContainer, Func<T, int, Location> selector)
        {
            if (unchecked((uint)slot) >= locationsContainer.Length)
                throw ExceptionUtilities.UnexpectedValue((slot, index));

            return selector(locationsContainer[slot], index);
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.MoveNextLocation(int, int)"/> for a cached list.
        /// </summary>
        /// <remarks>
        /// A slot of <c>-1</c> means start at the beginning.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex, ImmutableArray<Location> locations)
        {
            if (previousSlot >= -1 && previousSlot < locations.Length - 1)
            {
                return (true, previousSlot + 1, 0);
            }
            else if (previousSlot == locations.Length - 1 || previousSlot == locations.Length)
            {
                return (false, locations.Length, 0);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation<T>(int previousSlot, int previousIndex, ImmutableArray<T> locations)
        {
            if (previousSlot >= -1 && previousSlot < locations.Length - 1)
            {
                return (true, previousSlot + 1, 0);
            }
            else if (previousSlot == locations.Length - 1 || previousSlot == locations.Length)
            {
                return (false, locations.Length, 0);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }

        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation<T>(int previousSlot, int previousIndex, ImmutableArray<T> locationsContainer, Func<T, int, (bool hasNext, int nextSlot, int nextIndex)> moveNextLocationWithinContainer)
        {
            while (true)
            {
                if (previousSlot == -1)
                {
                    // If locationsContainer is empty, we move to slot 0 which leads to a false return. Otherwise, on
                    // the next iteration this becomes a request to start at the first item of the first collection.
                    previousSlot = 0;
                    previousIndex = -1;
                    continue;
                }
                else if (previousSlot >= 0 && previousSlot < locationsContainer.Length)
                {
                    var nestedMove = moveNextLocationWithinContainer(locationsContainer[previousSlot], previousIndex);
                    if (nestedMove.hasNext)
                    {
                        if (nestedMove.nextIndex != 0)
                            throw new InvalidOperationException();

                        return (true, previousSlot, nextIndex: nestedMove.nextSlot);
                    }
                    else
                    {
                        // We reached the end of the current slot. Continue with the next slot.
                        previousSlot++;
                        previousIndex = -1;
                        continue;
                    }
                }
                else if (previousSlot == locationsContainer.Length)
                {
                    return (false, locationsContainer.Length, 0);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
                }
            }
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for a cached list.
        /// </summary>
        /// <remarks>
        /// A slot of <see cref="int.MaxValue"/> means start from the end.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed<T>(int previousSlot, int previousIndex, ImmutableArray<T> locationsContainer, Func<T, int, (bool hasNext, int nextSlot, int nextIndex)> moveNextLocationReversedWithinContainer)
        {
            while (true)
            {
                switch (previousSlot)
                {
                    case int.MaxValue:
                        // Move to the end of the last collection in the container.
                        previousSlot = locationsContainer.Length - 1;
                        previousIndex = int.MaxValue;
                        continue;
                    case >= 0 when previousSlot < locationsContainer.Length:
                        var nestedMove = moveNextLocationReversedWithinContainer(locationsContainer[previousSlot], previousIndex);
                        if (nestedMove.hasNext)
                        {
                            if (nestedMove.nextIndex != 0)
                                throw new InvalidOperationException();

                            return (true, previousSlot, nextIndex: nestedMove.nextSlot);
                        }
                        else
                        {
                            // We reached the end of the current slot (in reverse). Continue with the next slot.
                            previousSlot--;
                            previousIndex = int.MaxValue;
                            continue;
                        }
                    case -1:
                        return (false, -1, 0);
                    default:
                        throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
                }
            }
        }

        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed<T>(int previousSlot, int previousIndex, ImmutableArray<T> locations)
        {
            switch (previousSlot)
            {
                case int.MaxValue when !locations.IsEmpty:
                    return (true, locations.Length - 1, 0);
                case > 0 when previousSlot < locations.Length:
                    return (true, previousSlot - 1, 0);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
    }

    public static class Filtered
    {
        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for a filtered view of a cached list.
        /// </summary>
        public static int LocationsCount<T>(ImmutableArray<T> locations, Func<T, bool> predicate) => locations.Count(predicate);

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.GetCurrentLocation(int, int)"/> for a filtered view of a cached list.
        /// </summary>
        public static Location GetCurrentLocation<T>(int slot, int index, ImmutableArray<T> locations, Func<T, Location> selector)
        {
            if (unchecked((uint)slot) >= locations.Length)
                throw ExceptionUtilities.UnexpectedValue((slot, index));

            return selector(locations[slot]);
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.MoveNextLocation(int, int)"/> for a filtered view of a cached list.
        /// </summary>
        /// <remarks>
        /// A slot of <c>-1</c> means start at the beginning.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation<T>(int previousSlot, int previousIndex, ImmutableArray<T> locations, Func<T, bool> predicate)
        {
            while (true)
            {
                if (previousSlot >= -1 && previousSlot < locations.Length - 1)
                {
                    if (predicate(locations[previousSlot + 1]))
                    {
                        return (true, previousSlot + 1, 0);
                    }
                    else
                    {
                        previousSlot++;
                        continue;
                    }
                }
                else if (previousSlot == locations.Length - 1 || previousSlot == locations.Length)
                {
                    return (false, locations.Length, 0);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
                }
            }
        }

        /// <summary>
        /// Provides the implementation for <see cref="ISymbolInternal.LocationsCount"/> for a filtered view of a cached list.
        /// </summary>
        /// <remarks>
        /// A slot of <see cref="int.MaxValue"/> means start from the end.
        /// </remarks>
        public static (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed<T>(int previousSlot, int previousIndex, ImmutableArray<T> locations, Func<T, bool> predicate)
        {
            while (true)
            {
                switch (previousSlot)
                {
                    case int.MaxValue when !locations.IsEmpty:
                        if (predicate(locations[locations.Length - 1]))
                        {
                            return (true, locations.Length - 1, 0);
                        }
                        else
                        {
                            previousSlot = locations.Length - 1;
                            continue;
                        }
                    case > 0 when previousSlot < locations.Length:
                        if (predicate(locations[previousSlot - 1]))
                        {
                            return (true, previousSlot - 1, 0);
                        }
                        else
                        {
                            previousSlot--;
                            continue;
                        }
                    case 0:
                    case -1:
                        return (false, -1, 0);
                    default:
                        throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
                }
            }
        }
    }
}
