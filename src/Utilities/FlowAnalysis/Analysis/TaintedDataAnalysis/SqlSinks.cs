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
        public static ImmutableDictionary<string, SinkInfo> InterfaceSinks { get; }
        public static ImmutableDictionary<string, SinkInfo> ConcreteSinks { get; }

        static SqlSinks()
        {
            ImmutableDictionary<string, SinkInfo>.Builder interfaceSinksBuilder = 
                ImmutableDictionary.CreateBuilder<string, SinkInfo>(StringComparer.Ordinal);
            ImmutableDictionary<string, SinkInfo>.Builder concreteSinksBuilder = 
                ImmutableDictionary.CreateBuilder<string, SinkInfo>(StringComparer.Ordinal);

            AddSink(
                interfaceSinksBuilder,
                WellKnownTypes.SystemDataIDbCommand,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: new string[] {
                    "CommandText",
                },
                sinkMethodParameters: null);

            AddSink(
                interfaceSinksBuilder,
                WellKnownTypes.SystemDataIDataAdapter,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: null,
                sinkMethodParameters: null);

            AddSink(
                concreteSinksBuilder,
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

            InterfaceSinks = interfaceSinksBuilder.ToImmutable();
            ConcreteSinks = concreteSinksBuilder.ToImmutable();
        }

        private static void AddSink(
            ImmutableDictionary<string, SinkInfo>.Builder builder,
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
            builder.Add(fullTypeName, sinkInfo);
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
    }
}
