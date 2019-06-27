// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SarifV2ErrorLogger : StreamErrorLogger, IDisposable
    {
        public SarifV2ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
            : base(stream)
        {
        }

        public override void LogDiagnostic(Diagnostic diagnostic)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
