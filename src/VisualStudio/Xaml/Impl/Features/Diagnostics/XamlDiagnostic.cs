﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics
{
    internal class XamlDiagnostic
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public XamlDiagnosticSeverity Severity { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public string? Tool { get; set; }
        public string? ExtendedMessage { get; set; }
        public string[]? CustomTags { get; set; }
    }
}
