// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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
                sinkMethodParameters: new Dictionary<string, IEnumerable<string>>()
                {
                });
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
