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
    /// Base class for logging compiler diagnostics.
    /// </summary>
    internal abstract class ErrorLogger
    {
        public abstract void LogDiagnostic(Diagnostic diagnostic);
    }

    /// <summary>
    /// Used for logging all compiler diagnostics into a given <see cref="Stream"/>.
    /// This logger is responsible for closing the given stream on <see cref="Dispose"/>.
    /// It is incorrect to use the logger concurrently from multiple threads.
    ///
    /// The log format is SARIF (Static Analysis Results Interchange Format)
    /// https://sarifweb.azurewebsites.net
    /// https://github.com/sarif-standard/sarif-spec
    /// </summary>
    internal sealed class StreamErrorLogger : ErrorLogger, IDisposable
    {
        private readonly JsonWriter _writer;
        private readonly DiagnosticDescriptorSet _descriptors;
        private readonly CultureInfo _culture;

        public StreamErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.Position == 0);

            _writer = new JsonWriter(new StreamWriter(stream));
            _descriptors = new DiagnosticDescriptorSet();
            _culture = culture;

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
            WriteProperties(diagnostic);

            _writer.WriteObjectEnd(); // result
        }

        private void WriteLocations(Location location, IReadOnlyList<Location> additionalLocations)
        {
            if (HasPath(location))
            {
                _writer.WriteArrayStart("locations");
                _writer.WriteObjectStart(); // location
                _writer.WriteKey("resultFile");

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

        private void WritePhysicalLocation(Location location)
        {
            Debug.Assert(HasPath(location));

            FileLinePositionSpan span = location.GetLineSpan();

            _writer.WriteObjectStart();
            _writer.Write("uri", GetUri(span.Path));

            // Note that SARIF lines and columns are 1-based, but FileLinePositionSpan is 0-based

            _writer.WriteObjectStart("region");
            _writer.Write("startLine", span.StartLinePosition.Line + 1);
            _writer.Write("startColumn", span.StartLinePosition.Character + 1);
            _writer.Write("endLine", span.EndLinePosition.Line + 1);
            _writer.Write("endColumn", span.EndLinePosition.Character + 1);
            _writer.WriteObjectEnd(); // region

            _writer.WriteObjectEnd();
        }

        private static bool HasPath(Location location)
        {
            return !string.IsNullOrEmpty(location.GetLineSpan().Path);
        }

        private static readonly Uri _fileRoot = new Uri("file:///");

        private static string GetUri(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            // Note that in general, these "paths" are opaque strings to be 
            // interpreted by resolvers (see SyntaxTree.FilePath documentation).

            // Common case: absolute path -> absolute URI
            Uri uri;
            if (Uri.TryCreate(path, UriKind.Absolute, out uri))
            {
                // We use Uri.AbsoluteUri and not Uri.ToString() because Uri.ToString() 
                // is unescaped (e.g. spaces remain unreplaced by %20) and therefore 
                // not well-formed.
                return uri.AbsoluteUri;
            }

            // First fallback attempt: attempt to interpret as relative path/URI.
            // (Perhaps the resolver works that way.)
            if (Uri.TryCreate(path, UriKind.Relative, out uri))
            {
                // There is no AbsoluteUri equivalent for relative URI references and ToString() 
                // won't escape without this relative -> absolute -> relative trick.
                return _fileRoot.MakeRelativeUri(new Uri(_fileRoot, uri)).ToString();
            }

            // Last resort: UrlEncode the whole opaque string.
            return System.Net.WebUtility.UrlEncode(path);
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
        /// value is given that value as its unique key. Subsequent adds with the same ID will have .NNN
        /// appended to their with an auto-incremented numeric value.
        /// </summary>
        private sealed class DiagnosticDescriptorSet
        {
            // DiagnosticDescriptor.Id -> auto-incremented counter
            private Dictionary<string, int> _counters = new Dictionary<string, int>();

            // DiagnosticDescriptor -> unique key
            private Dictionary<DiagnosticDescriptor, string> _keys = new Dictionary<DiagnosticDescriptor, string>(new Comparer());

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
                string key;
                if (_keys.TryGetValue(descriptor, out key))
                {
                    return key;
                }

                // Case 2: First time we see a descriptor with a given ID -> use its ID as the key.
                int counter;
                if (!_counters.TryGetValue(descriptor.Id, out counter))
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

            /// <summary>
            /// Compares descriptors by the values that we write to the log and nothing else.
            ///
            /// We cannot just use <see cref="DiagnosticDescriptor"/>'s built-in implementation
            /// of <see cref="IEquatable{DiagnosticDescriptor}"/> for two reasons:
            ///
            /// 1. <see cref="DiagnosticDescriptor.MessageFormat"/> is part of that built-in 
            ///    equatability, but we do not write it out, and so descriptors differing only
            ///    by MessageFormat (common) would lead to duplicate rule metadata entries in
            ///    the log.
            ///
            /// 2. <see cref="DiagnosticDescriptor.CustomTags"/> is *not* part of that built-in
            ///    equatability, but we do write them out, and so descriptors differing only
            ///    by CustomTags (rare) would cause only one set of tags to be reported in the
            ///    log.
            /// </summary>
            private sealed class Comparer : IEqualityComparer<DiagnosticDescriptor>
            {
                public bool Equals(DiagnosticDescriptor x, DiagnosticDescriptor y)
                {
                    if (ReferenceEquals(x, y))
                    {
                        return true;
                    }

                    if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    {
                        return false;
                    }

                    // The properties are guaranteed to be non-null by DiagnosticDescriptor invariants.
                    Debug.Assert(x.Description != null && x.Title != null && x.CustomTags != null);
                    Debug.Assert(y.Description != null && y.Title != null && y.CustomTags != null);

                    return (x.Category == y.Category
                        && x.DefaultSeverity == y.DefaultSeverity
                        && x.Description.Equals(y.Description)
                        && x.HelpLinkUri == y.HelpLinkUri
                        && x.Id == y.Id
                        && x.IsEnabledByDefault == y.IsEnabledByDefault
                        && x.Title.Equals(y.Title)
                        && x.CustomTags.SequenceEqual(y.CustomTags));
                }

                public int GetHashCode(DiagnosticDescriptor obj)
                {
                    if (ReferenceEquals(obj, null))
                    {
                        return 0;
                    }

                    // The properties are guaranteed to be non-null by DiagnosticDescriptor invariants.
                    Debug.Assert(obj.Category != null && obj.Description != null && obj.HelpLinkUri != null
                        && obj.Id != null && obj.Title != null && obj.CustomTags != null);

                    return Hash.Combine(obj.Category.GetHashCode(),
                        Hash.Combine(obj.DefaultSeverity.GetHashCode(),
                        Hash.Combine(obj.Description.GetHashCode(),
                        Hash.Combine(obj.HelpLinkUri.GetHashCode(),
                        Hash.Combine(obj.Id.GetHashCode(),
                        Hash.Combine(obj.IsEnabledByDefault.GetHashCode(),
                        Hash.Combine(obj.Title.GetHashCode(),
                        Hash.CombineValues(obj.CustomTags))))))));
                }
            }
        }
    }
}

#pragma warning restore RS0013
