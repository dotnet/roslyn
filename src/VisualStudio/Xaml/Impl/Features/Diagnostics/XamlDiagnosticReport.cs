// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics
{
    internal class XamlDiagnosticReport
    {
        public string? ResultId { get; set; }
        public ImmutableArray<XamlDiagnostic>? Diagnostics { get; set; }

        public XamlDiagnosticReport(string? resultId = null, ImmutableArray<XamlDiagnostic>? diagnostics = null)
        {
            this.ResultId = resultId;
            this.Diagnostics = diagnostics;
        }
    }
}
