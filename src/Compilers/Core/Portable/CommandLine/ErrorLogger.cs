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
    /// The log format is SARIF (Static Analysis Results Interchange Format)
    /// https://github.com/sarif-standard/sarif-spec
    /// </summary>
    internal partial class ErrorLogger : IDisposable
    {
        // Internal for testing purposes.
        internal const string OutputFormatVersion = "0.1";

        private readonly JsonWriter _writer;

        public ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.Position == 0);

            _writer = new JsonWriter(new StreamWriter(stream));

            _writer.WriteObjectStart(); // root
            _writer.Write("version", OutputFormatVersion);

            _writer.WriteArrayStart("runLogs");
            _writer.WriteObjectStart(); // runLog

            WriteToolInfo(toolName, toolFileVersion, toolAssemblyVersion);

            _writer.WriteArrayStart("issues");
        }

        private void WriteToolInfo(string name, string fileVersion, Version assemblyVersion)
        {
            _writer.WriteObjectStart("toolInfo");
            _writer.Write("name", name);
            _writer.Write("version", assemblyVersion.ToString(fieldCount: 3));
            _writer.Write("fileVersion", fileVersion);
            _writer.WriteObjectEnd();
        }

        internal void LogDiagnostic(Diagnostic diagnostic, CultureInfo culture)
        {
            _writer.WriteObjectStart(); // issue
            _writer.Write("ruleId", diagnostic.Id);

            WriteLocations(diagnostic.Location, diagnostic.AdditionalLocations);

            string message = diagnostic.GetMessage(culture);
            if (string.IsNullOrEmpty(message))
            {
                message = "<None>";
            }

            string description = diagnostic.Descriptor.Description.ToString(culture);
            if (string.IsNullOrEmpty(description))
            {
                _writer.Write("fullMessage", message);
            }
            else
            {
                _writer.Write("shortMessage", message);
                _writer.Write("fullMessage", description);
            }

            WriteProperties(diagnostic, culture);

            _writer.WriteObjectEnd(); // issue
        }

        private void WriteLocations(Location location, IReadOnlyList<Location> additionalLocations)
        {
            _writer.WriteArrayStart("locations");

            WriteLocation(location);

            if (additionalLocations != null)
            {
                foreach (var additionalLocation in additionalLocations)
                {
                    WriteLocation(additionalLocation);
                }
            }

            _writer.WriteArrayEnd();
        }

        private void WriteLocation(Location location)
        {
            if (location.SourceTree == null)
            {
                return;
            }

            _writer.WriteObjectStart(); // location

            _writer.WriteArrayStart("analysisTarget");
            _writer.WriteObjectStart(); // physical location component

            _writer.Write("uri", GetUri(location.SourceTree));

            // Note that SARIF lines and columns are 1-based, but FileLinePositionSpan is 0-based
            FileLinePositionSpan span = location.GetLineSpan();
            _writer.WriteKey("region");
            _writer.WriteObjectStart();
            _writer.Write("startLine", span.StartLinePosition.Line + 1);
            _writer.Write("startColumn", span.StartLinePosition.Character + 1);
            _writer.Write("endLine", span.EndLinePosition.Line + 1);
            _writer.Write("endColumn", span.EndLinePosition.Character + 1);
            _writer.WriteObjectEnd(); // region

            _writer.WriteObjectEnd(); // physical location component
            _writer.WriteArrayEnd();  // analysisTarget
            _writer.WriteObjectEnd(); // location
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

        private void WriteProperties(Diagnostic diagnostic, CultureInfo culture)
        {
            _writer.WriteObjectStart("properties");

            _writer.Write("severity", diagnostic.Severity.ToString());

            if (diagnostic.Severity == DiagnosticSeverity.Warning)
            {
                _writer.Write("warningLevel", diagnostic.WarningLevel.ToString());
            }

            _writer.Write("defaultSeverity", diagnostic.DefaultSeverity.ToString());

            string title = diagnostic.Descriptor.Title.ToString(culture);
            if (!string.IsNullOrEmpty(title))
            {
                _writer.Write("title", title);
            }

            _writer.Write("category", diagnostic.Category);

            string helpLink = diagnostic.Descriptor.HelpLinkUri;
            if (!string.IsNullOrEmpty(helpLink))
            {
                _writer.Write("helpLink", helpLink);
            }

            _writer.Write("isEnabledByDefault", diagnostic.IsEnabledByDefault.ToString());

            _writer.Write("isSuppressedInSource", diagnostic.IsSuppressed.ToString());

            if (diagnostic.CustomTags.Count > 0)
            {
                _writer.Write("customTags", diagnostic.CustomTags.WhereNotNull().Join(";"));
            }

            foreach (var pair in diagnostic.Properties.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                _writer.Write("customProperties." + pair.Key, pair.Value);
            }

            _writer.WriteObjectEnd(); // properties
        }

        public void Dispose()
        {
            _writer.WriteArrayEnd();  // issues
            _writer.WriteObjectEnd(); // single runLog
            _writer.WriteArrayEnd();  // runLogs
            _writer.WriteObjectEnd(); // root
            _writer.Dispose();
        }
    }
}

#pragma warning restore RS0013