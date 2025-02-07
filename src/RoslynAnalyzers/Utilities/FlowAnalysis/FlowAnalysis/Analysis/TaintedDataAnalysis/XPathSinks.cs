// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
