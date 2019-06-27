// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SarifV2ErrorLogger : StreamErrorLogger, IDisposable
    {
        private readonly DiagnosticDescriptorSet _descriptors;

        private readonly string _toolName;
        private readonly string _toolFileVersion;
        private readonly Version _toolAssemblyVersion;
        private readonly CultureInfo _culture;

        public SarifV2ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
            : base(stream)
        {
            _descriptors = new DiagnosticDescriptorSet();

            _toolName = toolName;
            _toolFileVersion = toolFileVersion;
            _toolAssemblyVersion = toolAssemblyVersion;
            _culture = culture;

            _writer.WriteObjectStart(); // root
            _writer.Write("$schema", "http://json.schemastore.org/sarif-2.1.0");
            _writer.Write("version", "2.1.0");
            _writer.WriteArrayStart("runs");
            _writer.WriteObjectStart(); // run

            _writer.WriteArrayStart("results");
        }

        public override void LogDiagnostic(Diagnostic diagnostic)
        {
            _writer.WriteObjectStart(); // result
            _writer.Write("ruleId", diagnostic.Id);
            int ruleIndex = _descriptors.Add(diagnostic.Descriptor);
            _writer.Write("ruleIndex", ruleIndex.ToString(CultureInfo.InvariantCulture));

            string message = diagnostic.GetMessage(_culture);
            if (!string.IsNullOrEmpty(message))
            {
                _writer.WriteObjectStart("message");
                _writer.Write("text", message);
                _writer.WriteObjectEnd();
            }

            _writer.WriteObjectEnd(); // result
        }

        public override void Dispose()
        {
            _writer.WriteArrayEnd(); //results

            WriteTool();

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
            _writer.Write("fileVersion", _toolFileVersion);
            _writer.Write("version", _toolAssemblyVersion.ToString());
            _writer.Write("semanticVersion", _toolAssemblyVersion.ToString(fieldCount: 3));

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

                    //_writer.Write("defaultLevel", GetLevel(descriptor.DefaultSeverity));

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

                _writer.WriteArrayEnd(); // rules
            }
        }

        /// <summary>
        /// Represents a distinct set of <see cref="DiagnosticDescriptor"/>s and provides unique integer indices
        /// to distinguish them.
        /// </summary>
        private sealed class DiagnosticDescriptorSet
        {
            // DiagnosticDescriptor -> integer index
            private readonly Dictionary<DiagnosticDescriptor, int> _distinctDescriptors = new Dictionary<DiagnosticDescriptor, int>(new SarifDiagnosticComparer());

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
