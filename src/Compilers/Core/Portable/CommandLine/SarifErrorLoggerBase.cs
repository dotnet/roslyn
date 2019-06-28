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

        public override void Dispose()
        {
            base.Dispose();
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
    }
}