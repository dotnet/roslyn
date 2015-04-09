' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class BannerTextBuilder
        Private _builder As StringBuilder

        Public Sub New()
            _builder = New StringBuilder()
        End Sub

        Public Sub New(capacity As Integer)
            _builder = New StringBuilder(capacity)
        End Sub

        Private Sub Append(text As Char)
            _builder.Append(text)
        End Sub

        Friend Sub Append(text As String)
            _builder.Append(text)
        End Sub

        Private Sub AppendCommaSeparatedSyntaxList(Of T As SyntaxNode)(list As SeparatedSyntaxList(Of T), Optional appendItem As Action(Of T) = Nothing)
            If appendItem Is Nothing Then
                appendItem = Sub(node) _builder.Append(node.ToString)
            End If

            If list.Count >= 1 Then
                appendItem(list(0))

                For i = 1 To list.Count - 1
                    _builder.Append(", ")
                    appendItem(list(i))
                Next
            End If
        End Sub

        Private Sub AppendTypeParameter(typeParam As TypeParameterSyntax)
            Contract.ThrowIfNull(typeParam)

            Append(typeParam.Identifier.ToString())

            ' Append type parameter constraints
            If typeParam.TypeParameterConstraintClause IsNot Nothing Then
                If TypeOf typeParam.TypeParameterConstraintClause Is TypeParameterSingleConstraintClauseSyntax Then
                    Dim singleConstraint = DirectCast(typeParam.TypeParameterConstraintClause, TypeParameterSingleConstraintClauseSyntax)
                    Append(" As ")
                    Append(singleConstraint.Constraint.ToString)
                ElseIf TypeOf typeParam.TypeParameterConstraintClause Is TypeParameterMultipleConstraintClauseSyntax Then
                    Dim multiConstraint = DirectCast(typeParam.TypeParameterConstraintClause, TypeParameterMultipleConstraintClauseSyntax)
                    Append(" As {")
                    AppendCommaSeparatedSyntaxList(multiConstraint.Constraints)
                    Append("}"c)
                End If
            End If
        End Sub

        Friend Sub AppendTypeParameterList(typeParametersOpt As TypeParameterListSyntax)
            If typeParametersOpt IsNot Nothing Then
                Append("(Of ")
                AppendCommaSeparatedSyntaxList(
                    typeParametersOpt.Parameters,
                    AddressOf AppendTypeParameter)
                Append(")"c)
            End If
        End Sub

        Private Sub AppendParameter(param As ParameterSyntax)
            Contract.ThrowIfNull(param)

            Dim validModifiers = param.Modifiers.Where(Function(m) m.Kind <> SyntaxKind.ByValKeyword)
            For Each modifier In validModifiers
                Append(modifier.ToString())
                Append(" "c)
            Next

            Append(param.Identifier.ToString)

            If param.AsClause IsNot Nothing Then
                Append(" As ")
                Append(param.AsClause.Type.ToString)
            End If

            If param.Default IsNot Nothing Then
                Append(" = ")
                Append(param.Default.Value.ToString)
            End If
        End Sub

        ''' <summary>
        ''' Appends a parameter list to the banner text.
        ''' </summary>
        ''' <param name="parametersOpt">The <see cref="ParameterListSyntax" /> to append. This parameter may be null.</param>
        ''' <param name="emptyParentheses">If true, empty parentheses will be appended if <paramref name="parametersOpt" /> is null.</param>
        Friend Sub AppendParameterList(parametersOpt As ParameterListSyntax, emptyParentheses As Boolean)
            Dim addParentheses =
                emptyParentheses OrElse
                (parametersOpt IsNot Nothing AndAlso parametersOpt.Parameters.Count > 0)

            If addParentheses Then
                Append("("c)
            End If

            If parametersOpt IsNot Nothing Then
                AppendCommaSeparatedSyntaxList(
                    parametersOpt.Parameters,
                    AddressOf AppendParameter)
            End If

            If addParentheses Then
                Append(")"c)
            End If
        End Sub

        Friend Sub AppendAsClause(asClauseOpt As AsClauseSyntax)
            If asClauseOpt IsNot Nothing Then
                Append(" As ")
                Append(asClauseOpt.Type.ToString)
            End If
        End Sub

        Friend Sub AppendHandlesClause(handlesClauseOpt As HandlesClauseSyntax)
            If handlesClauseOpt IsNot Nothing Then
                Append(" Handles ")
                AppendCommaSeparatedSyntaxList(handlesClauseOpt.Events)
            End If
        End Sub

        Friend Sub AppendImplementsClause(implementsClauseOpt As ImplementsClauseSyntax)
            If implementsClauseOpt IsNot Nothing Then
                Append(" Implements ")
                AppendCommaSeparatedSyntaxList(implementsClauseOpt.InterfaceMembers)
            End If
        End Sub

        Public Overrides Function ToString() As String
            Return _builder.ToString()
        End Function
    End Class
End Namespace
