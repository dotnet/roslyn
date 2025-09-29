// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class XmlSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data Xml injection sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static XmlSinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlAttribute,
                SinkKind.Xml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "InnerXml",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlDocument,
                SinkKind.Xml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "InnerXml",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlDocumentFragment,
                SinkKind.Xml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "InnerXml",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlElement,
                SinkKind.Xml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "InnerXml",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlEntity,
                SinkKind.Xml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "InnerXml",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlNode,
                SinkKind.Xml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "InnerXml",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlNotation,
                SinkKind.Xml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "InnerXml",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlTextWriter,
                SinkKind.Xml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "WriteRaw", new[] { "buffer", "data" }),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
