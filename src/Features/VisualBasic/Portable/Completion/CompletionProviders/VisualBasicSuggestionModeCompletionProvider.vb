' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(VisualBasicSuggestionModeCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(NamedParameterCompletionProvider))>
    <[Shared]>
    Friend Class VisualBasicSuggestionModeCompletionProvider
        Inherits AbstractSuggestionModeCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Async Function GetSuggestionModeItemAsync(document As Document, position As Integer, itemSpan As TextSpan, trigger As CompletionTrigger, cancellationToken As CancellationToken) As Task(Of CompletionItem)
            Dim text = Await document.GetValueTextAsync(cancellationToken).ConfigureAwait(False)

            Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(False)
            Dim syntaxTree = semanticModel.SyntaxTree

            ' If we're option explicit off, then basically any expression context can have a
            ' builder, since it might be an implicit local declaration.
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)

            If semanticModel.OptionExplicit = False AndAlso (syntaxTree.IsExpressionContext(position, targetToken, cancellationToken) OrElse syntaxTree.IsSingleLineStatementContext(position, targetToken, cancellationToken)) Then
                Return CreateSuggestionModeItem("", "")
            End If

            ' Builder if we're typing a field
            Dim description = VBFeaturesResources.Type_a_name_here_to_declare_a_new_field & vbCrLf &
                              VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab

            If syntaxTree.IsFieldNameDeclarationContext(position, targetToken, cancellationToken) Then
                Return CreateSuggestionModeItem(VBFeaturesResources.new_field, description)
            End If

            If targetToken.Kind = SyntaxKind.None OrElse targetToken.FollowsEndOfStatement(position) Then
                Return Nothing
            End If

            ' Builder if we're typing a parameter
            If syntaxTree.IsParameterNameDeclarationContext(position, cancellationToken) Then
                ' Don't provide a builder if only the "Optional" keyword is recommended --
                ' it's mandatory in that case!
                Dim methodDeclaration = targetToken.GetAncestor(Of MethodBaseSyntax)()
                If methodDeclaration IsNot Nothing Then
                    If targetToken.Kind = SyntaxKind.CommaToken AndAlso targetToken.Parent.Kind = SyntaxKind.ParameterList Then
                        For Each parameter In methodDeclaration.ParameterList.Parameters.Where(Function(p) p.FullSpan.End < position)
                            ' A previous parameter was Optional, so the suggested Optional is an offer they can't refuse. No builder.
                            If parameter.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.OptionalKeyword) Then
                                Return Nothing
                            End If
                        Next
                    End If
                End If

                description = VBFeaturesResources.Type_a_name_here_to_declare_a_parameter_If_no_preceding_keyword_is_used_ByVal_will_be_assumed_and_the_argument_will_be_passed_by_value & vbCrLf &
                              VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab

                ' Otherwise just return a builder. It won't show up unless other modifiers are
                ' recommended, which is what we want.
                Return CreateSuggestionModeItem(VBFeaturesResources.parameter_name, description)
            End If

            ' Builder in select clause: after Select, after comma
            If targetToken.Parent.Kind = SyntaxKind.SelectClause Then
                If targetToken.IsKind(SyntaxKind.SelectKeyword, SyntaxKind.CommaToken) Then
                    description = VBFeaturesResources.Type_a_new_name_for_the_column_followed_by_Otherwise_the_original_column_name_with_be_used & vbCrLf &
                                  VBFeaturesResources.Note_colon_Use_tab_for_automatic_completion_space_completion_is_disabled_to_avoid_interfering_with_a_new_name

                    Return CreateSuggestionModeItem(VBFeaturesResources.result_alias, description)
                End If
            End If

            ' Build after For
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.ForKeyword) AndAlso
               targetToken.Parent.IsKind(SyntaxKind.ForStatement) Then

                description = VBFeaturesResources.Type_a_new_variable_name & vbCrLf &
                              VBFeaturesResources.Note_colon_Space_and_completion_are_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab

                Return CreateSuggestionModeItem(VBFeaturesResources.new_variable, description)
            End If

            ' Build after Using
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.UsingKeyword) AndAlso
               targetToken.Parent.IsKind(SyntaxKind.UsingStatement) Then

                description = VBFeaturesResources.Type_a_new_variable_name & vbCrLf &
                              VBFeaturesResources.Note_colon_Space_and_completion_are_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab

                Return CreateSuggestionModeItem(VBFeaturesResources.new_resource, description)
            End If

            ' Builder at Namespace declaration name
            If syntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken) Then

                description = VBFeaturesResources.Type_a_name_here_to_declare_a_namespace & vbCrLf &
                              VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab

                Return CreateSuggestionModeItem(VBFeaturesResources.namespace_name, description)
            End If

            Dim statementSyntax As TypeStatementSyntax = Nothing

            ' Builder after Partial (Class|Structure|Interface|Module) 
            If syntaxTree.IsPartialTypeDeclarationNameContext(position, cancellationToken, statementSyntax) Then

                Select Case statementSyntax.DeclarationKeyword.Kind()
                    Case SyntaxKind.ClassKeyword
                        Return CreateSuggestionModeItem(
                            VBFeaturesResources.class_name,
                            VBFeaturesResources.Type_a_name_here_to_declare_a_partial_class & vbCrLf &
                            VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab)

                    Case SyntaxKind.InterfaceKeyword
                        Return CreateSuggestionModeItem(
                            VBFeaturesResources.interface_name,
                            VBFeaturesResources.Type_a_name_here_to_declare_a_partial_interface & vbCrLf &
                            VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab)

                    Case SyntaxKind.StructureKeyword
                        Return CreateSuggestionModeItem(
                            VBFeaturesResources.structure_name,
                            VBFeaturesResources.Type_a_name_here_to_declare_a_partial_structure & vbCrLf &
                            VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab)

                    Case SyntaxKind.ModuleKeyword
                        Return CreateSuggestionModeItem(
                            VBFeaturesResources.module_name,
                            VBFeaturesResources.Type_a_name_here_to_declare_a_partial_module & vbCrLf &
                            VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab)

                End Select

            End If

            Return Nothing
        End Function
    End Class
End Namespace
