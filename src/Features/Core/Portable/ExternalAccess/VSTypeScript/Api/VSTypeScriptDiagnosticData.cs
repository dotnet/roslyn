﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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

        /// <summary>
        /// Note: the <paramref name="useMapped"/> parameter is ignored.
        /// </summary>
        [Obsolete("Use overload that only takes a SourceText")]
        public LinePositionSpan GetLinePositionSpan(SourceText sourceText, bool useMapped)
        {
            // TypeScript has no concept of mapped spans, so this should never be passed 'true'.
            Contract.ThrowIfTrue(useMapped);
            return _data.DataLocation.UnmappedFileSpan.GetClampedSpan(sourceText);
        }

        public LinePositionSpan GetLinePositionSpan(SourceText sourceText)
            => _data.DataLocation.UnmappedFileSpan.GetClampedSpan(sourceText);
    }
}
