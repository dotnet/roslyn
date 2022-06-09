// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using TelemetryInfo = System.Tuple<string, string>;

namespace BuildActionTelemetryTable
{
    public class Program
    {
        private static readonly string s_executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        private static ImmutableHashSet<string> IgnoredCodeActions { get; } = new HashSet<string>()
        {
            "Microsoft.CodeAnalysis.CodeFixes.DocumentBasedFixAllProvider+PostProcessCodeAction",
            "Microsoft.CodeAnalysis.CodeActions.CodeAction+CodeActionWithNestedActions",
            "Microsoft.CodeAnalysis.CodeActions.CodeAction+DocumentChangeAction",
            "Microsoft.CodeAnalysis.CodeActions.CodeAction+SolutionChangeAction",
            "Microsoft.CodeAnalysis.CodeActions.CodeAction+NoChangeAction",
            "Microsoft.CodeAnalysis.CodeActions.CustomCodeActions+DocumentChangeAction",
            "Microsoft.CodeAnalysis.CodeActions.CustomCodeActions+SolutionChangeAction",
        }.ToImmutableHashSet();

        private static ImmutableDictionary<string, string> CodeActionDescriptionMap { get; } = new Dictionary<string, string>()
        {
            { "Microsoft.CodeAnalysis.CodeStyle.CSharpFormattingCodeFixProvider", "Formatting" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.CSharpAddDocCommentNodesCodeFixProvider", "Add Doc Comment Nodes" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.CSharpRemoveDocCommentNodeCodeFixProvider", "Remove Doc Comment Node" },
            { "Microsoft.CodeAnalysis.CSharp.TypeStyle.UseExplicitTypeCodeFixProvider", "Use Explicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.TypeStyle.UseImplicitTypeCodeFixProvider", "Use Implicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly.CSharpMakeFieldReadonlyCodeFixProvider", "Make Field Readonly" },
            { "Microsoft.CodeAnalysis.CSharp.UseInterpolatedVerbatimString.CSharpUseInterpolatedVerbatimStringCodeFixProvider", "Use Interpolated Verbatim String" },
            { "Microsoft.CodeAnalysis.CSharp.UnsealClass.CSharpUnsealClassCodeFixProvider", "Unseal Class" },
            { "Microsoft.CodeAnalysis.CSharp.MakeTypeAbstract.CSharpMakeTypeAbstractCodeFixProvider", "Make Type Abstract" },
            { "Microsoft.CodeAnalysis.CSharp.MakeMemberStatic.CSharpMakeMemberStaticCodeFixProvider", "Make Member Static" },
            { "Microsoft.CodeAnalysis.CSharp.DisambiguateSameVariable.CSharpDisambiguateSameVariableCodeFixProvider", "Disambiguate Same Variable" },
            { "Microsoft.CodeAnalysis.CSharp.AliasAmbiguousType.CSharpAliasAmbiguousTypeCodeFixProvider", "Alias Ambiguous Type" },
            { "Microsoft.CodeAnalysis.CSharp.AddObsoleteAttribute.CSharpAddObsoleteAttributeCodeFixProvider", "Add Obsolete Attribute" },
            { "Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral.UseUTF8StringLiteralCodeFixProvider", "Use UTF 8 String Literal" },
            { "Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement.UseSimpleUsingStatementCodeFixProvider", "Use Simple Using Statement" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternCombinators.CSharpUsePatternCombinatorsCodeFixProvider", "Use Pattern Combinators" },
            { "Microsoft.CodeAnalysis.CSharp.UseObjectInitializer.CSharpUseObjectInitializerCodeFixProvider", "Use Object Initializer" },
            { "Microsoft.CodeAnalysis.CSharp.UseNullPropagation.CSharpUseNullPropagationCodeFixProvider", "Use Null Propagation" },
            { "Microsoft.CodeAnalysis.CSharp.UseTupleSwap.CSharpUseTupleSwapCodeFixProvider", "Use Tuple Swap" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider", "Use Is Null Check For Cast And Equality Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider", "Use Is Null Check For Reference Equals" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseNullCheckOverTypeCheckCodeFixProvider", "Use Null Check Over Type Check" },
            { "Microsoft.CodeAnalysis.CSharp.UseInferredMemberName.CSharpUseInferredMemberNameCodeFixProvider", "Use Inferred Member Name" },
            { "Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseIndexOperatorCodeFixProvider", "Use Index Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseRangeOperatorCodeFixProvider", "Use Range Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation.CSharpUseImplicitObjectCreationCodeFixProvider", "Use Implicit Object Creation" },
            { "Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer.CSharpUseCollectionInitializerCodeFixProvider", "Use Collection Initializer" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues.CSharpRemoveUnusedValuesCodeFixProvider", "Remove Unused Values" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers.CSharpRemoveUnusedMembersCodeFixProvider", "Remove Unused Members" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports.CSharpRemoveUnnecessaryImportsCodeFixProvider", "Remove Unnecessary Imports" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast.CSharpRemoveUnnecessaryCastCodeFixProvider", "Remove Unnecessary Cast" },
            { "Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess.CSharpQualifyMemberAccessCodeFixProvider", "Qualify Member Access" },
            { "Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral.CSharpUseDefaultLiteralCodeFixProvider", "Use Default Literal" },
            { "Microsoft.CodeAnalysis.CSharp.UseDeconstruction.CSharpUseDeconstructionCodeFixProvider", "Use Deconstruction" },
            { "Microsoft.CodeAnalysis.CSharp.UseConditionalExpression.CSharpUseConditionalExpressionForAssignmentCodeFixProvider", "Use Conditional Expression For Assignment" },
            { "Microsoft.CodeAnalysis.CSharp.UseConditionalExpression.CSharpUseConditionalExpressionForReturnCodeFixProvider", "Use Conditional Expression For Return" },
            { "Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment.CSharpUseCompoundAssignmentCodeFixProvider", "Use Compound Assignment" },
            { "Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment.CSharpUseCompoundCoalesceAssignmentCodeFixProvider", "Use Compound Coalesce Assignment" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyPropertyPattern.CSharpSimplifyPropertyPatternCodeFixProvider", "Simplify Property Pattern" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression.CSharpSimplifyLinqExpressionCodeFixProvider", "Simplify Linq Expression" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyInterpolation.CSharpSimplifyInterpolationCodeFixProvider", "Simplify Interpolation" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode.CSharpRemoveUnreachableCodeCodeFixProvider", "Remove Unreachable Code" },
            { "Microsoft.CodeAnalysis.CSharp.PopulateSwitch.CSharpPopulateSwitchExpressionCodeFixProvider", "Populate Switch Expression" },
            { "Microsoft.CodeAnalysis.CSharp.PopulateSwitch.CSharpPopulateSwitchStatementCodeFixProvider", "Populate Switch Statement" },
            { "Microsoft.CodeAnalysis.CSharp.OrderModifiers.CSharpOrderModifiersCodeFixProvider", "Order Modifiers" },
            { "Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives.MisplacedUsingDirectivesCodeFixProvider", "Misplaced Using Directives" },
            { "Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable.CSharpMakeStructFieldsWritableCodeFixProvider", "Make Struct Fields Writable" },
            { "Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess.InvokeDelegateWithConditionalAccessCodeFixProvider", "Invoke Delegate With Conditional Access" },
            { "Microsoft.CodeAnalysis.CSharp.InlineDeclaration.CSharpInlineDeclarationCodeFixProvider", "Inline Declaration" },
            { "Microsoft.CodeAnalysis.CSharp.FileHeaders.CSharpFileHeaderCodeFixProvider", "File Header" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression.ConvertSwitchStatementToExpressionCodeFixProvider", "Convert Switch Statement To Expression" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveConfusingSuppression.CSharpRemoveConfusingSuppressionCodeFixProvider", "Remove Confusing Suppression" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses.CSharpRemoveUnnecessaryParenthesesCodeFixProvider", "Remove Unnecessary Parentheses" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryLambdaExpression.CSharpRemoveUnnecessaryLambdaExpressionCodeFixProvider", "Remove Unnecessary Lambda Expression" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation.CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider", "Remove Unnecessary Discard Designation" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.EmbeddedStatementPlacement.EmbeddedStatementPlacementCodeFixProvider", "Embedded Statement Placement" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement.ConstructorInitializerPlacementCodeFixProvider", "Constructor Initializer Placement" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveBracePlacement.ConsecutiveBracePlacementCodeFixProvider", "Consecutive Brace Placement" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf.CSharpConvertTypeOfToNameOfCodeFixProvider", "Convert Type Of To Name Of" },
            { "Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers.CSharpAddAccessibilityModifiersCodeFixProvider", "Add Accessibility Modifiers" },
            { "Microsoft.CodeAnalysis.CSharp.Wrapping.CSharpWrappingCodeRefactoringProvider", "Wrapping" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpIsAndCastCheckWithoutNameCodeFixProvider", "Is And Cast Check Without Name" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpUseNotPatternCodeFixProvider", "Use Not Pattern" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpAsAndNullCheckCodeFixProvider", "As And Null Check" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpIsAndCastCheckCodeFixProvider", "Is And Cast Check" },
            { "Microsoft.CodeAnalysis.CSharp.UseNamedArguments.CSharpUseNamedArgumentsCodeRefactoringProvider", "Use Named Arguments" },
            { "Microsoft.CodeAnalysis.CSharp.UseLocalFunction.CSharpUseLocalFunctionCodeFixProvider", "Use Local Function" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBody.UseExpressionBodyCodeRefactoringProvider", "Use Expression Body" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBody.UseExpressionBodyCodeFixProvider", "Use Expression Body" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda.UseExpressionBodyForLambdaCodeFixProvider", "Use Expression Body For Lambda" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda.UseExpressionBodyForLambdaCodeRefactoringProvider", "Use Expression Body For Lambda" },
            { "Microsoft.CodeAnalysis.CSharp.UseExplicitTypeForConst.UseExplicitTypeForConstCodeFixProvider", "Use Explicit Type For Const" },
            { "Microsoft.CodeAnalysis.CSharp.UseAutoProperty.CSharpUseAutoPropertyCodeFixProvider", "Use Auto Property" },
            { "Microsoft.CodeAnalysis.CSharp.UpgradeProject.CSharpUpgradeProjectCodeFixProvider", "Upgrade Project" },
            { "Microsoft.CodeAnalysis.CSharp.UpdateProjectToAllowUnsafe.CSharpUpdateProjectToAllowUnsafeCodeFixProvider", "Update Project To Allow Unsafe" },
            { "Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements.CSharpMergeConsecutiveIfStatementsCodeRefactoringProvider", "Merge Consecutive If Statements" },
            { "Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements.CSharpMergeNestedIfStatementsCodeRefactoringProvider", "Merge Nested If Statements" },
            { "Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements.CSharpSplitIntoConsecutiveIfStatementsCodeRefactoringProvider", "Split Into Consecutive If Statements" },
            { "Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements.CSharpSplitIntoNestedIfStatementsCodeRefactoringProvider", "Split Into Nested If Statements" },
            { "Microsoft.CodeAnalysis.CSharp.SpellCheck.CSharpSpellCheckCodeFixProvider", "Spell Check" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyTypeNames.SimplifyTypeNamesCodeFixProvider", "Simplify Type Names" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyThisOrMe.CSharpSimplifyThisOrMeCodeFixProvider", "Simplify This Or Me" },
            { "Microsoft.CodeAnalysis.CSharp.ReverseForStatement.CSharpReverseForStatementCodeRefactoringProvider", "Reverse For Statement" },
            { "Microsoft.CodeAnalysis.CSharp.ReplaceDocCommentTextWithTag.CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider", "Replace Doc Comment Text With Tag" },
            { "Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral.CSharpReplaceDefaultLiteralCodeFixProvider", "Replace Default Literal" },
            { "Microsoft.CodeAnalysis.CSharp.ReplaceConditionalWithStatements.CSharpReplaceConditionalWithStatementsCodeRefactoringProvider", "Replace Conditional With Statements" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable.CSharpRemoveUnusedVariableCodeFixProvider", "Remove Unused Variable" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedLocalFunction.CSharpRemoveUnusedLocalFunctionCodeFixProvider", "Remove Unused Local Function" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier.CSharpRemoveAsyncModifierCodeFixProvider", "Remove Async Modifier" },
            { "Microsoft.CodeAnalysis.CSharp.NameTupleElement.CSharpNameTupleElementCodeRefactoringProvider", "Name Tuple Element" },
            { "Microsoft.CodeAnalysis.CSharp.MoveDeclarationNearReference.CSharpMoveDeclarationNearReferenceCodeRefactoringProvider", "Move Declaration Near Reference" },
            { "Microsoft.CodeAnalysis.CSharp.MakeRefStruct.MakeRefStructCodeFixProvider", "Make Ref Struct" },
            { "Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous.CSharpMakeMethodSynchronousCodeFixProvider", "Make Method Synchronous" },
            { "Microsoft.CodeAnalysis.CSharp.MakeMethodAsynchronous.CSharpMakeMethodAsynchronousCodeFixProvider", "Make Method Asynchronous" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.MakeLocalFunctionStaticCodeFixProvider", "Make Local Function Static" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.MakeLocalFunctionStaticCodeRefactoringProvider", "Make Local Function Static" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.PassInCapturedVariablesAsArgumentsCodeFixProvider", "Pass In Captured Variables As Arguments" },
            { "Microsoft.CodeAnalysis.CSharp.InvertLogical.CSharpInvertLogicalCodeRefactoringProvider", "Invert Logical" },
            { "Microsoft.CodeAnalysis.CSharp.InvertIf.CSharpInvertIfCodeRefactoringProvider", "Invert If" },
            { "Microsoft.CodeAnalysis.CSharp.InvertConditional.CSharpInvertConditionalCodeRefactoringProvider", "Invert Conditional" },
            { "Microsoft.CodeAnalysis.CSharp.IntroduceVariable.CSharpIntroduceLocalForExpressionCodeRefactoringProvider", "Introduce Local For Expression" },
            { "Microsoft.CodeAnalysis.CSharp.IntroduceVariable.CSharpIntroduceParameterCodeRefactoringProvider", "Introduce Parameter" },
            { "Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement.CSharpIntroduceUsingStatementCodeRefactoringProvider", "Introduce Using Statement" },
            { "Microsoft.CodeAnalysis.CSharp.InitializeParameter.CSharpAddParameterCheckCodeRefactoringProvider", "Add Parameter Check" },
            { "Microsoft.CodeAnalysis.CSharp.InitializeParameter.CSharpInitializeMemberFromParameterCodeRefactoringProvider", "Initialize Member From Parameter" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementInterface.CSharpImplementExplicitlyCodeRefactoringProvider", "Implement Explicitly" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementInterface.CSharpImplementImplicitlyCodeRefactoringProvider", "Implement Implicitly" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementInterface.CSharpImplementInterfaceCodeFixProvider", "Implement Interface" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass.CSharpImplementAbstractClassCodeFixProvider", "Implement Abstract Class" },
            { "Microsoft.CodeAnalysis.CSharp.GenerateVariable.CSharpGenerateVariableCodeFixProvider", "Generate Variable" },
            { "Microsoft.CodeAnalysis.CSharp.GenerateDefaultConstructors.CSharpGenerateDefaultConstructorsCodeFixProvider", "Generate Default Constructors" },
            { "Microsoft.CodeAnalysis.CSharp.GenerateConstructor.GenerateConstructorCodeFixProvider", "Generate Constructor" },
            { "Microsoft.CodeAnalysis.CSharp.GenerateConstructorFromMembers.CSharpGenerateConstructorFromMembersCodeRefactoringProvider", "Generate Constructor From Members" },
            { "Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.CSharpJsonDetectionCodeFixProvider", "Json Detection" },
            { "Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces.CSharpAddBracesCodeFixProvider", "Add Braces" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertTupleToStruct.CSharpConvertTupleToStructCodeRefactoringProvider", "Convert Tuple To Struct" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertToRawString.ConvertRegularStringToRawStringCodeRefactoringProvider", "Convert Regular String To Raw String" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString.CSharpConvertConcatenationToInterpolatedStringRefactoringProvider", "Convert Concatenation To Interpolated String" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString.CSharpConvertPlaceholderToInterpolatedStringRefactoringProvider", "Convert Placeholder To Interpolated String" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertProgram.ConvertToProgramMainCodeFixProvider", "Convert To Program Main" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertProgram.ConvertToProgramMainCodeRefactoringProvider", "Convert To Program Main" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertProgram.ConvertToTopLevelStatementsCodeFixProvider", "Convert To Top Level Statements" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertProgram.ConvertToTopLevelStatementsCodeRefactoringProvider", "Convert To Top Level Statements" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNumericLiteral.CSharpConvertNumericLiteralCodeRefactoringProvider", "Convert Numeric Literal" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNamespace.ConvertNamespaceCodeRefactoringProvider", "Convert Namespace" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNamespace.ConvertNamespaceCodeFixProvider", "Convert Namespace" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqQueryToForEachProvider", "Convert Linq Query To For Each Provider" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery.CSharpConvertForEachToLinqQueryProvider", "Convert For Each To Linq Query Provider" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch.CSharpConvertIfToSwitchCodeRefactoringProvider", "Convert If To Switch" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertForToForEach.CSharpConvertForToForEachCodeRefactoringProvider", "Convert For To For Each" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertForEachToFor.CSharpConvertForEachToForCodeRefactoringProvider", "Convert For Each To For" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertCast.CSharpConvertDirectCastToTryCastCodeRefactoringProvider", "Convert Direct Cast To Try Cast" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertCast.CSharpConvertTryCastToDirectCastCodeRefactoringProvider", "Convert Try Cast To Direct Cast" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString.ConvertBetweenRegularAndVerbatimInterpolatedStringCodeRefactoringProvider", "Convert Between Regular And Verbatim Interpolated String" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString.ConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider", "Convert Between Regular And Verbatim String" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty.CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider", "Convert Auto Property To Full Property" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertAnonymousType.CSharpConvertAnonymousTypeToClassCodeRefactoringProvider", "Convert Anonymous Type To Class" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertAnonymousType.CSharpConvertAnonymousTypeToTupleCodeRefactoringProvider", "Convert Anonymous Type To Tuple" },
            { "Microsoft.CodeAnalysis.CSharp.ConflictMarkerResolution.CSharpResolveConflictMarkerCodeFixProvider", "Resolve Conflict Marker" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns.UseRecursivePatternsCodeRefactoringProvider", "Use Recursive Patterns" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseImplicitType.UseImplicitTypeCodeRefactoringProvider", "Use Implicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseExplicitType.UseExplicitTypeCodeRefactoringProvider", "Use Explicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace.CSharpSyncNamespaceCodeRefactoringProvider", "Sync Namespace" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.CSharpPullMemberUpCodeRefactoringProvider", "Pull Member Up" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers.CSharpMoveStaticMembersRefactoringProvider", "Move Static Members" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary.CSharpInlineTemporaryCodeRefactoringProvider", "Inline Temporary" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod.CSharpInlineMethodRefactoringProvider", "Inline Method" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ExtractClass.CSharpExtractClassCodeRefactoringProvider", "Extract Class" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable.EnableNullableCodeRefactoringProvider", "Enable Nullable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertLocalFunctionToMethod.CSharpConvertLocalFunctionToMethodCodeRefactoringProvider", "Convert Local Function To Method" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddMissingImports.CSharpAddMissingImportsRefactoringProvider", "Add Missing Imports" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddAwait.CSharpAddAwaitCodeRefactoringProvider", "Add Await" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.TransposeRecordKeyword.CSharpTransposeRecordKeywordCodeFixProvider", "Transpose Record Keyword" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.MatchFolderAndNamespace.CSharpChangeNamespaceToMatchFolderCodeFixProvider", "Change Namespace To Match Folder" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.FixIncorrectConstraint.CSharpFixIncorrectConstraintCodeFixProvider", "Fix Incorrect Constraint" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.ForEachCast.CSharpForEachCastCodeFixProvider", "For Each Cast" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable.CSharpDeclareAsNullableCodeFixProvider", "Declare As Nullable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeStatementAsynchronous.CSharpMakeStatementAsynchronousCodeFixProvider", "Make Statement Asynchronous" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator.CSharpAddYieldCodeFixProvider", "Add Yield" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator.CSharpChangeToIEnumerableCodeFixProvider", "Change To I Enumerable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase.HideBaseCodeFixProvider", "Hide Base" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.FixReturnType.CSharpFixReturnTypeCodeFixProvider", "Fix Return Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc.AddInheritdocCodeFixProvider", "Add Inheritdoc" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast.CSharpAddExplicitCastCodeFixProvider", "Add Explicit Cast" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify.CSharpFullyQualifyCodeFixProvider", "Fully Qualify" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveNewModifier.RemoveNewModifierCodeFixProvider", "Remove New Modifier" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveInKeyword.RemoveInKeywordCodeFixProvider", "Remove In Keyword" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateType.GenerateTypeCodeFixProvider", "Generate Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateDeconstructMethod.GenerateDeconstructMethodCodeFixProvider", "Generate Deconstruct Method" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod.GenerateConversionCodeFixProvider", "Generate Conversion" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod.GenerateMethodCodeFixProvider", "Generate Method" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateEnumMember.GenerateEnumMemberCodeFixProvider", "Generate Enum Member" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.ConvertToAsync.CSharpConvertToAsyncMethodCodeFixProvider", "Convert To Async Method" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation.CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider", "Add Parentheses Around Conditional Expression In Interpolated String" },
            { "Microsoft.CodeAnalysis.CSharp.AssignOutParameters.AssignOutParametersAboveReturnCodeFixProvider", "Assign Out Parameters Above Return" },
            { "Microsoft.CodeAnalysis.CSharp.AssignOutParameters.AssignOutParametersAtStartCodeFixProvider", "Assign Out Parameters At Start" },
            { "Microsoft.CodeAnalysis.CSharp.AddParameter.CSharpAddParameterCodeFixProvider", "Add Parameter" },
            { "Microsoft.CodeAnalysis.CSharp.AddPackage.CSharpAddSpecificPackageCodeFixProvider", "Add Specific Package" },
            { "Microsoft.CodeAnalysis.CSharp.AddMissingReference.CSharpAddMissingReferenceCodeFixProvider", "Add Missing Reference" },
            { "Microsoft.CodeAnalysis.CSharp.AddImport.CSharpAddImportCodeFixProvider", "Add Import" },
            { "Microsoft.CodeAnalysis.CSharp.AddFileBanner.CSharpAddFileBannerCodeRefactoringProvider", "Add File Banner" },
            { "Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay.CSharpAddDebuggerDisplayCodeRefactoringProvider", "Add Debugger Display" },
            { "Microsoft.CodeAnalysis.CSharp.AddAnonymousTypeMemberName.CSharpAddAnonymousTypeMemberNameCodeFixProvider", "Add Anonymous Type Member Name" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable.EnableNullableCodeRefactoringProvider+CustomCodeAction", "Custom" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase.HideBaseCodeFixProvider+AddNewKeywordAction", "Add New Keyword Action" },
            { "Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking.RenameTrackingCodeRefactoringProvider", "Rename Tracking" },
            { "Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking.RenameTrackingTaggerProvider+RenameTrackingCodeAction", "Rename Tracking" },
            { "Microsoft.CodeAnalysis.UpdateLegacySuppressions.UpdateLegacySuppressionsCodeFixProvider", "Update Legacy Suppressions" },
            { "Microsoft.CodeAnalysis.UseThrowExpression.UseThrowExpressionCodeFixProvider", "Use Throw Expression" },
            { "Microsoft.CodeAnalysis.UseSystemHashCode.UseSystemHashCodeCodeFixProvider", "Use System Hash Code" },
            { "Microsoft.CodeAnalysis.UseExplicitTupleName.UseExplicitTupleNameCodeFixProvider", "Use Explicit Tuple Name" },
            { "Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionCodeFixProvider", "Use Coalesce Expression" },
            { "Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionForNullableCodeFixProvider", "Use Coalesce Expression For Nullable" },
            { "Microsoft.CodeAnalysis.SimplifyBooleanExpression.SimplifyConditionalCodeFixProvider", "Simplify Conditional" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryInlineSuppressionsCodeFixProvider", "Remove Unnecessary Inline Suppressions" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryAttributeSuppressionsCodeFixProvider", "Remove Unnecessary Attribute Suppressions" },
            { "Microsoft.CodeAnalysis.RemoveRedundantEquality.RemoveRedundantEqualityCodeFixProvider", "Remove Redundant Equality" },
            { "Microsoft.CodeAnalysis.NewLines.MultipleBlankLines.MultipleBlankLinesCodeFixProvider", "Multiple Blank Lines" },
            { "Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement.ConsecutiveStatementPlacementCodeFixProvider", "Consecutive Statement Placement" },
            { "Microsoft.CodeAnalysis.AddRequiredParentheses.AddRequiredParenthesesCodeFixProvider", "Add Required Parentheses" },
            { "Microsoft.CodeAnalysis.Wrapping.WrapItemsAction", "Wrap Items Action" },
            { "Microsoft.CodeAnalysis.UpgradeProject.ProjectOptionsChangeAction", "Project Options Change Action" },
            { "Microsoft.CodeAnalysis.ReplacePropertyWithMethods.ReplacePropertyWithMethodsCodeRefactoringProvider", "Replace Property With Methods" },
            { "Microsoft.CodeAnalysis.ReplaceMethodWithProperty.ReplaceMethodWithPropertyCodeRefactoringProvider", "Replace Method With Property" },
            { "Microsoft.CodeAnalysis.PreferFrameworkType.PreferFrameworkTypeCodeFixProvider", "Prefer Framework Type" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.MoveToNamespaceCodeActionProvider", "Move To Namespace" },
            { "Microsoft.CodeAnalysis.MoveStaticMembers.MoveStaticMembersWithDialogCodeAction", "Move Static Members With Dialog" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.IntroduceVariableCodeRefactoringProvider", "Introduce Variable" },
            { "Microsoft.CodeAnalysis.GenerateOverrides.GenerateOverridesCodeRefactoringProvider", "Generate Overrides" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider", "Generate Equals And Get Hash Code From Members" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.GenerateDefaultConstructorsCodeRefactoringProvider", "Generate Default Constructors" },
            { "Microsoft.CodeAnalysis.GenerateComparisonOperators.GenerateComparisonOperatorsCodeRefactoringProvider", "Generate Comparison Operators" },
            { "Microsoft.CodeAnalysis.ExtractInterface.ExtractInterfaceCodeAction", "Extract Interface" },
            { "Microsoft.CodeAnalysis.ExtractInterface.ExtractInterfaceCodeRefactoringProvider", "Extract Interface" },
            { "Microsoft.CodeAnalysis.ExtractClass.ExtractClassWithDialogCodeAction", "Extract Class With Dialog" },
            { "Microsoft.CodeAnalysis.EncapsulateField.EncapsulateFieldRefactoringProvider", "Encapsulate Field" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.ConvertRegularStringToInterpolatedStringRefactoringProvider", "Convert Regular String To Interpolated String" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.FixAllCodeRefactoringCodeAction", "Fix All Code Refactoring" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.MoveType.MoveTypeCodeRefactoringProvider", "Move Type" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod.ExtractMethodCodeRefactoringProvider", "Extract Method" },
            { "Microsoft.CodeAnalysis.CodeFixes.FixMultipleCodeAction", "Fix Multiple" },
            { "Microsoft.CodeAnalysis.CodeFixes.NamingStyles.NamingStyleCodeFixProvider", "Naming Style" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.TopLevelSuppressionCodeAction", "Top Level Suppression" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.WrapperCodeFixProvider", "Wrapper" },
            { "Microsoft.CodeAnalysis.ChangeSignature.ChangeSignatureCodeRefactoringProvider", "Change Signature" },
            { "Microsoft.CodeAnalysis.ChangeSignature.ChangeSignatureCodeAction", "Change Signature" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallPackageDirectlyCodeAction", "Install Package Directly" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallPackageParentCodeAction", "Install Package Parent" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallWithPackageManagerCodeAction", "Install With Package Manager" },
            { "Microsoft.CodeAnalysis.AddMissingReference.AddMissingReferenceCodeAction", "Add Missing Reference" },
            { "Microsoft.CodeAnalysis.AddConstructorParametersFromMembers.AddConstructorParametersFromMembersCodeRefactoringProvider", "Add Constructor Parameters From Members" },
            { "Microsoft.CodeAnalysis.UseAutoProperty.AbstractUseAutoPropertyCodeFixProvider`5+UseAutoPropertyCodeAction", "Use Auto Property" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.AbstractMoveToNamespaceCodeAction+MoveItemsToNamespaceCodeAction", "Move Items To Namespace" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.AbstractMoveToNamespaceCodeAction+MoveTypeToNamespaceCodeAction", "Move Type To Namespace" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceVariableService`6+IntroduceVariableCodeAction", "Introduce Variable" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceVariableService`6+IntroduceVariableAllOccurrenceCodeAction", "Introduce Variable All Occurrence" },
            { "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction", "Implement Interface" },
            { "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction", "Implement Interface With Dispose Pattern" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+GenerateTypeCodeAction", "Generate Type" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+GenerateTypeCodeActionWithOption", "Generate Type" },
            { "Microsoft.CodeAnalysis.GenerateOverrides.GenerateOverridesCodeRefactoringProvider+GenerateOverridesWithDialogCodeAction", "Generate Overrides With Dialog" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateVariableCodeAction", "Generate Variable" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateLocalCodeAction", "Generate Local" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateParameterCodeAction", "Generate Parameter" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember.AbstractGenerateParameterizedMemberService`4+GenerateParameterizedMemberCodeAction", "Generate Parameterized Member" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember.AbstractGenerateEnumMemberService`3+GenerateEnumMemberCodeAction", "Generate Enum Member" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider+GenerateEqualsAndGetHashCodeAction", "Generate Equals And Get Hash" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider+GenerateEqualsAndGetHashCodeWithDialogCodeAction", "Generate Equals And Get Hash Code With Dialog" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+GenerateDefaultConstructorCodeAction", "Generate Default Constructor" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+CodeActionAll", "Code Action All" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+ConstructorDelegatingCodeAction", "Constructor Delegating" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+FieldDelegatingCodeAction", "Field Delegating" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+GenerateConstructorWithDialogCodeAction", "Generate Constructor With Dialog" },
            { "Microsoft.CodeAnalysis.CodeStyle.AbstractCodeStyleProvider`2+CodeRefactoringProvider", "" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.AbstractPullMemberUpRefactoringProvider+PullMemberUpWithDialogCodeAction", "Pull Member Up With Dialog" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace.AbstractSyncNamespaceCodeRefactoringProvider`3+MoveFileCodeAction", "Move File" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.MoveType.AbstractMoveTypeService`5+MoveTypeCodeAction", "Move Type" },
            { "Microsoft.CodeAnalysis.CodeFixes.NamingStyles.NamingStyleCodeFixProvider+FixNameCodeAction", "Fix Name" },
            { "Microsoft.CodeAnalysis.CodeFixes.FullyQualify.AbstractFullyQualifyCodeFixProvider+GroupingCodeAction", "Grouping" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageCodeAction", "Global Suppress Message" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageFixAllCodeAction", "Global Suppress Message Fix All" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+LocalSuppressMessageCodeAction", "Local Suppress Message" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+PragmaWarningCodeAction", "Pragma Warning" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity.ConfigureSeverityLevelCodeFixProvider+TopLevelBulkConfigureSeverityCodeAction", "Top Level Bulk Configure Severity" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity.ConfigureSeverityLevelCodeFixProvider+TopLevelConfigureSeverityCodeAction", "Top Level Configure Severity" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle.ConfigureCodeStyleOptionCodeFixProvider+TopLevelConfigureCodeStyleOptionCodeAction", "Top Level Configure Code Style Option" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+AssemblyReferenceCodeAction", "Assembly Reference" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+InstallPackageAndAddImportCodeAction", "Install Package And Add Import" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+InstallWithPackageManagerCodeAction", "Install With Package Manager" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+MetadataSymbolReferenceCodeAction", "Metadata Symbol Reference" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+ParentInstallPackageCodeAction", "Parent Install Package" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+ProjectSymbolReferenceCodeAction", "Project Symbol Reference" },
            { "Microsoft.CodeAnalysis.AddConstructorParametersFromMembers.AddConstructorParametersFromMembersCodeRefactoringProvider+AddConstructorParametersCodeAction", "Add Constructor Parameters" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageFixAllCodeAction+GlobalSuppressionSolutionChangeAction", "Global Suppression Solution Change Action" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+RemoveSuppressionCodeAction+AttributeRemoveAction", "Attribute Remove Action" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+RemoveSuppressionCodeAction+PragmaRemoveAction", "Pragma Remove Action" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddAnonymousTypeMemberName.VisualBasicAddAnonymousTypeMemberNameCodeFixProvider", "Add Anonymous Type Member Name" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay.VisualBasicAddDebuggerDisplayCodeRefactoringProvider", "Add Debugger Display" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddFileBanner.VisualBasicAddFileBannerCodeRefactoringProvider", "Add File Banner" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddImport.VisualBasicAddImportCodeFixProvider", "Add Import" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddPackage.VisualBasicAddSpecificPackageCodeFixProvider", "Add Specific Package" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddParameter.VisualBasicAddParameterCodeFixProvider", "Add Parameter" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddMissingReference.VisualBasicAddMissingReferenceCodeFixProvider", "Add Missing Reference" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ConvertToAsync.VisualBasicConvertToAsyncFunctionCodeFixProvider", "Convert To Async Function" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable.CorrectNextControlVariableCodeFixProvider", "Correct Next Control Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEndConstruct.GenerateEndConstructCodeFixProvider", "Generate End Construct" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEnumMember.GenerateEnumMemberCodeFixProvider", "Generate Enum Member" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent.GenerateEventCodeFixProvider", "Generate Event" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod.GenerateConversionCodeFixProvider", "Generate Conversion" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod.GenerateParameterizedMemberCodeFixProvider", "Generate Parameterized Member" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateType.GenerateTypeCodeFixProvider", "Generate Type" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider", "Incorrect Exit Continue" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectFunctionReturnType.IncorrectFunctionReturnTypeCodeFixProvider", "Incorrect Function Return Type" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile.MoveToTopOfFileCodeFixProvider", "Move To Top Of File" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase.OverloadBaseCodeFixProvider", "Overload Base" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify.VisualBasicFullyQualifyCodeFixProvider", "Fully Qualify" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddExplicitCast.VisualBasicAddExplicitCastCodeFixProvider", "Add Explicit Cast" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicChangeToYieldCodeFixProvider", "Change To Yield" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicConvertToIteratorCodeFixProvider", "Convert To Iterator" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.AddAwait.VisualBasicAddAwaitCodeRefactoringProvider", "Add Await" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary.VisualBasicInlineMethodRefactoringProvider", "Inline Method" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary.VisualBasicInlineTemporaryCodeRefactoringProvider", "Inline Temporary" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveStaticMembers.VisualBasicMoveStaticMembersRefactoringProvider", "Move Static Members" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeActions.RemoveStatementCodeAction", "Remove Statement" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConflictMarkerResolution.VisualBasicResolveConflictMarkerCodeFixProvider", "Resolve Conflict Marker" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousType.VisualBasicConvertAnonymousTypeToClassCodeRefactoringProvider", "Convert Anonymous Type To Class" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousTypeToTuple.VisualBasicConvertAnonymousTypeToTupleCodeRefactoringProvider", "Convert Anonymous Type To Tuple" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertAutoPropertyToFullProperty.VisualBasicConvertAutoPropertyToFullPropertyCodeRefactoringProvider", "Convert Auto Property To Full Property" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertCast.VisualBasicConvertDirectCastToTryCastCodeRefactoringProvider", "Convert Direct Cast To Try Cast" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertConversionOperators.VisualBasicConvertTryCastToDirectCastCodeRefactoringProvider", "Convert Try Cast To Direct Cast" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertForEachToFor.VisualBasicConvertForEachToForCodeRefactoringProvider", "Convert For Each To For" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertForToForEach.VisualBasicConvertForToForEachCodeRefactoringProvider", "Convert For To For Each" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch.VisualBasicConvertIfToSwitchCodeRefactoringProvider", "Convert If To Switch" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertNumericLiteral.VisualBasicConvertNumericLiteralCodeRefactoringProvider", "Convert Numeric Literal" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString.VisualBasicConvertConcatenationToInterpolatedStringRefactoringProvider", "Convert Concatenation To Interpolated String" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString.VisualBasicConvertPlaceholderToInterpolatedStringRefactoringProvider", "Convert Placeholder To Interpolated String" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertTupleToStruct.VisualBasicConvertTupleToStructCodeRefactoringProvider", "Convert Tuple To Struct" },
            { "Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages.VisualBasicJsonDetectionCodeFixProvider", "Json Detection" },
            { "Microsoft.CodeAnalysis.VisualBasic.GenerateConstructorFromMembers.VisualBasicGenerateConstructorFromMembersCodeRefactoringProvider", "Generate Constructor From Members" },
            { "Microsoft.CodeAnalysis.VisualBasic.GenerateConstructor.GenerateConstructorCodeFixProvider", "Generate Constructor" },
            { "Microsoft.CodeAnalysis.VisualBasic.GenerateDefaultConstructors.VisualBasicGenerateDefaultConstructorsCodeFixProvider", "Generate Default Constructors" },
            { "Microsoft.CodeAnalysis.VisualBasic.GenerateVariable.VisualBasicGenerateVariableCodeFixProvider", "Generate Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.ImplementAbstractClass.VisualBasicImplementAbstractClassCodeFixProvider", "Implement Abstract Class" },
            { "Microsoft.CodeAnalysis.VisualBasic.ImplementInterface.VisualBasicImplementInterfaceCodeFixProvider", "Implement Interface" },
            { "Microsoft.CodeAnalysis.VisualBasic.InitializeParameter.VisualBasicAddParameterCheckCodeRefactoringProvider", "Add Parameter Check" },
            { "Microsoft.CodeAnalysis.VisualBasic.InitializeParameter.VisualBasicInitializeMemberFromParameterCodeRefactoringProvider", "Initialize Member From Parameter" },
            { "Microsoft.CodeAnalysis.VisualBasic.IntroduceUsingStatement.VisualBasicIntroduceUsingStatementCodeRefactoringProvider", "Introduce Using Statement" },
            { "Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable.VisualBasicIntroduceLocalForExpressionCodeRefactoringProvider", "Introduce Local For Expression" },
            { "Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable.VisualBasicIntroduceParameterCodeRefactoringProvider", "Introduce Parameter" },
            { "Microsoft.CodeAnalysis.VisualBasic.InvertConditional.VisualBasicInvertConditionalCodeRefactoringProvider", "Invert Conditional" },
            { "Microsoft.CodeAnalysis.VisualBasic.InvertIf.VisualBasicInvertMultiLineIfCodeRefactoringProvider", "Invert Multi Line If" },
            { "Microsoft.CodeAnalysis.VisualBasic.InvertIf.VisualBasicInvertSingleLineIfCodeRefactoringProvider", "Invert Single Line If" },
            { "Microsoft.CodeAnalysis.VisualBasic.InvertLogical.VisualBasicInvertLogicalCodeRefactoringProvider", "Invert Logical" },
            { "Microsoft.CodeAnalysis.VisualBasic.MakeMethodAsynchronous.VisualBasicMakeMethodAsynchronousCodeFixProvider", "Make Method Asynchronous" },
            { "Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous.VisualBasicMakeMethodSynchronousCodeFixProvider", "Make Method Synchronous" },
            { "Microsoft.CodeAnalysis.VisualBasic.MoveDeclarationNearReference.VisualBasicMoveDeclarationNearReferenceCodeRefactoringProvider", "Move Declaration Near Reference" },
            { "Microsoft.CodeAnalysis.VisualBasic.NameTupleElement.VisualBasicNameTupleElementCodeRefactoringProvider", "Name Tuple Element" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveAsyncModifier.VisualBasicRemoveAsyncModifierCodeFixProvider", "Remove Async Modifier" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveSharedFromModuleMembers.VisualBasicRemoveSharedFromModuleMembersCodeFixProvider", "Remove Shared From Module Members" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable.VisualBasicRemoveUnusedVariableCodeFixProvider", "Remove Unused Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.ReplaceConditionalWithStatements.VisualBasicReplaceConditionalWithStatementsCodeRefactoringProvider", "Replace Conditional With Statements" },
            { "Microsoft.CodeAnalysis.VisualBasic.ReplaceDocCommentTextWithTag.VisualBasicReplaceDocCommentTextWithTagCodeRefactoringProvider", "Replace Doc Comment Text With Tag" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe.VisualBasicSimplifyThisOrMeCodeFixProvider", "Simplify This Or Me" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyTypeNames.SimplifyTypeNamesCodeFixProvider", "Simplify Type Names" },
            { "Microsoft.CodeAnalysis.VisualBasic.SpellCheck.VisualBasicSpellCheckCodeFixProvider", "Spell Check" },
            { "Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements.VisualBasicMergeConsecutiveIfStatementsCodeRefactoringProvider", "Merge Consecutive If Statements" },
            { "Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements.VisualBasicMergeNestedIfStatementsCodeRefactoringProvider", "Merge Nested If Statements" },
            { "Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements.VisualBasicSplitIntoConsecutiveIfStatementsCodeRefactoringProvider", "Split Into Consecutive If Statements" },
            { "Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements.VisualBasicSplitIntoNestedIfStatementsCodeRefactoringProvider", "Split Into Nested If Statements" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty.VisualBasicUseAutoPropertyCodeFixProvider", "Use Auto Property" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseNamedArguments.VisualBasicUseNamedArgumentsCodeRefactoringProvider", "Use Named Arguments" },
            { "Microsoft.CodeAnalysis.VisualBasic.Wrapping.VisualBasicWrappingCodeRefactoringProvider", "Wrapping" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers.VisualBasicAddAccessibilityModifiersCodeFixProvider", "Add Accessibility Modifiers" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf.VisualBasicConvertGetTypeToNameOfCodeFixProvider", "Convert Get Type To Name Of" },
            { "Microsoft.CodeAnalysis.VisualBasic.FileHeaders.VisualBasicFileHeaderCodeFixProvider", "File Header" },
            { "Microsoft.CodeAnalysis.VisualBasic.OrderModifiers.VisualBasicOrderModifiersCodeFixProvider", "Order Modifiers" },
            { "Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch.VisualBasicPopulateSwitchStatementCodeFixProvider", "Populate Switch Statement" },
            { "Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess.VisualBasicQualifyMemberAccessCodeFixProvider", "Qualify Member Access" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal.VisualBasicRemoveUnnecessaryByValCodeFixProvider", "Remove Unnecessary By Val" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCast.VisualBasicRemoveUnnecessaryCastCodeFixProvider", "Remove Unnecessary Cast" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports.VisualBasicRemoveUnnecessaryImportsCodeFixProvider", "Remove Unnecessary Imports" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses.VisualBasicRemoveUnnecessaryParenthesesCodeFixProvider", "Remove Unnecessary Parentheses" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers.VisualBasicRemoveUnusedMembersCodeFixProvider", "Remove Unused Members" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues.VisualBasicRemoveUnusedValuesCodeFixProvider", "Remove Unused Values" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation.VisualBasicSimplifyInterpolationCodeFixProvider", "Simplify Interpolation" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyLinqExpression.VisualBasicSimplifyLinqExpressionCodeFixProvider", "Simplify Linq Expression" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyObjectCreation.VisualBasicSimplifyObjectCreationCodeFixProvider", "Simplify Object Creation" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer.VisualBasicUseCollectionInitializerCodeFixProvider", "Use Collection Initializer" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment.VisualBasicUseCompoundAssignmentCodeFixProvider", "Use Compound Assignment" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression.VisualBasicUseConditionalExpressionForAssignmentCodeFixProvider", "Use Conditional Expression For Assignment" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression.VisualBasicUseConditionalExpressionForReturnCodeFixProvider", "Use Conditional Expression For Return" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName.VisualBasicUseInferredMemberNameCodeFixProvider", "Use Inferred Member Name" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression.VisualBasicUseIsNotExpressionCodeFixProvider", "Use Is Not Expression" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck.VisualBasicUseIsNullCheckForReferenceEqualsCodeFixProvider", "Use Is Null Check For Reference Equals" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation.VisualBasicUseNullPropagationCodeFixProvider", "Use Null Propagation" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer.VisualBasicUseObjectInitializerCodeFixProvider", "Use Object Initializer" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddObsoleteAttribute.VisualBasicAddObsoleteAttributeCodeFixProvider", "Add Obsolete Attribute" },
            { "Microsoft.CodeAnalysis.VisualBasic.AliasAmbiguousType.VisualBasicAliasAmbiguousTypeCodeFixProvider", "Alias Ambiguous Type" },
            { "Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly.VisualBasicMakeFieldReadonlyCodeFixProvider", "Make Field Readonly" },
            { "Microsoft.CodeAnalysis.VisualBasic.MakeTypeAbstract.VisualBasicMakeTypeAbstractCodeFixProvider", "Make Type Abstract" },
            { "Microsoft.CodeAnalysis.VisualBasic.UnsealClass.VisualBasicUnsealClassCodeFixProvider", "Unseal Class" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.VisualBasicRemoveDocCommentNodeCodeFixProvider", "Remove Doc Comment Node" },
            { "Microsoft.CodeAnalysis.CodeStyle.VisualBasicFormattingCodeFixProvider", "Formatting" },
            { "VisualBasicAddMissingImportsRefactoringProvider", "Add Missing Imports" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable.CorrectNextControlVariableCodeFixProvider+CorrectNextControlVariableCodeAction", "Correct Next Control Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent.GenerateEventCodeFixProvider+GenerateEventCodeAction", "Generate Event" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+AddKeywordCodeAction", "Add Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+ReplaceKeywordCodeAction", "Replace Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+ReplaceTokenKeywordCodeAction", "Replace Token Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile.MoveToTopOfFileCodeFixProvider+MoveToLineCodeAction", "Move To Line" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase.OverloadBaseCodeFixProvider+AddKeywordAction", "Add Keyword Action" },
            { "Microsoft.CodeAnalysis.CodeFixesAndRefactorings.DocumentBasedFixAllProviderHelpers+PostProcessCodeAction", "Post Process" },
        }.ToImmutableDictionary();

        public static void Main(string[] args)
        {
            Console.WriteLine("Loading assemblies and finding CodeActions ...");

            var assemblies = GetAssemblies(args);
            var codeActionAndProviderTypes = GetCodeActionAndProviderTypes(assemblies);

            Console.WriteLine($"Generating Kusto datatable of {codeActionAndProviderTypes.Length} CodeAction and provider hashes ...");

            var telemetryInfos = GetTelemetryInfos(codeActionAndProviderTypes);
            var datatable = GenerateKustoDatatable(telemetryInfos);

            var filepath = Path.GetFullPath(".\\ActionTable.txt");

            Console.WriteLine($"Writing datatable to {filepath} ...");

            File.WriteAllText(filepath, datatable);

            Console.WriteLine("Complete.");
        }

        internal static ImmutableArray<Assembly> GetAssemblies(string[] paths)
        {
            if (paths.Length == 0)
            {
                // By default inspect the Roslyn assemblies
                paths = Directory.EnumerateFiles(s_executingPath, "Microsoft.CodeAnalysis*.dll")
                    .ToArray();
            }

            var currentDirectory = new Uri(Environment.CurrentDirectory + "\\");
            return paths.Select(path =>
            {
                Console.WriteLine($"Loading assembly from {GetRelativePath(path, currentDirectory)}.");
                return Assembly.LoadFrom(path);
            }).ToImmutableArray();

            static string GetRelativePath(string path, Uri baseUri)
            {
                var rootedPath = Path.IsPathRooted(path)
                    ? path
                    : Path.GetFullPath(path);
                var relativePath = baseUri.MakeRelativeUri(new Uri(rootedPath));
                return relativePath.ToString();
            }
        }

        internal static ImmutableArray<Type> GetCodeActionAndProviderTypes(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(
                assembly => assembly.GetTypes().Where(
                    type => !type.GetTypeInfo().IsInterface && !type.GetTypeInfo().IsAbstract));

            return types
                .Where(t => isCodeActionType(t) || isCodeActionProviderType(t))
                .ToImmutableArray();

            static bool isCodeActionType(Type t) => typeof(CodeAction).IsAssignableFrom(t);

            static bool isCodeActionProviderType(Type t) => typeof(CodeFixProvider).IsAssignableFrom(t)
                || typeof(CodeRefactoringProvider).IsAssignableFrom(t);
        }

        internal static ImmutableArray<TelemetryInfo> GetTelemetryInfos(ImmutableArray<Type> codeActionAndProviderTypes)
        {
            return codeActionAndProviderTypes
                .Distinct(FullNameTypeComparer.Instance)
                .Select(GetTelemetryInfo)
                .ToImmutableArray();

            static TelemetryInfo GetTelemetryInfo(Type type)
            {
                // Generate dev17 telemetry hash
                var telemetryId = type.GetTelemetryId().ToString();
                var fnvHash = telemetryId.Substring(19);

                return Tuple.Create(type.FullName!, fnvHash);
            }
        }

        internal static string GenerateKustoDatatable(ImmutableArray<TelemetryInfo> telemetryInfos)
        {
            var missingDescriptions = new List<string>();

            var table = new StringBuilder();

            table.AppendLine("let actions = datatable(Description: string, ActionName: string, FnvHash: string)");
            table.AppendLine("[");

            foreach (var (actionTypeName, fnvHash) in telemetryInfos)
            {
                if (IgnoredCodeActions.Contains(actionTypeName))
                {
                    continue;
                }

                if (!CodeActionDescriptionMap.TryGetValue(actionTypeName, out var description))
                {
                    description = "**MISSING**";
                    missingDescriptions.Add(actionTypeName);
                }

                table.AppendLine(@$"  ""{description}"", ""{actionTypeName}"", ""{fnvHash}"",");
            }

            table.Append("];");

            if (missingDescriptions.Count > 0)
            {
                Console.WriteLine($"Descriptions were missing for the following type names:{Environment.NewLine}{string.Join(Environment.NewLine, missingDescriptions)}");
            }

            return table.ToString();
        }

        // NOTE: This method is unused but still present in case we want to auto-generate and refresh the "CodeActionsDescriptionMap".
        internal static string GenerateCodeActionsDescriptionMap(ImmutableArray<TelemetryInfo> telemetryInfos)
        {
            var builder = new StringBuilder();

            builder.AppendLine("{");

            var regex = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

            // Prefixes and suffixes to trim out.
            var prefixStrings = new[] { "CSharp", "VisualBasic" };
            var suffixStrings = new[] { "CodeFixProvider", "CodeRefactoringProvider", "RefactoringProvider",
                                        "CodeAction", "CodeActionWithOption", "CodeActionProvider" };

            foreach (var (actionOrProviderTypeName, _) in telemetryInfos)
            {
                if (IgnoredCodeActions.Contains(actionOrProviderTypeName))
                {
                    continue;
                }

                // We create the description string from the core type name after omitting the well-known prefixes and suffixes.

                var description = actionOrProviderTypeName.Split(".").Last();

                // Handle nested types
                if (description.Contains("+"))
                {
                    description = description.Substring(description.LastIndexOf("+") + 1);
                }

                foreach (var prefix in prefixStrings)
                {
                    if (description.StartsWith(prefix))
                    {
                        description = description.Substring(prefix.Length);
                        break;
                    }
                }

                foreach (var suffix in suffixStrings)
                {
                    if (description.EndsWith(suffix))
                    {
                        description = description.Substring(0, description.LastIndexOf(suffix));
                        break;
                    }
                }

                description = regex.Replace(description, " ");

                builder.AppendLine(@$"            {{ ""{actionOrProviderTypeName}"", ""{description}"" }},");
            }

            builder.Append("}");

            return builder.ToString();
        }

        private class FullNameTypeComparer : IEqualityComparer<Type>
        {
            public static FullNameTypeComparer Instance { get; } = new FullNameTypeComparer();

            public bool Equals(Type? x, Type? y)
                => Equals(x?.FullName, y?.FullName);

            public int GetHashCode([DisallowNull] Type obj)
            {
                return obj.FullName!.GetHashCode();
            }
        }
    }
}
