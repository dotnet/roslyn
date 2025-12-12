// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Mapping of <see cref="ITypeSymbol"/> to <see cref="ITaintedDataInfo"/> (tainted data source/sanitizer/sink info).
    /// </summary>
    internal class TaintedDataSymbolMap<TInfo> : IEquatable<TaintedDataSymbolMap<TInfo>?>
        where TInfo : ITaintedDataInfo
    {
        private static bool TryResolveDependencies(TInfo info, WellKnownTypeProvider wellKnownTypeProvider)
        {
            foreach (string dependency in info.DependencyFullTypeNames)
            {
                if (!wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(dependency, out INamedTypeSymbol? _))
                {
                    return false;
                }
            }

            return true;
        }
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
                if (!TryResolveDependencies(info, wellKnownTypeProvider))
                {
                    continue;
                }

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

                    if (info.RequiresParameterReferenceAnalysis)
                    {
                        RequiresParameterReferenceAnalysis = true;
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
        public bool IsEmpty => this.ConcreteInfos.IsEmpty && this.InterfaceInfos.IsEmpty;

        /// <summary>
        /// Indicates that any <see cref="ITaintedDataInfo"/> in this <see cref="TaintedDataSymbolMap&lt;TInfo&gt;"/> uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        public bool RequiresValueContentAnalysis { get; }

        /// <summary>
        /// Indicates that <see cref="OperationKind.ParameterReference"/> is required.
        /// </summary>
        public bool RequiresParameterReferenceAnalysis { get; }

        /// <summary>
        /// Gets an enumeration of infos for the given type.
        /// </summary>
        /// <param name="namedTypeSymbol">Type to find infos for.</param>
        /// <returns>Relevant infos for the given type.</returns>
        public IEnumerable<TInfo> GetInfosForType(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol == null)
            {
                Debug.Fail("Expected non-null 'namedTypeSymbol'");

                yield break;
            }

            if (!this.InterfaceInfos.IsEmpty)
            {
                if (namedTypeSymbol.TypeKind == TypeKind.Interface
                    && this.InterfaceInfos.TryGetValue(namedTypeSymbol.OriginalDefinition, out var infoForInterfaceSymbol))
                {
                    yield return infoForInterfaceSymbol;
                }

                foreach (INamedTypeSymbol interfaceSymbol in namedTypeSymbol.AllInterfaces)
                {
                    if (this.InterfaceInfos.TryGetValue(interfaceSymbol.OriginalDefinition, out var info))
                    {
                        yield return info;
                    }
                }
            }

            if (!this.ConcreteInfos.IsEmpty)
            {
                foreach (INamedTypeSymbol typeSymbol in namedTypeSymbol.GetBaseTypesAndThis())
                {
                    if (this.ConcreteInfos.TryGetValue(typeSymbol.OriginalDefinition, out var info))
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
            var hashCode = new RoslynHashCode();
            HashUtilities.Combine(this.InterfaceInfos, ref hashCode);
            HashUtilities.Combine(this.ConcreteInfos, ref hashCode);
            return hashCode.ToHashCode();
        }
    }
}
