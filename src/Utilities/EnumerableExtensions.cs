// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Builds an ImmutableDictionary mapping of <see cref="ITypeSymbol"/> to objects.
        /// </summary>
        /// <typeparam name="T">Type of mapping's values.</typeparam>
        /// <param name="things">Enumeration of objects.</param>
        /// <param name="wellKnownTypeProvider">Well known type provider for the relevant compilation.</param>
        /// <param name="metadataTypeNameSelector">Function to convert from an object to a type name.</param>
        /// <returns>ImmutableDictionary mapping of <see cref="ITypeSymbol"/> to objects.</returns>
        public static ImmutableDictionary<ITypeSymbol, T> ToBySymbolMap<T>(
            this IEnumerable<T> things,
            WellKnownTypeProvider wellKnownTypeProvider,
            Func<T, string> metadataTypeNameSelector)
        {
            ImmutableDictionary<ITypeSymbol, T>.Builder builder = 
                ImmutableDictionary.CreateBuilder<ITypeSymbol, T>();
            foreach (T thing in things)
            {
                string metadataTypeName = metadataTypeNameSelector(thing);
                if (wellKnownTypeProvider.TryGetTypeByMetadataName(metadataTypeName, out INamedTypeSymbol symbol))
                {
                    builder.Add(symbol, thing);
                }
            }

            return builder.ToImmutable();
        }
    }
}
