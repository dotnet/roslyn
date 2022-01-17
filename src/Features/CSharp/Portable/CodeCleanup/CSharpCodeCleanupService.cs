// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup
{
    [ExportLanguageService(typeof(ICodeCleanupService), LanguageNames.CSharp), Shared]
    internal class CSharpCodeCleanupService : AbstractCodeCleanupService
    {
        /// <summary>
        /// Maps format document code cleanup options to DiagnosticId[]
        /// </summary>
        private static readonly ImmutableArray<DiagnosticSet> s_diagnosticSets =
            ImmutableArray.Create(
                new DiagnosticSet(CSharpFeaturesResources.Apply_implicit_explicit_type_preferences,
                    IDEDiagnosticIds.UseImplicitTypeDiagnosticId, IDEDiagnosticIds.UseExplicitTypeDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Apply_this_qualification_preferences,
                    IDEDiagnosticIds.AddQualificationDiagnosticId, IDEDiagnosticIds.RemoveQualificationDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Apply_language_framework_type_preferences,
                    IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Add_remove_braces_for_single_line_control_statements,
                    IDEDiagnosticIds.AddBracesDiagnosticId),

                new DiagnosticSet(AnalyzersResources.Add_accessibility_modifiers,
                    IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Sort_accessibility_modifiers,
                    IDEDiagnosticIds.OrderModifiersDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Make_private_field_readonly_when_possible,
                    IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId),

                new DiagnosticSet(FeaturesResources.Remove_unnecessary_casts,
                    IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Apply_expression_block_body_preferences,
                    IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                    IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                    IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                    IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                    IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                    IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                    IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                    IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Apply_inline_out_variable_preferences,
                    IDEDiagnosticIds.InlineDeclarationDiagnosticId),

                new DiagnosticSet(FeaturesResources.Remove_unused_variables,
                    CSharpRemoveUnusedVariableCodeFixProvider.CS0168, CSharpRemoveUnusedVariableCodeFixProvider.CS0219),

                new DiagnosticSet(FeaturesResources.Apply_object_collection_initialization_preferences,
                    IDEDiagnosticIds.UseObjectInitializerDiagnosticId, IDEDiagnosticIds.UseCollectionInitializerDiagnosticId),

                new DiagnosticSet(CSharpFeaturesResources.Apply_using_directive_placement_preferences,
                    IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId),

                new DiagnosticSet(FeaturesResources.Apply_file_header_preferences,
                    IDEDiagnosticIds.FileHeaderMismatch));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeCleanupService(ICodeFixService codeFixService)
            : base(codeFixService)
        {
        }

        protected override string OrganizeImportsDescription
            => CSharpFeaturesResources.Organize_Usings;

        protected override ImmutableArray<DiagnosticSet> GetDiagnosticSets()
            => s_diagnosticSets;
    }
}
