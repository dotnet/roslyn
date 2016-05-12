// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Roslyn.Utilities;

#pragma warning disable RS0013 // We need to invoke Diagnostic.Descriptor here to log all the metadata properties of the diagnostic.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used for logging all compiler diagnostics into a given <see cref="Stream"/>.
    /// This logger is responsible for closing the given stream on <see cref="Dispose"/>.
    /// It is incorrect to use the logger concurrently from multiple threads.
    ///
    /// The log format is SARIF (Static Analysis Results Interchange Format)
    /// https://sarifweb.azurewebsites.net
    /// https://github.com/sarif-standard/sarif-spec
    /// </summary>
    internal partial class ErrorLogger : IDisposable
    {
        private readonly JsonWriter _writer;
        private readonly DiagnosticDescriptorSet _descriptors;
        private readonly CultureInfo _culture;

        public ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.Position == 0);

            _writer = new JsonWriter(new StreamWriter(stream));
            _descriptors = new DiagnosticDescriptorSet();
            _culture = culture;

            _writer.WriteObjectStart(); // root
            _writer.Write("$schema", "http://json.schemastore.org/sarif-1.0.0-beta.5");
            _writer.Write("version", "1.0.0-beta.5");
            _writer.WriteArrayStart("runs");
            _writer.WriteObjectStart(); // run

            WriteToolInfo(toolName, toolFileVersion, toolAssemblyVersion);

            _writer.WriteArrayStart("results");
        }

        private void WriteToolInfo(string name, string fileVersion, Version assemblyVersion)
        {
            _writer.WriteObjectStart("tool");
            _writer.Write("name", name);
            _writer.Write("version", assemblyVersion.ToString());
            _writer.Write("fileVersion", fileVersion);
            _writer.Write("semanticVersion", assemblyVersion.ToString(fieldCount: 3));
            _writer.WriteObjectEnd();
        }

        public void LogDiagnostic(Diagnostic diagnostic)
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
            WriteProperties(diagnostic);

            _writer.WriteObjectEnd(); // result
        }

        private void WriteLocations(Location location, IReadOnlyList<Location> additionalLocations)
        {
            if (location.SourceTree != null)
            {
                _writer.WriteArrayStart("locations");
                _writer.WriteObjectStart(); // location
                _writer.WriteKey("analysisTarget");

                WritePhysicalLocation(location);

                _writer.WriteObjectEnd(); // location
                _writer.WriteArrayEnd(); // locations
            }

            // See https://github.com/dotnet/roslyn/issues/11228 for discussion around
            // whether this is the correct treatment of Diagnostic.AdditionalLocations
            // as SARIF relatedLocations.
            if (additionalLocations != null &&
                additionalLocations.Count > 0 &&
                additionalLocations.Any(l => l.SourceTree != null))
            {
                _writer.WriteArrayStart("relatedLocations");

                foreach (var additionalLocation in additionalLocations)
                {
                    if (additionalLocation.SourceTree != null)
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

        private void WritePhysicalLocation(Location location)
        {
            Debug.Assert(location.SourceTree != null);

            _writer.WriteObjectStart();
            _writer.Write("uri", GetUri(location.SourceTree));

            // Note that SARIF lines and columns are 1-based, but FileLinePositionSpan is 0-based
            FileLinePositionSpan span = location.GetLineSpan();
            _writer.WriteObjectStart("region");
            _writer.Write("startLine", span.StartLinePosition.Line + 1);
            _writer.Write("startColumn", span.StartLinePosition.Character + 1);
            _writer.Write("endLine", span.EndLinePosition.Line + 1);
            _writer.Write("endColumn", span.EndLinePosition.Character + 1);
            _writer.WriteObjectEnd(); // region

            _writer.WriteObjectEnd();
        }

        private static string GetUri(SyntaxTree syntaxTree)
        {
            Uri uri;

            if (!Uri.TryCreate(syntaxTree.FilePath, UriKind.RelativeOrAbsolute, out uri))
            {
                // The only constraint on SyntaxTree.FilePath is that it can be interpreted by
                // various resolvers so there is no guarantee we can turn the arbitrary string
                // in to a URI. If our attempt to do so fails, use the original string as the
                // "URI".
                return syntaxTree.FilePath;
            }

            return uri.ToString();
        }

        private void WriteProperties(Diagnostic diagnostic)
        {
            // Currently, the following are always inherited from the descriptor and therefore will be
            // captured as rule metadata and need not be logged here. IsWarningAsError is also omitted
            // because it can be inferred from level vs. defaultLevel in the log.
            Debug.Assert(diagnostic.CustomTags.SequenceEqual(diagnostic.Descriptor.CustomTags));
            Debug.Assert(diagnostic.Category == diagnostic.Descriptor.Category);
            Debug.Assert(diagnostic.DefaultSeverity == diagnostic.Descriptor.DefaultSeverity);
            Debug.Assert(diagnostic.IsEnabledByDefault == diagnostic.Descriptor.IsEnabledByDefault);

            if (diagnostic.WarningLevel > 0 || diagnostic.Properties.Count > 0)
            {
                _writer.WriteObjectStart("properties");

                if (diagnostic.WarningLevel > 0)
                {
                    _writer.Write("warningLevel", diagnostic.WarningLevel);
                }

                if (diagnostic.Properties.Count > 0)
                {
                    _writer.WriteObjectStart("customProperties");

                    foreach (var pair in diagnostic.Properties.OrderBy(x => x.Key, StringComparer.Ordinal))
                    {
                        _writer.Write(pair.Key, pair.Value);
                    }

                    _writer.WriteObjectEnd();
                }

                _writer.WriteObjectEnd(); // properties
            }
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

        private static string GetLevel(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Info:
                    return "note";

                case DiagnosticSeverity.Error:
                    return "error";

                case DiagnosticSeverity.Warning:
                    return "warning";

                case DiagnosticSeverity.Hidden:
                default:
                    // hidden diagnostics are not reported on the command line and therefore not currently given to 
                    // the error logger. We could represent it with a custom property in the SARIF log if that changes.
                    Debug.Assert(false);
                    goto case DiagnosticSeverity.Warning;
            }
        }

        public void Dispose()
        {
            _writer.WriteArrayEnd();  // results

            WriteRules();

            _writer.WriteObjectEnd(); // run
            _writer.WriteArrayEnd();  // runs
            _writer.WriteObjectEnd(); // root
            _writer.Dispose();
        }

        /// <summary>
        /// Represents a distinct set of <see cref="DiagnosticDescriptor"/>s and provides unique string keys 
        /// to distinguish them.
        ///
        /// The first <see cref="DiagnosticDescriptor"/> added with a given <see cref="DiagnosticDescriptor.Id"/>
        /// value is given that value as its unique key. Subsequent adds with the same ID will have .NNNN
        /// apppended to their with an auto-incremented numeric value to disambiguate the collision.
        /// </summary>
        private sealed class DiagnosticDescriptorSet
        {
            // DiagnosticDescriptor.Id -> (descriptor -> key)
            private readonly Dictionary<string, Dictionary<DiagnosticDescriptor, string>> _descriptors
                = new Dictionary<string, Dictionary<DiagnosticDescriptor, string>>();
           
            /// <summary>
            /// The total number of descriptors in the set.
            /// </summary>
            public int Count { get; private set; }

            /// <summary>
            /// Adds a descriptor to the set if not already present.
            /// </summary>
            /// <returns>
            /// The unique key assigned to the given descriptor.
            /// </returns>
            public string Add(DiagnosticDescriptor descriptor)
            {
                Dictionary<DiagnosticDescriptor, string> keys;

                if (!_descriptors.TryGetValue(descriptor.Id, out keys))
                {
                    keys = new Dictionary<DiagnosticDescriptor, string>();
                    _descriptors.Add(descriptor.Id, keys);
                }

                string key;
                if (!keys.TryGetValue(descriptor, out key))
                {
                    key = descriptor.Id;

                    if (keys.Count > 0)
                    {
                        key += "." + keys.Count.ToString("000", CultureInfo.InvariantCulture);
                    }

                    keys.Add(descriptor, key);
                    Count++;
                }

                return key;
            }

            /// <summary>
            /// Converts the set to a list of (key, descriptor) pairs sorted by key.
            /// </summary>
            public List<KeyValuePair<string, DiagnosticDescriptor>> ToSortedList()
            {
                var list = new List<KeyValuePair<string, DiagnosticDescriptor>>(Count);

                foreach (var outerPair in _descriptors)
                {
                    foreach (var innerPair in outerPair.Value)
                    {
                        Debug.Assert(list.Capacity > list.Count);
                        list.Add(new KeyValuePair<string, DiagnosticDescriptor>(innerPair.Value, innerPair.Key));
                    }
                }

                Debug.Assert(list.Capacity == list.Count);
                list.Sort((x, y) => string.CompareOrdinal(x.Key, y.Key));
                return list;
            }
        }
    }
}

#pragma warning restore RS0013