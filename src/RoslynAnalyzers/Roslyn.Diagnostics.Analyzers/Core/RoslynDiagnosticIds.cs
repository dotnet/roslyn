// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.Diagnostics.Analyzers
{
    internal static class RoslynDiagnosticIds
    {
        public const string UseEmptyEnumerableRuleId = "RS0001";
        public const string UseSingletonEnumerableRuleId = "RS0002";
        // public const string DirectlyAwaitingTaskAnalyzerRuleId = "RS0003";           // Now CA2007 => Microsoft.ApiDesignGuidelines.Analyzers.DoNotDirectlyAwaitATaskAnalyzer
        public const string UseSiteDiagnosticsCheckerRuleId = "RS0004";
        //public const string DoNotUseCodeActionCreateRuleId = "RS0005";                // Removed (see https://github.com/dotnet/roslyn-analyzers/issues/5947)
        public const string MixedVersionsOfMefAttributesRuleId = "RS0006";
        // public const string UseArrayEmptyRuleId = "RS0007";                          // Now CA1825 => System.Runtime.Analyzers.AvoidZeroLengthArrayAllocationsAnalyzer
        // public const string ImplementIEquatableRuleId = "RS0008";                    // Now CA1067 => Microsoft.ApiDesignGuidelines.Analyzers.EquatableAnalyzer
        // public const string OverrideObjectEqualsRuleId = "RS0009";                   // Now CA1815 => Microsoft.ApiDesignGuidelines.Analyzers.OverrideEqualsAndOperatorEqualsOnValueTypesAnalyzer
        // public const string DoNotUseVerbatimCrefsRuleId = "RS0010";                  // Now RS0010 => XmlDocumentationComments.Analyzers.AvoidUsingCrefTagsWithAPrefixAnalyzer
        // public const string CancellationTokenMustBeLastRuleId = "RS0011";            // Now CA1068 => Microsoft.ApiDesignGuidelines.Analyzers.CancellationTokenParametersMustComeLastAnalyzer
        // public const string DoNotCallToImmutableArrayRuleId = "RS0012";              // Now CA2009 => System.Collections.Immutable.Analyzers.DoNotCallToImmutableCollectionOnAnImmutableCollectionValueAnalyzer
        //public const string DoNotAccessDiagnosticDescriptorRuleId = "RS0013";         // Removed (see https://github.com/dotnet/roslyn-analyzers/issues/3560)
        // public const string DoNotCallLinqOnIndexable = "RS0014";                     // Now RS0014 => System.Runtime.Analyzers.DoNotUseEnumerableMethodsOnIndexableCollectionsInsteadUseTheCollectionDirectlyAnalyzer
        // public const string ConsumePreserveSigRuleId = "RS0015";                     // Now CA2010 => System.Runtime.InteropServices.Analyzers.AlwaysConsumeTheValueReturnedByMethodsMarkedWithPreserveSigAttributeAnalyzer
        public const string DeclarePublicApiRuleId = "RS0016";
        public const string RemoveDeletedPublicApiRuleId = "RS0017";
        // public const string DoNotCreateTasksWithoutTaskSchedulerRuleId = "RS0018";   // Now CA2008 => System.Threading.Tasks.Analyzers.DoNotCreateTasksWithoutPassingATaskSchedulerAnalyzer
        public const string SymbolDeclaredEventRuleId = "RS0019";
        // public const string DeadCodeRuleId = "RS0020";                               // Now ???
        // public const string DeadCodeTriggerRuleId = "RS0021";                        // Now ???
        public const string ExposedNoninstantiableTypeRuleIdPublic = "RS0022";
        public const string MissingSharedAttributeRuleId = "RS0023";
        public const string PublicApiFilesInvalid = "RS0024";
        public const string DuplicatedSymbolInPublicApiFiles = "RS0025";
        public const string AvoidMultipleOverloadsWithOptionalParametersPublic = "RS0026";
        public const string OverloadWithOptionalParametersShouldHaveMostParametersPublic = "RS0027";
        public const string RoslynAnalyzerMustUseIdInSpecifiedRangeRuleId = "RS0028";
        public const string RoslynAnalyzerMustUseCategoriesFromSpecifiedRangeRuleId = "RS0029";
        public const string SymbolIsBannedRuleId = "RS0030";
        public const string DuplicateBannedSymbolRuleId = "RS0031";
        public const string TestExportsShouldNotBeDiscoverableRuleId = "RS0032";
        public const string ImportingConstructorShouldBeObsoleteRuleId = "RS0033";
        public const string ExportedPartsShouldHaveImportingConstructorRuleId = "RS0034";
        public const string RestrictedInternalsVisibleToRuleId = "RS0035";
        public const string AnnotatePublicApiRuleId = "RS0036";
        public const string ShouldAnnotatePublicApiFilesRuleId = "RS0037";
        public const string PreferNullLiteralRuleId = "RS0038";
        public const string RelaxTestNamingSuppressionRuleId = "RS0039";
        public const string DefaultableTypeShouldHaveDefaultableFieldsRuleId = "RS0040";
        public const string ObliviousPublicApiRuleId = "RS0041";
        public const string DoNotCopyValueRuleId = "RS0042";
        public const string DoNotCallGetTestAccessorRuleId = "RS0043";
        // public const string CreateTestAccessorRuleId = "RS0044"; // Now converted to a refactoring
        // public const string ExposeMemberForTestingRuleId = "RS0045"; // Now converted to a refactoring
        public const string AvoidOptSuffixForNullableEnableCodeRuleId = "RS0046";
        public const string NamedTypeFullNameNotNullSuppressionRuleId = "RS0047";
        public const string PublicApiFileMissing = "RS0048";
        public const string TemporaryArrayAsRefRuleId = "RS0049";

        public const string RemovedApiIsNotActuallyRemovedRuleId = "RS0050";

        public const string DeclareInternalApiRuleId = "RS0051";
        public const string RemoveDeletedInternalApiRuleId = "RS0052";
        public const string InternalApiFilesInvalid = "RS0053";
        public const string DuplicatedSymbolInInternalApiFiles = "RS0054";
        public const string AnnotateInternalApiRuleId = "RS0055";
        public const string ShouldAnnotateInternalApiFilesRuleId = "RS0056";
        public const string ObliviousInternalApiRuleId = "RS0057";
        public const string InternalApiFileMissing = "RS0058";
        public const string AvoidMultipleOverloadsWithOptionalParametersInternal = "RS0059";
        public const string OverloadWithOptionalParametersShouldHaveMostParametersInternal = "RS0060";
        public const string ExposedNoninstantiableTypeRuleIdInternal = "RS0061";
        public const string DoNotCapturePrimaryConstructorParametersRuleId = "RS0062";
        public const string DoNotUseInterpolatedStringsWithDebugAssertRuleId = "RS0063";

        //public const string WrapStatementsRuleId = "RS0100"; // Now ported to dotnet/roslyn https://github.com/dotnet/roslyn/pull/50358
        //public const string BlankLinesRuleId = "RS0101"; // Now ported to dotnet/roslyn https://github.com/dotnet/roslyn/pull/50358
        //public const string BracePlacementRuleId = "RS0102"; // Now ported to dotnet/roslyn https://github.com/dotnet/roslyn/pull/50358
    }
}
