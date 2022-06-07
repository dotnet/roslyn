// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Diagnostics
{
    [Shared]
    [ExportLanguageService(typeof(FSharpUnusedDeclarationsDiagnosticAnalyzerService), LanguageNames.FSharp)]
    internal class FSharpUnusedDeclarationsDiagnosticAnalyzerService : ILanguageService
    {
        private readonly IFSharpUnusedDeclarationsDiagnosticAnalyzer _analyzer;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpUnusedDeclarationsDiagnosticAnalyzerService(IFSharpUnusedDeclarationsDiagnosticAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(DiagnosticDescriptor descriptor, Document document, CancellationToken cancellationToken)
        {
            return _analyzer.AnalyzeSemanticsAsync(descriptor, document, cancellationToken);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.FSharp)]
    internal class FSharpUnusedDeclarationsDiagnosticAnalyzer : DocumentDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private const string DescriptorId = "FS1182";

        private readonly DiagnosticDescriptor _descriptor =
            new DiagnosticDescriptor(
                    DescriptorId,
                    ExternalAccessFSharpResources.TheValueIsUnused,
                    ExternalAccessFSharpResources.TheValueIsUnused,
                    DiagnosticCategory.Style, DiagnosticSeverity.Hidden, isEnabledByDefault: true, customTags: FSharpDiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_descriptor);

        public CodeActionRequestPriority RequestPriority => CodeActionRequestPriority.Normal;

        public override int Priority => 80; // Default = 50

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            var analyzer = document.Project.LanguageServices.GetService<FSharpUnusedDeclarationsDiagnosticAnalyzerService>();
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

        public bool OpenFileOnly(SimplifierOptions options)
        {
            return true;
        }
    }
}
