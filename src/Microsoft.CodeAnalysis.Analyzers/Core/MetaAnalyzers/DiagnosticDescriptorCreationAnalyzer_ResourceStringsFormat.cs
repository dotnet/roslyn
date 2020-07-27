// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using PooledResourcesDataValueConcurrentDictionary = PooledConcurrentDictionary<string, ImmutableDictionary<string, (string value, Location location)>>;

    public sealed partial class DiagnosticDescriptorCreationAnalyzer
    {
        private static bool HasResxAdditionalFiles(AnalyzerOptions options)
        {
            foreach (var file in options.AdditionalFiles)
            {
                if (string.Equals(".resx", Path.GetExtension(file.Path), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static ImmutableDictionary<string, (string value, Location location)> GetOrCreateResourceMap(
            AnalyzerOptions options,
            string resourceFileName,
            PooledResourcesDataValueConcurrentDictionary resourceMap,
            CancellationToken cancellationToken)
        {
            Debug.Assert(HasResxAdditionalFiles(options));

            resourceFileName += ".resx";
            if (resourceMap.TryGetValue(resourceFileName, out var map))
            {
                return map;
            }

            map = CreateResourceMap(options, resourceFileName, cancellationToken);
            return resourceMap.GetOrAdd(resourceFileName, map);
        }

        private static ImmutableDictionary<string, (string value, Location location)> CreateResourceMap(AnalyzerOptions options, string resourceFileName, CancellationToken cancellationToken)
        {
            foreach (var file in options.AdditionalFiles)
            {
                var fileName = Path.GetFileName(file.Path);
                if (string.Equals(resourceFileName, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return CreateResourceMapForFile(file, cancellationToken);
                }
            }

            return ImmutableDictionary<string, (string value, Location location)>.Empty;

            static ImmutableDictionary<string, (string value, Location location)> CreateResourceMapForFile(AdditionalText file, CancellationToken cancellationToken)
            {
                const string valueTagPrefix = @"<value>";
                const string valueTagSuffix = @"</value>";
                var builder = ImmutableDictionary.CreateBuilder<string, (string value, Location location)>();
                var sourceText = file.GetText(cancellationToken);
                var sourceTextStr = sourceText.ToString();
                var parsedDocument = XDocument.Parse(sourceTextStr, LoadOptions.PreserveWhitespace);
                foreach (var dataElement in parsedDocument.Descendants("data"))
                {
                    if (dataElement.Attribute("name")?.Value is { } name &&
                        dataElement.Elements("value").FirstOrDefault() is { } valueElement)
                    {
                        var dataElementStr = dataElement.ToString();
                        var valueElementStr = valueElement.ToString();
                        var indexOfDataElement = sourceTextStr.IndexOf(dataElementStr, StringComparison.Ordinal);
                        if (indexOfDataElement < 0 ||
                            !valueElementStr.StartsWith(valueTagPrefix, StringComparison.OrdinalIgnoreCase) ||
                            !valueElementStr.EndsWith(valueTagSuffix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var indexOfValue = indexOfDataElement +
                                dataElementStr.IndexOf(valueElementStr, StringComparison.Ordinal) +
                                valueTagPrefix.Length;
                        var valueLength = valueElementStr.Length - (valueTagPrefix.Length + valueTagSuffix.Length);
                        var span = new TextSpan(indexOfValue, valueLength);
                        var linePositionSpan = sourceText.Lines.GetLinePositionSpan(span);
                        var location = Location.Create(file.Path, span, linePositionSpan);

                        var value = valueElementStr.Substring(valueTagPrefix.Length, valueLength);
                        builder[name] = (value, location);
                    }
                }

                return builder.ToImmutable();
            }
        }
    }
}
