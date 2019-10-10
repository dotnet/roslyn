// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
                        writer.WriteAttributeString("Name", data.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                        break;

                    case SymbolKind.Method:
                    case SymbolKind.Field:
                    case SymbolKind.Event:
                    case SymbolKind.Property:
                        var location = data.Symbol.Locations.First();
                        writer.WriteAttributeString("Name", data.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                        writer.WriteAttributeString("File", location.SourceTree?.FilePath ?? "UNKNOWN");
                        writer.WriteAttributeString("Line", (location.GetLineSpan().StartLinePosition.Line + 1).ToString());
                        break;

                    default:
                        writer.WriteAttributeString("Name", data.Symbol.ToDisplayString());
                        break;
                }
            }

            void writeMetrics()
            {
                writer.WriteStartElement("Metrics");

                WriteMetric("MaintainabilityIndex", data.MaintainabilityIndex.ToString(), writer);
                WriteMetric("CyclomaticComplexity", data.CyclomaticComplexity.ToString(), writer);
                WriteMetric("ClassCoupling", data.CoupledNamedTypes.Count.ToString(), writer);
                if (data.DepthOfInheritance.HasValue)
                {
                    WriteMetric("DepthOfInheritance", data.DepthOfInheritance.Value.ToString(), writer);
                }

                // For legacy mode, output only ExecutableLinesOfCode
                // For non-legacy mode, output both SourceLinesOfCode and ExecutableLinesOfCode
#if LEGACY_CODE_METRICS_MODE
                WriteMetric("LinesOfCode", data.ExecutableLines.ToString(), writer);
#else
                WriteMetric("SourceLines", data.SourceLines.ToString(), writer);
                WriteMetric("ExecutableLines", data.ExecutableLines.ToString(), writer);
#endif
                writer.WriteEndElement();
            }

            void writeChildren()
            {
                if (data.Children.Length == 0)
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
