' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Friend Module ClassificationHelpers
        ''' <summary>
        ''' Return the classification type associated with this token.
        ''' </summary>
        ''' <param name="token">The token to be classified.</param>
        ''' <returns>The classification type for the token</returns>
        ''' <remarks></remarks>
        Public Function GetClassification(token As SyntaxToken) As String
            If IsControlKeyword(token) Then
                Return ClassificationTypeNames.ControlKeyword
            ElseIf SyntaxFacts.IsKeywordKind(token.Kind) Then
                Return ClassificationTypeNames.Keyword
            ElseIf IsStringToken(token) Then
                Return ClassificationTypeNames.StringLiteral
            ElseIf SyntaxFacts.IsPunctuation(token.Kind) Then
                Return ClassifyPunctuation(token)
            ElseIf token.Kind = SyntaxKind.IdentifierToken Then
                Return ClassifyIdentifierSyntax(token)
            ElseIf token.IsNumericLiteral() Then
                Return ClassificationTypeNames.NumericLiteral
            ElseIf token.Kind = SyntaxKind.XmlNameToken Then
                Return ClassificationTypeNames.XmlLiteralName
            ElseIf token.Kind = SyntaxKind.XmlTextLiteralToken Then
                Select Case token.Parent.Kind
                    Case SyntaxKind.XmlString
                        Return ClassificationTypeNames.XmlLiteralAttributeValue
                    Case SyntaxKind.XmlProcessingInstruction
                        Return ClassificationTypeNames.XmlLiteralProcessingInstruction
                    Case SyntaxKind.XmlComment
                        Return ClassificationTypeNames.XmlLiteralComment
                    Case SyntaxKind.XmlCDataSection
                        Return ClassificationTypeNames.XmlLiteralCDataSection
                    Case Else
                        Return ClassificationTypeNames.XmlLiteralText
                End Select
            ElseIf token.Kind = SyntaxKind.XmlEntityLiteralToken Then
                Return ClassificationTypeNames.XmlLiteralEntityReference
            ElseIf token.IsKind(SyntaxKind.None, SyntaxKind.BadToken) Then
                Return Nothing
            Else
                Return Contract.FailWithReturn(Of String)("Unhandled token kind: " & token.Kind().ToString())
            End If
        End Function

        Private Function IsControlKeyword(token As SyntaxToken) As Boolean
            If token.Parent Is Nothing Then
                Return False
            End If

            ' For Exit Statments classify everything as a control keyword
            If token.Parent.IsKind(
                SyntaxKind.ExitFunctionStatement,
                SyntaxKind.ExitOperatorStatement,
                SyntaxKind.ExitPropertyStatement,
                SyntaxKind.ExitSubStatement) Then
                Return True
            End If

            ' Control keywords are used in other contexts so check that it is
            ' being used in a supported context.
            Return IsControlKeywordKind(token.Kind) AndAlso
                IsControlStatementKind(token.Parent.Kind)
        End Function

        ''' <summary>
        ''' Determine if the kind represents a control keyword
        ''' </summary>
        Private Function IsControlKeywordKind(kind As SyntaxKind) As Boolean
            Select Case kind
                Case _
                SyntaxKind.CaseKeyword,
                SyntaxKind.CatchKeyword,
                SyntaxKind.ContinueKeyword,
                SyntaxKind.DoKeyword,
                SyntaxKind.EachKeyword,
                SyntaxKind.ElseKeyword,
                SyntaxKind.ElseIfKeyword,
                SyntaxKind.EndKeyword,
                SyntaxKind.ExitKeyword,
                SyntaxKind.FinallyKeyword,
                SyntaxKind.ForKeyword,
                SyntaxKind.GoToKeyword,
                SyntaxKind.IfKeyword,
                SyntaxKind.InKeyword,
                SyntaxKind.LoopKeyword,
                SyntaxKind.NextKeyword,
                SyntaxKind.ResumeKeyword,
                SyntaxKind.ReturnKeyword,
                SyntaxKind.SelectKeyword,
                SyntaxKind.ThenKeyword,
                SyntaxKind.TryKeyword,
                SyntaxKind.WhileKeyword,
                SyntaxKind.WendKeyword,
                SyntaxKind.UntilKeyword,
                SyntaxKind.EndIfKeyword,
                SyntaxKind.GosubKeyword,
                SyntaxKind.YieldKeyword,
                SyntaxKind.ToKeyword
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Determine if the kind represents a control statement
        ''' </summary>
        Private Function IsControlStatementKind(kind As SyntaxKind) As Boolean
            Select Case kind
                Case _
                SyntaxKind.CallStatement,
                SyntaxKind.CaseElseStatement,
                SyntaxKind.CaseStatement,
                SyntaxKind.CatchStatement,
                SyntaxKind.ContinueDoStatement,
                SyntaxKind.ContinueForStatement,
                SyntaxKind.ContinueWhileStatement,
                SyntaxKind.DoUntilStatement,
                SyntaxKind.DoWhileStatement,
                SyntaxKind.ElseIfStatement,
                SyntaxKind.ElseStatement,
                SyntaxKind.EndIfStatement,
                SyntaxKind.EndSelectStatement,
                SyntaxKind.EndTryStatement,
                SyntaxKind.EndWhileStatement,
                SyntaxKind.ExitDoStatement,
                SyntaxKind.ExitForStatement,
                SyntaxKind.ExitSelectStatement,
                SyntaxKind.ExitTryStatement,
                SyntaxKind.ExitWhileStatement,
                SyntaxKind.FinallyStatement,
                SyntaxKind.ForEachStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.GoToStatement,
                SyntaxKind.IfStatement,
                SyntaxKind.LoopUntilStatement,
                SyntaxKind.LoopWhileStatement,
                SyntaxKind.NextStatement,
                SyntaxKind.ResumeLabelStatement,
                SyntaxKind.ResumeNextStatement,
                SyntaxKind.ReturnStatement,
                SyntaxKind.SelectStatement,
                SyntaxKind.SimpleDoStatement,
                SyntaxKind.SimpleLoopStatement,
                SyntaxKind.SingleLineIfStatement,
                SyntaxKind.ThrowStatement,
                SyntaxKind.TryStatement,
                SyntaxKind.UntilClause,
                SyntaxKind.WhileClause,
                SyntaxKind.WhileStatement,
                SyntaxKind.YieldStatement,
                SyntaxKind.TernaryConditionalExpression
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Private Function ClassifyPunctuation(token As SyntaxToken) As String
            If AllOperators.Contains(token.Kind) Then
                ' special cases...
                Select Case token.Kind
                    Case SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken
                        If TypeOf token.Parent Is AttributeListSyntax Then
                            Return ClassificationTypeNames.Punctuation
                        End If
                End Select

                Return ClassificationTypeNames.Operator
            Else
                Return ClassificationTypeNames.Punctuation
            End If
        End Function

        Private Function ClassifyIdentifierSyntax(identifier As SyntaxToken) As String
            'Note: parent might be Nothing, if we are classifying raw tokens.
            Dim parent = identifier.Parent

            Dim classification As String = Nothing

            If TypeOf parent Is IdentifierNameSyntax AndAlso IsNamespaceName(DirectCast(parent, IdentifierNameSyntax)) Then
                Return ClassificationTypeNames.NamespaceName
            ElseIf TypeOf parent Is TypeStatementSyntax AndAlso DirectCast(parent, TypeStatementSyntax).Identifier = identifier Then
                Return ClassifyTypeDeclarationIdentifier(identifier)
            ElseIf TypeOf parent Is EnumStatementSyntax AndAlso DirectCast(parent, EnumStatementSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.EnumName
            ElseIf TypeOf parent Is DelegateStatementSyntax AndAlso DirectCast(parent, DelegateStatementSyntax).Identifier = identifier AndAlso
                  (parent.Kind = SyntaxKind.DelegateSubStatement OrElse parent.Kind = SyntaxKind.DelegateFunctionStatement) Then

                Return ClassificationTypeNames.DelegateName
            ElseIf TypeOf parent Is TypeParameterSyntax AndAlso DirectCast(parent, TypeParameterSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.TypeParameterName
            ElseIf TypeOf parent Is MethodStatementSyntax AndAlso DirectCast(parent, MethodStatementSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.MethodName
            ElseIf TypeOf parent Is DeclareStatementSyntax AndAlso DirectCast(parent, DeclareStatementSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.MethodName
            ElseIf TypeOf parent Is PropertyStatementSyntax AndAlso DirectCast(parent, PropertyStatementSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.PropertyName
            ElseIf TypeOf parent Is EventStatementSyntax AndAlso DirectCast(parent, EventStatementSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.EventName
            ElseIf TypeOf parent Is EnumMemberDeclarationSyntax AndAlso DirectCast(parent, EnumMemberDeclarationSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.EnumMemberName
            ElseIf TypeOf parent Is LabelStatementSyntax AndAlso DirectCast(parent, LabelStatementSyntax).LabelToken = identifier Then
                Return ClassificationTypeNames.LabelName
            ElseIf TypeOf parent?.Parent Is CatchStatementSyntax AndAlso DirectCast(parent.Parent, CatchStatementSyntax).IdentifierName.Identifier = identifier Then
                Return ClassificationTypeNames.LocalName
            ElseIf TryClassifyModifiedIdentifer(parent, identifier, classification) Then
                Return classification
            ElseIf (identifier.ToString() = "IsTrue" OrElse identifier.ToString() = "IsFalse") AndAlso
                TypeOf parent Is OperatorStatementSyntax AndAlso DirectCast(parent, OperatorStatementSyntax).OperatorToken = identifier Then

                Return ClassificationTypeNames.Keyword
            End If

            Return ClassificationTypeNames.Identifier
        End Function

        Private Function IsNamespaceName(identifierSyntax As IdentifierNameSyntax) As Boolean
            Dim parent = identifierSyntax.Parent

            While TypeOf parent Is QualifiedNameSyntax
                parent = parent.Parent
            End While

            Return TypeOf parent Is NamespaceStatementSyntax
        End Function

        Public Function IsStaticallyDeclared(identifier As SyntaxToken) As Boolean
            'Note: parent might be Nothing, if we are classifying raw tokens.
            Dim parent = identifier.Parent

            If parent.IsKind(SyntaxKind.EnumMemberDeclaration) Then
                ' EnumMembers are not classified as static since there is no
                ' instance equivalent of the concept and they have their own
                ' classification type.
                Return False
            ElseIf parent.IsKind(SyntaxKind.ModifiedIdentifier) Then
                parent = parent.Parent?.Parent

                ' We are specifically looking for field declarations or constants.
                If Not parent.IsKind(SyntaxKind.FieldDeclaration) Then
                    Return False
                End If

                If parent.GetModifiers().Any(SyntaxKind.ConstKeyword) Then
                    Return True
                End If
            End If

            Return parent.GetModifiers().Any(SyntaxKind.SharedKeyword)
        End Function

        Private Function IsStringToken(token As SyntaxToken) As Boolean
            If token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken, SyntaxKind.InterpolatedStringTextToken) Then
                Return True
            End If

            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.DoubleQuoteToken) AndAlso
                   token.Parent.IsKind(SyntaxKind.InterpolatedStringExpression)
        End Function

        Private Function TryClassifyModifiedIdentifer(node As SyntaxNode, identifier As SyntaxToken, ByRef classification As String) As Boolean
            classification = Nothing

            If TypeOf node IsNot ModifiedIdentifierSyntax OrElse DirectCast(node, ModifiedIdentifierSyntax).Identifier <> identifier Then
                Return False
            End If

            If TypeOf node.Parent Is ParameterSyntax Then
                classification = ClassificationTypeNames.ParameterName
                Return True
            End If

            If TypeOf node.Parent IsNot VariableDeclaratorSyntax Then
                Return False
            End If

            If TypeOf node.Parent.Parent Is LocalDeclarationStatementSyntax Then
                Dim localDeclaration = DirectCast(node.Parent.Parent, LocalDeclarationStatementSyntax)
                classification = If(localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword), ClassificationTypeNames.ConstantName, ClassificationTypeNames.LocalName)
                Return True
            End If

            If TypeOf node.Parent.Parent Is FieldDeclarationSyntax Then
                Dim localDeclaration = DirectCast(node.Parent.Parent, FieldDeclarationSyntax)
                classification = If(localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword), ClassificationTypeNames.ConstantName, ClassificationTypeNames.FieldName)
                Return True
            End If

            Return False
        End Function

        Private Function ClassifyTypeDeclarationIdentifier(identifier As SyntaxToken) As String
            Select Case identifier.Parent.Kind
                Case SyntaxKind.ClassStatement
                    Return ClassificationTypeNames.ClassName
                Case SyntaxKind.ModuleStatement
                    Return ClassificationTypeNames.ModuleName
                Case SyntaxKind.InterfaceStatement
                    Return ClassificationTypeNames.InterfaceName
                Case SyntaxKind.StructureStatement
                    Return ClassificationTypeNames.StructName
                Case Else
                    Return Contract.FailWithReturn(Of String)("Unhandled type declaration")
            End Select
        End Function

        Friend Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Dim text2 = text.ToString(textSpan)
            Dim tokens = SyntaxFactory.ParseTokens(text2, initialTokenPosition:=textSpan.Start)
            Worker.CollectClassifiedSpans(tokens, textSpan, result, cancellationToken)
        End Sub

        Friend Function AdjustStaleClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan
            ' TODO: Do we need to do the same work here that we do in C#?
            Return classifiedSpan
        End Function
    End Module
End Namespace
