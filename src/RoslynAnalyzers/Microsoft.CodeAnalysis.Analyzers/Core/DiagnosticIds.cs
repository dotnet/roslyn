// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Analyzers
{
    internal static class DiagnosticIds
    {
        public const string MissingDiagnosticAnalyzerAttributeRuleId = "RS1001";
        public const string MissingKindArgumentToRegisterActionRuleId = "RS1002";
        public const string UnsupportedSymbolKindArgumentRuleId = "RS1003";
        public const string AddLanguageSupportToAnalyzerRuleId = "RS1004";
        public const string InvalidReportDiagnosticRuleId = "RS1005";
        public const string InvalidSyntaxKindTypeArgumentRuleId = "RS1006";
        public const string UseLocalizableStringsInDescriptorRuleId = "RS1007";
        public const string DoNotStorePerCompilationDataOntoFieldsRuleId = "RS1008";
        public const string InternalImplementationOnlyRuleId = "RS1009";
        public const string CreateCodeActionWithEquivalenceKeyRuleId = "RS1010";
        public const string OverrideCodeActionEquivalenceKeyRuleId = "RS1011";
        public const string StartActionWithNoRegisteredActionsRuleId = "RS1012";
        public const string StartActionWithOnlyEndActionRuleId = "RS1013";
        public const string DoNotIgnoreReturnValueOnImmutableObjectMethodInvocation = "RS1014";
        public const string ProvideHelpUriInDescriptorRuleId = "RS1015";
        public const string OverrideGetFixAllProviderRuleId = "RS1016";
        public const string DiagnosticIdMustBeAConstantRuleId = "RS1017";
        public const string DiagnosticIdMustBeInSpecifiedFormatRuleId = "RS1018";
        public const string UseUniqueDiagnosticIdRuleId = "RS1019";
        public const string UseCategoriesFromSpecifiedRangeRuleId = "RS1020";
        public const string AnalyzerCategoryAndIdRangeFileInvalidRuleId = "RS1021";
        public const string DoNotUseTypesFromAssemblyRuleId = "RS1022";
        public const string UpgradeMSBuildWorkspaceRuleId = "RS1023";
        public const string CompareSymbolsCorrectlyRuleId = "RS1024";
        public const string ConfigureGeneratedCodeAnalysisRuleId = "RS1025";
        public const string EnableConcurrentExecutionRuleId = "RS1026";
        public const string TypeIsNotDiagnosticAnalyzerRuleId = "RS1027";
        public const string ProvideCustomTagsInDescriptorRuleId = "RS1028";
        public const string DoNotUseReservedDiagnosticIdRuleId = "RS1029";
        public const string DoNotUseCompilationGetSemanticModelRuleId = "RS1030";
        public const string DefineDiagnosticTitleCorrectlyRuleId = "RS1031";
        public const string DefineDiagnosticMessageCorrectlyRuleId = "RS1032";
        public const string DefineDiagnosticDescriptionCorrectlyRuleId = "RS1033";
        public const string PreferIsKindRuleId = "RS1034";
        public const string SymbolIsBannedInAnalyzersRuleId = "RS1035";
        public const string NoSettingSpecifiedSymbolIsBannedInAnalyzersRuleId = "RS1036";
        public const string AddCompilationEndCustomTagRuleId = "RS1037";
        public const string DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleId = "RS1038";
        public const string SemanticModelGetDeclaredSymbolAlwaysReturnsNull = "RS1039";
        public const string SemanticModelGetDeclaredSymbolAlwaysReturnsNullForField = "RS1040";
        public const string DoNotRegisterCompilerTypesWithBadTargetFrameworkRuleId = "RS1041";
        public const string ImplementationIsObsoleteRuleId = "RS1042";
        public const string DoNotUseFileTypesForAnalyzersOrGenerators = "RS1043";

        // Release tracking analyzer IDs
        public const string DeclareDiagnosticIdInAnalyzerReleaseRuleId = "RS2000";
        public const string UpdateDiagnosticIdInAnalyzerReleaseRuleId = "RS2001";
        public const string RemoveUnshippedDeletedDiagnosticIdRuleId = "RS2002";
        public const string RemoveShippedDeletedDiagnosticIdRuleId = "RS2003";
        public const string UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdRuleId = "RS2004";
        public const string RemoveDuplicateEntriesForAnalyzerReleaseRuleId = "RS2005";
        public const string RemoveDuplicateEntriesBetweenAnalyzerReleasesRuleId = "RS2006";
        public const string InvalidEntryInAnalyzerReleasesFileRuleId = "RS2007";
        public const string EnableAnalyzerReleaseTrackingRuleId = "RS2008";
    }
}
