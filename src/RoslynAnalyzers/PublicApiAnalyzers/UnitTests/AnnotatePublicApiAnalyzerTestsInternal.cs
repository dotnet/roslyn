// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Diagnostics.Analyzers;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers.UnitTests
{
    public class AnnotatePublicApiAnalyzerTestsInternal : AnnotatePublicApiAnalyzerTestsBase
    {
        protected override bool IsInternalTest => true;
        protected override string EnabledModifier => "internal";
        protected override string ShippedFileName => DeclarePublicApiAnalyzer.InternalShippedFileName;
        protected override string UnshippedFileName => DeclarePublicApiAnalyzer.InternalUnshippedFileName;
        protected override string UnshippedFileNamePrefix => DeclarePublicApiAnalyzer.InternalUnshippedFileNamePrefix;
        protected override string AnnotateApiId => RoslynDiagnosticIds.AnnotateInternalApiRuleId;
        protected override string ShouldAnnotateApiFilesId => RoslynDiagnosticIds.ShouldAnnotateInternalApiFilesRuleId;
        protected override string ObliviousApiId => RoslynDiagnosticIds.ObliviousInternalApiRuleId;

        protected override IEnumerable<string> DisabledDiagnostics => new[] {
            RoslynDiagnosticIds.DeclarePublicApiRuleId,
            RoslynDiagnosticIds.RemoveDeletedPublicApiRuleId,
            RoslynDiagnosticIds.PublicApiFilesInvalid,
            RoslynDiagnosticIds.DuplicatedSymbolInPublicApiFiles,
            RoslynDiagnosticIds.AnnotatePublicApiRuleId,
            RoslynDiagnosticIds.ShouldAnnotatePublicApiFilesRuleId,
            RoslynDiagnosticIds.ObliviousPublicApiRuleId,
            RoslynDiagnosticIds.PublicApiFileMissing,
            RoslynDiagnosticIds.AvoidMultipleOverloadsWithOptionalParametersPublic,
            RoslynDiagnosticIds.OverloadWithOptionalParametersShouldHaveMostParametersPublic,
            RoslynDiagnosticIds.ExposedNoninstantiableTypeRuleIdPublic,
        };
    }
}
