// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Diagnostics.Analyzers;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers.UnitTests
{
    public class DeclarePublicApiAnalyzerTestsPublic : DeclarePublicApiAnalyzerTestsBase
    {
        protected override bool IsInternalTest => false;
        protected override string EnabledModifierCSharp => "public";
        protected override string DisabledModifierCSharp => "internal";
        protected override string EnabledModifierVB => "Public";
        protected override string DisabledModifierVB => "Friend";
        protected override string ShippedFileName => DeclarePublicApiAnalyzer.PublicShippedFileName;
        protected override string UnshippedFileName => DeclarePublicApiAnalyzer.PublicUnshippedFileName;
        protected override string UnshippedFileNamePrefix => DeclarePublicApiAnalyzer.PublicUnshippedFileNamePrefix;
        protected override string AddNewApiId => RoslynDiagnosticIds.DeclarePublicApiRuleId;
        protected override string RemoveApiId => RoslynDiagnosticIds.RemoveDeletedPublicApiRuleId;
        protected override string DuplicatedSymbolInApiFileId => RoslynDiagnosticIds.DuplicatedSymbolInPublicApiFiles;
        protected override string ShouldAnnotateApiFilesId => RoslynDiagnosticIds.ShouldAnnotatePublicApiFilesRuleId;
        protected override string ObliviousApiId => RoslynDiagnosticIds.ObliviousPublicApiRuleId;
        protected override DiagnosticDescriptor DeclareNewApiRule => DeclarePublicApiAnalyzer.DeclareNewPublicApiRule;
        protected override DiagnosticDescriptor RemoveDeletedApiRule => DeclarePublicApiAnalyzer.RemoveDeletedPublicApiRule;
        protected override DiagnosticDescriptor DuplicateSymbolInApiFiles => DeclarePublicApiAnalyzer.DuplicateSymbolInPublicApiFiles;
        protected override DiagnosticDescriptor AvoidMultipleOverloadsWithOptionalParameters => DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParametersPublic;
        protected override DiagnosticDescriptor OverloadWithOptionalParametersShouldHaveMostParameters => DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParametersPublic;
        protected override DiagnosticDescriptor AnnotateApiRule => DeclarePublicApiAnalyzer.AnnotatePublicApiRule;
        protected override DiagnosticDescriptor ObliviousApiRule => DeclarePublicApiAnalyzer.ObliviousPublicApiRule;
        protected override DiagnosticDescriptor ApiFilesInvalid => DeclarePublicApiAnalyzer.PublicApiFilesInvalid;
        protected override DiagnosticDescriptor ApiFileMissing => DeclarePublicApiAnalyzer.PublicApiFileMissing;

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
