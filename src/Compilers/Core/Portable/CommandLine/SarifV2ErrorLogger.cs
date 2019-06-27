// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Globalization;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SarifV2ErrorLogger : StreamErrorLogger, IDisposable
    {

        private readonly string _toolName;
        private readonly string _toolFileVersion;
        private readonly Version _toolAssemblyVersion;

        public SarifV2ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion, CultureInfo culture)
            : base(stream)
        {
            _toolName = toolName;
            _toolFileVersion = toolFileVersion;
            _toolAssemblyVersion = toolAssemblyVersion;

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
            _writer.WriteArrayStart("rules");
            _writer.WriteArrayEnd(); // rules
        }
    }
}
