// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Globalization;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SarifV2ErrorLogger : StreamErrorLogger, IDisposable
    {
        public SarifV2ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
            : base(stream)
        {
            _writer.WriteObjectStart(); // root
            _writer.Write("$schema", "http://json.schemastore.org/sarif-2.1.0");
            _writer.Write("version", "2.1.0");
            _writer.WriteArrayStart("runs");
            _writer.WriteObjectStart(); // run

            _writer.WriteObjectStart("tool");
            _writer.WriteObjectStart("driver");
            _writer.Write("name", toolName);
            _writer.Write("fileVersion", toolFileVersion);
            _writer.Write("version", toolAssemblyVersion.ToString());
            _writer.Write("semanticVersion", toolAssemblyVersion.ToString(fieldCount: 3));

            WriteRules();

            _writer.WriteObjectEnd(); // driver
            _writer.WriteObjectEnd(); // tool

            _writer.WriteArrayStart("results");
        }

        public override void LogDiagnostic(Diagnostic diagnostic)
        {
            _writer.WriteObjectStart(); // result
            _writer.Write("ruleId", diagnostic.Id);

            _writer.WriteObjectEnd(); // result
        }

        public override void Dispose()
        {
            _writer.WriteArrayEnd(); //results
            _writer.WriteObjectEnd(); // run
            _writer.WriteArrayEnd();  // runs
            _writer.WriteObjectEnd(); // root
            base.Dispose();
        }

        private void WriteRules()
        {
            _writer.WriteArrayStart("rules");
            _writer.WriteArrayEnd(); // rules
        }
    }
}
