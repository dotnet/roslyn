// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class XPathSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data XPath injection sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static XPathSinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemWebUIPageTheme,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "XPath", new[] { "xPathExpression" }),
                    ( "XPathSelect", ["xPathExpression"]),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemWebUITemplateControl,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "XPath", new[] { "xPathExpression" }),
                    ( "XPathSelect", ["xPathExpression"]),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemWebUIWebControlsXmlDataSource,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "XPath",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemWebUIXPathBinder,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "Eval", new[] { "xPath" }),
                    ( "Select", ["xPath"]),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlSchemaXmlSchemaXPath,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "XPath",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXmlNode,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "SelectNodes", new[] { "xpath" }),
                    ( "SelectSingleNode", ["xpath"]),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXPathXPathExpression,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "Compile", new[] { "xpath" }),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemXmlXPathXPathNavigator,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "Compile", new[] { "xpath" }),
                    ( "Evaluate", ["xpath"]),
                    ( "Matches", ["xpath"]),
                    ( "Select", ["xpath"]),
                    ( "SelectAncestors", ["name"]),
                    ( "SelectChildren", ["name"]),
                    ( "SelectDescendants", ["name"]),
                    ( "SelectSingleNode", ["xpath"]),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
