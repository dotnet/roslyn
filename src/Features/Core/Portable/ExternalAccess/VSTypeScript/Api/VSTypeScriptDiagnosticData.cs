// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptDiagnosticData
    {
        private readonly DiagnosticData _data;

        internal VSTypeScriptDiagnosticData(DiagnosticData data)
        {
            _data = data;
        }

        public DiagnosticSeverity Severity
            => _data.Severity;

        public string? Message
            => _data.Message;

        public string Id
            => _data.Id;

        public ImmutableArray<string> CustomTags
            => _data.CustomTags;

        public LinePositionSpan GetLinePositionSpan(SourceText sourceText, bool useMapped)
            => DiagnosticData.GetLinePositionSpan(_data.DataLocation, sourceText, useMapped);
    }
}
