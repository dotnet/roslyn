// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Diagnostics.Analyzers;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers.UnitTests
{
    public class DeclarePublicApiAnalyzerTestsInternal : DeclarePublicApiAnalyzerTestsBase
    {
        protected override bool IsInternalTest => true;
        protected override string EnabledModifierCSharp => "internal";
        protected override string DisabledModifierCSharp => "public";
        protected override string EnabledModifierVB => "Friend";
        protected override string DisabledModifierVB => "Public";
        protected override string ShippedFileName => DeclarePublicApiAnalyzer.InternalShippedFileName;
        protected override string UnshippedFileName => DeclarePublicApiAnalyzer.InternalUnshippedFileName;
        protected override string UnshippedFileNamePrefix => DeclarePublicApiAnalyzer.InternalUnshippedFileNamePrefix;
        protected override string AddNewApiId => RoslynDiagnosticIds.DeclareInternalApiRuleId;
        protected override string RemoveApiId => RoslynDiagnosticIds.RemoveDeletedInternalApiRuleId;
        protected override string DuplicatedSymbolInApiFileId => RoslynDiagnosticIds.DuplicatedSymbolInInternalApiFiles;
        protected override string ShouldAnnotateApiFilesId => RoslynDiagnosticIds.ShouldAnnotateInternalApiFilesRuleId;
        protected override string ObliviousApiId => RoslynDiagnosticIds.ObliviousInternalApiRuleId;
        protected override DiagnosticDescriptor DeclareNewApiRule => DeclarePublicApiAnalyzer.DeclareNewInternalApiRule;
        protected override DiagnosticDescriptor RemoveDeletedApiRule => DeclarePublicApiAnalyzer.RemoveDeletedInternalApiRule;
        protected override DiagnosticDescriptor DuplicateSymbolInApiFiles => DeclarePublicApiAnalyzer.DuplicateSymbolInInternalApiFiles;
        protected override DiagnosticDescriptor AvoidMultipleOverloadsWithOptionalParameters => DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParametersInternal;
        protected override DiagnosticDescriptor OverloadWithOptionalParametersShouldHaveMostParameters => DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParametersInternal;
        protected override DiagnosticDescriptor AnnotateApiRule => DeclarePublicApiAnalyzer.AnnotateInternalApiRule;
        protected override DiagnosticDescriptor ObliviousApiRule => DeclarePublicApiAnalyzer.ObliviousInternalApiRule;
        protected override DiagnosticDescriptor ApiFilesInvalid => DeclarePublicApiAnalyzer.InternalApiFilesInvalid;
        protected override DiagnosticDescriptor ApiFileMissing => DeclarePublicApiAnalyzer.InternalApiFileMissing;

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
