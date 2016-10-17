// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class IDEDiagnosticIds
    {
        public const string SimplifyNamesDiagnosticId = "IDE0001";
        public const string SimplifyMemberAccessDiagnosticId = "IDE0002";
        public const string RemoveQualificationDiagnosticId = "IDE0003";
        public const string RemoveUnnecessaryCastDiagnosticId = "IDE0004";
        public const string RemoveUnnecessaryImportsDiagnosticId = "IDE0005";
        public const string IntellisenseBuildFailedDiagnosticId = "IDE0006";
        public const string UseImplicitTypeDiagnosticId = "IDE0007";
        public const string UseExplicitTypeDiagnosticId = "IDE0008";
        public const string AddQualificationDiagnosticId = "IDE0009";
        public const string PopulateSwitchDiagnosticId = "IDE0010";
        public const string AddBracesDiagnosticId = "IDE0011";
        public const string PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId = "IDE0012";
        public const string PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId = "IDE0013";
        public const string PreferFrameworkTypeInDeclarationsDiagnosticId = "IDE0014";
        public const string PreferFrameworkTypeInMemberAccessDiagnosticId = "IDE0015";
        public const string UseThrowExpressionDiagnosticId = "IDE0016";
        public const string UseObjectInitializerDiagnosticId = "IDE0017";
        public const string InlineDeclarationDiagnosticId = "IDE0018";
        public const string InlineAsTypeCheckId = "IDE0019";
        public const string InlineIsTypeCheckId = "IDE0020";

        public const string UseExpressionBodyForConstructorsDiagnosticId = "IDE0020";
        public const string UseExpressionBodyForMethodsDiagnosticId = "IDE0021";
        public const string UseExpressionBodyForConversionOperatorsDiagnosticId = "IDE0022";
        public const string UseExpressionBodyForOperatorsDiagnosticId = "IDE0023";
        public const string UseExpressionBodyForPropertiesDiagnosticId = "IDE0024";
        public const string UseExpressionBodyForIndexersDiagnosticId = "IDE0025";
        public const string UseExpressionBodyForAccessorsDiagnosticId = "IDE0026";

        // Analyzer error Ids
        public const string AnalyzerChangedId = "IDE1001";
        public const string AnalyzerDependencyConflictId = "IDE1002";
        public const string MissingAnalyzerReferenceId = "IDE1003";
        public const string ErrorReadingRulesetId = "IDE1004";
        public const string InvokeDelegateWithConditionalAccessId = "IDE1005";
        public const string NamingRuleId = "IDE1006";
    }
}
