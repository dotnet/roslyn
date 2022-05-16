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
            { "Microsoft.CodeAnalysis.CSharp.TypeStyle.UseExplicitTypeCodeFixProvider", "Use Explicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.TypeStyle.UseImplicitTypeCodeFixProvider", "Use Implicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement.UseSimpleUsingStatementCodeFixProvider", "Use Simple Using Statement" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider", "Use 'Is Null' Check" },
            { "Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseIndexOperatorCodeFixProvider", "Use Index Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseRangeOperatorCodeFixProvider", "Use Range Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation.CSharpUseImplicitObjectCreationCodeFixProvider", "Use Implicit Object Creation" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast.CSharpRemoveUnnecessaryCastCodeFixProvider", "Remove Unnecessary Cast" },
            { "Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral.CSharpUseDefaultLiteralCodeFixProvider", "Use Default Literal" },
            { "Microsoft.CodeAnalysis.CSharp.UseDeconstruction.CSharpUseDeconstructionCodeFixProvider", "Use Deconstruction" },
            { "Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment.CSharpUseCompoundCoalesceAssignmentCodeFixProvider", "Use Compound Assignment" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode.CSharpRemoveUnreachableCodeCodeFixProvider", "Remove Unreachable Code" },
            { "Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives.MisplacedUsingDirectivesCodeFixProvider+MoveMisplacedUsingsCodeAction", "Misplaced Using Directives" },
            { "Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable.CSharpMakeStructFieldsWritableCodeFixProvider", "Make Struct Fields Writable" },
            { "Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess.InvokeDelegateWithConditionalAccessCodeFixProvider", "Invoke Delegate With Conditional Access" },
            { "Microsoft.CodeAnalysis.CSharp.InlineDeclaration.CSharpInlineDeclarationCodeFixProvider", "Inline Declaration" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression.ConvertSwitchStatementToExpressionCodeFixProvider", "Convert Switch Statement To Expression" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveConfusingSuppression.CSharpRemoveConfusingSuppressionCodeFixProvider", "Remove Confusing Suppressino" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation.CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider", "Remove Unneccessary Discard Designation" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.EmbeddedStatementPlacement.EmbeddedStatementPlacementCodeFixProvider", "New Lines: Embedded Statement Placement" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement.ConstructorInitializerPlacementCodeFixProvider", "New Lines: Constructor Initializer Placement" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveBracePlacement.ConsecutiveBracePlacementCodeFixProvider", "New Lines: Consecutive Brace Placement" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpIsAndCastCheckWithoutNameCodeFixProvider", "Use Pattern Matching: Is And Cast Check Without Name" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpUseNotPatternCodeFixProvider", "Use Pattern Matching: Use Not Pattern" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpAsAndNullCheckCodeFixProvider", "Use Pattern Matching: As And Null Check" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpIsAndCastCheckCodeFixProvider", "Use Pattern Matching: Is And Cast Check" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternCombinators.CSharpUsePatternCombinatorsCodeFixProvider", "Use Pattern Mathcing: Use Pattern Combinators" },
            { "Microsoft.CodeAnalysis.CSharp.UseLocalFunction.CSharpUseLocalFunctionCodeFixProvider", "Use Local Function" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBody.UseExpressionBodyCodeRefactoringProvider", "Use Expression Body (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBody.UseExpressionBodyCodeFixProvider", "Use Expression Body (Codefix)" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda.UseExpressionBodyForLambdaCodeStyleProvider", "Use Expression Body For Lambda" },
            { "Microsoft.CodeAnalysis.CSharp.UseExplicitTypeForConst.UseExplicitTypeForConstCodeFixProvider", "Use Explicit Type For Const" },
            { "Microsoft.CodeAnalysis.CSharp.ReverseForStatement.CSharpReverseForStatementCodeRefactoringProvider", "Reverse For Statement" },
            { "Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral.CSharpReplaceDefaultLiteralCodeFixProvider", "Replace Default Literal" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedLocalFunction.CSharpRemoveUnusedLocalFunctionCodeFixProvider", "Remove Unused Local Function" },
            { "Microsoft.CodeAnalysis.CSharp.MakeRefStruct.MakeRefStructCodeFixProvider", "Make Ref Struct" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.MakeLocalFunctionStaticCodeFixProvider", "Make Local Function Static (CodeFix)" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.MakeLocalFunctionStaticCodeRefactoringProvider", "Make Local Function Static (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.PassInCapturedVariablesAsArgumentsCodeFixProvider", "Make Local Function Static Pass In Captured Variables As Arguments" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementInterface.AbstractChangeImplementionCodeRefactoringProvider", "Implement Interface" },
            { "Microsoft.CodeAnalysis.CSharp.DisambiguateSameVariable.CSharpDisambiguateSameVariableCodeFixProvider", "Disambiguate Same Variable" },
            { "Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces.CSharpAddBracesCodeFixProvider", "Add Braces" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString.AbstractConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider`1", "Convert Between Regular And Verbatim String" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseType.AbstractUseTypeCodeRefactoringProvider", "Use Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.LambdaSimplifier.LambdaSimplifierCodeRefactoringProvider", "Lambda Simplifier" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary.CSharpInlineTemporaryCodeRefactoringProvider", "Inline Temporary" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable.EnableNullableCodeRefactoringProvider", "Enable nullable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertLocalFunctionToMethod.CSharpConvertLocalFunctionToMethodCodeRefactoringProvider", "Convert Local Function To Method" },
            { "Microsoft.CodeAnalysis.CSharp.UseInterpolatedVerbatimString.CSharpUseInterpolatedVerbatimStringCodeFixProvider", "Use Interpolated Verbatim String" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveNewModifier.RemoveNewModifierCodeFixProvider", "Remove New Modifier" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveInKeyword.RemoveInKeywordCodeFixProvider", "Remove In Keyword" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable.CSharpDeclareAsNullableCodeFixProvider", "Declare As Nullable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeStatementAsynchronous.CSharpMakeStatementAsynchronousCodeFixProvider", "Make Statement Asynchronous" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator.CSharpAddYieldCodeFixProvider", "Add Yield" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator.CSharpChangeToIEnumerableCodeFixProvider", "Change To IEnumerable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase.HideBaseCodeFixProvider+AddNewKeywordAction", "Hide Basen" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.FixReturnType.CSharpFixReturnTypeCodeFixProvider", "Fix Return Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation.CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider", "Add Parentheses Around Conditional Expression In String Interpolation" },
            { "Microsoft.CodeAnalysis.CSharp.AssignOutParameters.AbstractAssignOutParametersCodeFixProvider", "Assign Out Parameters" },
            { "Microsoft.CodeAnalysis.Wrapping.WrapItemsAction", "Wrap Items" },
            { "Microsoft.CodeAnalysis.UpgradeProject.ProjectOptionsChangeAction", "Upgrade Project" },
            { "Microsoft.CodeAnalysis.ExtractInterface.ExtractInterfaceCodeAction", "Extract Interface" },
            { "Microsoft.CodeAnalysis.ExtractClass.ExtractClassWithDialogCodeAction", "Extract Class With Dialog" },
            { "Microsoft.CodeAnalysis.CodeFixes.FixMultipleCodeAction", "Fix Multiple" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.TopLevelSuppressionCodeAction", "Top Level Suppression" },
            { "Microsoft.CodeAnalysis.ChangeSignature.ChangeSignatureCodeAction", "Change Signature" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallPackageDirectlyCodeAction", "Install Package Directly" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallPackageParentCodeAction", "Install Package Parent" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallWithPackageManagerCodeAction", "Install With Package Manager" },
            { "Microsoft.CodeAnalysis.AddMissingReference.AddMissingReferenceCodeAction", "Add Missing Reference" },
            { "Microsoft.CodeAnalysis.UpdateLegacySuppressions.UpdateLegacySuppressionsCodeFixProvider", "Update Legacy Suppressions" },
            { "Microsoft.CodeAnalysis.UseThrowExpression.UseThrowExpressionCodeFixProvider", "Use Throw Expression" },
            { "Microsoft.CodeAnalysis.UseSystemHashCode.UseSystemHashCodeCodeFixProvider", "Use System.HashCode" },
            { "Microsoft.CodeAnalysis.UseObjectInitializer.AbstractUseObjectInitializerCodeFixProvider`7", "Use Object Initializer" },
            { "Microsoft.CodeAnalysis.UseNullPropagation.AbstractUseNullPropagationCodeFixProvider`10", "Use Null Propagation" },
            { "Microsoft.CodeAnalysis.UseExplicitTupleName.UseExplicitTupleNameCodeFixProvider", "Use Explicit Tuple Name" },
            { "Microsoft.CodeAnalysis.UseIsNullCheck.AbstractUseIsNullCheckForReferenceEqualsCodeFixProvider`1", "Use 'Is Null' Check" },
            { "Microsoft.CodeAnalysis.UseInferredMemberName.AbstractUseInferredMemberNameCodeFixProvider", "Use Inferred Member Name" },
            { "Microsoft.CodeAnalysis.UseConditionalExpression.AbstractUseConditionalExpressionForAssignmentCodeFixProvider`6", "Use Conditional Expression For Assignment" },
            { "Microsoft.CodeAnalysis.UseConditionalExpression.AbstractUseConditionalExpressionForReturnCodeFixProvider`4", "Use Conditional Expression For Return" },
            { "Microsoft.CodeAnalysis.UseCompoundAssignment.AbstractUseCompoundAssignmentCodeFixProvider`3", "Use Compound Assignment" },
            { "Microsoft.CodeAnalysis.UseCollectionInitializer.AbstractUseCollectionInitializerCodeFixProvider`8", "Use Collection Initializer" },
            { "Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionCodeFixProvider", "Use Coalesce Expression" },
            { "Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionForNullableCodeFixProvider", "Use Coalesce Expression For Nullable" },
            { "Microsoft.CodeAnalysis.SimplifyLinqExpression.AbstractSimplifyLinqExpressionCodeFixProvider`3", "Simplify Linq Expression" },
            { "Microsoft.CodeAnalysis.SimplifyInterpolation.AbstractSimplifyInterpolationCodeFixProvider`7", "Simplify Interpolation" },
            { "Microsoft.CodeAnalysis.SimplifyBooleanExpression.SimplifyConditionalCodeFixProvider", "Simplify Boolean Expression" },
            { "Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues.AbstractRemoveUnusedValuesCodeFixProvider`11", "Remove Unused Parameters And Values" },
            { "Microsoft.CodeAnalysis.RemoveUnusedMembers.AbstractRemoveUnusedMembersCodeFixProvider`1", "Remove Unused Members" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessaryImports.AbstractRemoveUnnecessaryImportsCodeFixProvider", "Remove Unnecessary Imports" },
            { "Microsoft.CodeAnalysis.QualifyMemberAccess.AbstractQualifyMemberAccessCodeFixprovider`2", "Qualify Member Access" },
            { "Microsoft.CodeAnalysis.PopulateSwitch.AbstractPopulateSwitchCodeFixProvider`4", "Populate Switch" },
            { "Microsoft.CodeAnalysis.OrderModifiers.AbstractOrderModifiersCodeFixProvider", "Order Modifiers" },
            { "Microsoft.CodeAnalysis.MakeFieldReadonly.AbstractMakeFieldReadonlyCodeFixProvider`2", "Make Field Readonly" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryInlineSuppressionsCodeFixProvider", "Remove Unnecessary Inline Suppressions" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryAttributeSuppressionsCodeFixProvider", "Remove Unnecessary Attribute Suppressions" },
            { "Microsoft.CodeAnalysis.RemoveRedundantEquality.RemoveRedundantEqualityCodeFixProvider", "Remove Redundant Equality" },
            { "Microsoft.CodeAnalysis.NewLines.MultipleBlankLines.MultipleBlankLinesCodeFixProvider", "New Lines: Multiple Blank Lines" },
            { "Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement.ConsecutiveStatementPlacementCodeFixProvider", "New Lines: Consecutive Statement Placement" },
            { "Microsoft.CodeAnalysis.FileHeaders.AbstractFileHeaderCodeFixProvider", "File Headers" },
            { "Microsoft.CodeAnalysis.ConvertTypeOfToNameOf.AbstractConvertTypeOfToNameOfCodeFixProvider", "Convert TypeOf To NameOf" },
            { "Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple.AbstractConvertAnonymousTypeToTupleCodeFixProvider`3", "Convert Anonymous Type To Tuple" },
            { "Microsoft.CodeAnalysis.AddRequiredParentheses.AddRequiredParenthesesCodeFixProvider", "Add Required Parentheses" },
            { "Microsoft.CodeAnalysis.AddAccessibilityModifiers.AbstractAddAccessibilityModifiersCodeFixProvider", "Add Accessibility Modifiers" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses.AbstractRemoveUnnecessaryParenthesesCodeFixProvider`1", "Remove Unnecessary Parentheses" },
            { "Microsoft.CodeAnalysis.UseNamedArguments.AbstractUseNamedArgumentsCodeRefactoringProvider", "Use Named Arguments" },
            { "Microsoft.CodeAnalysis.UseAutoProperty.AbstractUseAutoPropertyCodeFixProvider`5+UseAutoPropertyCodeAction", "Use Auto Property" },
            { "Microsoft.CodeAnalysis.UnsealClass.AbstractUnsealClassCodeFixProvider", "Unseal Class" },
            { "Microsoft.CodeAnalysis.SplitOrMergeIfStatements.AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider", "Merge Consecutive If Statements" },
            { "Microsoft.CodeAnalysis.SplitOrMergeIfStatements.AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider", "Split Into Consecutive If Statements" },
            { "Microsoft.CodeAnalysis.SplitOrMergeIfStatements.AbstractMergeNestedIfStatementsCodeRefactoringProvider", "Merge Nested If Statements" },
            { "Microsoft.CodeAnalysis.SplitOrMergeIfStatements.AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider", "Split Into Nested If Statements" },
            { "Microsoft.CodeAnalysis.SpellCheck.AbstractSpellCheckCodeFixProvider`1+SpellCheckCodeAction", "Spell Check" },
            { "Microsoft.CodeAnalysis.SpellCheck.AbstractSpellCheckCodeFixProvider`1", "Spell Check" },
            { "Microsoft.CodeAnalysis.SimplifyTypeNames.AbstractSimplifyTypeNamesCodeFixProvider`1", "Simplify Type Names" },
            { "Microsoft.CodeAnalysis.SimplifyThisOrMe.AbstractSimplifyThisOrMeCodeFixProvider`1", "Simplify This Or Me" },
            { "Microsoft.CodeAnalysis.ReplacePropertyWithMethods.ReplacePropertyWithMethodsCodeRefactoringProvider+ReplacePropertyWithMethodsCodeAction", "Replace Property With Methods" },
            { "Microsoft.CodeAnalysis.ReplaceMethodWithProperty.ReplaceMethodWithPropertyCodeRefactoringProvider+ReplaceMethodWithPropertyCodeAction", "Replace Method With Property" },
            { "Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag.AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider", "Replace Doc Comment Text With Tag" },
            { "Microsoft.CodeAnalysis.RemoveUnusedVariable.AbstractRemoveUnusedVariableCodeFixProvider`3", "Remove Unused Variable" },
            { "Microsoft.CodeAnalysis.RemoveAsyncModifier.AbstractRemoveAsyncModifierCodeFixProvider`2", "Remove Async Modifier" },
            { "Microsoft.CodeAnalysis.PreferFrameworkType.PreferFrameworkTypeCodeFixProvider+PreferFrameworkTypeCodeAction", "Prefer Framework Type" },
            { "Microsoft.CodeAnalysis.NameTupleElement.AbstractNameTupleElementCodeRefactoringProvider`2", "Name Tuple Element" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.AbstractMoveToNamespaceCodeAction+MoveItemsToNamespaceCodeAction", "Move Items To Namespace" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.AbstractMoveToNamespaceCodeAction+MoveTypeToNamespaceCodeAction", "Move Type To Namespace" },
            { "Microsoft.CodeAnalysis.MoveDeclarationNearReference.AbstractMoveDeclarationNearReferenceCodeRefactoringProvider`1", "Move Declaration Near Reference" },
            { "Microsoft.CodeAnalysis.MakeMethodSynchronous.AbstractMakeMethodSynchronousCodeFixProvider", "Make Method Synchronous" },
            { "Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider", "Make Method Asynchronous" },
            { "Microsoft.CodeAnalysis.MakeMemberStatic.AbstractMakeMemberStaticCodeFixProvider", "Make Member Static" },
            { "Microsoft.CodeAnalysis.MakeTypeAbstract.AbstractMakeTypeAbstractCodeFixProvider`1", "Make Type Abstract" },
            { "Microsoft.CodeAnalysis.InvertLogical.AbstractInvertLogicalCodeRefactoringProvider`3", "Invert Logical" },
            { "Microsoft.CodeAnalysis.InvertIf.AbstractInvertIfCodeRefactoringProvider`3", "Invert If" },
            { "Microsoft.CodeAnalysis.InvertConditional.AbstractInvertConditionalCodeRefactoringProvider`1", "Invert Conditional (Refactoring)" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceVariableService`6+IntroduceVariableCodeAction", "Introduce Variable" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceVariableService`6+IntroduceVariableAllOccurrenceCodeAction", "Introduce Variable All Occurrence" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceLocalForExpressionCodeRefactoringProvider`4", "Introduce Variable For Expression" },
            { "Microsoft.CodeAnalysis.IntroduceUsingStatement.AbstractIntroduceUsingStatementCodeRefactoringProvider`2", "Introduce Using Statement" },
            { "Microsoft.CodeAnalysis.InlineMethod.AbstractInlineMethodRefactoringProvider`4+MySolutionChangeAction", "Inline Method (Refactoring)" },
            { "Microsoft.CodeAnalysis.InitializeParameter.AbstractInitializeParameterCodeRefactoringProvider`4", "Initialize Parameter" },
            { "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction", "Implement Interface" },
            { "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction", "Implement Interface With Dispose Pattern" },
            { "Microsoft.CodeAnalysis.ImplementAbstractClass.AbstractImplementAbstractClassCodeFixProvider`1", "Implement Abstract Class" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+GenerateTypeCodeAction", "Generate Type" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+GenerateTypeCodeActionWithOption", "Generate Type With Option" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6", "Generate Type" },
            { "Microsoft.CodeAnalysis.GenerateOverrides.GenerateOverridesCodeRefactoringProvider+GenerateOverridesWithDialogCodeAction", "Generate Overrides With Dialog" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateVariableCodeAction", "Generate Variable" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3", "Generate Variable" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateLocalCodeAction", "Generate Local" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateParameterCodeAction", "Generate Parameter" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember.AbstractGenerateParameterizedMemberService`4+GenerateParameterizedMemberCodeAction", "Generate Parameterized Member" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember.AbstractGenerateEnumMemberService`3+GenerateEnumMemberCodeAction", "Generate Enum Member" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+GenerateDefaultConstructorCodeAction", "Generate Default Constructors" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+CodeActionAll", "Generate Default Constructors All" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor.AbstractGenerateConstructorService`2", "Generate Constructor" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider+GenerateEqualsAndGetHashCodeAction", "Generate Equals And Get Hash Code From Members" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider+GenerateEqualsAndGetHashCodeWithDialogCodeAction", "Generate Equals And Get Hash Code From Members With Dialog" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+ConstructorDelegatingCodeAction", "Generate Constructor From Members (Constructor Delegating)" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+FieldDelegatingCodeAction", "Generate Constructor From Members (Field Delegating)" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+GenerateConstructorWithDialogCodeAction", "Generate Constructor From Members With Dialog" },
            { "Microsoft.CodeAnalysis.GenerateComparisonOperators.GenerateComparisonOperatorsCodeRefactoringProvider", "Generate Comparison Operators" },
            { "Microsoft.CodeAnalysis.Formatting.FormattingCodeFixProvider", "Fix Formatting" },
            { "Microsoft.CodeAnalysis.EncapsulateField.AbstractEncapsulateFieldService", "Encapsulate Field" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.AbstractAddDocCommentNodesCodeFixProvider`4", "Diagnostic Comments: Add DocComment Nodes" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.AbstractRemoveDocCommentNodeCodeFixProvider`2", "Diagnostic Comments: Remove DocComment Node" },
            { "Microsoft.CodeAnalysis.ConvertTupleToStruct.AbstractConvertTupleToStructCodeRefactoringProvider`10", "Convert Tuple To Struct" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.AbstractConvertConcatenationToInterpolatedStringRefactoringProvider`1", "Convert Concatenation To Interpolated String" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider`5+ConvertToInterpolatedStringCodeAction", "Convert Placeholder To Interpolated String" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.ConvertRegularStringToInterpolatedStringRefactoringProvider", "Convert Regular String To Interpolated String" },
            { "Microsoft.CodeAnalysis.ConvertNumericLiteral.AbstractConvertNumericLiteralCodeRefactoringProvider`1", "Convert Numeric Literal" },
            { "Microsoft.CodeAnalysis.ConvertLinq.AbstractConvertLinqQueryToForEachProvider`2", "Convert Linq Query To ForEach" },
            { "Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery.AbstractConvertForEachToLinqQueryProvider`2+ForEachToLinqQueryCodeAction", "Convert ForEach To Linq Query" },
            { "Microsoft.CodeAnalysis.ConvertIfToSwitch.AbstractConvertIfToSwitchCodeRefactoringProvider`4", "Convert If To Switch" },
            { "Microsoft.CodeAnalysis.ConvertForToForEach.AbstractConvertForToForEachCodeRefactoringProvider`6", "Convert For To ForEach" },
            { "Microsoft.CodeAnalysis.ConvertForEachToFor.AbstractConvertForEachToForCodeRefactoringProvider`2+ForEachToForCodeAction", "Convert ForEach To For" },
            { "Microsoft.CodeAnalysis.ConvertCast.AbstractConvertCastCodeRefactoringProvider`3", "Convert Cast" },
            { "Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty.AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider`3+ConvertAutoPropertyToFullPropertyCodeAction", "Convert AutoProperty To Full Property" },
            { "Microsoft.CodeAnalysis.ConflictMarkerResolution.AbstractResolveConflictMarkerCodeFixProvider", "Resolve Conflict Marker" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToClass.AbstractConvertAnonymousTypeToClassCodeRefactoringProvider`6", "Convert Anonymous Type To Class" },
            { "Microsoft.CodeAnalysis.AddMissingImports.AbstractAddMissingImportsRefactoringProvider+AddMissingImportsCodeAction", "Add Missing Imports (Paste)" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.AbstractPullMemberUpRefactoringProvider+PullMemberUpWithDialogCodeAction", "Pull Member Up" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace.AbstractSyncNamespaceCodeRefactoringProvider`3+ChangeNamespaceCodeAction", "Sync Namespace: Change Namespace" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace.AbstractSyncNamespaceCodeRefactoringProvider`3+MoveFileCodeAction", "Sync Namespace: Move File" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.MoveType.AbstractMoveTypeService`5+MoveTypeCodeAction", "Move Type" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod.ExtractMethodCodeRefactoringProvider", "Extract Method" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.AddAwait.AbstractAddAwaitCodeRefactoringProvider`1", "Add Await" },
            { "Microsoft.CodeAnalysis.CodeFixes.NamingStyles.NamingStyleCodeFixProvider+FixNameCodeAction", "Fix Naming Style" },
            { "Microsoft.CodeAnalysis.CodeFixes.MatchFolderAndNamespace.AbstractChangeNamespaceToMatchFolderCodeFixProvider", "Change Namespace To Match Folder" },
            { "Microsoft.CodeAnalysis.CodeFixes.FullyQualify.AbstractFullyQualifyCodeFixProvider", "Fully Qualify" },
            { "Microsoft.CodeAnalysis.CodeFixes.FullyQualify.AbstractFullyQualifyCodeFixProvider+GroupingCodeAction", "Fully Qualify (Grouping)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageCodeAction", "Suppression.: Global Suppress Message" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageFixAllCodeAction", "Suppression: Global Suppress Message (FixAll)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+LocalSuppressMessageCodeAction", "Suppression: Local Suppress Message" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+PragmaWarningCodeAction", "Suppression: Pragma Warning" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity.ConfigureSeverityLevelCodeFixProvider+TopLevelBulkConfigureSeverityCodeAction", "Configure Severity: TopLevel Bulk Configure Severity" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity.ConfigureSeverityLevelCodeFixProvider+TopLevelConfigureSeverityCodeAction", "Configure Severity: TopLevel Configure Severity" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle.ConfigureCodeStyleOptionCodeFixProvider+TopLevelConfigureCodeStyleOptionCodeAction", "Configure CodeStyle Option: TopLevel Configure CodeStyle Option" },
            { "Microsoft.CodeAnalysis.CodeFixes.Async.AbstractConvertToAsyncCodeFixProvider", "Convert To Async" },
            { "Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast.AbstractAddExplicitCastCodeFixProvider`1", "Add Explicit Cast" },
            { "Microsoft.CodeAnalysis.AliasAmbiguousType.AbstractAliasAmbiguousTypeCodeFixProvider", "Alias Ambiguous Type" },
            { "Microsoft.CodeAnalysis.AddParameter.AbstractAddParameterCodeFixProvider`6", "Add Parameter" },
            { "Microsoft.CodeAnalysis.AddObsoleteAttribute.AbstractAddObsoleteAttributeCodeFixProvider", "Add Obsolete Attribute" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+AssemblyReferenceCodeAction", "AddImport (Assembly Reference)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+InstallPackageAndAddImportCodeAction", "AddImport (Install Package And Add Import)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+InstallWithPackageManagerCodeAction", "AddImport (Install With PackageManager)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+MetadataSymbolReferenceCodeAction", "AddImport (Metadata Symbol Reference)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+ParentInstallPackageCodeAction", "Add Import (Install Nuget Package)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+ProjectSymbolReferenceCodeAction", "Add Import (Project Symbol Reference)" },
            { "Microsoft.CodeAnalysis.AddFileBanner.AbstractAddFileBannerCodeRefactoringProvider", "Add File Banner" },
            { "Microsoft.CodeAnalysis.AddDebuggerDisplay.AbstractAddDebuggerDisplayCodeRefactoringProvider`2", "Add Debugger Display" },
            { "Microsoft.CodeAnalysis.AddConstructorParametersFromMembers.AddConstructorParametersFromMembersCodeRefactoringProvider+AddConstructorParametersCodeAction", "Add Constructor Parameters From Members" },
            { "Microsoft.CodeAnalysis.AddAnonymousTypeMemberName.AbstractAddAnonymousTypeMemberNameCodeFixProvider`3", "Add Anonymous Type Member Name" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageFixAllCodeAction+GlobalSuppressionSolutionChangeAction", "Suppression: Global Suppress Message (FixAll)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+RemoveSuppressionCodeAction+AttributeRemoveAction", "Suppression: Remove Suppression (Attribute)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+RemoveSuppressionCodeAction+PragmaRemoveAction", "Suppression: Remove Suppression (Pragma)" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeActions.RemoveStatementCodeAction", "Remove Statement" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable.CorrectNextControlVariableCodeFixProvider+CorrectNextControlVariableCodeAction", "Correct Next Control Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEndConstruct.GenerateEndConstructCodeFixProvider", "Generate End Construct" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent.GenerateEventCodeFixProvider+GenerateEventCodeAction", "Generate Event" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+AddKeywordCodeAction", "Incorrect Exit Continue: Add Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+ReplaceKeywordCodeAction", "Incorrect Exit Continue: Replace Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+ReplaceTokenKeywordCodeAction", "Incorrect Exit Continue: Replace Token Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectFunctionReturnType.IncorrectFunctionReturnTypeCodeFixProvider", "Incorrect Function Return Type" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicChangeToYieldCodeFixProvider", "Change To Yield" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicConvertToIteratorCodeFixProvider", "Convert To Iterator" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile.MoveToTopOfFileCodeFixProvider+MoveToLineCodeAction", "Move To Top Of File" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase.OverloadBaseCodeFixProvider+AddKeywordAction", "Overload Base: Add Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary.VisualBasicInlineTemporaryCodeRefactoringProvider", "Inline Temporary" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveSharedFromModuleMembers.VisualBasicRemoveSharedFromModuleMembersCodeFixProvider", "Remove Shared From Module Members" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal.VisualBasicRemoveUnnecessaryByValCodeFixProvider", "Remove Unnecessary ByVal" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCast.VisualBasicRemoveUnnecessaryCastCodeFixProvider", "Remove Unnecessary Cast" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression.VisualBasicUseIsNotExpressionCodeFixProvider", "Use IsNot Expression" },
            { "Microsoft.CodeAnalysis.CSharp.UseTupleSwap.CSharpUseTupleSwapCodeFixProvider", "Use Tuple Swap" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseNullCheckOverTypeCheckCodeFixProvider", "Use Null Check Over Type Check" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyPropertyPattern.CSharpSimplifyPropertyPatternCodeFixProvider", "Simplify Property Pattern" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNamespace.ConvertNamespaceCodeRefactoringProvider", "Convert Namespace Refactoring (FileScope/BlockScope)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNamespace.ConvertNamespaceCodeFixProvider", "Convert Namespace CodeFix (FileScope/BlockScope)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns.UseRecursivePatternsCodeRefactoringProvider", "Use Recursive Patterns" },
            { "Microsoft.CodeAnalysis.MoveStaticMembers.MoveStaticMembersWithDialogCodeAction", "Move Static Members" },
            { "Microsoft.CodeAnalysis.SimplifyInterpolation.AbstractSimplifyInterpolationCodeFixProvider`5", "Simplify Interpolation" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceParameterService`4", "Introduce Parameter" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+GenerateDefaultConstructorCodeAction", "Generate Default Constructor" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+CodeActionAll", "Generate Default Constructur (All)" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider`6+ConvertToInterpolatedStringCodeAction", "Convert Placeholder To Interpolated String" },
            { "Microsoft.CodeAnalysis.ConvertAnonymousType.AbstractConvertAnonymousTypeToClassCodeRefactoringProvider`6", "Convert Anonymous Type To Class" },
            { "Microsoft.CodeAnalysis.ConvertAnonymousType.AbstractConvertAnonymousTypeToTupleCodeRefactoringProvider`3", "Convert Anonymous Type To Tuple" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyObjectCreation.VisualBasicSimplifyObjectCreationCodeFixProvider", "Simplify Object Creation" },
            { "Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking.RenameTrackingTaggerProvider+RenameTrackingCodeAction", "Rename Tracking" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc.AddInheritdocCodeFixProvider", "Add Inheritdoc" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.TransposeRecordKeyword.CSharpTransposeRecordKeywordCodeFixProvider", "Fix record declaration" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertToRawString.ConvertRegularStringToRawStringCodeRefactoringProvider", "Convert to raw string" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryLambdaExpression.CSharpRemoveUnnecessaryLambdaExpressionCodeFixProvider", "Remove Unnecessary Lambda Expression" },
            { "Microsoft.CodeAnalysis.CSharp.UseParameterNullChecking.CSharpUseParameterNullCheckingCodeFixProvider", "Use Parameter Null Checking" },
            { "Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices.AbstractJsonDetectionCodeFixProvider", "Enable all JSON editor features" },
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
