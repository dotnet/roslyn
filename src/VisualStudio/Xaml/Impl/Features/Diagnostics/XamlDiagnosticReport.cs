// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics
{
    internal class XamlDiagnosticReport
    {
        public string? ResultId { get; set; }
        public XamlDiagnostic[]? Diagnostics { get; set; }

        public XamlDiagnosticReport(string? resultId = null, XamlDiagnostic[]? diagnostics = null)
        {
            this.ResultId = resultId;
            this.Diagnostics = diagnostics;
        }
    }
}
