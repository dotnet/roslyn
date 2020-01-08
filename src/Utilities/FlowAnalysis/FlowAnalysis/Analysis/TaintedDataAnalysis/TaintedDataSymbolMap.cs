// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Mapping of <see cref="ITypeSymbol"/> to <see cref="ITaintedDataInfo"/> (tainted data source/sanitizer/sink info).
    /// </summary>
    internal class TaintedDataSymbolMap<TInfo> : IEquatable<TaintedDataSymbolMap<TInfo>?>
        where TInfo : ITaintedDataInfo
    {
        public TaintedDataSymbolMap(WellKnownTypeProvider wellKnownTypeProvider, IEnumerable<TInfo> taintedDataInfos)
        {
            if (wellKnownTypeProvider == null)
            {
                throw new ArgumentNullException(nameof(wellKnownTypeProvider));
            }

            if (taintedDataInfos == null)
            {
                throw new ArgumentNullException(nameof(taintedDataInfos));
            }

            ImmutableDictionary<ITypeSymbol, TInfo>.Builder concreteInfosBuilder = ImmutableDictionary.CreateBuilder<ITypeSymbol, TInfo>();
            ImmutableDictionary<ITypeSymbol, TInfo>.Builder interfaceInfosBuilder = ImmutableDictionary.CreateBuilder<ITypeSymbol, TInfo>();

            foreach (TInfo info in taintedDataInfos)
            {
                if (wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(info.FullTypeName, out INamedTypeSymbol? namedTypeSymbol))
                {
                    if (info.IsInterface)
                    {
                        interfaceInfosBuilder[namedTypeSymbol] = info;
                    }
                    else
                    {
                        concreteInfosBuilder[namedTypeSymbol] = info;
                    }

                    if (info.RequiresValueContentAnalysis)
                    {
                        RequiresValueContentAnalysis = true;
                    }
                }
            }

            this.ConcreteInfos = concreteInfosBuilder.ToImmutable();
            this.InterfaceInfos = interfaceInfosBuilder.ToImmutable();
        }

        /// <summary>
        /// Mapping for concrete types.
        /// </summary>
        private ImmutableDictionary<ITypeSymbol, TInfo> ConcreteInfos { get; }

        /// <summary>
        /// Mapping for interface types.
        /// </summary>
        private ImmutableDictionary<ITypeSymbol, TInfo> InterfaceInfos { get; }

        /// <summary>
        /// Indicates that this mapping is empty, i.e. there are no types referenced by the compilation represented by the <see cref="WellKnownTypeProvider"/>.
        /// </summary>
        public bool IsEmpty { get { return this.ConcreteInfos.IsEmpty && this.InterfaceInfos.IsEmpty; } }

        /// <summary>
        /// Indicates that any <see cref="ITaintedDataInfo"/> in this <see cref="TaintedDataSymbolMap&lt;TInfo&gt;"/> uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        public bool RequiresValueContentAnalysis { get; }

        /// <summary>
        /// Gets an enumeration of infos for the given type.
        /// </summary>
        /// <param name="namedTypeSymbol">Type to find infos for.</param>
        /// <returns>Relevant infos for the given type.</returns>
        public IEnumerable<TInfo> GetInfosForType(INamedTypeSymbol namedTypeSymbol)
        {
            Debug.Assert(namedTypeSymbol != null);

            if (namedTypeSymbol == null)
            {
                yield break;
            }

            if (!this.InterfaceInfos.IsEmpty)
            {
                if (namedTypeSymbol.TypeKind == TypeKind.Interface
                    && this.InterfaceInfos.TryGetValue(namedTypeSymbol, out TInfo infoForInterfaceSymbol))
                {
                    yield return infoForInterfaceSymbol;
                }

                foreach (INamedTypeSymbol interfaceSymbol in namedTypeSymbol.AllInterfaces)
                {
                    if (this.InterfaceInfos.TryGetValue(interfaceSymbol, out TInfo info))
                    {
                        yield return info;
                    }
                }
            }

            if (!this.ConcreteInfos.IsEmpty)
            {
                foreach (INamedTypeSymbol typeSymbol in namedTypeSymbol.GetBaseTypesAndThis())
                {
                    if (this.ConcreteInfos.TryGetValue(typeSymbol, out TInfo info))
                    {
                        yield return info;
                    }
                }
            }
        }

        public bool Equals(TaintedDataSymbolMap<TInfo>? other)
        {
            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null
                && this.InterfaceInfos == other.InterfaceInfos
                && this.ConcreteInfos == other.ConcreteInfos;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TaintedDataSymbolMap<TInfo>);
        }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(this.InterfaceInfos,
                HashUtilities.Combine(this.ConcreteInfos,
                0));
        }
    }
}
