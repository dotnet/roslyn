// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class SqlSinks
    {
        private static Dictionary<string, SinkInfo> InterfaceSinks { get; set; }
        private static Dictionary<string, SinkInfo> ConcreteSinks { get; set; }

        static SqlSinks()
        {
            InterfaceSinks = new Dictionary<string, SinkInfo>(StringComparer.Ordinal);
            ConcreteSinks = new Dictionary<string, SinkInfo>(StringComparer.Ordinal);

            AddInterfaceSink(
                WellKnownTypes.SystemDataIDbCommand,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: new string[] {
                    "CommandText",
                },
                sinkMethodParameters: null);

            AddInterfaceSink(
                WellKnownTypes.SystemDataIDataAdapter,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: null,
                sinkMethodParameters: null);

            AddConcreteSink(
                WellKnownTypes.SystemWebUIWebControlsSqlDataSource,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new string[] {
                    "ConnectionString",
                    "DeleteCommand",
                    "InsertCommand",
                    "SelectCommand",
                    "UpdateCommand",
                },
                sinkMethodParameters: null);
        }

        private static void AddInterfaceSink(
            string fullTypeName,
            bool isAnyStringParameterInConstructorASink,
            IEnumerable<string> sinkProperties,
            IDictionary<string, IEnumerable<string>> sinkMethodParameters)
        {
            SinkInfo sinkInfo = new SinkInfo(
                fullTypeName,
                isInterface: true,
                isAnyStringParameterInConstructorASink: isAnyStringParameterInConstructorASink,
                sinkProperties:
                    sinkProperties != null
                        ? sinkProperties.ToImmutableHashSet()
                        : ImmutableHashSet<string>.Empty,
                sinkMethodParameters:
                    sinkMethodParameters != null
                        ? sinkMethodParameters
                             .Select(kvp => new KeyValuePair<string, ImmutableHashSet<string>>(kvp.Key, kvp.Value.ToImmutableHashSet()))
                             .ToImmutableDictionary()
                        : ImmutableDictionary<string, ImmutableHashSet<string>>.Empty);
            InterfaceSinks.Add(fullTypeName, sinkInfo);
        }

        private static void AddConcreteSink(
            string fullTypeName,
            bool isAnyStringParameterInConstructorASink,
            IEnumerable<string> sinkProperties,
            IDictionary<string, IEnumerable<string>> sinkMethodParameters)
        {
            SinkInfo sinkInfo = new SinkInfo(
                fullTypeName,
                isInterface: false,
                isAnyStringParameterInConstructorASink: isAnyStringParameterInConstructorASink,
                sinkProperties:
                    sinkProperties != null
                        ? sinkProperties.ToImmutableHashSet()
                        : ImmutableHashSet<string>.Empty,
                sinkMethodParameters:
                    sinkMethodParameters != null
                        ? sinkMethodParameters
                             .Select(kvp => new KeyValuePair<string, ImmutableHashSet<string>>(kvp.Key, kvp.Value.ToImmutableHashSet()))
                             .ToImmutableDictionary()
                        : ImmutableDictionary<string, ImmutableHashSet<string>>.Empty);
            ConcreteSinks.Add(fullTypeName, sinkInfo);
        }

        /// <summary>
        /// Determines if tainted data passed as arguments to a method enters a tainted data sink.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known type provider for the compilation being analyzed.</param>
        /// <param name="method">Method being invoked.</param>
        /// <param name="taintedArguments">Arguments passed to the method invocation that are tainted.</param>
        /// <returns>True if any of the tainted data arguments enters a sink,  false otherwise.</returns>
        public static bool IsMethodArgumentASink(WellKnownTypeProvider wellKnownTypeProvider, IMethodSymbol method, IEnumerable<IArgumentOperation> taintedArguments)
        {
            if (method.ContainingType == null || !taintedArguments.Any())
            {
                return false;
            }

            foreach (SinkInfo sinkInfo in GetSinkInfosForType(wellKnownTypeProvider, method.ContainingType))
            {
                if (method.MethodKind == MethodKind.Constructor
                    && sinkInfo.IsAnyStringParameterInConstructorASink
                    && taintedArguments.Any(a => a.Parameter.Type.SpecialType == SpecialType.System_String))
                {
                    return true;
                }

                if (sinkInfo.SinkMethodParameters.TryGetValue(method.MetadataName, out ImmutableHashSet<string> sinkParameters)
                    && taintedArguments.Any(a => sinkParameters.Contains(a.Parameter.MetadataName)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a property is a sink.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known type provider for the compilation being analyzed.</param>
        /// <param name="propertyReferenceOperation">Property to check if it's a sink.</param>
        /// <returns>True if the property is a sink, false otherwise.</returns>
        public static bool IsPropertyASink(WellKnownTypeProvider wellKnownTypeProvider, IPropertyReferenceOperation propertyReferenceOperation)
        {
            foreach (SinkInfo sinkInfo in GetSinkInfosForType(wellKnownTypeProvider, propertyReferenceOperation.Member.ContainingType))
            {
                if (sinkInfo.SinkProperties.Contains(propertyReferenceOperation.Member.MetadataName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a compilation (via its <see cref="WellKnownTypeProvider"/>) references a tainted data sink type.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known type provider for the compilation to check.</param>
        /// <returns>True if the compilation references a tainted data sink type, false otherwise.</returns>
        public static bool DoesCompilationIncludeSinks(WellKnownTypeProvider wellKnownTypeProvider)
        {
            foreach (string interfaceTypeName in InterfaceSinks.Keys)
            {
                if (wellKnownTypeProvider.TryGetType(interfaceTypeName, out INamedTypeSymbol unused))
                {
                    return true;
                }
            }
            
            foreach (string concreteTypeName in ConcreteSinks.Keys)
            {
                if (wellKnownTypeProvider.TryGetType(concreteTypeName, out INamedTypeSymbol unused))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<SinkInfo> GetSinkInfosForType(WellKnownTypeProvider wellKnownTypeProvider, INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol == null)
            {
                yield break;
            }

            foreach (INamedTypeSymbol interfaceSymbol in namedTypeSymbol.AllInterfaces)
            {
                if (!wellKnownTypeProvider.TryGetFullTypeName(interfaceSymbol, out string interfaceFullName)
                    || !InterfaceSinks.TryGetValue(interfaceFullName, out SinkInfo sinkInfo))
                {
                    continue;
                }

                yield return sinkInfo;
            }

            for (INamedTypeSymbol typeSymbol = namedTypeSymbol; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
            {
                if (!wellKnownTypeProvider.TryGetFullTypeName(typeSymbol, out string typeFullName)
                    || !ConcreteSinks.TryGetValue(typeFullName, out SinkInfo sinkInfo))
                {
                    continue;
                }

                yield return sinkInfo;
            }
        }
    }
}
