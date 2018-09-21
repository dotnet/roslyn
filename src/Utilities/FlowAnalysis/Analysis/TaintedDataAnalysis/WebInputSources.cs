// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class WebInputSources
    {
        /// <summary>
        /// Info for tainted data sources.
        /// </summary>
        private class SourceInfo
        {
            /// <summary>
            /// Constructs.
            /// </summary>
            /// <param name="fullTypeName">Full type name of the...type (namespace + type).</param>
            /// <param name="taintedProperties">Properties that generate tainted data.</param>
            /// <param name="taintedMethods">Methods that generate tainted data.</param>
            public SourceInfo(string fullTypeName, HashSet<string> taintedProperties, HashSet<string> taintedMethods)
            {
                this.FullTypeName = fullTypeName;
                this.TaintedProperties = taintedProperties;
                this.TaintedMethods = taintedMethods;
            }

            /// <summary>
            /// Full type name of the...type (namespace + type).
            /// </summary>
            public string FullTypeName { get; private set; }

            /// <summary>
            /// Properties that generate tainted data.
            /// </summary>
            public HashSet<string> TaintedProperties { get; private set; }

            /// <summary>
            /// Methods that generate tainted data.
            /// </summary>
            public HashSet<string> TaintedMethods { get; private set; }
        }

        /// <summary>
        /// Metadata for tainted data sources.
        /// </summary>
        /// <remarks>Keys are full type names (namespace + type name), values are the metadatas.</remarks>
        private static Dictionary<string, SourceInfo> SourceInfos { get; set; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static WebInputSources()
        {
            SourceInfos = new Dictionary<string, SourceInfo>(StringComparer.Ordinal);

            AddSourceMetadata(
                WellKnownTypes.SystemWebHttpRequest,
                taintedProperties: new string[] {
                    "AcceptTypes",
                    "AnonymousID",
                    // Anything potentially bad in Browser?
                    "ContentType",
                    "Cookies",
                    "Form",
                    "Headers",
                    "HttpMethod",
                    "InputStream",
                    "Item",
                    "Params",
                    "Path",
                    "PathInfo",
                    "QueryString",
                    "RawUrl",
                    "Url",
                    "UrlReferrer",
                    "UserAgent",
                    "UserLanguages",
                },
                taintedMethods: new string[] {
                    "BinaryRead",
                    "GetBufferedInputStream",
                    "GetBufferlessInputStream",
                });
        }

        private static void AddSourceMetadata(string fullTypeName, IEnumerable<string> taintedProperties, IEnumerable<string> taintedMethods)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                new HashSet<string>(taintedProperties, StringComparer.Ordinal),
                new HashSet<string>(taintedMethods, StringComparer.Ordinal));
            SourceInfos.Add(metadata.FullTypeName, metadata);
        }

        /// <summary>
        /// Determines if the instance property reference generates tainted data.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known types cache.</param>
        /// <param name="propertyReferenceOperation">IOperation representing the property reference.</param>
        /// <returns>True if the property returns tainted data, false otherwise.</returns>
        public static bool IsTaintedProperty(WellKnownTypeProvider wellKnownTypeProvider, IPropertyReferenceOperation propertyReferenceOperation)
        {
            return propertyReferenceOperation != null
                && propertyReferenceOperation.Instance != null
                && propertyReferenceOperation.Member != null
                && wellKnownTypeProvider.TryGetFullTypeName(propertyReferenceOperation.Instance.Type, out string instanceType)
                && SourceInfos.TryGetValue(instanceType, out SourceInfo sourceMetadata)
                && sourceMetadata.TaintedProperties.Contains(propertyReferenceOperation.Member.MetadataName);
        }

        /// <summary>
        /// Determines if the instance method call returns tainted data.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known types cache.</param>
        /// <param name="instance">IOperation representing the instance.</param>
        /// <param name="method">Instance method being called.</param>
        /// <returns>True if the method returns tainted data, false otherwise.</returns>
        public static bool IsTaintedMethod(WellKnownTypeProvider wellKnownTypeProvider, IOperation instance, IMethodSymbol method)
        {
            return instance != null
                && instance.Type != null
                && method != null
                && wellKnownTypeProvider.TryGetFullTypeName(instance.Type, out string instanceType)
                && SourceInfos.TryGetValue(instanceType, out SourceInfo sourceMetadata)
                && sourceMetadata.TaintedMethods.Contains(method.MetadataName);
        }

        /// <summary>
        /// Determines if the compilation (via its <see cref="WellKnownTypeProvider"/>) references a tainted data source type.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known type provider to check.</param>
        /// <returns>True if the compilation references at least one tainted data source type.</returns>
        public static bool DoesCompilationIncludeSources(WellKnownTypeProvider wellKnownTypeProvider)
        {
            foreach (string metadataTypeName in SourceInfos.Keys)
            {
                if (wellKnownTypeProvider.TryGetType(metadataTypeName, out INamedTypeSymbol unused))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
