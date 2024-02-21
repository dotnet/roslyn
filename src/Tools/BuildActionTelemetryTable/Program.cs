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

namespace BuildActionTelemetryTable
{
    public class Program
    {
        private static readonly string s_executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        private static ImmutableHashSet<string> IgnoredCodeActions { get; } = new HashSet<string>()
        {
            "Microsoft.CodeAnalysis.CodeActions.CodeAction+CodeActionWithNestedActions",
            "Microsoft.CodeAnalysis.CodeActions.CodeAction+DocumentChangeAction",
            "Microsoft.CodeAnalysis.CodeActions.CodeAction+NoChangeAction",
            "Microsoft.CodeAnalysis.CodeActions.CodeAction+SolutionChangeAction",
            "Microsoft.CodeAnalysis.CodeActions.CustomCodeActions+DocumentChangeAction",
            "Microsoft.CodeAnalysis.CodeActions.CustomCodeActions+SolutionChangeAction",
            "Microsoft.CodeAnalysis.CodeFixes.DocumentBasedFixAllProvider+PostProcessCodeAction",
        }.ToImmutableHashSet();

        private static ImmutableDictionary<string, string> CodeActionDescriptionMap { get; } = new Dictionary<string, string>()
        {
            { "Microsoft.CodeAnalysis.AddConstructorParametersFromMembers.AddConstructorParametersFromMembersCodeRefactoringProvider", "Add Constructor Parameters From Members (Refactoring)" },
            { "Microsoft.CodeAnalysis.AddConstructorParametersFromMembers.AddConstructorParametersFromMembersCodeRefactoringProvider+AddConstructorParametersCodeAction", "Add Constructor Parameters From Members: Add Constructor Parameters (Refactoring)" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+AssemblyReferenceCodeAction", "Add Import: Assembly Reference" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+InstallPackageAndAddImportCodeAction", "Add Import: Install Package And Add Import" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+InstallWithPackageManagerCodeAction", "Add Import: Install With Package Manager" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+MetadataSymbolReferenceCodeAction", "Add Import: Metadata Symbol Reference" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+ParentInstallPackageCodeAction", "Add Import: Parent Install Package" },
            { "Microsoft.CodeAnalysis.AddImport.AbstractAddImportFeatureService`1+ProjectSymbolReferenceCodeAction", "Add Import: Project Symbol Reference" },
            { "Microsoft.CodeAnalysis.AddMissingReference.AddMissingReferenceCodeAction", "Add Missing Reference" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallPackageDirectlyCodeAction", "Add Package: Install Package Directly" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallPackageParentCodeAction", "Add Package: Install Package Parent" },
            { "Microsoft.CodeAnalysis.AddPackage.InstallWithPackageManagerCodeAction", "Add Package: Install With Package Manager" },
            { "Microsoft.CodeAnalysis.AddRequiredParentheses.AddRequiredParenthesesCodeFixProvider", "Add Required Parentheses" },
            { "Microsoft.CodeAnalysis.ChangeSignature.ChangeSignatureCodeAction", "Change Signature" },
            { "Microsoft.CodeAnalysis.ChangeSignature.ChangeSignatureCodeRefactoringProvider", "Change Signature (Refactoring)" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle.ConfigureCodeStyleOptionCodeFixProvider+TopLevelConfigureCodeStyleOptionCodeAction", "Configure Code Style Option: Top Level Configure Code Style Option" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity.ConfigureSeverityLevelCodeFixProvider+TopLevelBulkConfigureSeverityCodeAction", "Configure Severity Level: Top Level Bulk Configure Severity" },
            { "Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity.ConfigureSeverityLevelCodeFixProvider+TopLevelConfigureSeverityCodeAction", "Configure Severity Level: Top Level Configure Severity" },
            { "Microsoft.CodeAnalysis.CodeFixes.FixMultipleCodeAction", "Fix Multiple" },
            { "Microsoft.CodeAnalysis.CodeFixes.FullyQualify.AbstractFullyQualifyCodeFixProvider+GroupingCodeAction", "Fully Qualify: Grouping" },
            { "Microsoft.CodeAnalysis.CodeFixes.NamingStyles.NamingStyleCodeFixProvider", "Naming Styles: Naming Style" },
            { "Microsoft.CodeAnalysis.CodeFixes.NamingStyles.NamingStyleCodeFixProvider+FixNameCodeAction", "Naming Style: Fix Name" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageCodeAction", "Suppression: Global Suppress Message" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageFixAllCodeAction", "Suppression: Global Suppress Message Fix All" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+GlobalSuppressMessageFixAllCodeAction+GlobalSuppressionSolutionChangeAction", "Global Suppress Message Fix All: Global Suppression Solution Change" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+LocalSuppressMessageCodeAction", "Suppression: Local Suppress Message" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+PragmaWarningCodeAction", "Suppression: Pragma Warning" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+RemoveSuppressionCodeAction+AttributeRemoveAction", "Remove Suppression: Attribute Remove" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.AbstractSuppressionCodeFixProvider+RemoveSuppressionCodeAction+PragmaRemoveAction", "Remove Suppression: Pragma Remove" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.TopLevelSuppressionCodeAction", "Suppression: Top Level Suppression" },
            { "Microsoft.CodeAnalysis.CodeFixes.Suppression.WrapperCodeFixProvider", "Suppression: Wrapper" },
            { "Microsoft.CodeAnalysis.CodeFixesAndRefactorings.DocumentBasedFixAllProviderHelpers+PostProcessCodeAction", "Document Based Fix All: Post Process" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod.ExtractMethodCodeRefactoringProvider", "Extract Method (Refactoring)" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.FixAllCodeRefactoringCodeAction", "Code Refactorings: Fix All Code Refactoring" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.MoveType.AbstractMoveTypeService`5+MoveTypeCodeAction", "Move Type" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.MoveType.MoveTypeCodeRefactoringProvider", "Move Type (Refactoring)" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.AbstractPullMemberUpRefactoringProvider+PullMemberUpWithDialogCodeAction", "Pull Member Up: With Dialog (Refactoring)" },
            { "Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace.AbstractSyncNamespaceCodeRefactoringProvider`3+MoveFileCodeAction", "Sync Namespace: Move File (Refactoring)" },
            { "Microsoft.CodeAnalysis.CodeStyle.CSharpFormattingCodeFixProvider", "Formatting" },
            { "Microsoft.CodeAnalysis.CodeStyle.VisualBasicFormattingCodeFixProvider", "Formatting" },
            { "Microsoft.CodeAnalysis.ConvertToInterpolatedString.ConvertRegularStringToInterpolatedStringRefactoringProvider", "Convert To Interpolated String: Convert Regular String To Interpolated String (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers.CSharpAddAccessibilityModifiersCodeFixProvider", "Add Accessibility Modifiers" },
            { "Microsoft.CodeAnalysis.CSharp.AddAnonymousTypeMemberName.CSharpAddAnonymousTypeMemberNameCodeFixProvider", "Add Anonymous Type Member Name" },
            { "Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay.CSharpAddDebuggerDisplayCodeRefactoringProvider", "Add Debugger Display (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.AddFileBanner.CSharpAddFileBannerCodeRefactoringProvider", "Add File Banner (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.AddImport.CSharpAddImportCodeFixProvider", "Add Import" },
            { "Microsoft.CodeAnalysis.CSharp.AddMissingReference.CSharpAddMissingReferenceCodeFixProvider", "Add Missing Reference" },
            { "Microsoft.CodeAnalysis.CSharp.AddObsoleteAttribute.CSharpAddObsoleteAttributeCodeFixProvider", "Add Obsolete Attribute" },
            { "Microsoft.CodeAnalysis.CSharp.AddPackage.CSharpAddSpecificPackageCodeFixProvider", "Add Package: Add Specific Package" },
            { "Microsoft.CodeAnalysis.CSharp.AddParameter.CSharpAddParameterCodeFixProvider", "Add Parameter" },
            { "Microsoft.CodeAnalysis.CSharp.AliasAmbiguousType.CSharpAliasAmbiguousTypeCodeFixProvider", "Alias Ambiguous Type" },
            { "Microsoft.CodeAnalysis.CSharp.AssignOutParameters.AssignOutParametersAboveReturnCodeFixProvider", "Assign Out Parameters: Above Return" },
            { "Microsoft.CodeAnalysis.CSharp.AssignOutParameters.AssignOutParametersAtStartCodeFixProvider", "Assign Out Parameters: At Start" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast.CSharpAddExplicitCastCodeFixProvider", "Add Explicit Cast" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc.AddInheritdocCodeFixProvider", "Add Inheritdoc" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation.CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider", "Conditional Expression In String Interpolation: Add Parentheses Around Conditional Expression In Interpolated String" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.ConvertToAsync.CSharpConvertToAsyncMethodCodeFixProvider", "Convert To Async: Method" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable.CSharpDeclareAsNullableCodeFixProvider", "Declare As Nullable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.FixIncorrectConstraint.CSharpFixIncorrectConstraintCodeFixProvider", "Fix Incorrect Constraint" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.FixReturnType.CSharpFixReturnTypeCodeFixProvider", "Fix Return Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.ForEachCast.CSharpForEachCastCodeFixProvider", "For Each Cast" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify.CSharpFullyQualifyCodeFixProvider", "Fully Qualify" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateDeconstructMethod.GenerateDeconstructMethodCodeFixProvider", "Generate Deconstruct Method" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateEnumMember.GenerateEnumMemberCodeFixProvider", "Generate Enum Member" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod.GenerateConversionCodeFixProvider", "Generate Method: Generate Conversion" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod.GenerateMethodCodeFixProvider", "Generate Method" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateType.GenerateTypeCodeFixProvider", "Generate Type" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase.HideBaseCodeFixProvider", "Hide Base" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase.HideBaseCodeFixProvider+AddNewKeywordAction", "Hide Base: Add New Keyword" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator.CSharpAddYieldCodeFixProvider", "Iterator: Add Yield" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator.CSharpChangeToIEnumerableCodeFixProvider", "Iterator: Change To IEnumerable" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeStatementAsynchronous.CSharpMakeStatementAsynchronousCodeFixProvider", "Make Statement Asynchronous" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.MatchFolderAndNamespace.CSharpChangeNamespaceToMatchFolderCodeFixProvider", "Match Folder And Namespace: Change Namespace To Match Folder" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveInKeyword.RemoveInKeywordCodeFixProvider", "Remove In Keyword" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveNewModifier.RemoveNewModifierCodeFixProvider", "Remove New Modifier" },
            { "Microsoft.CodeAnalysis.CSharp.CodeFixes.TransposeRecordKeyword.CSharpTransposeRecordKeywordCodeFixProvider", "Transpose Record Keyword" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddAwait.CSharpAddAwaitCodeRefactoringProvider", "Add Await (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddMissingImports.CSharpAddMissingImportsRefactoringProvider", "Add Missing Imports (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertLocalFunctionToMethod.CSharpConvertLocalFunctionToMethodCodeRefactoringProvider", "Convert Local Function To Method (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable.EnableNullableCodeRefactoringProvider", "Enable Nullable (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable.EnableNullableCodeRefactoringProvider+CustomCodeAction", "Enable Nullable (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ExtractClass.CSharpExtractClassCodeRefactoringProvider", "Extract Class (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod.CSharpInlineMethodRefactoringProvider", "Inline Method (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary.CSharpInlineTemporaryCodeRefactoringProvider", "Inline Temporary (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers.CSharpMoveStaticMembersRefactoringProvider", "Move Static Members (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.CSharpPullMemberUpCodeRefactoringProvider", "Pull Member Up (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace.CSharpSyncNamespaceCodeRefactoringProvider", "Sync Namespace (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseExplicitType.UseExplicitTypeCodeRefactoringProvider", "Use Explicit Type (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseImplicitType.UseImplicitTypeCodeRefactoringProvider", "Use Implicit Type (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns.UseRecursivePatternsCodeRefactoringProvider", "Use Recursive Patterns (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConflictMarkerResolution.CSharpResolveConflictMarkerCodeFixProvider", "Conflict Marker Resolution: Resolve Conflict Marker" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertAnonymousType.CSharpConvertAnonymousTypeToClassCodeRefactoringProvider", "Convert Anonymous Type: To Class (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertAnonymousType.CSharpConvertAnonymousTypeToTupleCodeRefactoringProvider", "Convert Anonymous Type: To Tuple (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty.CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider", "Convert Auto Property To Full Property (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString.ConvertBetweenRegularAndVerbatimInterpolatedStringCodeRefactoringProvider", "Convert Between Regular And Verbatim String: Convert Between Regular And Verbatim Interpolated String (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString.ConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider", "Convert Between Regular And Verbatim String (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertCast.CSharpConvertDirectCastToTryCastCodeRefactoringProvider", "Convert Cast: Convert Direct Cast To Try Cast (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertCast.CSharpConvertTryCastToDirectCastCodeRefactoringProvider", "Convert Cast: Convert Try Cast To Direct Cast (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertForEachToFor.CSharpConvertForEachToForCodeRefactoringProvider", "Convert For Each To For (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertForToForEach.CSharpConvertForToForEachCodeRefactoringProvider", "Convert For To For Each (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch.CSharpConvertIfToSwitchCodeRefactoringProvider", "Convert If To Switch (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery.CSharpConvertForEachToLinqQueryProvider", "Convert For Each To Linq Query" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqQueryToForEachProvider", "Convert Linq: Query To For Each" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNamespace.ConvertNamespaceCodeFixProvider", "Convert Namespace" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNamespace.ConvertNamespaceCodeRefactoringProvider", "Convert Namespace (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertNumericLiteral.CSharpConvertNumericLiteralCodeRefactoringProvider", "Convert Numeric Literal (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertProgram.ConvertToProgramMainCodeFixProvider", "Convert Program: Convert To Program Main" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertProgram.ConvertToProgramMainCodeRefactoringProvider", "Convert Program: Convert To Program Main (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertProgram.ConvertToTopLevelStatementsCodeFixProvider", "Convert Program: Convert To Top Level Statements" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertProgram.ConvertToTopLevelStatementsCodeRefactoringProvider", "Convert Program: Convert To Top Level Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression.ConvertSwitchStatementToExpressionCodeFixProvider", "Convert Switch Statement To Expression" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString.CSharpConvertConcatenationToInterpolatedStringRefactoringProvider", "Convert To Interpolated String: Convert Concatenation To Interpolated String (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString.CSharpConvertPlaceholderToInterpolatedStringRefactoringProvider", "Convert To Interpolated String: Convert Placeholder To Interpolated String (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertToRawString.ConvertRegularStringToRawStringCodeRefactoringProvider", "Convert To Raw String: Convert Regular String To Raw String (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertTupleToStruct.CSharpConvertTupleToStructCodeRefactoringProvider", "Convert Tuple To Struct (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf.CSharpConvertTypeOfToNameOfCodeFixProvider", "Convert Type Of To Name Of" },
            { "Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces.CSharpAddBracesCodeFixProvider", "Add Braces" },
            { "Microsoft.CodeAnalysis.CSharp.DisambiguateSameVariable.CSharpDisambiguateSameVariableCodeFixProvider", "Disambiguate Same Variable" },
            { "Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.CSharpJsonDetectionCodeFixProvider", "Embedded Languages: Json Detection" },
            { "Microsoft.CodeAnalysis.CSharp.FileHeaders.CSharpFileHeaderCodeFixProvider", "File Headers: File Header" },
            { "Microsoft.CodeAnalysis.CSharp.GenerateConstructor.GenerateConstructorCodeFixProvider", "Generate Constructor" },
            { "Microsoft.CodeAnalysis.CSharp.GenerateConstructorFromMembers.CSharpGenerateConstructorFromMembersCodeRefactoringProvider", "Generate Constructor From Members (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.GenerateDefaultConstructors.CSharpGenerateDefaultConstructorsCodeFixProvider", "Generate Default Constructors" },
            { "Microsoft.CodeAnalysis.CSharp.GenerateVariable.CSharpGenerateVariableCodeFixProvider", "Generate Variable" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass.CSharpImplementAbstractClassCodeFixProvider", "Implement Abstract Class" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementInterface.CSharpImplementExplicitlyCodeRefactoringProvider", "Implement Interface: Implement Explicitly (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementInterface.CSharpImplementImplicitlyCodeRefactoringProvider", "Implement Interface: Implement Implicitly (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ImplementInterface.CSharpImplementInterfaceCodeFixProvider", "Implement Interface" },
            { "Microsoft.CodeAnalysis.CSharp.InitializeParameter.CSharpAddParameterCheckCodeRefactoringProvider", "Initialize Parameter: Add Parameter Check (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.InitializeParameter.CSharpInitializeMemberFromParameterCodeRefactoringProvider", "Initialize Parameter: Initialize Member From Parameter (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.InlineDeclaration.CSharpInlineDeclarationCodeFixProvider", "Inline Declaration" },
            { "Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement.CSharpIntroduceUsingStatementCodeRefactoringProvider", "Introduce Using Statement (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.IntroduceVariable.CSharpIntroduceLocalForExpressionCodeRefactoringProvider", "Introduce Variable: Introduce Local For Expression (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.IntroduceVariable.CSharpIntroduceParameterCodeRefactoringProvider", "Introduce Variable: Introduce Parameter (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.InvertConditional.CSharpInvertConditionalCodeRefactoringProvider", "Invert Conditional (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.InvertIf.CSharpInvertIfCodeRefactoringProvider", "Invert If (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.InvertLogical.CSharpInvertLogicalCodeRefactoringProvider", "Invert Logical (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess.InvokeDelegateWithConditionalAccessCodeFixProvider", "Invoke Delegate With Conditional Access" },
            { "Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly.CSharpMakeFieldReadonlyCodeFixProvider", "Make Field Readonly" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.MakeLocalFunctionStaticCodeFixProvider", "Make Local Function Static" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.MakeLocalFunctionStaticCodeRefactoringProvider", "Make Local Function Static (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic.PassInCapturedVariablesAsArgumentsCodeFixProvider", "Make Local Function Static: Pass In Captured Variables As Arguments" },
            { "Microsoft.CodeAnalysis.CSharp.MakeMemberStatic.CSharpMakeMemberStaticCodeFixProvider", "Make Member Static" },
            { "Microsoft.CodeAnalysis.CSharp.MakeMethodAsynchronous.CSharpMakeMethodAsynchronousCodeFixProvider", "Make Method Asynchronous" },
            { "Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous.CSharpMakeMethodSynchronousCodeFixProvider", "Make Method Synchronous" },
            { "Microsoft.CodeAnalysis.CSharp.MakeRefStruct.MakeRefStructCodeFixProvider", "Make Ref Struct" },
            { "Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable.CSharpMakeStructFieldsWritableCodeFixProvider", "Make Struct Fields Writable" },
            { "Microsoft.CodeAnalysis.CSharp.MakeTypeAbstract.CSharpMakeTypeAbstractCodeFixProvider", "Make Type Abstract" },
            { "Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives.MisplacedUsingDirectivesCodeFixProvider", "Misplaced Using Directives" },
            { "Microsoft.CodeAnalysis.CSharp.MoveDeclarationNearReference.CSharpMoveDeclarationNearReferenceCodeRefactoringProvider", "Move Declaration Near Reference (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.NameTupleElement.CSharpNameTupleElementCodeRefactoringProvider", "Name Tuple Element (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveBracePlacement.ConsecutiveBracePlacementCodeFixProvider", "Consecutive Brace Placement" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement.ConstructorInitializerPlacementCodeFixProvider", "Constructor Initializer Placement" },
            { "Microsoft.CodeAnalysis.CSharp.NewLines.EmbeddedStatementPlacement.EmbeddedStatementPlacementCodeFixProvider", "Embedded Statement Placement" },
            { "Microsoft.CodeAnalysis.CSharp.OrderModifiers.CSharpOrderModifiersCodeFixProvider", "Order Modifiers" },
            { "Microsoft.CodeAnalysis.CSharp.PopulateSwitch.CSharpPopulateSwitchExpressionCodeFixProvider", "Populate Switch: Expression" },
            { "Microsoft.CodeAnalysis.CSharp.PopulateSwitch.CSharpPopulateSwitchStatementCodeFixProvider", "Populate Switch: Statement" },
            { "Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess.CSharpQualifyMemberAccessCodeFixProvider", "Qualify Member Access" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier.CSharpRemoveAsyncModifierCodeFixProvider", "Remove Async Modifier" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveConfusingSuppression.CSharpRemoveConfusingSuppressionCodeFixProvider", "Remove Confusing Suppression" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast.CSharpRemoveUnnecessaryCastCodeFixProvider", "Remove Unnecessary Cast" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation.CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider", "Remove Unnecessary Discard Designation" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports.CSharpRemoveUnnecessaryImportsCodeFixProvider", "Remove unnecessary imports" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryLambdaExpression.CSharpRemoveUnnecessaryLambdaExpressionCodeFixProvider", "Remove Unnecessary Lambda Expression" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses.CSharpRemoveUnnecessaryParenthesesCodeFixProvider", "Remove Unnecessary Parentheses" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode.CSharpRemoveUnreachableCodeCodeFixProvider", "Remove Unreachable Code" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedLocalFunction.CSharpRemoveUnusedLocalFunctionCodeFixProvider", "Remove Unused Local Function" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers.CSharpRemoveUnusedMembersCodeFixProvider", "Remove Unused Members" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues.CSharpRemoveUnusedValuesCodeFixProvider", "Remove Unused Parameters And Values: Remove Unused Values" },
            { "Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable.CSharpRemoveUnusedVariableCodeFixProvider", "Remove Unused Variable" },
            { "Microsoft.CodeAnalysis.CSharp.ReplaceConditionalWithStatements.CSharpReplaceConditionalWithStatementsCodeRefactoringProvider", "Replace Conditional With Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral.CSharpReplaceDefaultLiteralCodeFixProvider", "Replace Default Literal" },
            { "Microsoft.CodeAnalysis.CSharp.ReplaceDocCommentTextWithTag.CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider", "Replace Doc Comment Text With Tag (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.ReverseForStatement.CSharpReverseForStatementCodeRefactoringProvider", "Reverse For Statement (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyInterpolation.CSharpSimplifyInterpolationCodeFixProvider", "Simplify Interpolation" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression.CSharpSimplifyLinqExpressionCodeFixProvider", "Simplify Linq Expression" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyPropertyPattern.CSharpSimplifyPropertyPatternCodeFixProvider", "Simplify Property Pattern" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyThisOrMe.CSharpSimplifyThisOrMeCodeFixProvider", "Simplify This Or Me" },
            { "Microsoft.CodeAnalysis.CSharp.SimplifyTypeNames.SimplifyTypeNamesCodeFixProvider", "Simplify Type Names" },
            { "Microsoft.CodeAnalysis.CSharp.SpellCheck.CSharpSpellCheckCodeFixProvider", "Spell Check" },
            { "Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements.CSharpMergeConsecutiveIfStatementsCodeRefactoringProvider", "Split Or Merge If Statements: Merge Consecutive If Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements.CSharpMergeNestedIfStatementsCodeRefactoringProvider", "Split Or Merge If Statements: Merge Nested If Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements.CSharpSplitIntoConsecutiveIfStatementsCodeRefactoringProvider", "Split Or Merge If Statements: Split Into Consecutive If Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements.CSharpSplitIntoNestedIfStatementsCodeRefactoringProvider", "Split Or Merge If Statements: Split Into Nested If Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.TypeStyle.UseExplicitTypeCodeFixProvider", "Use Explicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.TypeStyle.UseImplicitTypeCodeFixProvider", "Use Implicit Type" },
            { "Microsoft.CodeAnalysis.CSharp.UnsealClass.CSharpUnsealClassCodeFixProvider", "Unseal Class" },
            { "Microsoft.CodeAnalysis.CSharp.UpdateProjectToAllowUnsafe.CSharpUpdateProjectToAllowUnsafeCodeFixProvider", "Update Project To Allow Unsafe" },
            { "Microsoft.CodeAnalysis.CSharp.UpgradeProject.CSharpUpgradeProjectCodeFixProvider", "Upgrade Project" },
            { "Microsoft.CodeAnalysis.CSharp.UseAutoProperty.CSharpUseAutoPropertyCodeFixProvider", "Use Auto Property" },
            { "Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer.CSharpUseCollectionInitializerCodeFixProvider", "Use Collection Initializer" },
            { "Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment.CSharpUseCompoundAssignmentCodeFixProvider", "Use Compound Assignment" },
            { "Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment.CSharpUseCompoundCoalesceAssignmentCodeFixProvider", "Use Compound Assignment: Use Compound Coalesce Assignment" },
            { "Microsoft.CodeAnalysis.CSharp.UseConditionalExpression.CSharpUseConditionalExpressionForAssignmentCodeFixProvider", "Use Conditional Expression: For Assignment" },
            { "Microsoft.CodeAnalysis.CSharp.UseConditionalExpression.CSharpUseConditionalExpressionForReturnCodeFixProvider", "Use Conditional Expression: For Return" },
            { "Microsoft.CodeAnalysis.CSharp.UseDeconstruction.CSharpUseDeconstructionCodeFixProvider", "Use Deconstruction" },
            { "Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral.CSharpUseDefaultLiteralCodeFixProvider", "Use Default Literal" },
            { "Microsoft.CodeAnalysis.CSharp.UseExplicitTypeForConst.UseExplicitTypeForConstCodeFixProvider", "Use Explicit Type For Const" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBody.UseExpressionBodyCodeFixProvider", "Use Expression Body" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBody.UseExpressionBodyCodeRefactoringProvider", "Use Expression Body (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda.UseExpressionBodyForLambdaCodeFixProvider", "Use Expression Body For Lambda" },
            { "Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda.UseExpressionBodyForLambdaCodeRefactoringProvider", "Use Expression Body For Lambda (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation.CSharpUseImplicitObjectCreationCodeFixProvider", "Use Implicit Object Creation" },
            { "Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseIndexOperatorCodeFixProvider", "Use Index Or Range Operator: Use Index Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseRangeOperatorCodeFixProvider", "Use Index Or Range Operator: Use Range Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseInferredMemberName.CSharpUseInferredMemberNameCodeFixProvider", "Use Inferred Member Name" },
            { "Microsoft.CodeAnalysis.CSharp.UseInterpolatedVerbatimString.CSharpUseInterpolatedVerbatimStringCodeFixProvider", "Use Interpolated Verbatim String" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider", "Use Is Null Check: For Cast And Equality Operator" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider", "Use Is Null Check: For Reference Equals" },
            { "Microsoft.CodeAnalysis.CSharp.UseIsNullCheck.CSharpUseNullCheckOverTypeCheckCodeFixProvider", "Use Is Null Check: Use Null Check Over Type Check" },
            { "Microsoft.CodeAnalysis.CSharp.UseLocalFunction.CSharpUseLocalFunctionCodeFixProvider", "Use Local Function" },
            { "Microsoft.CodeAnalysis.CSharp.UseNamedArguments.CSharpUseNamedArgumentsCodeRefactoringProvider", "Use Named Arguments (Refactoring)" },
            { "Microsoft.CodeAnalysis.CSharp.UseNullPropagation.CSharpUseNullPropagationCodeFixProvider", "Use Null Propagation" },
            { "Microsoft.CodeAnalysis.CSharp.UseObjectInitializer.CSharpUseObjectInitializerCodeFixProvider", "Use Object Initializer" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternCombinators.CSharpUsePatternCombinatorsCodeFixProvider", "Use Pattern Combinators" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpAsAndNullCheckCodeFixProvider", "Use Pattern Matching: As And Null Check" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpIsAndCastCheckCodeFixProvider", "Use Pattern Matching: Is And Cast Check" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpIsAndCastCheckWithoutNameCodeFixProvider", "Use Pattern Matching: Is And Cast Check Without Name" },
            { "Microsoft.CodeAnalysis.CSharp.UsePatternMatching.CSharpUseNotPatternCodeFixProvider", "Use Pattern Matching: Use Not Pattern" },
            { "Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement.UseSimpleUsingStatementCodeFixProvider", "Use Simple Using Statement" },
            { "Microsoft.CodeAnalysis.CSharp.UseTupleSwap.CSharpUseTupleSwapCodeFixProvider", "Use Tuple Swap" },
            { "Microsoft.CodeAnalysis.CSharp.UseUtf8StringLiteral.UseUtf8StringLiteralCodeFixProvider", "Use UTF-8 String Literal" },
            { "Microsoft.CodeAnalysis.CSharp.Wrapping.CSharpWrappingCodeRefactoringProvider", "Wrapping (Refactoring)" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.CSharpAddDocCommentNodesCodeFixProvider", "Add Doc Comment Nodes" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.CSharpRemoveDocCommentNodeCodeFixProvider", "Remove Doc Comment Node" },
            { "Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes.VisualBasicRemoveDocCommentNodeCodeFixProvider", "Remove Doc Comment Node" },
            { "Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking.RenameTrackingCodeRefactoringProvider", "Rename Tracking (Refactoring)" },
            { "Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking.RenameTrackingTaggerProvider+RenameTrackingCodeAction", "Rename Tracking" },
            { "Microsoft.CodeAnalysis.EncapsulateField.EncapsulateFieldRefactoringProvider", "Encapsulate Field (Refactoring)" },
            { "Microsoft.CodeAnalysis.ExtractClass.ExtractClassWithDialogCodeAction", "Extract Class: With Dialog" },
            { "Microsoft.CodeAnalysis.ExtractInterface.ExtractInterfaceCodeAction", "Extract Interface" },
            { "Microsoft.CodeAnalysis.ExtractInterface.ExtractInterfaceCodeRefactoringProvider", "Extract Interface (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateComparisonOperators.GenerateComparisonOperatorsCodeRefactoringProvider", "Generate Comparison Operators (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+ConstructorDelegatingCodeAction", "Generate Constructor From Members: Constructor Delegating (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+FieldDelegatingCodeAction", "Generate Constructor From Members: Field Delegating (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateConstructorFromMembers.AbstractGenerateConstructorFromMembersCodeRefactoringProvider+GenerateConstructorWithDialogCodeAction", "Generate Constructor From Members: Generate Constructor With Dialog (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+CodeActionAll", "Generate Default Constructors: Code Action All" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.AbstractGenerateDefaultConstructorsService`1+GenerateDefaultConstructorCodeAction", "Generate Default Constructors: Generate Default Constructor" },
            { "Microsoft.CodeAnalysis.GenerateDefaultConstructors.GenerateDefaultConstructorsCodeRefactoringProvider", "Generate Default Constructors (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider", "Generate Equals And Get Hash Code From Members (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider+GenerateEqualsAndGetHashCodeAction", "Generate Equals And Get Hash Code From Members: Generate Equals And Get Hash (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider+GenerateEqualsAndGetHashCodeWithDialogCodeAction", "Generate Equals And Get Hash Code From Members: Generate Equals And Get Hash Code With Dialog (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember.AbstractGenerateEnumMemberService`3+GenerateEnumMemberCodeAction", "Generate Enum Member" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember.AbstractGenerateParameterizedMemberService`4+GenerateParameterizedMemberCodeAction", "Generate Parameterized Member" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateLocalCodeAction", "Generate Variable: Generate Local" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateParameterCodeAction", "Generate Variable: Generate Parameter" },
            { "Microsoft.CodeAnalysis.GenerateMember.GenerateVariable.AbstractGenerateVariableService`3+GenerateVariableCodeAction", "Generate Variable" },
            { "Microsoft.CodeAnalysis.GenerateOverrides.GenerateOverridesCodeRefactoringProvider", "Generate Overrides (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateOverrides.GenerateOverridesCodeRefactoringProvider+GenerateOverridesWithDialogCodeAction", "Generate Overrides: With Dialog (Refactoring)" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+GenerateTypeCodeAction", "Generate Type" },
            { "Microsoft.CodeAnalysis.GenerateType.AbstractGenerateTypeService`6+GenerateTypeCodeActionWithOption", "Generate Type: With Option" },
            { "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction", "Implement Interface" },
            { "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction", "Implement Interface: With Dispose Pattern" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceVariableService`6+IntroduceVariableAllOccurrenceCodeAction", "Introduce Variable: All Occurrence" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.AbstractIntroduceVariableService`6+IntroduceVariableCodeAction", "Introduce Variable" },
            { "Microsoft.CodeAnalysis.IntroduceVariable.IntroduceVariableCodeRefactoringProvider", "Introduce Variable (Refactoring)" },
            { "Microsoft.CodeAnalysis.MoveStaticMembers.MoveStaticMembersWithDialogCodeAction", "Move Static Members: With Dialog" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.AbstractMoveToNamespaceCodeAction+MoveItemsToNamespaceCodeAction", "Move To Namespace: Move Items To Namespace" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.AbstractMoveToNamespaceCodeAction+MoveTypeToNamespaceCodeAction", "Move To Namespace: Move Type To Namespace" },
            { "Microsoft.CodeAnalysis.MoveToNamespace.MoveToNamespaceCodeActionProvider", "Move To Namespace" },
            { "Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement.ConsecutiveStatementPlacementCodeFixProvider", "Consecutive Statement Placement" },
            { "Microsoft.CodeAnalysis.NewLines.MultipleBlankLines.MultipleBlankLinesCodeFixProvider", "Multiple Blank Lines" },
            { "Microsoft.CodeAnalysis.PreferFrameworkType.PreferFrameworkTypeCodeFixProvider", "Prefer Framework Type" },
            { "Microsoft.CodeAnalysis.RemoveRedundantEquality.RemoveRedundantEqualityCodeFixProvider", "Remove Redundant Equality" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryAttributeSuppressionsCodeFixProvider", "Remove Unnecessary Suppressions: Remove Unnecessary Attribute Suppressions" },
            { "Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryInlineSuppressionsCodeFixProvider", "Remove Unnecessary Suppressions: Remove Unnecessary Inline Suppressions" },
            { "Microsoft.CodeAnalysis.ReplaceMethodWithProperty.ReplaceMethodWithPropertyCodeRefactoringProvider", "Replace Method With Property (Refactoring)" },
            { "Microsoft.CodeAnalysis.ReplacePropertyWithMethods.ReplacePropertyWithMethodsCodeRefactoringProvider", "Replace Property With Methods (Refactoring)" },
            { "Microsoft.CodeAnalysis.SimplifyBooleanExpression.SimplifyConditionalCodeFixProvider", "Simplify Boolean Expression: Simplify Conditional" },
            { "Microsoft.CodeAnalysis.UpdateLegacySuppressions.UpdateLegacySuppressionsCodeFixProvider", "Update Legacy Suppressions" },
            { "Microsoft.CodeAnalysis.UpgradeProject.ProjectOptionsChangeAction", "Upgrade Project: Project Options Change" },
            { "Microsoft.CodeAnalysis.UseAutoProperty.AbstractUseAutoPropertyCodeFixProvider`5+UseAutoPropertyCodeAction", "Use Auto Property" },
            { "Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionCodeFixProvider", "Use Coalesce Expression" },
            { "Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionForNullableCodeFixProvider", "Use Coalesce Expression: For Nullable" },
            { "Microsoft.CodeAnalysis.UseExplicitTupleName.UseExplicitTupleNameCodeFixProvider", "Use Explicit Tuple Name" },
            { "Microsoft.CodeAnalysis.UseSystemHashCode.UseSystemHashCodeCodeFixProvider", "Use System Hash Code" },
            { "Microsoft.CodeAnalysis.UseThrowExpression.UseThrowExpressionCodeFixProvider", "Use Throw Expression" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers.VisualBasicAddAccessibilityModifiersCodeFixProvider", "Add Accessibility Modifiers" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddAnonymousTypeMemberName.VisualBasicAddAnonymousTypeMemberNameCodeFixProvider", "Add Anonymous Type Member Name" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay.VisualBasicAddDebuggerDisplayCodeRefactoringProvider", "Add Debugger Display (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddFileBanner.VisualBasicAddFileBannerCodeRefactoringProvider", "Add File Banner (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddImport.VisualBasicAddImportCodeFixProvider", "Add Import" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddMissingReference.VisualBasicAddMissingReferenceCodeFixProvider", "Add Missing Reference" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddObsoleteAttribute.VisualBasicAddObsoleteAttributeCodeFixProvider", "Add Obsolete Attribute" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddPackage.VisualBasicAddSpecificPackageCodeFixProvider", "Add Package: Add Specific Package" },
            { "Microsoft.CodeAnalysis.VisualBasic.AddParameter.VisualBasicAddParameterCodeFixProvider", "Add Parameter" },
            { "Microsoft.CodeAnalysis.VisualBasic.AliasAmbiguousType.VisualBasicAliasAmbiguousTypeCodeFixProvider", "Alias Ambiguous Type" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeActions.RemoveStatementCodeAction", "Code Actions: Remove Statement" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddExplicitCast.VisualBasicAddExplicitCastCodeFixProvider", "Add Explicit Cast" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ConvertToAsync.VisualBasicConvertToAsyncFunctionCodeFixProvider", "Convert To Async: Function" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable.CorrectNextControlVariableCodeFixProvider", "Correct Next Control Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable.CorrectNextControlVariableCodeFixProvider+CorrectNextControlVariableCodeAction", "Correct Next Control Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify.VisualBasicFullyQualifyCodeFixProvider", "Fully Qualify" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEndConstruct.GenerateEndConstructCodeFixProvider", "Generate End Construct" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEnumMember.GenerateEnumMemberCodeFixProvider", "Generate Enum Member" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent.GenerateEventCodeFixProvider", "Generate Event" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent.GenerateEventCodeFixProvider+GenerateEventCodeAction", "Generate Event" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod.GenerateConversionCodeFixProvider", "Generate Method: Generate Conversion" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod.GenerateParameterizedMemberCodeFixProvider", "Generate Method: Generate Parameterized Member" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateType.GenerateTypeCodeFixProvider", "Generate Type" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider", "Incorrect Exit Continue" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+AddKeywordCodeAction", "Incorrect Exit Continue: Add Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+ReplaceKeywordCodeAction", "Incorrect Exit Continue: Replace Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue.IncorrectExitContinueCodeFixProvider+ReplaceTokenKeywordCodeAction", "Incorrect Exit Continue: Replace Token Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectFunctionReturnType.IncorrectFunctionReturnTypeCodeFixProvider", "Incorrect Function Return Type" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicChangeToYieldCodeFixProvider", "Iterator: Change To Yield" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicConvertToIteratorCodeFixProvider", "Iterator: Convert To Iterator" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile.MoveToTopOfFileCodeFixProvider", "Move To Top Of File" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile.MoveToTopOfFileCodeFixProvider+MoveToLineCodeAction", "Move To Top Of File: Move To Line" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase.OverloadBaseCodeFixProvider", "Overload Base" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase.OverloadBaseCodeFixProvider+AddKeywordAction", "Overload Base: Add Keyword" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.AddAwait.VisualBasicAddAwaitCodeRefactoringProvider", "Add Await (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary.VisualBasicInlineMethodRefactoringProvider", "Inline Temporary: Inline Method (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary.VisualBasicInlineTemporaryCodeRefactoringProvider", "Inline Temporary (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveStaticMembers.VisualBasicMoveStaticMembersRefactoringProvider", "Move Static Members (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConflictMarkerResolution.VisualBasicResolveConflictMarkerCodeFixProvider", "Conflict Marker Resolution: Resolve Conflict Marker" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousType.VisualBasicConvertAnonymousTypeToClassCodeRefactoringProvider", "Convert Anonymous Type: To Class (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousTypeToTuple.VisualBasicConvertAnonymousTypeToTupleCodeRefactoringProvider", "Convert Anonymous Type To Tuple (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertAutoPropertyToFullProperty.VisualBasicConvertAutoPropertyToFullPropertyCodeRefactoringProvider", "Convert Auto Property To Full Property (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertCast.VisualBasicConvertDirectCastToTryCastCodeRefactoringProvider", "Convert Cast: Convert Direct Cast To Try Cast (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertConversionOperators.VisualBasicConvertTryCastToDirectCastCodeRefactoringProvider", "Convert Conversion Operators: Convert Try Cast To Direct Cast (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertForEachToFor.VisualBasicConvertForEachToForCodeRefactoringProvider", "Convert For Each To For (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertForToForEach.VisualBasicConvertForToForEachCodeRefactoringProvider", "Convert For To For Each (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch.VisualBasicConvertIfToSwitchCodeRefactoringProvider", "Convert If To Switch (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertNumericLiteral.VisualBasicConvertNumericLiteralCodeRefactoringProvider", "Convert Numeric Literal (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString.VisualBasicConvertConcatenationToInterpolatedStringRefactoringProvider", "Convert To Interpolated String: Convert Concatenation To Interpolated String (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString.VisualBasicConvertPlaceholderToInterpolatedStringRefactoringProvider", "Convert To Interpolated String: Convert Placeholder To Interpolated String (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertTupleToStruct.VisualBasicConvertTupleToStructCodeRefactoringProvider", "Convert Tuple To Struct (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf.VisualBasicConvertGetTypeToNameOfCodeFixProvider", "Convert Type Of To Name Of: Convert Get Type To Name Of" },
            { "Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages.VisualBasicJsonDetectionCodeFixProvider", "Embedded Languages: Json Detection" },
            { "Microsoft.CodeAnalysis.VisualBasic.FileHeaders.VisualBasicFileHeaderCodeFixProvider", "File Headers: File Header" },
            { "Microsoft.CodeAnalysis.VisualBasic.GenerateConstructor.GenerateConstructorCodeFixProvider", "Generate Constructor" },
            { "Microsoft.CodeAnalysis.VisualBasic.GenerateConstructorFromMembers.VisualBasicGenerateConstructorFromMembersCodeRefactoringProvider", "Generate Constructor From Members (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.GenerateDefaultConstructors.VisualBasicGenerateDefaultConstructorsCodeFixProvider", "Generate Default Constructors" },
            { "Microsoft.CodeAnalysis.VisualBasic.GenerateVariable.VisualBasicGenerateVariableCodeFixProvider", "Generate Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.ImplementAbstractClass.VisualBasicImplementAbstractClassCodeFixProvider", "Implement Abstract Class" },
            { "Microsoft.CodeAnalysis.VisualBasic.ImplementInterface.VisualBasicImplementInterfaceCodeFixProvider", "Implement Interface" },
            { "Microsoft.CodeAnalysis.VisualBasic.InitializeParameter.VisualBasicAddParameterCheckCodeRefactoringProvider", "Initialize Parameter: Add Parameter Check (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.InitializeParameter.VisualBasicInitializeMemberFromParameterCodeRefactoringProvider", "Initialize Parameter: Initialize Member From Parameter (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.IntroduceUsingStatement.VisualBasicIntroduceUsingStatementCodeRefactoringProvider", "Introduce Using Statement (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable.VisualBasicIntroduceLocalForExpressionCodeRefactoringProvider", "Introduce Variable: Introduce Local For Expression (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable.VisualBasicIntroduceParameterCodeRefactoringProvider", "Introduce Variable: Introduce Parameter (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.InvertConditional.VisualBasicInvertConditionalCodeRefactoringProvider", "Invert Conditional (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.InvertIf.VisualBasicInvertMultiLineIfCodeRefactoringProvider", "Invert If: Invert Multi Line If (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.InvertIf.VisualBasicInvertSingleLineIfCodeRefactoringProvider", "Invert If: Invert Single Line If (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.InvertLogical.VisualBasicInvertLogicalCodeRefactoringProvider", "Invert Logical (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly.VisualBasicMakeFieldReadonlyCodeFixProvider", "Make Field Readonly" },
            { "Microsoft.CodeAnalysis.VisualBasic.MakeMethodAsynchronous.VisualBasicMakeMethodAsynchronousCodeFixProvider", "Make Method Asynchronous" },
            { "Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous.VisualBasicMakeMethodSynchronousCodeFixProvider", "Make Method Synchronous" },
            { "Microsoft.CodeAnalysis.VisualBasic.MakeTypeAbstract.VisualBasicMakeTypeAbstractCodeFixProvider", "Make Type Abstract" },
            { "Microsoft.CodeAnalysis.VisualBasic.MoveDeclarationNearReference.VisualBasicMoveDeclarationNearReferenceCodeRefactoringProvider", "Move Declaration Near Reference (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.NameTupleElement.VisualBasicNameTupleElementCodeRefactoringProvider", "Name Tuple Element (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.OrderModifiers.VisualBasicOrderModifiersCodeFixProvider", "Order Modifiers" },
            { "Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch.VisualBasicPopulateSwitchStatementCodeFixProvider", "Populate Switch: Statement" },
            { "Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess.VisualBasicQualifyMemberAccessCodeFixProvider", "Qualify Member Access" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveAsyncModifier.VisualBasicRemoveAsyncModifierCodeFixProvider", "Remove Async Modifier" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveSharedFromModuleMembers.VisualBasicRemoveSharedFromModuleMembersCodeFixProvider", "Remove Shared From Module Members" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal.VisualBasicRemoveUnnecessaryByValCodeFixProvider", "Remove Unnecessary By Val" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCast.VisualBasicRemoveUnnecessaryCastCodeFixProvider", "Remove Unnecessary Cast" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports.VisualBasicRemoveUnnecessaryImportsCodeFixProvider", "Remove unnecessary imports" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses.VisualBasicRemoveUnnecessaryParenthesesCodeFixProvider", "Remove Unnecessary Parentheses" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers.VisualBasicRemoveUnusedMembersCodeFixProvider", "Remove Unused Members" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues.VisualBasicRemoveUnusedValuesCodeFixProvider", "Remove Unused Parameters And Values: Remove Unused Values" },
            { "Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable.VisualBasicRemoveUnusedVariableCodeFixProvider", "Remove Unused Variable" },
            { "Microsoft.CodeAnalysis.VisualBasic.ReplaceConditionalWithStatements.VisualBasicReplaceConditionalWithStatementsCodeRefactoringProvider", "Replace Conditional With Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.ReplaceDocCommentTextWithTag.VisualBasicReplaceDocCommentTextWithTagCodeRefactoringProvider", "Replace Doc Comment Text With Tag (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation.VisualBasicSimplifyInterpolationCodeFixProvider", "Simplify Interpolation" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyLinqExpression.VisualBasicSimplifyLinqExpressionCodeFixProvider", "Simplify Linq Expression" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyObjectCreation.VisualBasicSimplifyObjectCreationCodeFixProvider", "Simplify Object Creation" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe.VisualBasicSimplifyThisOrMeCodeFixProvider", "Simplify This Or Me" },
            { "Microsoft.CodeAnalysis.VisualBasic.SimplifyTypeNames.SimplifyTypeNamesCodeFixProvider", "Simplify Type Names" },
            { "Microsoft.CodeAnalysis.VisualBasic.SpellCheck.VisualBasicSpellCheckCodeFixProvider", "Spell Check" },
            { "Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements.VisualBasicMergeConsecutiveIfStatementsCodeRefactoringProvider", "Split Or Merge If Statements: Merge Consecutive If Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements.VisualBasicMergeNestedIfStatementsCodeRefactoringProvider", "Split Or Merge If Statements: Merge Nested If Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements.VisualBasicSplitIntoConsecutiveIfStatementsCodeRefactoringProvider", "Split Or Merge If Statements: Split Into Consecutive If Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements.VisualBasicSplitIntoNestedIfStatementsCodeRefactoringProvider", "Split Or Merge If Statements: Split Into Nested If Statements (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.UnsealClass.VisualBasicUnsealClassCodeFixProvider", "Unseal Class" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty.VisualBasicUseAutoPropertyCodeFixProvider", "Use Auto Property" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer.VisualBasicUseCollectionInitializerCodeFixProvider", "Use Collection Initializer" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment.VisualBasicUseCompoundAssignmentCodeFixProvider", "Use Compound Assignment" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression.VisualBasicUseConditionalExpressionForAssignmentCodeFixProvider", "Use Conditional Expression: For Assignment" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression.VisualBasicUseConditionalExpressionForReturnCodeFixProvider", "Use Conditional Expression: For Return" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName.VisualBasicUseInferredMemberNameCodeFixProvider", "Use Inferred Member Name" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression.VisualBasicUseIsNotExpressionCodeFixProvider", "Use Is Not Expression" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck.VisualBasicUseIsNullCheckForReferenceEqualsCodeFixProvider", "Use Is Null Check: For Reference Equals" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseNamedArguments.VisualBasicUseNamedArgumentsCodeRefactoringProvider", "Use Named Arguments (Refactoring)" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation.VisualBasicUseNullPropagationCodeFixProvider", "Use Null Propagation" },
            { "Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer.VisualBasicUseObjectInitializerCodeFixProvider", "Use Object Initializer" },
            { "Microsoft.CodeAnalysis.VisualBasic.Wrapping.VisualBasicWrappingCodeRefactoringProvider", "Wrapping (Refactoring)" },
            { "Microsoft.CodeAnalysis.Wrapping.WrapItemsAction", "Wrapping: Wrap Items" },
            { "VisualBasicAddMissingImportsRefactoringProvider", "Add Missing Imports (Refactoring)" },
        }.ToImmutableDictionary();

        public static void Main(string[] args)
        {
            Console.WriteLine("Loading assemblies and finding CodeActions ...");

            var assemblies = GetAssemblies(args);
            var codeActionAndProviderTypes = GetCodeActionAndProviderTypes(assemblies);

            Console.WriteLine($"Generating Kusto datatable of {codeActionAndProviderTypes.Length} CodeAction and provider hashes ...");

            var telemetryInfos = GetTelemetryInfos(codeActionAndProviderTypes);
            var datatable = GenerateKustoDatatable(telemetryInfos); // GenerateCodeActionsDescriptionMap(telemetryInfos);

            var filepath = Path.GetFullPath("ActionTable.txt");

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

        internal static ImmutableArray<(string TypeName, string Hash)> GetTelemetryInfos(ImmutableArray<Type> codeActionAndProviderTypes)
        {
            return codeActionAndProviderTypes
                .Distinct(FullNameTypeComparer.Instance)
                .Select(GetTelemetryInfo)
                .OrderBy(info => info.TypeName)
                .ToImmutableArray();

            static (string TypeName, string Hash) GetTelemetryInfo(Type type)
            {
                // Generate dev17 telemetry hash
                var telemetryId = type.GetTelemetryId().ToString();
                var fnvHash = telemetryId.Substring(19);

                return (type.FullName!, fnvHash);
            }
        }

        internal static string GenerateKustoDatatable(ImmutableArray<(string TypeName, string Hash)> telemetryInfos)
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
        internal static string GenerateCodeActionsDescriptionMap(ImmutableArray<(string TypeName, string Hash)> telemetryInfos)
        {
            var builder = new StringBuilder();

            builder.AppendLine("{");

            // Regex to split where letter capitalization changes. Try not to split up interface names such as IEnumerable.
            var regex = new Regex(@"
                (?<=[I])(?=[A-Z]) &
                (?<=[A-Z])(?=[A-Z][a-z]) |
                (?<=[^A-Z])(?=[A-Z]) |
                (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

            // Prefixes and suffixes to trim out.
            var prefixStrings = new[]
            {
                "Abstract",
                "CSharp",
                "VisualBasic",
                "CodeFixes",
                "CodeStyle",
                "TypeStyle",
            };

            var suffixStrings = new[]
            {
                "CodeFixProvider",
                "CodeRefactoringProvider",
                "RefactoringProvider",
                "TaggerProvider",
                "CustomCodeAction",
                "CodeAction",
                "CodeActionWithOption",
                "CodeActionProvider",
                "Action",
                "FeatureService",
                "Service",
                "ProviderHelpers",
                "Provider",
            };

            foreach (var (actionOrProviderTypeName, _) in telemetryInfos)
            {
                if (IgnoredCodeActions.Contains(actionOrProviderTypeName))
                {
                    continue;
                }

                // We create the description string from the namespace and type name after omitting the well-known prefixes and suffixes.
                var descriptionParts = actionOrProviderTypeName.Replace('.', '+').Split('+');

                // When there is deep nesting, construct the descriptions from the inner two names.
                var startIndex = Math.Max(0, descriptionParts.Length - 2);

                var description = string.Empty;
                var isRefactoring = false;

                for (int index = startIndex; index < descriptionParts.Length; index++)
                {
                    var part = descriptionParts[index];

                    // Remove TypeParameter count
                    if (part.Contains('`'))
                    {
                        part = part.Split('`')[0];
                    }

                    foreach (var prefix in prefixStrings)
                    {
                        if (part.StartsWith(prefix))
                        {
                            part = part.Substring(prefix.Length);
                            break;
                        }
                    }

                    foreach (var suffix in suffixStrings)
                    {
                        if (part.EndsWith(suffix))
                        {
                            part = part.Substring(0, part.LastIndexOf(suffix));

                            if (suffix == "CodeActionWithOption")
                            {
                                part += "WithOption";
                            }
                            else if (suffix == "CodeRefactoringProvider" || suffix == "RefactoringProvider")
                            {
                                isRefactoring = true;
                            }

                            break;
                        }
                    }

                    if (part.Length == 0)
                    {
                        continue;
                    }

                    // Split type name into words
                    part = regex.Replace(part, " ");

                    if (description.Length == 0)
                    {
                        description = part;
                        continue;
                    }

                    if (description == part)
                    {
                        // Don't repeat the containing type name.
                        continue;
                    }

                    if (part.StartsWith(description))
                    {
                        // Don't repeat the containing type name.
                        part = part.Substring(description.Length).TrimStart();
                    }

                    description = $"{description}: {part}";
                }

                if (isRefactoring)
                {
                    // Ensure we differentiate refactorings from similar named fixes.
                    description += " (Refactoring)";
                }

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
