' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module StatementSyntaxExtensions
        <Extension()>
        Public Function GetAttributes(member As StatementSyntax) As SyntaxList(Of AttributeListSyntax)
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.ClassBlock,
                        SyntaxKind.InterfaceBlock,
                        SyntaxKind.ModuleBlock,
                        SyntaxKind.StructureBlock
                        Return DirectCast(member, TypeBlockSyntax).BlockStatement.AttributeLists
                    Case SyntaxKind.EnumBlock
                        Return DirectCast(member, EnumBlockSyntax).EnumStatement.AttributeLists
                    Case SyntaxKind.ClassStatement,
                        SyntaxKind.InterfaceStatement,
                        SyntaxKind.ModuleStatement,
                        SyntaxKind.StructureStatement
                        Return DirectCast(member, TypeStatementSyntax).AttributeLists
                    Case SyntaxKind.EnumStatement
                        Return DirectCast(member, EnumStatementSyntax).AttributeLists
                    Case SyntaxKind.EnumMemberDeclaration
                        Return DirectCast(member, EnumMemberDeclarationSyntax).AttributeLists
                    Case SyntaxKind.FieldDeclaration
                        Return DirectCast(member, FieldDeclarationSyntax).AttributeLists
                    Case SyntaxKind.EventBlock
                        Return DirectCast(member, EventBlockSyntax).EventStatement.AttributeLists
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).AttributeLists
                    Case SyntaxKind.PropertyBlock
                        Return DirectCast(member, PropertyBlockSyntax).PropertyStatement.AttributeLists
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).AttributeLists
                    Case SyntaxKind.FunctionBlock,
                        SyntaxKind.SubBlock,
                        SyntaxKind.ConstructorBlock,
                        SyntaxKind.OperatorBlock,
                        SyntaxKind.GetAccessorBlock,
                        SyntaxKind.SetAccessorBlock,
                        SyntaxKind.AddHandlerAccessorBlock,
                        SyntaxKind.RemoveHandlerAccessorBlock,
                        SyntaxKind.RaiseEventAccessorBlock
                        Return DirectCast(member, MethodBlockBaseSyntax).BlockStatement.AttributeLists
                    Case SyntaxKind.SubStatement,
                        SyntaxKind.FunctionStatement,
                        SyntaxKind.SubNewStatement,
                        SyntaxKind.OperatorStatement,
                        SyntaxKind.GetAccessorStatement,
                        SyntaxKind.SetAccessorStatement,
                        SyntaxKind.AddHandlerAccessorStatement,
                        SyntaxKind.RemoveHandlerAccessorStatement,
                        SyntaxKind.RaiseEventAccessorStatement,
                        SyntaxKind.DeclareSubStatement,
                        SyntaxKind.DeclareFunctionStatement,
                        SyntaxKind.DelegateSubStatement,
                        SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(member, MethodBaseSyntax).AttributeLists
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function WithAttributeLists(member As StatementSyntax, attributeLists As SyntaxList(Of AttributeListSyntax)) As StatementSyntax
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.ClassBlock,
                        SyntaxKind.InterfaceBlock,
                        SyntaxKind.ModuleBlock,
                        SyntaxKind.StructureBlock
                        Dim typeBlock = DirectCast(member, TypeBlockSyntax)
                        Dim newBegin = DirectCast(typeBlock.BlockStatement.WithAttributeLists(attributeLists), TypeStatementSyntax)
                        Return typeBlock.WithBlockStatement(newBegin)
                    Case SyntaxKind.EnumBlock
                        Dim enumBlock = DirectCast(member, EnumBlockSyntax)
                        Dim newEnumStatement = enumBlock.EnumStatement.WithAttributeLists(attributeLists)
                        Return enumBlock.WithEnumStatement(newEnumStatement)
                    Case SyntaxKind.ClassStatement
                        Return DirectCast(member, ClassStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.InterfaceStatement
                        Return DirectCast(member, InterfaceStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.ModuleStatement
                        Return DirectCast(member, ModuleStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.StructureStatement
                        Return DirectCast(member, StructureStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.EnumStatement
                        Return DirectCast(member, EnumStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.EnumMemberDeclaration
                        Return DirectCast(member, EnumMemberDeclarationSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.FieldDeclaration
                        Return DirectCast(member, FieldDeclarationSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.EventBlock
                        Dim eventBlock = DirectCast(member, EventBlockSyntax)
                        Dim newEventStatement = DirectCast(eventBlock.EventStatement.WithAttributeLists(attributeLists), EventStatementSyntax)
                        Return eventBlock.WithEventStatement(newEventStatement)
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.PropertyBlock
                        Dim propertyBlock = DirectCast(member, PropertyBlockSyntax)
                        Dim newPropertyStatement = propertyBlock.PropertyStatement.WithAttributeLists(attributeLists)
                        Return propertyBlock.WithPropertyStatement(newPropertyStatement)
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.FunctionBlock,
                        SyntaxKind.SubBlock,
                        SyntaxKind.ConstructorBlock,
                        SyntaxKind.OperatorBlock,
                        SyntaxKind.GetAccessorBlock,
                        SyntaxKind.SetAccessorBlock,
                        SyntaxKind.AddHandlerAccessorBlock,
                        SyntaxKind.RemoveHandlerAccessorBlock,
                        SyntaxKind.RaiseEventAccessorBlock
                        Dim methodBlock = DirectCast(member, MethodBlockBaseSyntax)
                        Dim newBegin = methodBlock.BlockStatement.WithAttributeLists(attributeLists)
                        Return methodBlock.ReplaceNode(methodBlock.BlockStatement, newBegin)
                    Case SyntaxKind.SubStatement,
                        SyntaxKind.FunctionStatement
                        Return DirectCast(member, MethodStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.SubNewStatement
                        Return DirectCast(member, SubNewStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.OperatorStatement
                        Return DirectCast(member, OperatorStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.GetAccessorStatement,
                         SyntaxKind.SetAccessorStatement,
                         SyntaxKind.AddHandlerAccessorStatement,
                         SyntaxKind.RemoveHandlerAccessorStatement,
                         SyntaxKind.RaiseEventAccessorStatement
                        Return DirectCast(member, AccessorStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement
                        Return DirectCast(member, DeclareStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.DelegateSubStatement,
                        SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(member, DelegateStatementSyntax).WithAttributeLists(attributeLists)
                    Case SyntaxKind.IncompleteMember
                        Return DirectCast(member, IncompleteMemberSyntax).WithAttributeLists(attributeLists)
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function AddAttributeLists(member As StatementSyntax, ParamArray attributeLists As AttributeListSyntax()) As StatementSyntax
            Return member.WithAttributeLists(member.GetAttributes().AddRange(attributeLists))
        End Function

        <Extension()>
        Public Function GetArity(member As StatementSyntax) As Integer
            Dim list = GetTypeParameterList(member)
            Return If(list Is Nothing, 0, list.Parameters.Count)
        End Function

        <Extension()>
        Public Function IsTopLevelDeclaration(statement As StatementSyntax) As Boolean
            If statement Is Nothing Then
                Return False
            End If

            Select Case statement.Kind
                Case SyntaxKind.NamespaceStatement,
                    SyntaxKind.ClassStatement,
                    SyntaxKind.InterfaceStatement,
                    SyntaxKind.ModuleStatement,
                    SyntaxKind.StructureStatement,
                    SyntaxKind.EnumStatement,
                    SyntaxKind.EnumMemberDeclaration,
                    SyntaxKind.DelegateSubStatement,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.FieldDeclaration,
                    SyntaxKind.EventStatement,
                    SyntaxKind.PropertyStatement,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.SubStatement,
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.DeclareFunctionStatement,
                    SyntaxKind.DeclareSubStatement
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsTopLevelBlock(statement As StatementSyntax) As Boolean
            If statement Is Nothing Then
                Return False
            End If

            Select Case statement.Kind
                Case SyntaxKind.NamespaceBlock,
                    SyntaxKind.ClassBlock,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.ModuleBlock,
                    SyntaxKind.StructureBlock,
                    SyntaxKind.EnumBlock,
                    SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.EventBlock
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function GetTopLevelBlockBegin(statement As StatementSyntax) As DeclarationStatementSyntax
            If statement Is Nothing Then
                Return Nothing
            End If

            If statement.IsMemberDeclaration() Then
                Return DirectCast(statement, DeclarationStatementSyntax)
            End If

            Select Case statement.Kind
                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(statement, NamespaceBlockSyntax).NamespaceStatement
                Case SyntaxKind.ClassBlock,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.ModuleBlock,
                    SyntaxKind.StructureBlock
                    Return DirectCast(statement, TypeBlockSyntax).BlockStatement
                Case SyntaxKind.EnumBlock
                    Return DirectCast(statement, EnumBlockSyntax).EnumStatement
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.OperatorBlock
                    Return DirectCast(statement, MethodBlockBaseSyntax).BlockStatement
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(statement, PropertyBlockSyntax).PropertyStatement
                Case SyntaxKind.EventBlock
                    Return DirectCast(statement, EventBlockSyntax).EventStatement
                Case Else
                    Return Nothing
            End Select
        End Function

        <Extension()>
        Public Function IsMemberDeclaration(statement As StatementSyntax) As Boolean
            If statement Is Nothing Then
                Return False
            End If

            Select Case statement.Kind
                Case SyntaxKind.ClassStatement,
                    SyntaxKind.InterfaceStatement,
                    SyntaxKind.ModuleStatement,
                    SyntaxKind.StructureStatement,
                    SyntaxKind.EnumStatement,
                    SyntaxKind.EnumMemberDeclaration,
                    SyntaxKind.DelegateSubStatement,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.FieldDeclaration,
                    SyntaxKind.EventStatement,
                    SyntaxKind.PropertyStatement,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.SubStatement,
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.DeclareFunctionStatement,
                    SyntaxKind.DeclareSubStatement
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsMemberBlock(statement As StatementSyntax) As Boolean
            If statement Is Nothing Then
                Return False
            End If

            Select Case statement.Kind
                Case SyntaxKind.ClassBlock,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.ModuleBlock,
                    SyntaxKind.StructureBlock,
                    SyntaxKind.EnumBlock,
                    SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.EventBlock
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function GetMemberBlockBegin(statement As StatementSyntax) As DeclarationStatementSyntax
            If statement Is Nothing Then
                Return Nothing
            End If

            If statement.IsMemberDeclaration() Then
                Return DirectCast(statement, DeclarationStatementSyntax)
            End If

            Select Case statement.Kind
                Case SyntaxKind.ClassBlock,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.ModuleBlock,
                    SyntaxKind.StructureBlock
                    Return DirectCast(statement, TypeBlockSyntax).BlockStatement
                Case SyntaxKind.EnumBlock
                    Return DirectCast(statement, EnumBlockSyntax).EnumStatement
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.OperatorBlock
                    Return DirectCast(statement, MethodBlockBaseSyntax).BlockStatement
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(statement, PropertyBlockSyntax).PropertyStatement
                Case SyntaxKind.EventBlock
                    Return DirectCast(statement, EventBlockSyntax).EventStatement
                Case Else
                    Return Nothing
            End Select
        End Function

        <Extension()>
        Public Function GetPreviousStatement(statement As StatementSyntax) As StatementSyntax
            If statement IsNot Nothing Then
                ' VB statements are *really* weird.  Take, for example:
                '
                ' if goo
                '
                ' This is a MultiLineIfBlock (a statement), with an IfPart (not a statement) with an
                ' IfStatement (a statement).
                '
                ' We want to find the thing that contains the 'if goo' statement that really isn't
                ' the MultiLineIfBlock.  So we find the first ancestor statement that does *not* have
                ' the same starting position as the statement we're on.  
                Dim outerStatement = statement.GetAncestors(Of StatementSyntax)().Where(Function(s) s.SpanStart <> statement.SpanStart).FirstOrDefault()

                ' Also, when we look backward, we will have separated lists with terminator tokens
                ' (bleagh).  So we skip those as well.
                Dim previousToken = statement.GetFirstToken().GetPreviousToken()

                ' Now, given the end token of the previous statement, walk up it until we a
                ' statement that is one below the outer statement we found.  This is our previous
                ' sibling statement.
                Return previousToken.GetAncestors(Of StatementSyntax)().FirstOrDefault(Function(s) s.GetAncestors(Of StatementSyntax)().Contains(outerStatement))
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetNextNonEmptyStatement(statement As StatementSyntax) As StatementSyntax
            If statement IsNot Nothing Then
                Dim nextToken = statement.GetLastToken().GetNextToken()

                If nextToken.Kind = SyntaxKind.None Then
                    Return Nothing
                End If

                Return nextToken.Parent.FirstAncestorOrSelf(Of StatementSyntax)()
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function IsConstructorInitializer(statement As StatementSyntax) As Boolean
            If statement.IsParentKind(SyntaxKind.ConstructorBlock) AndAlso
               DirectCast(statement.Parent, ConstructorBlockSyntax).Statements.FirstOrDefault() Is statement Then

                Dim invocation As ExpressionSyntax
                If statement.IsKind(SyntaxKind.CallStatement) Then
                    invocation = DirectCast(statement, CallStatementSyntax).Invocation
                ElseIf statement.IsKind(SyntaxKind.ExpressionStatement) Then
                    invocation = DirectCast(statement, ExpressionStatementSyntax).Expression
                Else
                    Return False
                End If

                Dim expression As ExpressionSyntax = Nothing
                If invocation.IsKind(SyntaxKind.InvocationExpression) Then
                    expression = DirectCast(invocation, InvocationExpressionSyntax).Expression
                ElseIf invocation.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                    expression = invocation
                End If

                If expression IsNot Nothing Then
                    If expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                        Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)

                        Return memberAccess.IsConstructorInitializer()
                    End If
                End If
            End If

            Return False
        End Function
    End Module
End Namespace
