// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <inheritdoc cref="ILspBuildOnlyDiagnostics"/>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class LspBuildOnlyDiagnosticsAttribute : ExportAttribute, ILspBuildOnlyDiagnosticsMetadata
    {
        public string[] BuildOnlyDiagnostics { get; }

        public LspBuildOnlyDiagnosticsAttribute(params string[] buildOnlyDiagnostics) : base(typeof(ILspBuildOnlyDiagnostics))
        {
            BuildOnlyDiagnostics = buildOnlyDiagnostics;
        }
    }
}
