// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Base class for the <see cref="SarifV1ErrorLogger"/> and <see cref="SarifV2ErrorLogger"/> classes.
    /// The SarifV2ErrorLogger produces the standardized SARIF v2.1.0. The SarifV1ErrorLogger produces
    /// the non-standardized SARIF v1.0.0. It is retained (and in fact, is retained as the default)
    /// for compatibility with previous versions of the compiler. Customers who want to integrate
    /// with standardized SARIF tooling should specify /errorlog:logFilePath;version=2 on the command
    /// line to produce SARIF v2.1.0.
    /// </summary>
    internal abstract class SarifErrorLogger : ErrorLogger, IDisposable
    {
        protected JsonWriter _writer { get; }
        protected CultureInfo _culture { get; }

        protected SarifErrorLogger(Stream stream, CultureInfo culture)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream!.Position == 0);

            _writer = new JsonWriter(new StreamWriter(stream));
            _culture = culture;
        }

        //
        protected abstract string PrimaryLocationPropertyName { get; }

        protected abstract void WritePhysicalLocation(Location diagnosticLocation);

        public virtual void Dispose()
        {
            _writer.Dispose();
        }

        protected void WriteRegion(FileLinePositionSpan span)
        {
            // Note that SARIF lines and columns are 1-based, but FileLinePositionSpan is 0-based
            _writer.WriteObjectStart("region");
            _writer.Write("startLine", span.StartLinePosition.Line + 1);
            _writer.Write("startColumn", span.StartLinePosition.Character + 1);
            _writer.Write("endLine", span.EndLinePosition.Line + 1);
            _writer.Write("endColumn", span.EndLinePosition.Character + 1);
            _writer.WriteObjectEnd(); // region
        }

        protected static string GetLevel(DiagnosticSeverity severity)
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

        protected void WriteResultProperties(Diagnostic diagnostic)
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

        protected static bool HasPath(Location location)
        {
            return !string.IsNullOrEmpty(location.GetLineSpan().Path);
        }

        private static readonly Uri s_fileRoot = new Uri("file:///");

        protected static string GetUri(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            // Note that in general, these "paths" are opaque strings to be 
            // interpreted by resolvers (see SyntaxTree.FilePath documentation).

            // Common case: absolute path -> absolute URI
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
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
                return s_fileRoot.MakeRelativeUri(new Uri(s_fileRoot, uri)).ToString();
            }

            // Last resort: UrlEncode the whole opaque string.
            return System.Net.WebUtility.UrlEncode(path);
        }
    }
}
