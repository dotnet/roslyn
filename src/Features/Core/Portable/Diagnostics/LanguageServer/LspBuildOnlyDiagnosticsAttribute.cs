﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <inheritdoc cref="ILspBuildOnlyDiagnostics"/>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class LspBuildOnlyDiagnosticsAttribute(string languageName, params string[] buildOnlyDiagnostics) : ExportAttribute(typeof(ILspBuildOnlyDiagnostics)), ILspBuildOnlyDiagnosticsMetadata
    {
        public string LanguageName { get; } = languageName;
        public string[] BuildOnlyDiagnostics { get; } = buildOnlyDiagnostics;
    }
}
