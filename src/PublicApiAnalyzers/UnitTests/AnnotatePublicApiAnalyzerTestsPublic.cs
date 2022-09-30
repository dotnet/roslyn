// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
