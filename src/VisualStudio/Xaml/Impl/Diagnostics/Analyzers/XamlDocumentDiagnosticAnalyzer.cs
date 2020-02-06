﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.VisualStudio.LanguageServices.Xaml;

namespace Microsoft.CodeAnalysis.Xaml.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(StringConstants.XamlLanguageName)]
    internal class XamlDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return XamlTextViewCreationListener.AnalyzerService?.SupportedDiagnostics ?? ImmutableArray<DiagnosticDescriptor>.Empty;
            }
        }

        public override async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            if (XamlTextViewCreationListener.AnalyzerService == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            return await XamlTextViewCreationListener.AnalyzerService.AnalyzeSyntaxAsync(document, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            if (XamlTextViewCreationListener.AnalyzerService == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            return await XamlTextViewCreationListener.AnalyzerService.AnalyzeSemanticsAsync(document, cancellationToken).ConfigureAwait(false);
        }
    }
}
