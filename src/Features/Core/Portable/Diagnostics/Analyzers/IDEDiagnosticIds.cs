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

        // IDE0012-IDE0015 deprecated and replaced with PreferBuiltInOrFrameworkTypeDiagnosticId (IDE0049)
        // public const string PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId = "IDE0012";
        // public const string PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId = "IDE0013";
        // public const string PreferFrameworkTypeInDeclarationsDiagnosticId = "IDE0014";
        // public const string PreferFrameworkTypeInMemberAccessDiagnosticId = "IDE0015";

        public const string UseThrowExpressionDiagnosticId = "IDE0016";
        public const string UseObjectInitializerDiagnosticId = "IDE0017";
        public const string InlineDeclarationDiagnosticId = "IDE0018";
        public const string InlineAsTypeCheckId = "IDE0019";
        public const string InlineIsTypeCheckId = "IDE0020";

        public const string UseExpressionBodyForConstructorsDiagnosticId = "IDE0021";
        public const string UseExpressionBodyForMethodsDiagnosticId = "IDE0022";
        public const string UseExpressionBodyForConversionOperatorsDiagnosticId = "IDE0023";
        public const string UseExpressionBodyForOperatorsDiagnosticId = "IDE0024";
        public const string UseExpressionBodyForPropertiesDiagnosticId = "IDE0025";
        public const string UseExpressionBodyForIndexersDiagnosticId = "IDE0026";
        public const string UseExpressionBodyForAccessorsDiagnosticId = "IDE0027";

        public const string UseCollectionInitializerDiagnosticId = "IDE0028";

        public const string UseCoalesceExpressionDiagnosticId = "IDE0029";
        public const string UseCoalesceExpressionForNullableDiagnosticId = "IDE0030";

        public const string UseNullPropagationDiagnosticId = "IDE0031";

        public const string UseAutoPropertyDiagnosticId = "IDE0032";

        public const string UseExplicitTupleNameDiagnosticId = "IDE0033";

        public const string UseDefaultLiteralDiagnosticId = "IDE0034";

        public const string RemoveUnreachableCodeDiagnosticId = "IDE0035";

        public const string OrderModifiersDiagnosticId = "IDE0036";

        public const string UseInferredMemberNameDiagnosticId = "IDE0037";

        public const string InlineIsTypeWithoutNameCheckDiagnosticsId = "IDE0038";

        public const string UseLocalFunctionDiagnosticId = "IDE0039";

        public const string AddAccessibilityModifiersDiagnosticId = "IDE0040";

        public const string UseIsNullCheckDiagnosticId = "IDE0041";

        public const string UseDeconstructionDiagnosticId = "IDE0042";

        public const string ValidateFormatStringDiagnosticID = "IDE0043";

        public const string MakeFieldReadonlyDiagnosticId = "IDE0044";

        public const string UseConditionalExpressionForAssignmentDiagnosticId = "IDE0045";
        public const string UseConditionalExpressionForReturnDiagnosticId = "IDE0046";

        public const string RemoveUnnecessaryParenthesesDiagnosticId = "IDE0047";
        public const string AddRequiredParenthesesDiagnosticId = "IDE0048";

        public const string PreferBuiltInOrFrameworkTypeDiagnosticId = "IDE0049";

        public const string ConvertAnonymousTypeToTupleDiagnosticId = "IDE0050";

        public const string RemoveUnusedMembersDiagnosticId = "IDE0051";
        public const string RemoveUnreadMembersDiagnosticId = "IDE0052";

        public const string UseExpressionBodyForLambdaExpressionsDiagnosticId = "IDE0053";

        public const string UseCompoundAssignmentDiagnosticId = "IDE0054";

        public const string FormattingDiagnosticId = "IDE0055";

        public const string UseIndexOperatorDiagnosticId = "IDE0056";
        public const string UseRangeOperatorDiagnosticId = "IDE0057";

        public const string ExpressionValueIsUnusedDiagnosticId = "IDE0058";
        public const string ValueAssignedIsUnusedDiagnosticId = "IDE0059";
        public const string UnusedParameterDiagnosticId = "IDE0060";

        // Conceptually belongs with IDE0021-IDE0027 & IDE0053, but is here because it was added later
        public const string UseExpressionBodyForLocalFunctionsDiagnosticId = "IDE0061";

        public const string MakeLocalFunctionStaticDiagnosticId = "IDE0062";
        public const string UseSimpleUsingStatementDiagnosticId = "IDE0063";

        public const string MakeStructFieldsWritable = "IDE0064";

        public const string MoveMisplacedUsingDirectivesDiagnosticId = "IDE0065";

        public const string ConvertSwitchStatementToExpressionDiagnosticId = "IDE0066";

        public const string DisposeObjectsBeforeLosingScopeDiagnosticId = "IDE0067";
        public const string UseRecommendedDisposePatternDiagnosticId = "IDE0068";
        public const string DisposableFieldsShouldBeDisposedDiagnosticId = "IDE0069";

        // Analyzer error Ids
        public const string AnalyzerChangedId = "IDE1001";
        public const string AnalyzerDependencyConflictId = "IDE1002";
        public const string MissingAnalyzerReferenceId = "IDE1003";
        public const string ErrorReadingRulesetId = "IDE1004";
        public const string InvokeDelegateWithConditionalAccessId = "IDE1005";
        public const string NamingRuleId = "IDE1006";
        public const string UnboundIdentifierId = "IDE1007";
        public const string UnboundConstructorId = "IDE1008";
    }
}
