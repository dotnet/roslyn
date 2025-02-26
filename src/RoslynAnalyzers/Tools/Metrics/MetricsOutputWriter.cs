﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;

namespace Metrics
{
    internal static class MetricsOutputWriter
    {
        private const string Version = "1.0";

        public static void WriteMetricFile(ImmutableArray<(string, CodeAnalysisMetricData)> data, XmlTextWriter writer)
        {
            writer.Formatting = Formatting.Indented;

            writer.WriteStartDocument();
            writer.WriteStartElement("CodeMetricsReport");
            writer.WriteAttributeString("Version", Version);
            writer.WriteStartElement("Targets");
            writeMetrics();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();

            return;

            void writeMetrics()
            {
                foreach (var kvp in data)
                {
                    string filePath = kvp.Item1;
                    CodeAnalysisMetricData metric = kvp.Item2;

                    writer.WriteStartElement("Target");
                    writer.WriteAttributeString("Name", Path.GetFileName(filePath));

                    WriteMetricData(metric, writer);

                    writer.WriteEndElement();
                }
            }
        }

        private static void WriteMetricData(CodeAnalysisMetricData data, XmlTextWriter writer)
        {
            writeHeader();
            writeMetrics();
            writeChildren();
            writer.WriteEndElement();

            return;

            void writeHeader()
            {
                writer.WriteStartElement(data.Symbol.Kind.ToString());
                switch (data.Symbol.Kind)
                {
                    case SymbolKind.NamedType:
                        var minimalTypeName = new StringBuilder(data.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

                        var containingType = data.Symbol.ContainingType;
                        while (containingType != null)
                        {
                            minimalTypeName.Insert(0, ".");
                            minimalTypeName.Insert(0, containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                            containingType = containingType.ContainingType;
                        }

                        writer.WriteAttributeString("Name", minimalTypeName.ToString());
                        break;

                    case SymbolKind.Method:
                    case SymbolKind.Field:
                    case SymbolKind.Event:
                    case SymbolKind.Property:
                        var location = data.Symbol.Locations.First();
                        writer.WriteAttributeString("Name", data.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                        writer.WriteAttributeString("File", location.SourceTree?.FilePath ?? "UNKNOWN");
                        writer.WriteAttributeString("Line", (location.GetLineSpan().StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture));
                        break;

                    default:
                        writer.WriteAttributeString("Name", data.Symbol.ToDisplayString());
                        break;
                }
            }

            void writeMetrics()
            {
                writer.WriteStartElement("Metrics");

                WriteMetric("MaintainabilityIndex", data.MaintainabilityIndex.ToString(CultureInfo.InvariantCulture), writer);
                WriteMetric("CyclomaticComplexity", data.CyclomaticComplexity.ToString(CultureInfo.InvariantCulture), writer);
                WriteMetric("ClassCoupling", data.CoupledNamedTypes.Count.ToString(CultureInfo.InvariantCulture), writer);
                if (data.DepthOfInheritance.HasValue)
                {
                    WriteMetric("DepthOfInheritance", data.DepthOfInheritance.Value.ToString(CultureInfo.InvariantCulture), writer);
                }

                // For legacy mode, output only ExecutableLinesOfCode
                // For non-legacy mode, output both SourceLinesOfCode and ExecutableLinesOfCode
#if LEGACY_CODE_METRICS_MODE
                WriteMetric("LinesOfCode", data.ExecutableLines.ToString(CultureInfo.InvariantCulture), writer);
#else
                WriteMetric("SourceLines", data.SourceLines.ToString(CultureInfo.InvariantCulture), writer);
                WriteMetric("ExecutableLines", data.ExecutableLines.ToString(CultureInfo.InvariantCulture), writer);
#endif
                writer.WriteEndElement();
            }

            void writeChildren()
            {
                if (data.Children.IsEmpty)
                {
                    return;
                }

                bool needsEndElement;
                switch (data.Symbol.Kind)
                {
                    case SymbolKind.Assembly:
                        writer.WriteStartElement("Namespaces");
                        needsEndElement = true;
                        break;

                    case SymbolKind.Namespace:
                        writer.WriteStartElement("Types");
                        needsEndElement = true;
                        break;

                    case SymbolKind.NamedType:
                        writer.WriteStartElement("Members");
                        needsEndElement = true;
                        break;

                    case SymbolKind.Property:
                    case SymbolKind.Event:
                        writer.WriteStartElement("Accessors");
                        needsEndElement = true;
                        break;

                    default:
                        needsEndElement = false;
                        break;
                }

                foreach (var child in data.Children)
                {
                    WriteMetricData(child, writer);
                }

                if (needsEndElement)
                {
                    writer.WriteEndElement();
                }
            }
        }

        private static void WriteMetric(string name, string value, XmlTextWriter writer)
        {
            writer.WriteStartElement("Metric");
            writer.WriteAttributeString("Name", name);
            writer.WriteAttributeString("Value", value);
            writer.WriteEndElement();
        }
    }
}
