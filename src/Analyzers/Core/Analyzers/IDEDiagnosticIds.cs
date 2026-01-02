// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class IDEDiagnosticIds
{
    public const string SimplifyNamesDiagnosticId = "IDE0001";
    public const string SimplifyMemberAccessDiagnosticId = "IDE0002";
    public const string RemoveThisOrMeQualificationDiagnosticId = "IDE0003";
    public const string RemoveUnnecessaryCastDiagnosticId = "IDE0004";

    public const string RemoveUnnecessaryImportsDiagnosticId = "IDE0005";
    public const string RemoveUnnecessaryImportsGeneratedCodeDiagnosticId = RemoveUnnecessaryImportsDiagnosticId + "_gen";

    public const string IntellisenseBuildFailedDiagnosticId = "IDE0006";
    public const string UseImplicitTypeDiagnosticId = "IDE0007";
    public const string UseExplicitTypeDiagnosticId = "IDE0008";
    public const string AddThisOrMeQualificationDiagnosticId = "IDE0009";
    public const string PopulateSwitchStatementDiagnosticId = "IDE0010";
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

    public const string UseCoalesceExpressionForTernaryConditionalCheckDiagnosticId = "IDE0029";
    public const string UseCoalesceExpressionForNullableTernaryConditionalCheckDiagnosticId = "IDE0030";

    public const string UseNullPropagationDiagnosticId = "IDE0031";

    public const string UseAutoPropertyDiagnosticId = "IDE0032";

    public const string UseExplicitTupleNameDiagnosticId = "IDE0033";

    public const string UseDefaultLiteralDiagnosticId = "IDE0034";

    public const string RemoveUnreachableCodeDiagnosticId = "IDE0035";

    public const string OrderModifiersDiagnosticId = "IDE0036";

    public const string UseInferredMemberNameDiagnosticId = "IDE0037";

    public const string InlineIsTypeWithoutNameCheckDiagnosticsId = "IDE0038";

    public const string UseLocalFunctionDiagnosticId = "IDE0039";

    public const string AddOrRemoveAccessibilityModifiersDiagnosticId = "IDE0040";

    public const string UseIsNullCheckDiagnosticId = "IDE0041";

    public const string UseDeconstructionDiagnosticId = "IDE0042";

    public const string ValidateFormatStringDiagnosticID = "IDE0043";

    public const string MakeFieldReadonlyDiagnosticId = "IDE0044";

    public const string UseConditionalExpressionForAssignmentDiagnosticId = "IDE0045";
    public const string UseConditionalExpressionForReturnDiagnosticId = "IDE0046";

    public const string RemoveUnnecessaryParenthesesDiagnosticId = "IDE0047";
    public const string AddRequiredParenthesesDiagnosticId = "IDE0048";

    public const string PreferBuiltInOrFrameworkTypeDiagnosticId = "IDE0049";

    // public const string ConvertAnonymousTypeToTupleDiagnosticId = "IDE0050";

    public const string RemoveUnusedMembersDiagnosticId = "IDE0051";
    public const string RemoveUnreadMembersDiagnosticId = "IDE0052";

    public const string UseExpressionBodyForLambdaExpressionsDiagnosticId = "IDE0053";

    public const string UseCompoundAssignmentDiagnosticId = "IDE0054";

    public const string FormattingDiagnosticId = FormattingDiagnosticIds.FormattingDiagnosticId;

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

    // IDE0067-IDE0069 deprecated in favor of CA2000 and CA2213
    // public const string DisposeObjectsBeforeLosingScopeDiagnosticId = "IDE0067";
    // public const string UseRecommendedDisposePatternDiagnosticId = "IDE0068";
    // public const string DisposableFieldsShouldBeDisposedDiagnosticId = "IDE0069";

    public const string UseSystemHashCode = "IDE0070";

    public const string SimplifyInterpolationId = "IDE0071";

    public const string PopulateSwitchExpressionDiagnosticId = "IDE0072";

    /// <summary>
    /// Reported when a file header is missing or does not match the expected string.
    /// </summary>
    public const string FileHeaderMismatch = "IDE0073";

    public const string UseCoalesceCompoundAssignmentDiagnosticId = "IDE0074";

    public const string SimplifyConditionalExpressionDiagnosticId = "IDE0075";

    public const string InvalidSuppressMessageAttributeDiagnosticId = "IDE0076";
    public const string LegacyFormatSuppressMessageAttributeDiagnosticId = "IDE0077";

    public const string UsePatternCombinatorsDiagnosticId = "IDE0078";

    public const string RemoveUnnecessarySuppressionDiagnosticId = "IDE0079";

    public const string RemoveConfusingSuppressionForIsExpressionDiagnosticId = "IDE0080";
    public const string RemoveUnnecessaryByValDiagnosticId = "IDE0081";

    public const string ConvertTypeOfToNameOfDiagnosticId = "IDE0082";

    public const string UseNotPatternDiagnosticId = "IDE0083";
    public const string UseIsNotExpressionDiagnosticId = "IDE0084";

    public const string UseImplicitObjectCreationDiagnosticId = "IDE0090";

    public const string RemoveRedundantEqualityDiagnosticId = "IDE0100";

    public const string RemoveUnnecessaryDiscardDesignationDiagnosticId = "IDE0110";

    public const string SimplifyLinqExpressionDiagnosticId = "IDE0120";
    public const string SimplifyLinqTypeCheckAndCastDiagnosticId = "IDE0121";

    public const string MatchFolderAndNamespaceDiagnosticId = "IDE0130";

    public const string SimplifyObjectCreationDiagnosticId = "IDE0140";

    public const string UseNullCheckOverTypeCheckDiagnosticId = "IDE0150";

    public const string UseBlockScopedNamespaceDiagnosticId = "IDE0160";
    public const string UseFileScopedNamespaceDiagnosticId = "IDE0161";

    public const string SimplifyPropertyPatternDiagnosticId = "IDE0170";

    public const string UseTupleSwapDiagnosticId = "IDE0180";

    // Don't use "IDE0190". It corresponds to the deleted field UseParameterNullCheckingId which was previously shipped.

    public const string RemoveUnnecessaryLambdaExpressionDiagnosticId = "IDE0200";

    public const string UseTopLevelStatementsId = "IDE0210";
    public const string UseProgramMainId = "IDE0211";

    public const string ForEachCastDiagnosticId = "IDE0220";

    public const string UseUtf8StringLiteralDiagnosticId = "IDE0230";

    public const string RemoveRedundantNullableDirectiveDiagnosticId = "IDE0240";
    public const string RemoveUnnecessaryNullableDirectiveDiagnosticId = "IDE0241";

    public const string MakeStructReadOnlyDiagnosticId = "IDE0250";
    public const string MakeStructMemberReadOnlyDiagnosticId = "IDE0251";

    public const string UsePatternMatchingAsAndMemberAccessDiagnosticId = "IDE0260";

    public const string UseCoalesceExpressionForIfNullCheckDiagnosticId = "IDE0270";

    public const string UseNameofInAttributeDiagnosticId = "IDE0280";

    public const string UsePrimaryConstructorDiagnosticId = "IDE0290";

    public const string UseCollectionExpressionForArrayDiagnosticId = "IDE0300";
    public const string UseCollectionExpressionForEmptyDiagnosticId = "IDE0301";
    public const string UseCollectionExpressionForStackAllocDiagnosticId = "IDE0302";
    public const string UseCollectionExpressionForCreateDiagnosticId = "IDE0303";
    public const string UseCollectionExpressionForBuilderDiagnosticId = "IDE0304";
    public const string UseCollectionExpressionForFluentDiagnosticId = "IDE0305";
    public const string UseCollectionExpressionForNewDiagnosticId = "IDE0306";

    public const string MakeAnonymousFunctionStaticDiagnosticId = "IDE0320";

    public const string UseSystemThreadingLockDiagnosticId = "IDE0330";

    public const string UseUnboundGenericTypeInNameOfDiagnosticId = "IDE0340";

    public const string UseImplicitlyTypedLambdaExpressionDiagnosticId = "IDE0350";

    public const string SimplifyPropertyAccessorDiagnosticId = "IDE0360";

    public const string RemoveUnnecessaryNullableWarningSuppression = "IDE0370";

    public const string RemoveUnnecessaryUnsafeModifier = "IDE0380";

    public const string RemoveUnnecessaryAsyncModifier = "IDE0390";
    public const string RemoveUnnecessaryAsyncModifierInterfaceImplementationOrOverride = "IDE0391";

    // Analyzer error Ids
    public const string AnalyzerChangedId = "IDE1001";
    public const string AnalyzerDependencyConflictId = "IDE1002";
    public const string MissingAnalyzerReferenceId = "IDE1003";
    // public const string ErrorReadingRulesetId = "IDE1004";
    public const string InvokeDelegateWithConditionalAccessId = "IDE1005";
    public const string NamingRuleId = "IDE1006";
    public const string UnboundIdentifierId = "IDE1007";

    // Reserved for workspace error ids IDE1100-IDE1200 (see WorkspaceDiagnosticDescriptors)

    // Experimental features

    // 2000 range for experimental formatting enforcement
    public const string MultipleBlankLinesDiagnosticId = "IDE2000";
    public const string EmbeddedStatementPlacementDiagnosticId = "IDE2001";
    public const string ConsecutiveBracePlacementDiagnosticId = "IDE2002";
    public const string ConsecutiveStatementPlacementDiagnosticId = "IDE2003";
    public const string ConstructorInitializerPlacementDiagnosticId = "IDE2004";
    public const string ConditionalExpressionPlacementDiagnosticId = "IDE2005";
    public const string ArrowExpressionClausePlacementDiagnosticId = "IDE2006";

    // 3000 range for copilot features.
    public const string CopilotImplementNotImplementedExceptionDiagnosticId = "IDE3000";
}
