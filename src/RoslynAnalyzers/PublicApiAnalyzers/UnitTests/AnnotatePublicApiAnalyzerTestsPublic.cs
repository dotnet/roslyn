// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Diagnostics.Analyzers;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers.UnitTests
{
    public class AnnotatePublicApiAnalyzerTestsPublic : AnnotatePublicApiAnalyzerTestsBase
    {
        protected override bool IsInternalTest => false;
        protected override string EnabledModifier => "public";
        protected override string ShippedFileName => DeclarePublicApiAnalyzer.PublicShippedFileName;
        protected override string UnshippedFileName => DeclarePublicApiAnalyzer.PublicUnshippedFileName;
        protected override string UnshippedFileNamePrefix => DeclarePublicApiAnalyzer.PublicUnshippedFileNamePrefix;
        protected override string AnnotateApiId => RoslynDiagnosticIds.AnnotatePublicApiRuleId;
        protected override string ShouldAnnotateApiFilesId => RoslynDiagnosticIds.ShouldAnnotatePublicApiFilesRuleId;
        protected override string ObliviousApiId => RoslynDiagnosticIds.ObliviousPublicApiRuleId;

        protected override IEnumerable<string> DisabledDiagnostics => new[] {
            RoslynDiagnosticIds.DeclareInternalApiRuleId,
            RoslynDiagnosticIds.RemoveDeletedInternalApiRuleId,
            RoslynDiagnosticIds.InternalApiFilesInvalid,
            RoslynDiagnosticIds.DuplicatedSymbolInInternalApiFiles,
            RoslynDiagnosticIds.AnnotateInternalApiRuleId,
            RoslynDiagnosticIds.ShouldAnnotateInternalApiFilesRuleId,
            RoslynDiagnosticIds.ObliviousInternalApiRuleId,
            RoslynDiagnosticIds.InternalApiFileMissing,
            RoslynDiagnosticIds.AvoidMultipleOverloadsWithOptionalParametersInternal,
            RoslynDiagnosticIds.OverloadWithOptionalParametersShouldHaveMostParametersInternal,
            RoslynDiagnosticIds.ExposedNoninstantiableTypeRuleIdInternal,
        };
    }
}
