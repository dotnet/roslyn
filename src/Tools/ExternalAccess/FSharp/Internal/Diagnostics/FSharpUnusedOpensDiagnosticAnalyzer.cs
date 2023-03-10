// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Diagnostics
{
    [Shared]
    [ExportLanguageService(typeof(FSharpUnusedOpensDiagnosticAnalyzerService), LanguageNames.FSharp)]
    internal class FSharpUnusedOpensDiagnosticAnalyzerService : ILanguageService
    {
        private readonly IFSharpUnusedOpensDiagnosticAnalyzer _analyzer;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpUnusedOpensDiagnosticAnalyzerService(IFSharpUnusedOpensDiagnosticAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(DiagnosticDescriptor descriptor, Document document, CancellationToken cancellationToken)
        {
            return _analyzer.AnalyzeSemanticsAsync(descriptor, document, cancellationToken);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.FSharp)]
    internal class FSharpUnusedOpensDeclarationsDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor _descriptor =
            new DiagnosticDescriptor(
                    IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId,
                    ExternalAccessFSharpResources.RemoveUnusedOpens,
                    ExternalAccessFSharpResources.UnusedOpens,
                    DiagnosticCategory.Style, DiagnosticSeverity.Hidden, isEnabledByDefault: true, customTags: FSharpDiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_descriptor);

        public override int Priority => 90; // Default = 50

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            var analyzer = document.Project.Services.GetService<FSharpUnusedOpensDiagnosticAnalyzerService>();
            if (analyzer == null)
            {
                return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
            }

            return analyzer.AnalyzeSemanticsAsync(_descriptor, document, cancellationToken);
        }

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public bool OpenFileOnly(Workspace workspace)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            return true;
        }
    }
}
