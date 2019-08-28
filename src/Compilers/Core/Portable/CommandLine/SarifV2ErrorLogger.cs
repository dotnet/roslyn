// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

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

        private readonly string _toolName;
        private readonly string _toolFileVersion;
        private readonly Version _toolAssemblyVersion;

        public SarifV2ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
            : base(stream, culture)
        {
            _descriptors = new DiagnosticDescriptorSet();

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

        public override void LogDiagnostic(Diagnostic diagnostic)
        {
            _writer.WriteObjectStart(); // result
            _writer.Write("ruleId", diagnostic.Id);
            int ruleIndex = _descriptors.Add(diagnostic.Descriptor);
            _writer.Write("ruleIndex", ruleIndex);

            _writer.Write("level", GetLevel(diagnostic.Severity));

            string message = diagnostic.GetMessage(_culture);
            if (!string.IsNullOrEmpty(message))
            {
                _writer.WriteObjectStart("message");
                _writer.Write("text", message);
                _writer.WriteObjectEnd();
            }

            if (diagnostic.IsSuppressed)
            {
                _writer.WriteArrayStart("suppressions");
                _writer.WriteObjectStart(); // suppression
                _writer.Write("kind", "inSource");
                _writer.WriteObjectEnd(); // suppression
                _writer.WriteArrayEnd();
            }

            WriteLocations(diagnostic.Location, diagnostic.AdditionalLocations);

            WriteResultProperties(diagnostic);

            _writer.WriteObjectEnd(); // result
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
            _writer.Write("language", _culture.Name);

            WriteRules();

            _writer.WriteObjectEnd(); // driver
            _writer.WriteObjectEnd(); // tool
        }

        private void WriteRules()
        {
            if (_descriptors.Count > 0)
            {
                _writer.WriteArrayStart("rules");

                foreach (var pair in _descriptors.ToSortedList())
                {
                    DiagnosticDescriptor descriptor = pair.Value;

                    _writer.WriteObjectStart(); // rule
                    _writer.Write("id", descriptor.Id);

                    string shortDescription = descriptor.Title.ToString(_culture);
                    if (!string.IsNullOrEmpty(shortDescription))
                    {
                        _writer.WriteObjectStart("shortDescription");
                        _writer.Write("text", shortDescription);
                        _writer.WriteObjectEnd();
                    }

                    string fullDescription = descriptor.Description.ToString(_culture);
                    if (!string.IsNullOrEmpty(fullDescription))
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

                    if (!string.IsNullOrEmpty(descriptor.Category) || descriptor.CustomTags.Any())
                    {
                        _writer.WriteObjectStart("properties");

                        if (!string.IsNullOrEmpty(descriptor.Category))
                        {
                            _writer.Write("category", descriptor.Category);
                        }

                        if (descriptor.CustomTags.Any())
                        {
                            _writer.WriteArrayStart("tags");

                            foreach (string tag in descriptor.CustomTags)
                            {
                                _writer.Write(tag);
                            }

                            _writer.WriteArrayEnd(); // tags
                        }

                        _writer.WriteObjectEnd(); // properties
                    }

                    _writer.WriteObjectEnd(); // rule
                }

                _writer.WriteArrayEnd(); // rules
            }
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
            // DiagnosticDescriptor -> integer index
            private readonly Dictionary<DiagnosticDescriptor, int> _distinctDescriptors = new Dictionary<DiagnosticDescriptor, int>(SarifDiagnosticComparer.Instance);

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
            public int Add(DiagnosticDescriptor descriptor)
            {
                if (_distinctDescriptors.TryGetValue(descriptor, out int index))
                {
                    // Descriptor has already been seen.
                    return index;
                }
                else
                {
                    _distinctDescriptors.Add(descriptor, Count);
                    return Count - 1;
                }
            }

            /// <summary>
            /// Converts the set to a list, sorted by index.
            /// </summary>
            public List<KeyValuePair<int, DiagnosticDescriptor>> ToSortedList()
            {
                Debug.Assert(Count > 0);

                var list = new List<KeyValuePair<int, DiagnosticDescriptor>>(Count);

                foreach (var pair in _distinctDescriptors)
                {
                    Debug.Assert(list.Capacity > list.Count);
                    list.Add(new KeyValuePair<int, DiagnosticDescriptor>(pair.Value, pair.Key));
                }

                Debug.Assert(list.Capacity == list.Count);
                list.Sort((x, y) => x.Key.CompareTo(y.Key));
                return list;
            }
        }
    }
}
