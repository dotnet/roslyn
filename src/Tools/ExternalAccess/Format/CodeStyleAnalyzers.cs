// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.InlineDeclaration;
using Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess;
using Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly;
using Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.UseAutoProperty;
using Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression;
using Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;
using Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.CSharp.UseDeconstruction;
using Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator;
using Microsoft.CodeAnalysis.CSharp.UseInferredMemberName;
using Microsoft.CodeAnalysis.CSharp.UseIsNullCheck;
using Microsoft.CodeAnalysis.CSharp.UseLocalFunction;
using Microsoft.CodeAnalysis.CSharp.UseNullPropagation;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement;
using Microsoft.CodeAnalysis.CSharp.UseThrowExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MakeFieldReadonly;
using Microsoft.CodeAnalysis.PreferFrameworkType;
using Microsoft.CodeAnalysis.UseCoalesceExpression;
using Microsoft.CodeAnalysis.UseExplicitTupleName;
using Microsoft.CodeAnalysis.UseThrowExpression;
using Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.VisualBasic.AddRequiredParentheses;
using Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers;
using Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly;
using Microsoft.CodeAnalysis.VisualBasic.OrderModifiers;
using Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess;
using Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty;
using Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression;
using Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer;
using Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment;
using Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression;
using Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName;
using Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck;
using Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation;
using Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Format
{
    internal class CodeStyleAnalyzers
    {
        public static AnalyzerOptions GetWorkspaceAnalyzerOptions(Project project)
            => new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution.Options, project.Solution);

        public static ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            return new DiagnosticAnalyzer[]
            {
                // .NET code style settings

                // "This." and "Me." qualifiers
                // - dotnet_style_qualification_for_field
                // - dotnet_style_qualification_for_property
                // - dotnet_style_qualification_for_method
                // - dotnet_style_qualification_for_event
                new CSharpQualifyMemberAccessDiagnosticAnalyzer(),
                new VisualBasicQualifyMemberAccessDiagnosticAnalyzer(),

                // Language keywords instead of framework type names for type references
                // - dotnet_style_predefined_type_for_locals_parameters_members
                // - dotnet_style_predefined_type_for_member_access
                new CSharpPreferFrameworkTypeDiagnosticAnalyzer(),
                new VisualBasicPreferFrameworkTypeDiagnosticAnalyzer(),

                // Modifier preferences
                // - dotnet_style_require_accessibility_modifiers
                new CSharpAddAccessibilityModifiersDiagnosticAnalyzer(),
                new VisualBasicAddAccessibilityModifiersDiagnosticAnalyzer(),
                // - csharp_preferred_modifier_order
                new CSharpOrderModifiersDiagnosticAnalyzer(),
                // - visual_basic_preferred_modifier_order
                new VisualBasicOrderModifiersDiagnosticAnalyzer(),
                // - dotnet_style_readonly_field
                new MakeFieldReadonlyDiagnosticAnalyzer(),

                // Parentheses preferences
                // - dotnet_style_parentheses_in_arithmetic_binary_operators
                // - dotnet_style_parentheses_in_other_binary_operators
                // - dotnet_style_parentheses_in_other_operators
                // - dotnet_style_parentheses_in_relational_binary_operators
                new CSharpAddRequiredParenthesesDiagnosticAnalyzer(),
                new CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer(),
                new VisualBasicAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer(),
                new VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer(),

                // Expression-level preferences
                // - dotnet_style_object_initializer
                new CSharpUseObjectInitializerDiagnosticAnalyzer(),
                new VisualBasicUseObjectInitializerDiagnosticAnalyzer(),
                // - dotnet_style_collection_initializer
                new CSharpUseCollectionInitializerDiagnosticAnalyzer(),
                new VisualBasicUseCollectionInitializerDiagnosticAnalyzer(),
                // - dotnet_style_explicit_tuple_names
                new UseExplicitTupleNameDiagnosticAnalyzer(),
                // - dotnet_style_prefer_inferred_tuple_names
                // - dotnet_style_prefer_inferred_anonymous_type_member_names
                new CSharpUseInferredMemberNameDiagnosticAnalyzer(),
                new VisualBasicUseInferredMemberNameDiagnosticAnalyzer(),
                // - dotnet_style_prefer_auto_properties
                new CSharpUseAutoPropertyAnalyzer(),
                new VisualBasicUseAutoPropertyAnalyzer(),
                // - dotnet_style_prefer_is_null_check_over_reference_equality_method
                new CSharpUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(),
                new VisualBasicUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(),
                // - dotnet_style_prefer_conditional_expression_over_assignment
                new CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer(),
                new VisualBasicUseConditionalExpressionForAssignmentDiagnosticAnalyzer(),
                // - dotnet_style_prefer_conditional_expression_over_return
                new CSharpUseConditionalExpressionForReturnDiagnosticAnalyzer(),
                new VisualBasicUseConditionalExpressionForReturnDiagnosticAnalyzer(),
                // - dotnet_style_prefer_compound_assignment
                new CSharpUseCompoundAssignmentDiagnosticAnalyzer(),
                new VisualBasicUseCompoundAssignmentDiagnosticAnalyzer(),

                // "Null" checking preferences
                // - dotnet_style_coalesce_expression
                new CSharpUseCoalesceExpressionDiagnosticAnalyzer(),
                new VisualBasicUseCoalesceExpressionDiagnosticAnalyzer(),
                // - dotnet_style_null_propagation
                new CSharpUseNullPropagationDiagnosticAnalyzer(),
                new VisualBasicUseNullPropagationDiagnosticAnalyzer(),

                // C# code style settings

                // Implicit and explicit types
                // - csharp_style_var_for_built_in_types
                // - csharp_style_var_when_type_is_apparent
                // - csharp_style_var_elsewhere
                new CSharpUseExplicitTypeDiagnosticAnalyzer(),
                new CSharpUseImplicitTypeDiagnosticAnalyzer(),

                // Expression-bodied members
                // - csharp_style_expression_bodied_methods
                // - csharp_style_expression_bodied_constructors
                // - csharp_style_expression_bodied_operators
                // - csharp_style_expression_bodied_properties
                // - csharp_style_expression_bodied_indexers
                // - csharp_style_expression_bodied_accessors
                // - csharp_style_expression_bodied_lambdas
                // - csharp_style_expression_bodied_local_functions
                new UseExpressionBodyDiagnosticAnalyzer(),

                // Pattern matching
                // - csharp_style_pattern_matching_over_is_with_cast_check
                new CSharpIsAndCastCheckDiagnosticAnalyzer(),
                // - csharp_style_pattern_matching_over_as_with_null_check
                new CSharpAsAndNullCheckDiagnosticAnalyzer(),
                // - csharp_style_prefer_switch_expression
                new ConvertSwitchStatementToExpressionDiagnosticAnalyzer(),

                // Inlined variable declarationsC:\Users\jorobich\Source\repos\roslyn\src\Features\VisualBasic\Portable\SignatureHelp\
                // - csharp_style_inlined_variable_declaration
                new CSharpInlineDeclarationDiagnosticAnalyzer(),

                // Expression-level preferences
                // - csharp_prefer_simple_default_expression
                new CSharpUseDefaultLiteralDiagnosticAnalyzer(),
                // - csharp_style_deconstructed_variable_declaration
                new CSharpUseDeconstructionDiagnosticAnalyzer(),
                // - csharp_style_pattern_local_over_anonymous_function
                new CSharpUseLocalFunctionDiagnosticAnalyzer(),
                // - csharp_style_prefer_index_operator
                new CSharpUseIndexOperatorDiagnosticAnalyzer(),
                // - csharp_style_prefer_range_operator
                new CSharpUseRangeOperatorDiagnosticAnalyzer(),
                // - csharp_prefer_static_local_function
                new MakeLocalFunctionStaticDiagnosticAnalyzer(),
                // - csharp_prefer_simple_using_statement
                new UseSimpleUsingStatementDiagnosticAnalyzer(),

                // "Null" checking preferences
                // - csharp_style_throw_expression
                new CSharpUseThrowExpressionDiagnosticAnalyzer(),
                // - csharp_style_conditional_delegate_call
                new InvokeDelegateWithConditionalAccessAnalyzer(),

                // Code block preferences
                // - csharp_prefer_braces
                new CSharpAddBracesDiagnosticAnalyzer(),

            }.ToImmutableArray();
        }

        public static ImmutableArray<CodeFixProvider> GetCodeFixProviders()
        {
            return new CodeFixProvider[]
            {
                // .NET code style settings

                // "This." and "Me." qualifiers
                // - dotnet_style_qualification_for_field
                // - dotnet_style_qualification_for_property
                // - dotnet_style_qualification_for_method
                // - dotnet_style_qualification_for_event
                new CSharpQualifyMemberAccessCodeFixProvider(),
                new VisualBasicQualifyMemberAccessCodeFixProvider(),

                // Language keywords instead of framework type names for type references
                // - dotnet_style_predefined_type_for_locals_parameters_members
                // - dotnet_style_predefined_type_for_member_access
                new PreferFrameworkTypeCodeFixProvider(),

                // Modifier preferences
                // - dotnet_style_require_accessibility_modifiers
                new CSharpAddAccessibilityModifiersCodeFixProvider(),
                new VisualBasicAddAccessibilityModifiersCodeFixProvider(),
                // - csharp_preferred_modifier_order
                new CSharpOrderModifiersCodeFixProvider(),
                // - visual_basic_preferred_modifier_order
                new VisualBasicOrderModifiersCodeFixProvider(),
                // - dotnet_style_readonly_field
                new CSharpMakeFieldReadonlyCodeFixProvider(),
                new VisualBasicMakeFieldReadonlyCodeFixProvider(),

                // Parentheses preferences
                // - dotnet_style_parentheses_in_arithmetic_binary_operators
                // - dotnet_style_parentheses_in_other_binary_operators
                // - dotnet_style_parentheses_in_other_operators
                // - dotnet_style_parentheses_in_relational_binary_operators
                new AddRequiredParenthesesCodeFixProvider(),
                new CSharpRemoveUnnecessaryParenthesesCodeFixProvider(),
                new VisualBasicRemoveUnnecessaryParenthesesCodeFixProvider(),

                // Expression-level preferences
                // - dotnet_style_object_initializer
                new CSharpUseObjectInitializerCodeFixProvider(),
                new VisualBasicUseObjectInitializerCodeFixProvider(),
                // - dotnet_style_collection_initializer
                new CSharpUseCollectionInitializerCodeFixProvider(),
                new VisualBasicUseCollectionInitializerCodeFixProvider(),
                // - dotnet_style_explicit_tuple_names
                new UseExplicitTupleNameCodeFixProvider(),
                // - dotnet_style_prefer_inferred_tuple_names
                // - dotnet_style_prefer_inferred_anonymous_type_member_names
                new CSharpUseInferredMemberNameCodeFixProvider(),
                new VisualBasicUseInferredMemberNameCodeFixProvider(),
                // - dotnet_style_prefer_auto_properties
                new CSharpUseAutoPropertyCodeFixProvider(),
                new VisualBasicUseAutoPropertyCodeFixProvider(),
                // - dotnet_style_prefer_is_null_check_over_reference_equality_method
                new CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider(),
                new VisualBasicUseIsNullCheckForReferenceEqualsCodeFixProvider(),
                // - dotnet_style_prefer_conditional_expression_over_assignment
                new CSharpUseConditionalExpressionForAssignmentCodeRefactoringProvider(),
                new VisualBasicUseConditionalExpressionForAssignmentCodeRefactoringProvider(),
                // - dotnet_style_prefer_conditional_expression_over_return
                new CSharpUseConditionalExpressionForReturnCodeRefactoringProvider(),
                new VisualBasicUseConditionalExpressionForReturnCodeRefactoringProvider(),
                // - dotnet_style_prefer_compound_assignment
                new CSharpUseCompoundAssignmentCodeFixProvider(),
                new VisualBasicUseCompoundAssignmentCodeFixProvider(),

                // "Null" checking preferences
                // - dotnet_style_coalesce_expression
                new UseCoalesceExpressionCodeFixProvider(),
                // - dotnet_style_null_propagation
                new CSharpUseNullPropagationCodeFixProvider(),
                new VisualBasicUseNullPropagationCodeFixProvider(),

                // C# code style settings

                // Implicit and explicit types
                // - csharp_style_var_for_built_in_types
                // - csharp_style_var_when_type_is_apparent
                // - csharp_style_var_elsewhere
                new UseExplicitTypeCodeFixProvider(),
                new UseImplicitTypeCodeFixProvider(),

                // Expression-bodied members
                // - csharp_style_expression_bodied_methods
                // - csharp_style_expression_bodied_constructors
                // - csharp_style_expression_bodied_operators
                // - csharp_style_expression_bodied_properties
                // - csharp_style_expression_bodied_indexers
                // - csharp_style_expression_bodied_accessors
                // - csharp_style_expression_bodied_lambdas
                // - csharp_style_expression_bodied_local_functions
                new UseExpressionBodyCodeFixProvider(),

                // Pattern matching
                // - csharp_style_pattern_matching_over_is_with_cast_check
                new CSharpIsAndCastCheckCodeFixProvider(),
                // - csharp_style_pattern_matching_over_as_with_null_check
                new CSharpAsAndNullCheckCodeFixProvider(),
                // - csharp_style_prefer_switch_expression
                new ConvertSwitchStatementToExpressionCodeFixProvider(),

                // Inlined variable declarationsC:\Users\jorobich\Source\repos\roslyn\src\Features\VisualBasic\Portable\SignatureHelp\
                // - csharp_style_inlined_variable_declaration
                new CSharpInlineDeclarationCodeFixProvider(),

                // Expression-level preferences
                // - csharp_prefer_simple_default_expression
                new CSharpUseDefaultLiteralCodeFixProvider(),
                // - csharp_style_deconstructed_variable_declaration
                new CSharpUseDeconstructionCodeFixProvider(),
                // - csharp_style_pattern_local_over_anonymous_function
                new CSharpUseLocalFunctionCodeFixProvider(),
                // - csharp_style_prefer_index_operator
                new CSharpUseIndexOperatorCodeFixProvider(),
                // - csharp_style_prefer_range_operator
                new CSharpUseRangeOperatorCodeFixProvider(),
                // - csharp_prefer_static_local_function
                new MakeLocalFunctionStaticCodeFixProvider(),
                // - csharp_prefer_simple_using_statement
                new UseSimpleUsingStatementCodeFixProvider(),

                // "Null" checking preferences
                // - csharp_style_throw_expression
                new UseThrowExpressionCodeFixProvider(),
                // - csharp_style_conditional_delegate_call
                new InvokeDelegateWithConditionalAccessCodeFixProvider(),

                // Code block preferences
                // - csharp_prefer_braces
                new CSharpAddBracesCodeFixProvider(),
            }.ToImmutableArray();
        }
    }
}
