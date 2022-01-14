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
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
            { "Microsoft.CodeAnalysis.CSharp.TypeStyle.UseExplicitTypeCodeFixProvider+MyCodeAction", "Use Explicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.TypeStyle.UseImplicitTypeCodeFixProvider+MyCodeAction", "Use Implicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement.UseSimpleUsingStatementCodeFixProvider+MyCodeAction", "Use Simple Using Statement" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider+MyCodeAction", "Use 'Is Null' Check" },
            { "Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseIndexOperatorCodeFixProvider+MyCodeAction", "Use Index Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseRangeOperatorCodeFixProvider+MyCodeAction", "Use Range Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation.CSharpUseImplicitObjectCreationCodeFixProvider+MyCodeAction", "Use Implicit Object Creation" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast.CSharpRemoveUnnecessaryCastCodeFixProvider+MyCodeAction", "Remove Unnecessary Cast" },
            { "Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral.CSharpUseDefaultLiteralCodeFixProvider+MyCodeAction", "Use Default Literal" },
            { "Microsoft.CodeAnalysis.CSharp.UseDeconstruction.CSharpUseDeconstructionCodeFixProvider+MyCodeAction", "Use Deconstruction" },
            { "Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment.CSharpUseCompoundCoalesceAssignmentCodeFixProvider+MyCodeAction", "Use Compound Assignment" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode.CSharpRemoveUnreachableCodeCodeFixProvider+MyCodeAction", "Remove Unreachable Code" },
            { "Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives.MisplacedUsingDirectivesCodeFixProvider+MoveMisplacedUsingsCodeAction", "Misplaced Using Directives" },
            { "Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable.CSharpMakeStructFieldsWritableCodeFixProvider+MyCodeAction", "Make Struct Fields Writable" },
            { "Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess.InvokeDelegateWithConditionalAccessCodeFixProvider+MyCodeAction", "Invoke Delegate With Conditional Access" },
            { "Microsoft.CodeAnalysis.CSharp.InlineDeclaration.CSharpInlineDeclarationCodeFixProvider+MyCodeAction", "Inline Declaration" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression.ConvertSwitchStatementToExpressionCodeFixProvider+MyCodeAction", "Convert Switch Statement To Expression" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveConfusingSuppression.CSharpRemoveConfusingSuppressionCodeFixProvider+MyCodeAction", "Remove Confusing Suppressino" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation.CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider+MyCodeAction", "Remove Unneccessary Discard Designation" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.EmbeddedStatementPlacement.EmbeddedStatementPlacementCodeFixProvider+MyCodeAction", "New Lines: Embedded Statement Placement" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement.ConstructorInitializerPlacementCodeFixProvider+MyCodeAction", "New Lines: Constructor Initializer Placement" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveBracePlacement.ConsecutiveBracePlacementCodeFixProvider+MyCodeAction", "New Lines: Consecutive Brace Placement" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpIsAndCastCheckWithoutNameCodeFixProvider+MyCodeAction", "Use Pattern Matching: Is And Cast Check Without Name" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpUseNotPatternCodeFixProvider+MyCodeAction", "Use Pattern Matching: Use Not Pattern" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpAsAndNullCheckCodeFixProvider+MyCodeAction", "Use Pattern Matching: As And Null Check" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpIsAndCastCheckCodeFixProvider+MyCodeAction", "Use Pattern Matching: Is And Cast Check" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternCombinators.CSharpUsePatternCombinatorsCodeFixProvider+MyCodeAction", "Use Pattern Mathcing: Use Pattern Combinators" },
            { "Microsoft.CodeAnalysis.CSharp.UseLocalFunction.CSharpUseLocalFunctionCodeFixProvider+MyCodeAction", "Use Local Function" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBody.UseExpressionBodyCodeRefactoringProvider+MyCodeAction", "Use Expression Body (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBody.UseExpressionBodyCodeFixProvider+MyCodeAction", "Use Expression Body (Codefix)" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda.UseExpressionBodyForLambdaCodeStyleProvider+MyCodeAction", "Use Expression Body For Lambda" },
            { "Microsoft.CodeAnalysis.CSharp.UseExplicitTypeForConst.UseExplicitTypeForConstCodeFixProvider+MyCodeAction", "Use Explicit Type For Const" },
            { "Microsoft.CodeAnalysis.CSharp.ReverseForStatement.CSharpReverseForStatementCodeRefactoringProvider+MyCodeAction", "Reverse For Statement" },
            { "Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral.CSharpReplaceDefaultLiteralCodeFixProvider+MyCodeAction", "Replace Default Literal" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedLocalFunction.CSharpRemoveUnusedLocalFunctionCodeFixProvider+MyCodeAction", "Remove Unused Local Function" },
            { "Microsoft.CodeAnalysis.CSharp.MakeRefStruct.MakeRefStructCodeFixProvider+MyCodeAction", "Make Ref Struct" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.MakeLocalFunctionStaticCodeFixProvider+MyCodeAction", "Make Local Function Static (CodeFix)" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.MakeLocalFunctionStaticCodeRefactoringProvider+MyCodeAction", "Make Local Function Static (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.PassInCapturedVariablesAsArgumentsCodeFixProvider+MyCodeAction", "Make Local Function Static Pass In Captured Variables As Arguments" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementInterface.AbstractChangeImplementionCodeRefactoringProvider+MyCodeAction", "Implement Interface" },
            { "Microsoft.CodeAnalysis.CSharp.DisambiguateSameVariable.CSharpDisambiguateSameVariableCodeFixProvider+MyCodeAction", "Disambiguate Same Variable" },
            { "Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces.CSharpAddBracesCodeFixProvider+MyCodeAction", "Add Braces" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString.AbstractConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider`1+MyCodeAction", "Convert Between Regular And Verbatim String" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseType.AbstractUseTypeCodeRefactoringProvider+MyCodeAction", "Use Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.LambdaSimplifier.LambdaSimplifierCodeRefactoringProvider+MyCodeAction", "Lambda Simplifier" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary.CSharpInlineTemporaryCodeRefactoringProvider+MyCodeAction", "Inline Temporary" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable.EnableNullableCodeRefactoringProvider+MyCodeAction", "Enable nullable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertLocalFunctionToMethod.CSharpConvertLocalFunctionToMethodCodeRefactoringProvider+MyCodeAction", "Convert Local Function To Method" },
            { "Microsoft.CodeAnalysis.CSharp.UseInterpolatedVerbatimString.CSharpUseInterpolatedVerbatimStringCodeFixProvider+MyCodeAction", "Use Interpolated Verbatim String" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveNewModifier.RemoveNewModifierCodeFixProvider+MyCodeAction", "Remove New Modifier" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveInKeyword.RemoveInKeywordCodeFixProvider+MyCodeAction", "Remove In Keyword" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable.CSharpDeclareAsNullableCodeFixProvider+MyCodeAction", "Declare As Nullable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeStatementAsynchronous.CSharpMakeStatementAsynchronousCodeFixProvider+MyCodeAction", "Make Statement Asynchronous" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator.CSharpAddYieldCodeFixProvider+MyCodeAction", "Add Yield" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator.CSharpChangeToIEnumerableCodeFixProvider+MyCodeAction", "Change To IEnumerable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase.HideBaseCodeFixProvider+AddNewKeywordAction", "Hide Basen" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.FixReturnType.CSharpFixReturnTypeCodeFixProvider+MyCodeAction", "Fix Return Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation.CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider+MyCodeAction", "Add Parentheses Around Conditional Expression In String Interpolation" },
            { "Microsoft.CodeAnalysis.CSharp.AssignOutParameters.AbstractAssignOutParametersCodeFixProvider+MyCodeAction", "Assign Out Parameters" },
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
            { "Microsoft.CodeAnalysis.UpdateLegacySuppressions.UpdateLegacySuppressionsCodeFixProvider+MyCodeAction", "Update Legacy Suppressions" },
            { "Microsoft.CodeAnalysis.UseThrowExpression.UseThrowExpressionCodeFixProvider+MyCodeAction", "Use Throw Expression" },
            { "Microsoft.CodeAnalysis.UseSystemHashCode.UseSystemHashCodeCodeFixProvider+MyCodeAction", "Use System.HashCode" },
            { "Microsoft.CodeAnalysis.UseObjectInitializer.AbstractUseObjectInitializerCodeFixProvider`7+MyCodeAction", "Use Object Initializer" },
            { "Microsoft.CodeAnalysis.UseNullPropagation.AbstractUseNullPropagationCodeFixProvider`10+MyCodeAction", "Use Null Propagation" },
            { "Microsoft.CodeAnalysis.UseExplicitTupleName.UseExplicitTupleNameCodeFixProvider+MyCodeAction", "Use Explicit Tuple Name" },
            { "Microsoft.CodeAnalysis.UseIsNullCheck.AbstractUseIsNullCheckForReferenceEqualsCodeFixProvider`1+MyCodeAction", "Use 'Is Null' Check" },
            { "Microsoft.CodeAnalysis.UseInferredMemberName.AbstractUseInferredMemberNameCodeFixProvider+MyCodeAction", "Use Inferred Member Name" },
            { "Microsoft.CodeAnalysis.UseConditionalExpression.AbstractUseConditionalExpressionForAssignmentCodeFixProvider`6+MyCodeAction", "Use Conditional Expression For Assignment" },
            { "Microsoft.CodeAnalysis.UseConditionalExpression.AbstractUseConditionalExpressionForReturnCodeFixProvider`4+MyCodeAction", "Use Conditional Expression For Return" },
            { "Microsoft.CodeAnalysis.UseCompoundAssignment.AbstractUseCompoundAssignmentCodeFixProvider`3+MyCodeAction", "Use Compound Assignment" },
            { "Microsoft.CodeAnalysis.UseCollectionInitializer.AbstractUseCollectionInitializerCodeFixProvider`8+MyCodeAction", "Use Collection Initializer" },
            { "Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionCodeFixProvider+MyCodeAction", "Use Coalesce Expression" },
            { "Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionForNullableCodeFixProvider+MyCodeAction", "Use Coalesce Expression For Nullable" },
            { "Microsoft.CodeAnalysis.SimplifyLinqExpression.AbstractSimplifyLinqExpressionCodeFixProvider`3+MyCodeAction", "Simplify Linq Expression" },
            { "Microsoft.CodeAnalysis.SimplifyInterpolation.AbstractSimplifyInterpolationCodeFixProvider`7+MyCodeAction", "Simplify Interpolation" },
            { "Microsoft.CodeAnalysis.SimplifyBooleanExpression.SimplifyConditionalCodeFixProvider+MyCodeAction", "Simplify Boolean Expression" },
            { "Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues.AbstractRemoveUnusedValuesCodeFixProvider`11+MyCodeAction", "Remove Unused Parameters And Values" },
            { "Microsoft.CodeAnalysis.RemoveUnusedMembers.AbstractRemoveUnusedMembersCodeFixProvider`1+MyCodeAction", "Remove Unused Members" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessaryImports.AbstractRemoveUnnecessaryImportsCodeFixProvider+MyCodeAction", "Remove Unnecessary Imports" },
            { "Microsoft.CodeAnalysis.QualifyMemberAccess.AbstractQualifyMemberAccessCodeFixprovider`2+MyCodeAction", "Qualify Member Access" },
            { "Microsoft.CodeAnalysis.PopulateSwitch.AbstractPopulateSwitchCodeFixProvider`4+MyCodeAction", "Populate Switch" },
            { "Microsoft.CodeAnalysis.OrderModifiers.AbstractOrderModifiersCodeFixProvider+MyCodeAction", "Order Modifiers" },
            { "Microsoft.CodeAnalysis.MakeFieldReadonly.AbstractMakeFieldReadonlyCodeFixProvider`2+MyCodeAction", "Make Field Readonly" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryInlineSuppressionsCodeFixProvider+MyCodeAction", "Remove Unnecessary Inline Suppressions" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryAttributeSuppressionsCodeFixProvider+MyCodeAction", "Remove Unnecessary Attribute Suppressions" },
            { "Microsoft.CodeAnalysis.RemoveRedundantEquality.RemoveRedundantEqualityCodeFixProvider+MyCodeAction", "Remove Redundant Equality" },
            { "Microsoft.CodeAnalysis.NewLines.MultipleBlankLines.MultipleBlankLinesCodeFixProvider+MyCodeAction", "New Lines: Multiple Blank Lines" },
            { "Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement.ConsecutiveStatementPlacementCodeFixProvider+MyCodeAction", "New Lines: Consecutive Statement Placement" },
            { "Microsoft.CodeAnalysis.FileHeaders.AbstractFileHeaderCodeFixProvider+MyCodeAction", "File Headers" },
            { "Microsoft.CodeAnalysis.ConvertTypeOfToNameOf.AbstractConvertTypeOfToNameOfCodeFixProvider+MyCodeAction", "Convert TypeOf To NameOf" },
            { "Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple.AbstractConvertAnonymousTypeToTupleCodeFixProvider`3+MyCodeAction", "Convert Anonymous Type To Tuple" },
            { "Microsoft.CodeAnalysis.AddRequiredParentheses.AddRequiredParenthesesCodeFixProvider+MyCodeAction", "Add Required Parentheses" },
            { "Microsoft.CodeAnalysis.AddAccessibilityModifiers.AbstractAddAccessibilityModifiersCodeFixProvider+MyCodeAction", "Add Accessibility Modifiers" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses.AbstractRemoveUnnecessaryParenthesesCodeFixProvider`1+MyCodeAction", "Remove Unnecessary Parentheses" },
            { "Microsoft.CodeAnalysis.UseNamedArguments.AbstractUseNamedArgumentsCodeRefactoringProvider+MyCodeAction", "Use Named Arguments" },
            { "Microsoft.CodeAnalysis.UseAutoProperty.AbstractUseAutoPropertyCodeFixProvider`5+UseAutoPropertyCodeAction", "Use Auto Property" },
            { "Microsoft.CodeAnalysis.UnsealClass.AbstractUnsealClassCodeFixProvider+MyCodeAction", "Unseal Class" },
            { "Microsoft.CodeAnalysis.SplitOrMergeIfStatements.AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider+MyCodeAction", "Merge Consecutive If Statements" },
            { "Microsoft.CodeAnalysis.SplitOrMergeIfStatements.AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider+MyCodeAction", "Split Into Consecutive If Statements" },
            { "Microsoft.CodeAnalysis.SplitOrMergeIfStatements.AbstractMergeNestedIfStatementsCodeRefactoringProvider+MyCodeAction", "Merge Nested If Statements" },
            { "Microsoft.CodeAnalysis.SplitOrMergeIfStatements.AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider+MyCodeAction", "Split Into Nested If Statements" },
            { "Microsoft.CodeAnalysis.SpellCheck.AbstractSpellCheckCodeFixProvider`1+SpellCheckCodeAction", "Spell Check" },
            { "Microsoft.CodeAnalysis.SpellCheck.AbstractSpellCheckCodeFixProvider`1+MyCodeAction", "Spell Check" },
            { "Microsoft.CodeAnalysis.SimplifyTypeNames.AbstractSimplifyTypeNamesCodeFixProvider`1+MyCodeAction", "Simplify Type Names" },
            { "Microsoft.CodeAnalysis.SimplifyThisOrMe.AbstractSimplifyThisOrMeCodeFixProvider`1+MyCodeAction", "Simplify This Or Me" },
            { "Microsoft.CodeAnalysis.ReplacePropertyWithMethods.ReplacePropertyWithMethodsCodeRefactoringProvider+ReplacePropertyWithMethodsCodeAction", "Replace Property With Methods" },
            { "Microsoft.CodeAnalysis.ReplaceMethodWithProperty.ReplaceMethodWithPropertyCodeRefactoringProvider+ReplaceMethodWithPropertyCodeAction", "Replace Method With Property" },
            { "Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag.AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider+MyCodeAction", "Replace Doc Comment Text With Tag" },
            { "Microsoft.CodeAnalysis.RemoveUnusedVariable.AbstractRemoveUnusedVariableCodeFixProvider`3+MyCodeAction", "Remove Unused Variable" },
            { "Microsoft.CodeAnalysis.RemoveAsyncModifier.AbstractRemoveAsyncModifierCodeFixProvider`2+MyCodeAction", "Remove Async Modifier" },
            { "Microsoft.CodeAnalysis.PreferFrameworkType.PreferFrameworkTypeCodeFixProvider+PreferFrameworkTypeCodeAction", "Prefer Framework Type" },
            { "Microsoft.CodeAnalysis.NameTupleElement.AbstractNameTupleElementCodeRefactoringProvider`2+MyCodeAction", "Name Tuple Element" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.AbstractMoveToNamespaceCodeAction+MoveItemsToNamespaceCodeAction", "Move Items To Namespace" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.AbstractMoveToNamespaceCodeAction+MoveTypeToNamespaceCodeAction", "Move Type To Namespace" },
            { "Microsoft.CodeAnalysis.MoveDeclarationNearReference.AbstractMoveDeclarationNearReferenceCodeRefactoringProvider`1+MyCodeAction", "Move Declaration Near Reference" },
            { "Microsoft.CodeAnalysis.MakeMethodSynchronous.AbstractMakeMethodSynchronousCodeFixProvider+MyCodeAction", "Make Method Synchronous" },
            { "Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider+MyCodeAction", "Make Method Asynchronous" },
            { "Microsoft.CodeAnalysis.MakeMemberStatic.AbstractMakeMemberStaticCodeFixProvider+MyCodeAction", "Make Member Static" },
            { "Microsoft.CodeAnalysis.MakeTypeAbstract.AbstractMakeTypeAbstractCodeFixProvider`1+MyCodeAction", "Make Type Abstract" },
            { "Microsoft.CodeAnalysis.InvertLogical.AbstractInvertLogicalCodeRefactoringProvider`3+MyCodeAction", "Invert Logical" },
            { "Microsoft.CodeAnalysis.InvertIf.AbstractInvertIfCodeRefactoringProvider`3+MyCodeAction", "Invert If" },
            { "Microsoft.CodeAnalysis.InvertConditional.AbstractInvertConditionalCodeRefactoringProvider`1+MyCodeAction", "Invert Conditional (Refactoring)" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceVariableService`6+IntroduceVariableCodeAction", "Introduce Variable" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceVariableService`6+IntroduceVariableAllOccurrenceCodeAction", "Introduce Variable All Occurrence" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceLocalForExpressionCodeRefactoringProvider`4+MyCodeAction", "Introduce Variable For Expression" },
            { "Microsoft.CodeAnalysis.IntroduceUsingStatement.AbstractIntroduceUsingStatementCodeRefactoringProvider`2+MyCodeAction", "Introduce Using Statement" },
            { "Microsoft.CodeAnalysis.InlineMethod.AbstractInlineMethodRefactoringProvider`4+MySolutionChangeAction", "Inline Method (Refactoring)" },
            { "Microsoft.CodeAnalysis.InitializeParameter.AbstractInitializeParameterCodeRefactoringProvider`4+MyCodeAction", "Initialize Parameter" },
            { "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction", "Implement Interface" },
            { "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction", "Implement Interface With Dispose Pattern" },
            { "Microsoft.CodeAnalysis.ImplementAbstractClass.AbstractImplementAbstractClassCodeFixProvider`1+MyCodeAction", "Implement Abstract Class" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+GenerateTypeCodeAction", "Generate Type" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+GenerateTypeCodeActionWithOption", "Generate Type With Option" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+MyCodeAction", "Generate Type" },
            { "Microsoft.CodeAnalysis.GenerateOverrides.GenerateOverridesCodeRefactoringProvider+GenerateOverridesWithDialogCodeAction", "Generate Overrides With Dialog" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateVariableCodeAction", "Generate Variable" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+MyCodeAction", "Generate Variable" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateLocalCodeAction", "Generate Local" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateParameterCodeAction", "Generate Parameter" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember.AbstractGenerateParameterizedMemberService`4+GenerateParameterizedMemberCodeAction", "Generate Parameterized Member" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember.AbstractGenerateEnumMemberService`3+GenerateEnumMemberCodeAction", "Generate Enum Member" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+GenerateDefaultConstructorCodeAction", "Generate Default Constructors" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+CodeActionAll", "Generate Default Constructors All" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor.AbstractGenerateConstructorService`2+MyCodeAction", "Generate Constructor" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider+GenerateEqualsAndGetHashCodeAction", "Generate Equals And Get Hash Code From Members" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider+GenerateEqualsAndGetHashCodeWithDialogCodeAction", "Generate Equals And Get Hash Code From Members With Dialog" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+ConstructorDelegatingCodeAction", "Generate Constructor From Members (Constructor Delegating)" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+FieldDelegatingCodeAction", "Generate Constructor From Members (Field Delegating)" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+GenerateConstructorWithDialogCodeAction", "Generate Constructor From Members With Dialog" },
            { "Microsoft.CodeAnalysis.GenerateComparisonOperators.GenerateComparisonOperatorsCodeRefactoringProvider+MyCodeAction", "Generate Comparison Operators" },
            { "Microsoft.CodeAnalysis.Formatting.FormattingCodeFixProvider+MyCodeAction", "Fix Formatting" },
            { "Microsoft.CodeAnalysis.EncapsulateField.AbstractEncapsulateFieldService+MyCodeAction", "Encapsulate Field" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.AbstractAddDocCommentNodesCodeFixProvider`4+MyCodeAction", "Diagnostic Comments: Add DocComment Nodes" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.AbstractRemoveDocCommentNodeCodeFixProvider`2+MyCodeAction", "Diagnostic Comments: Remove DocComment Node" },
            { "Microsoft.CodeAnalysis.ConvertTupleToStruct.AbstractConvertTupleToStructCodeRefactoringProvider`10+MyCodeAction", "Convert Tuple To Struct" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.AbstractConvertConcatenationToInterpolatedStringRefactoringProvider`1+MyCodeAction", "Convert Concatenation To Interpolated String" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider`5+ConvertToInterpolatedStringCodeAction", "Convert Placeholder To Interpolated String" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.ConvertRegularStringToInterpolatedStringRefactoringProvider+MyCodeAction", "Convert Regular String To Interpolated String" },
            { "Microsoft.CodeAnalysis.ConvertNumericLiteral.AbstractConvertNumericLiteralCodeRefactoringProvider`1+MyCodeAction", "Convert Numeric Literal" },
            { "Microsoft.CodeAnalysis.ConvertLinq.AbstractConvertLinqQueryToForEachProvider`2+MyCodeAction", "Convert Linq Query To ForEach" },
            { "Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery.AbstractConvertForEachToLinqQueryProvider`2+ForEachToLinqQueryCodeAction", "Convert ForEach To Linq Query" },
            { "Microsoft.CodeAnalysis.ConvertIfToSwitch.AbstractConvertIfToSwitchCodeRefactoringProvider`4+MyCodeAction", "Convert If To Switch" },
            { "Microsoft.CodeAnalysis.ConvertForToForEach.AbstractConvertForToForEachCodeRefactoringProvider`6+MyCodeAction", "Convert For To ForEach" },
            { "Microsoft.CodeAnalysis.ConvertForEachToFor.AbstractConvertForEachToForCodeRefactoringProvider`2+ForEachToForCodeAction", "Convert ForEach To For" },
            { "Microsoft.CodeAnalysis.ConvertCast.AbstractConvertCastCodeRefactoringProvider`3+MyCodeAction", "Convert Cast" },
            { "Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty.AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider`2+ConvertAutoPropertyToFullPropertyCodeAction", "Convert AutoProperty To Full Property" },
            { "Microsoft.CodeAnalysis.ConflictMarkerResolution.AbstractResolveConflictMarkerCodeFixProvider+MyCodeAction", "Resolve Conflict Marker" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToClass.AbstractConvertAnonymousTypeToClassCodeRefactoringProvider`6+MyCodeAction", "Convert Anonymous Type To Class" },
            { "Microsoft.CodeAnalysis.AddMissingImports.AbstractAddMissingImportsRefactoringProvider+AddMissingImportsCodeAction", "Add Missing Imports (Paste)" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.AbstractPullMemberUpRefactoringProvider+PullMemberUpWithDialogCodeAction", "Pull Member Up" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace.AbstractSyncNamespaceCodeRefactoringProvider`3+ChangeNamespaceCodeAction", "Sync Namespace: Change Namespace" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace.AbstractSyncNamespaceCodeRefactoringProvider`3+MoveFileCodeAction", "Sync Namespace: Move File" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.MoveType.AbstractMoveTypeService`5+MoveTypeCodeAction", "Move Type" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod.ExtractMethodCodeRefactoringProvider+MyCodeAction", "Extract Method" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.AddAwait.AbstractAddAwaitCodeRefactoringProvider`1+MyCodeAction", "Add Await" },
            { "Microsoft.CodeAnalysis.CodeFixes.NamingStyles.NamingStyleCodeFixProvider+FixNameCodeAction", "Fix Naming Style" },
            { "Microsoft.CodeAnalysis.CodeFixes.MatchFolderAndNamespace.AbstractChangeNamespaceToMatchFolderCodeFixProvider+MyCodeAction", "Change Namespace To Match Folder" },
            { "Microsoft.CodeAnalysis.CodeFixes.FullyQualify.AbstractFullyQualifyCodeFixProvider+MyCodeAction", "Fully Qualify" },
            { "Microsoft.CodeAnalysis.CodeFixes.FullyQualify.AbstractFullyQualifyCodeFixProvider+GroupingCodeAction", "Fully Qualify (Grouping)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageCodeAction", "Suppression.: Global Suppress Message" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageFixAllCodeAction", "Suppression: Global Suppress Message (FixAll)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+LocalSuppressMessageCodeAction", "Suppression: Local Suppress Message" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+PragmaWarningCodeAction", "Suppression: Pragma Warning" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity.ConfigureSeverityLevelCodeFixProvider+TopLevelBulkConfigureSeverityCodeAction", "Configure Severity: TopLevel Bulk Configure Severity" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity.ConfigureSeverityLevelCodeFixProvider+TopLevelConfigureSeverityCodeAction", "Configure Severity: TopLevel Configure Severity" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle.ConfigureCodeStyleOptionCodeFixProvider+TopLevelConfigureCodeStyleOptionCodeAction", "Configure CodeStyle Option: TopLevel Configure CodeStyle Option" },
            { "Microsoft.CodeAnalysis.CodeFixes.Async.AbstractConvertToAsyncCodeFixProvider+MyCodeAction", "Convert To Async" },
            { "Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast.AbstractAddExplicitCastCodeFixProvider`1+MyCodeAction", "Add Explicit Cast" },
            { "Microsoft.CodeAnalysis.AliasAmbiguousType.AbstractAliasAmbiguousTypeCodeFixProvider+MyCodeAction", "Alias Ambiguous Type" },
            { "Microsoft.CodeAnalysis.AddParameter.AbstractAddParameterCodeFixProvider`6+MyCodeAction", "Add Parameter" },
            { "Microsoft.CodeAnalysis.AddObsoleteAttribute.AbstractAddObsoleteAttributeCodeFixProvider+MyCodeAction", "Add Obsolete Attribute" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+AssemblyReferenceCodeAction", "AddImport (Assembly Reference)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+InstallPackageAndAddImportCodeAction", "AddImport (Install Package And Add Import)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+InstallWithPackageManagerCodeAction", "AddImport (Install With PackageManager)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+MetadataSymbolReferenceCodeAction", "AddImport (Metadata Symbol Reference)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+ParentInstallPackageCodeAction", "Add Import (Install Nuget Package)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+ProjectSymbolReferenceCodeAction", "Add Import (Project Symbol Reference)" },
            { "Microsoft.CodeAnalysis.AddFileBanner.AbstractAddFileBannerCodeRefactoringProvider+MyCodeAction", "Add File Banner" },
            { "Microsoft.CodeAnalysis.AddDebuggerDisplay.AbstractAddDebuggerDisplayCodeRefactoringProvider`2+MyCodeAction", "Add Debugger Display" },
            { "Microsoft.CodeAnalysis.AddConstructorParametersFromMembers.AddConstructorParametersFromMembersCodeRefactoringProvider+AddConstructorParametersCodeAction", "Add Constructor Parameters From Members" },
            { "Microsoft.CodeAnalysis.AddAnonymousTypeMemberName.AbstractAddAnonymousTypeMemberNameCodeFixProvider`3+MyCodeAction", "Add Anonymous Type Member Name" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageFixAllCodeAction+GlobalSuppressionSolutionChangeAction", "Suppression: Global Suppress Message (FixAll)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+RemoveSuppressionCodeAction+AttributeRemoveAction", "Suppression: Remove Suppression (Attribute)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+RemoveSuppressionCodeAction+PragmaRemoveAction", "Suppression: Remove Suppression (Pragma)" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeActions.RemoveStatementCodeAction", "Remove Statement" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable.CorrectNextControlVariableCodeFixProvider+CorrectNextControlVariableCodeAction", "Correct Next Control Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEndConstruct.GenerateEndConstructCodeFixProvider+MyCodeAction", "Generate End Construct" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent.GenerateEventCodeFixProvider+GenerateEventCodeAction", "Generate Event" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+AddKeywordCodeAction", "Incorrect Exit Continue: Add Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+ReplaceKeywordCodeAction", "Incorrect Exit Continue: Replace Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+ReplaceTokenKeywordCodeAction", "Incorrect Exit Continue: Replace Token Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectFunctionReturnType.IncorrectFunctionReturnTypeCodeFixProvider+MyCodeAction", "Incorrect Function Return Type" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicChangeToYieldCodeFixProvider+MyCodeAction", "Change To Yield" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicConvertToIteratorCodeFixProvider+MyCodeAction", "Convert To Iterator" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile.MoveToTopOfFileCodeFixProvider+MoveToLineCodeAction", "Move To Top Of File" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase.OverloadBaseCodeFixProvider+AddKeywordAction", "Overload Base: Add Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary.VisualBasicInlineTemporaryCodeRefactoringProvider+MyCodeAction", "Inline Temporary" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveSharedFromModuleMembers.VisualBasicRemoveSharedFromModuleMembersCodeFixProvider+MyCodeAction", "Remove Shared From Module Members" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal.VisualBasicRemoveUnnecessaryByValCodeFixProvider+MyCodeAction", "Remove Unnecessary ByVal" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCast.VisualBasicRemoveUnnecessaryCastCodeFixProvider+MyCodeAction", "Remove Unnecessary Cast" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression.VisualBasicUseIsNotExpressionCodeFixProvider+MyCodeAction", "Use IsNot Expression" },
            { "Microsoft.CodeAnalysis.CSharp.UseTupleSwap.CSharpUseTupleSwapCodeFixProvider+MyCodeAction", "Use Tuple Swap" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseNullCheckOverTypeCheckCodeFixProvider+MyCodeAction", "Use Null Check Over Type Check" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyPropertyPattern.CSharpSimplifyPropertyPatternCodeFixProvider+MyCodeAction", "Simplify Property Pattern" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNamespace.ConvertNamespaceCodeRefactoringProvider+MyCodeAction", "Convert Namespace Refactoring (FileScope/BlockScope)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNamespace.ConvertNamespaceCodeFixProvider+MyCodeAction", "Convert Namespace CodeFix (FileScope/BlockScope)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns.UseRecursivePatternsCodeRefactoringProvider+MyCodeAction", "Use Recursive Patterns" },
            { "Microsoft.CodeAnalysis.MoveStaticMembers.MoveStaticMembersWithDialogCodeAction", "Move Static Members" },
            { "Microsoft.CodeAnalysis.SimplifyInterpolation.AbstractSimplifyInterpolationCodeFixProvider`5+MyCodeAction", "Simplify Interpolation" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceParameterService`4+MyCodeAction", "Introduce Parameter" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+GenerateDefaultConstructorCodeAction", "Generate Default Constructor" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+CodeActionAll", "Generate Default Constructur (All)" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider`6+ConvertToInterpolatedStringCodeAction", "Convert Placeholder To Interpolated String" },
            { "Microsoft.CodeAnalysis.ConvertAnonymousType.AbstractConvertAnonymousTypeToClassCodeRefactoringProvider`6+MyCodeAction", "Convert Anonymous Type To Class" },
            { "Microsoft.CodeAnalysis.ConvertAnonymousType.AbstractConvertAnonymousTypeToTupleCodeRefactoringProvider`3+MyCodeAction", "Convert Anonymous Type To Tuple" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyObjectCreation.VisualBasicSimplifyObjectCreationCodeFixProvider+MyCodeAction", "Simplify Object Creation" },
            { "Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking.RenameTrackingTaggerProvider+RenameTrackingCodeAction", "Rename Tracking" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc.AddInheritdocCodeFixProvider+MyCodeAction", "Add Inheritdoc" },
        }.ToImmutableDictionary();

        public static void Main(string[] args)
        {
            Console.WriteLine("Loading assemblies and finding CodeActions ...");

            var assemblies = GetAssemblies(args);
            var codeActionTypes = GetCodeActionTypes(assemblies);

            Console.WriteLine($"Generating Kusto datatable of {codeActionTypes.Length} CodeAction hashes ...");

            var telemetryInfos = GetTelemetryInfos(codeActionTypes);
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

        internal static ImmutableArray<Type> GetCodeActionTypes(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(
                assembly => assembly.GetTypes().Where(
                    type => !type.GetTypeInfo().IsInterface && !type.GetTypeInfo().IsAbstract));

            return types
                .Where(t => typeof(CodeAction).IsAssignableFrom(t))
                .ToImmutableArray();
        }

        internal static ImmutableArray<TelemetryInfo> GetTelemetryInfos(ImmutableArray<Type> codeActionTypes)
        {
            return codeActionTypes
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
