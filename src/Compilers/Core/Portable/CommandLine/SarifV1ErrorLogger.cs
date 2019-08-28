// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

#pragma warning disable RS0013 // We need to invoke Diagnostic.Descriptor here to log all the metadata properties of the diagnostic.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used for logging compiler diagnostics to a stream in the unstandardized SARIF
    /// (Static Analysis Results Interchange Format) v1.0.0 format.
    /// https://github.com/sarif-standard/sarif-spec
    /// https://rawgit.com/sarif-standard/sarif-spec/master/Static%20Analysis%20Results%20Interchange%20Format%20(SARIF).html
    /// </summary>
    /// <remarks>
    /// To log diagnostics in the standardized SARIF v2.1.0 format, use the SarifV2ErrorLogger.
    /// </remarks>
    internal sealed class SarifV1ErrorLogger : SarifErrorLogger, IDisposable
    {
        private readonly DiagnosticDescriptorSet _descriptors;
        public SarifV1ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
            : base(stream, culture)
        {
            _descriptors = new DiagnosticDescriptorSet();

            _writer.WriteObjectStart(); // root
            _writer.Write("$schema", "http://json.schemastore.org/sarif-1.0.0");
            _writer.Write("version", "1.0.0");
            _writer.WriteArrayStart("runs");
            _writer.WriteObjectStart(); // run

            _writer.WriteObjectStart("tool");
            _writer.Write("name", toolName);
            _writer.Write("version", toolAssemblyVersion.ToString());
            _writer.Write("fileVersion", toolFileVersion);
            _writer.Write("semanticVersion", toolAssemblyVersion.ToString(fieldCount: 3));
            _writer.Write("language", culture.Name);
            _writer.WriteObjectEnd(); // tool

            _writer.WriteArrayStart("results");
        }

        protected override string PrimaryLocationPropertyName => "resultFile";

        public override void LogDiagnostic(Diagnostic diagnostic)
        {
            _writer.WriteObjectStart(); // result
            _writer.Write("ruleId", diagnostic.Id);

            string ruleKey = _descriptors.Add(diagnostic.Descriptor);
            if (ruleKey != diagnostic.Id)
            {
                _writer.Write("ruleKey", ruleKey);
            }

            _writer.Write("level", GetLevel(diagnostic.Severity));

            string message = diagnostic.GetMessage(_culture);
            if (!string.IsNullOrEmpty(message))
            {
                _writer.Write("message", message);
            }

            if (diagnostic.IsSuppressed)
            {
                _writer.WriteArrayStart("suppressionStates");
                _writer.Write("suppressedInSource");
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

        protected override void WritePhysicalLocation(Location location)
        {
            Debug.Assert(HasPath(location));

            FileLinePositionSpan span = location.GetLineSpan();

            _writer.WriteObjectStart();
            _writer.Write("uri", GetUri(span.Path));

            WriteRegion(span);

            _writer.WriteObjectEnd();
        }

        private void WriteRules()
        {
            if (_descriptors.Count > 0)
            {
                _writer.WriteObjectStart("rules");

                foreach (var pair in _descriptors.ToSortedList())
                {
                    DiagnosticDescriptor descriptor = pair.Value;

                    _writer.WriteObjectStart(pair.Key); // rule
                    _writer.Write("id", descriptor.Id);

                    string shortDescription = descriptor.Title.ToString(_culture);
                    if (!string.IsNullOrEmpty(shortDescription))
                    {
                        _writer.Write("shortDescription", shortDescription);
                    }

                    string fullDescription = descriptor.Description.ToString(_culture);
                    if (!string.IsNullOrEmpty(fullDescription))
                    {
                        _writer.Write("fullDescription", fullDescription);
                    }

                    _writer.Write("defaultLevel", GetLevel(descriptor.DefaultSeverity));

                    if (!string.IsNullOrEmpty(descriptor.HelpLinkUri))
                    {
                        _writer.Write("helpUri", descriptor.HelpLinkUri);
                    }

                    _writer.WriteObjectStart("properties");

                    if (!string.IsNullOrEmpty(descriptor.Category))
                    {
                        _writer.Write("category", descriptor.Category);
                    }

                    _writer.Write("isEnabledByDefault", descriptor.IsEnabledByDefault);

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
                    _writer.WriteObjectEnd(); // rule
                }

                _writer.WriteObjectEnd(); // rules
            }
        }

        public override void Dispose()
        {
            _writer.WriteArrayEnd();  // results

            WriteRules();

            _writer.WriteObjectEnd(); // run
            _writer.WriteArrayEnd();  // runs
            _writer.WriteObjectEnd(); // root

            base.Dispose();
        }

        /// <summary>
        /// Represents a distinct set of <see cref="DiagnosticDescriptor"/>s and provides unique string keys 
        /// to distinguish them.
        ///
        /// The first <see cref="DiagnosticDescriptor"/> added with a given <see cref="DiagnosticDescriptor.Id"/>
        /// value is given that value as its unique key. Subsequent adds with the same ID will have .NNN
        /// appended to their with an auto-incremented numeric value.
        /// </summary>
        private sealed class DiagnosticDescriptorSet
        {
            // DiagnosticDescriptor.Id -> auto-incremented counter
            private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();

            // DiagnosticDescriptor -> unique key
            private readonly Dictionary<DiagnosticDescriptor, string> _keys = new Dictionary<DiagnosticDescriptor, string>(SarifDiagnosticComparer.Instance);

            /// <summary>
            /// The total number of descriptors in the set.
            /// </summary>
            public int Count => _keys.Count;

            /// <summary>
            /// Adds a descriptor to the set if not already present.
            /// </summary>
            /// <returns>
            /// The unique key assigned to the given descriptor.
            /// </returns>
            public string Add(DiagnosticDescriptor descriptor)
            {
                // Case 1: Descriptor has already been seen -> retrieve key from cache.
                if (_keys.TryGetValue(descriptor, out string key))
                {
                    return key;
                }

                // Case 2: First time we see a descriptor with a given ID -> use its ID as the key.
                if (!_counters.TryGetValue(descriptor.Id, out int counter))
                {
                    _counters.Add(descriptor.Id, 0);
                    _keys.Add(descriptor, descriptor.Id);
                    return descriptor.Id;
                }

                // Case 3: We've already seen a different descriptor with the same ID -> generate a key.
                //
                // This will only need to loop in the corner case where there is an actual descriptor 
                // with non-generated ID=X.NNN and more than one descriptor with ID=X.
                do
                {
                    _counters[descriptor.Id] = ++counter;
                    key = descriptor.Id + "-" + counter.ToString("000", CultureInfo.InvariantCulture);
                } while (_counters.ContainsKey(key));

                _keys.Add(descriptor, key);
                return key;
            }

            /// <summary>
            /// Converts the set to a list of (key, descriptor) pairs sorted by key.
            /// </summary>
            public List<KeyValuePair<string, DiagnosticDescriptor>> ToSortedList()
            {
                Debug.Assert(Count > 0);

                var list = new List<KeyValuePair<string, DiagnosticDescriptor>>(Count);

                foreach (var pair in _keys)
                {
                    Debug.Assert(list.Capacity > list.Count);
                    list.Add(new KeyValuePair<string, DiagnosticDescriptor>(pair.Value, pair.Key));
                }

                Debug.Assert(list.Capacity == list.Count);
                list.Sort((x, y) => string.CompareOrdinal(x.Key, y.Key));
                return list;
            }
        }
    }
}

#pragma warning restore RS0013
