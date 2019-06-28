// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Globalization;

namespace Microsoft.CodeAnalysis
{
    internal abstract class SarifErrorLoggerBase : StreamErrorLogger, IDisposable
    {
        protected readonly CultureInfo _culture;

        protected SarifErrorLoggerBase(Stream stream, CultureInfo culture)
            : base(stream)
        {
            _culture = culture;
        }

        //
        protected abstract string PrimaryLocationPropertyName { get; }

        protected abstract void WritePhysicalLocation(Location diagnosticLocation);

        public override void Dispose()
        {
            base.Dispose();
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