﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

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
                    ( "XPathSelect", new[] { "xPathExpression" }),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemWebUITemplateControl,
                SinkKind.XPath,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "XPath", new[] { "xPathExpression" }),
                    ( "XPathSelect", new[] { "xPathExpression" }),
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
                    ( "Select", new[] { "xPath" }),
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
                    ( "SelectSingleNode", new[] { "xpath" }),
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
                    ( "Evaluate", new[] { "xpath" }),
                    ( "Matches", new[] { "xpath" }),
                    ( "Select", new[] { "xpath" }),
                    ( "SelectAncestors", new[] { "name" }),
                    ( "SelectChildren", new[] { "name" }),
                    ( "SelectDescendants", new[] { "name" }),
                    ( "SelectSingleNode", new[] { "xpath" }),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
