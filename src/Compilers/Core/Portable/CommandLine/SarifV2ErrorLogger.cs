// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used for logging compiler diagnostics to a stream in the standardized SARIF
    /// (Static Analysis Results Interchange Format) v2.1.0 format.
    /// http://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html
    /// </summary>
    internal sealed class SarifV2ErrorLogger : SarifErrorLogger, IDisposable
    {
        private readonly DiagnosticDescriptorSet _descriptors;
        private readonly HashSet<string> _diagnosticIdsWithAnySourceSuppressions;

        private readonly string _toolName;
        private readonly string _toolFileVersion;
        private readonly Version _toolAssemblyVersion;

        private string? _totalAnalyzerExecutionTime;

        public SarifV2ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
            : base(stream, culture)
        {
            _descriptors = new DiagnosticDescriptorSet();
            _diagnosticIdsWithAnySourceSuppressions = new HashSet<string>();

            _toolName = toolName;
            _toolFileVersion = toolFileVersion;
            _toolAssemblyVersion = toolAssemblyVersion;

            _writer.WriteObjectStart(); // root
            _writer.Write("$schema", "http://json.schemastore.org/sarif-2.1.0");
            _writer.Write("version", "2.1.0");
            _writer.WriteArrayStart("runs");
            _writer.WriteObjectStart(); // run

            _writer.WriteArrayStart("results");
        }

        protected override string PrimaryLocationPropertyName => "physicalLocation";

        public override void LogDiagnostic(Diagnostic diagnostic, SuppressionInfo? suppressionInfo)
        {
            _writer.WriteObjectStart(); // result
            _writer.Write("ruleId", diagnostic.Id);
            int ruleIndex = _descriptors.Add(diagnostic.Descriptor);
            _writer.Write("ruleIndex", ruleIndex);

            _writer.Write("level", GetLevel(diagnostic.Severity));

            string? message = diagnostic.GetMessage(_culture);
            if (!RoslynString.IsNullOrEmpty(message))
            {
                _writer.WriteObjectStart("message");
                _writer.Write("text", message);
                _writer.WriteObjectEnd();
            }

            if (diagnostic.IsSuppressed)
            {
                _diagnosticIdsWithAnySourceSuppressions.Add(diagnostic.Id);

                _writer.WriteArrayStart("suppressions");
                _writer.WriteObjectStart(); // suppression
                _writer.Write("kind", "inSource");
                string? justification = suppressionInfo?.Attribute?.DecodeNamedArgument<string>("Justification", SpecialType.System_String);
                if (justification != null)
                {
                    _writer.Write("justification", justification);
                }

                string? suppressionType = null;
                if (diagnostic.ProgrammaticSuppressionInfo is { } programmaticSuppressionInfo)
                {
                    var suppressionsStr = programmaticSuppressionInfo.Suppressions
                        .OrderBy(idAndJustification => idAndJustification.Id)
                        .Select(idAndJustification => $"Suppression Id: {idAndJustification.Id}, Suppression Justification: {idAndJustification.Justification}")
                        .Join(", ");
                    suppressionType = $"DiagnosticSuppressor {{ {suppressionsStr} }}";
                }
                else if (suppressionInfo != null)
                {
                    suppressionType = suppressionInfo.Attribute != null ? "SuppressMessageAttribute" : "Pragma Directive";
                }

                if (suppressionType != null)
                {
                    _writer.WriteObjectStart("properties");

                    _writer.Write("suppressionType", suppressionType);

                    _writer.WriteObjectEnd(); // properties
                }

                _writer.WriteObjectEnd(); // suppression
                _writer.WriteArrayEnd();
            }

            WriteLocations(diagnostic.Location, diagnostic.AdditionalLocations);

            WriteResultProperties(diagnostic);

            _writer.WriteObjectEnd(); // result
        }

        public override void AddAnalyzerDescriptorsAndExecutionTime(ImmutableArray<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> descriptors, double totalAnalyzerExecutionTime)
        {
            foreach (var (descriptor, info) in descriptors.OrderBy(d => d.Descriptor.Id))
            {
                _descriptors.Add(descriptor, info);
            }

            _totalAnalyzerExecutionTime = ReportAnalyzerUtil.GetFormattedAnalyzerExecutionTime(totalAnalyzerExecutionTime, _culture).Trim();
        }

        private void WriteLocations(Location location, IReadOnlyList<Location> additionalLocations)
        {
            if (HasPath(location))
            {
                _writer.WriteArrayStart("locations");
                _writer.WriteObjectStart(); // location
                _writer.WriteKey(PrimaryLocationPropertyName);

                WritePhysicalLocation(location);

                _writer.WriteObjectEnd(); // location
                _writer.WriteArrayEnd(); // locations
            }

            // See https://github.com/dotnet/roslyn/issues/11228 for discussion around
            // whether this is the correct treatment of Diagnostic.AdditionalLocations
            // as SARIF relatedLocations.
            if (additionalLocations != null &&
                additionalLocations.Count > 0 &&
                additionalLocations.Any(l => HasPath(l)))
            {
                _writer.WriteArrayStart("relatedLocations");

                foreach (var additionalLocation in additionalLocations)
                {
                    if (HasPath(additionalLocation))
                    {
                        _writer.WriteObjectStart(); // annotatedCodeLocation
                        _writer.WriteKey("physicalLocation");

                        WritePhysicalLocation(additionalLocation);

                        _writer.WriteObjectEnd(); // annotatedCodeLocation
                    }
                }

                _writer.WriteArrayEnd(); // relatedLocations
            }
        }

        protected override void WritePhysicalLocation(Location diagnosticLocation)
        {
            Debug.Assert(HasPath(diagnosticLocation));

            FileLinePositionSpan span = diagnosticLocation.GetLineSpan();

            _writer.WriteObjectStart(); // physicalLocation

            _writer.WriteObjectStart("artifactLocation");
            _writer.Write("uri", GetUri(span.Path));
            _writer.WriteObjectEnd(); // artifactLocation

            WriteRegion(span);

            _writer.WriteObjectEnd();
        }

        public override void Dispose()
        {
            _writer.WriteArrayEnd(); //results

            if (!string.IsNullOrEmpty(_totalAnalyzerExecutionTime))
            {
                _writer.WriteObjectStart("properties");

                _writer.Write("analyzerExecutionTime", _totalAnalyzerExecutionTime);

                _writer.WriteObjectEnd(); // properties
            }

            WriteTool();

            _writer.Write("columnKind", "utf16CodeUnits");

            _writer.WriteObjectEnd(); // run
            _writer.WriteArrayEnd();  // runs
            _writer.WriteObjectEnd(); // root
            base.Dispose();
        }

        private void WriteTool()
        {
            _writer.WriteObjectStart("tool");
            _writer.WriteObjectStart("driver");
            _writer.Write("name", _toolName);
            _writer.Write("version", _toolFileVersion);
            _writer.Write("dottedQuadFileVersion", _toolAssemblyVersion.ToString());
            _writer.Write("semanticVersion", _toolAssemblyVersion.ToString(fieldCount: 3));

            // Emit the 'language' property only if it is a non-empty string to match the SARIF spec.
            if (_culture.Name.Length > 0)
                _writer.Write("language", _culture.Name);
            var effectiveSeverities = WriteRules();

            _writer.WriteObjectEnd(); // driver
            _writer.WriteObjectEnd(); // tool

            WriteInvocations(effectiveSeverities);
        }

        private ImmutableArray<(string DescriptorId, int DescriptorIndex, ImmutableHashSet<ReportDiagnostic> EffectiveSeverities)> WriteRules()
        {
            var effectiveSeveritiesBuilder = ArrayBuilder<(string DescriptorId, int DescriptorIndex, ImmutableHashSet<ReportDiagnostic> EffectiveSeverities)>.GetInstance(_descriptors.Count);

            if (_descriptors.Count > 0)
            {
                _writer.WriteArrayStart("rules");

                var reportAnalyzerExecutionTime = !string.IsNullOrEmpty(_totalAnalyzerExecutionTime);
                foreach (var (index, descriptor, descriptorInfo) in _descriptors.ToSortedList())
                {
                    _writer.WriteObjectStart(); // rule
                    _writer.Write("id", descriptor.Id);

                    string? shortDescription = descriptor.Title.ToString(_culture);
                    if (!RoslynString.IsNullOrEmpty(shortDescription))
                    {
                        _writer.WriteObjectStart("shortDescription");
                        _writer.Write("text", shortDescription);
                        _writer.WriteObjectEnd();
                    }

                    string? fullDescription = descriptor.Description.ToString(_culture);
                    if (!RoslynString.IsNullOrEmpty(fullDescription))
                    {
                        _writer.WriteObjectStart("fullDescription");
                        _writer.Write("text", fullDescription);
                        _writer.WriteObjectEnd();
                    }

                    WriteDefaultConfiguration(descriptor);

                    if (!string.IsNullOrEmpty(descriptor.HelpLinkUri))
                    {
                        _writer.Write("helpUri", descriptor.HelpLinkUri);
                    }

                    // We report the rule as isEverSuppressed if either of the following is true:
                    // 1. If there is any external non-source suppression for the rule ID from
                    //    editorconfig, ruleset, command line options, etc. that disables the rule
                    //    either for part of the compilation or the entire compilation.
                    // 2. If there is any source suppression for diagnostic(s) with the rule ID through pragma directive,
                    //    SuppressMessageAttribute, DiagnosticSuppressor, etc.
                    var hasAnySourceSuppression = _diagnosticIdsWithAnySourceSuppressions.Contains(descriptor.Id);
                    var isEverSuppressed = descriptorInfo.HasAnyExternalSuppression || hasAnySourceSuppression;

                    Debug.Assert(reportAnalyzerExecutionTime || descriptorInfo.ExecutionTime == 0);
                    Debug.Assert(reportAnalyzerExecutionTime || descriptorInfo.ExecutionPercentage == 0);

                    if (!string.IsNullOrEmpty(descriptor.Category) || isEverSuppressed || reportAnalyzerExecutionTime || descriptor.ImmutableCustomTags.Any())
                    {
                        _writer.WriteObjectStart("properties");

                        if (!string.IsNullOrEmpty(descriptor.Category))
                        {
                            _writer.Write("category", descriptor.Category);
                        }

                        if (isEverSuppressed)
                        {
                            _writer.Write("isEverSuppressed", "true");

                            _writer.WriteArrayStart("suppressionKinds");

                            if (descriptorInfo.HasAnyExternalSuppression)
                            {
                                _writer.Write("external");
                            }

                            if (hasAnySourceSuppression)
                            {
                                _writer.Write("inSource");
                            }

                            _writer.WriteArrayEnd(); // suppressionKinds
                        }

                        if (reportAnalyzerExecutionTime)
                        {
                            var executionTime = ReportAnalyzerUtil.GetFormattedAnalyzerExecutionTime(descriptorInfo.ExecutionTime, _culture).Trim();
                            _writer.Write("executionTimeInSeconds", executionTime);

                            var executionPercentage = ReportAnalyzerUtil.GetFormattedAnalyzerExecutionPercentage(descriptorInfo.ExecutionPercentage, _culture).Trim();
                            _writer.Write("executionTimeInPercentage", executionPercentage);
                        }

                        if (descriptor.ImmutableCustomTags.Any())
                        {
                            _writer.WriteArrayStart("tags");

                            foreach (string tag in descriptor.ImmutableCustomTags)
                            {
                                _writer.Write(tag);
                            }

                            _writer.WriteArrayEnd(); // tags
                        }

                        _writer.WriteObjectEnd(); // properties
                    }

                    _writer.WriteObjectEnd(); // rule

                    var defaultSeverity = descriptor.IsEnabledByDefault ? DiagnosticDescriptor.MapSeverityToReport(descriptor.DefaultSeverity) : ReportDiagnostic.Suppress;
                    var hasNonDefaultEffectiveSeverities = descriptorInfo.EffectiveSeverities != null &&
                        (descriptorInfo.EffectiveSeverities.Count != 1 || descriptorInfo.EffectiveSeverities.Single() != defaultSeverity);
                    if (hasNonDefaultEffectiveSeverities)
                    {
                        effectiveSeveritiesBuilder.Add((descriptor.Id, index, descriptorInfo.EffectiveSeverities!));
                    }
                }

                _writer.WriteArrayEnd(); // rules
            }

            return effectiveSeveritiesBuilder.ToImmutableAndFree();
        }

        private void WriteInvocations(ImmutableArray<(string DescriptorId, int DescriptorIndex, ImmutableHashSet<ReportDiagnostic> EffectiveSeverities)> effectiveSeverities)
        {
            if (effectiveSeverities.IsEmpty)
                return;

            // Emit effective severities for each overridden rule severity.
            /*
                "invocations": [                          # See §3.14.11.
                  {                                       # An invocation object (§3.20).
                    "executionSuccessful" : true,         # See $3.20.14. A boolean value.
                    "ruleConfigurationOverrides": [       # See §3.20.5.
                      {                                   # A configurationOverride object
                                                          #  (§3.51).
                        "descriptor": {                   # See §3.51.2.
                          "id": "CA1000",
                          "index": 0
                        },
                        "configuration": {                # See §3.51.3.
                          "level": "warning"
                        }
                      }
                    ],
                  ...
                  }
                ]
             */

            _writer.WriteArrayStart("invocations");
            _writer.WriteObjectStart(); // invocation

            // Boolean property that is true if the engineering system that started the process knows that the analysis tool succeeded,
            // and false if the engineering system knows that the tool failed.
            // https://github.com/dotnet/roslyn/issues/70069 tracks detecting when the compiler exits with an exception and emit "false".
            _writer.Write("executionSuccessful", true);

            _writer.WriteArrayStart("ruleConfigurationOverrides");

            foreach (var (id, index, severities) in effectiveSeverities)
            {
                Debug.Assert(!severities.IsEmpty);

                foreach (var severity in severities.OrderBy(Comparer<ReportDiagnostic>.Default))
                {
                    _writer.WriteObjectStart(); // ruleConfigurationOverride

                    _writer.WriteObjectStart("descriptor");
                    _writer.Write("id", id);
                    _writer.Write("index", index);
                    _writer.WriteObjectEnd(); // descriptor

                    // Emit 'configuration' property bag with "enabled: false" for disabled diagnostics and
                    // "level: severity" for enabled diagnostics with overridden severity. 
                    _writer.WriteObjectStart("configuration");
                    var reportDiagnostic = DiagnosticDescriptor.MapReportToSeverity(severity);
                    if (!reportDiagnostic.HasValue)
                    {
                        _writer.Write("enabled", false);
                    }
                    else
                    {
                        var level = GetLevel(reportDiagnostic.Value);
                        _writer.Write("level", level);
                    }
                    _writer.WriteObjectEnd(); // configuration

                    _writer.WriteObjectEnd(); // ruleConfigurationOverride
                }
            }

            _writer.WriteArrayEnd(); // ruleConfigurationOverrides

            _writer.WriteObjectEnd(); // invocation
            _writer.WriteArrayEnd(); // invocations
        }

        private void WriteDefaultConfiguration(DiagnosticDescriptor descriptor)
        {
            string defaultLevel = GetLevel(descriptor.DefaultSeverity);

            // Don't bother to emit default values.
            bool emitLevel = defaultLevel != "warning";

            // The default value for "enabled" is "true".
            bool emitEnabled = !descriptor.IsEnabledByDefault;

            if (emitLevel || emitEnabled)
            {
                _writer.WriteObjectStart("defaultConfiguration");

                if (emitLevel)
                {
                    _writer.Write("level", defaultLevel);
                }

                if (emitEnabled)
                {
                    _writer.Write("enabled", descriptor.IsEnabledByDefault);
                }

                _writer.WriteObjectEnd(); // defaultConfiguration
            }
        }

        /// <summary>
        /// Represents a distinct set of <see cref="DiagnosticDescriptor"/>s and provides unique integer indices
        /// to distinguish them.
        /// </summary>
        private sealed class DiagnosticDescriptorSet
        {
            private readonly record struct DescriptorInfoWithIndex(int Index, DiagnosticDescriptorErrorLoggerInfo Info);
            // DiagnosticDescriptor -> DescriptorInfo
            private readonly Dictionary<DiagnosticDescriptor, DescriptorInfoWithIndex> _distinctDescriptors = new(SarifDiagnosticComparer.Instance);

            /// <summary>
            /// The total number of descriptors in the set.
            /// </summary>
            public int Count => _distinctDescriptors.Count;

            /// <summary>
            /// Adds a descriptor to the set if not already present.
            /// </summary>
            /// <returns>
            /// The unique key assigned to the given descriptor.
            /// </returns>
            public int Add(DiagnosticDescriptor descriptor, DiagnosticDescriptorErrorLoggerInfo? info = null)
            {
                if (_distinctDescriptors.TryGetValue(descriptor, out var descriptorInfoWithIndex))
                {
                    // Descriptor has already been seen.
                    // Update 'Info' value if different from the saved one.
                    if (info.HasValue && descriptorInfoWithIndex.Info != info)
                    {
                        descriptorInfoWithIndex = new(descriptorInfoWithIndex.Index, info.Value);
                        _distinctDescriptors[descriptor] = descriptorInfoWithIndex;
                    }

                    return descriptorInfoWithIndex.Index;
                }
                else
                {
                    _distinctDescriptors.Add(descriptor, new(Index: Count, info ?? default));
                    return Count - 1;
                }
            }

            /// <summary>
            /// Converts the set to a list, sorted by index.
            /// </summary>
            public List<(int Index, DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> ToSortedList()
            {
                Debug.Assert(Count > 0);

                var list = new List<(int Index, DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)>(Count);

                foreach (var pair in _distinctDescriptors)
                {
                    Debug.Assert(list.Capacity > list.Count);
                    list.Add((pair.Value.Index, pair.Key, pair.Value.Info));
                }

                Debug.Assert(list.Capacity == list.Count);
                list.Sort((x, y) => x.Index.CompareTo(y.Index));
                return list;
            }
        }
    }
}
