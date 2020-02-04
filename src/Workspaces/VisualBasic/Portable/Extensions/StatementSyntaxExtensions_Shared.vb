' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module StatementSyntaxExtensions
        <Extension()>
        Public Function GetMemberKeywordToken(member As DeclarationStatementSyntax) As SyntaxToken
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.ConstructorBlock
                        Return DirectCast(DirectCast(member, ConstructorBlockSyntax).BlockStatement, SubNewStatementSyntax).SubKeyword
                    Case SyntaxKind.DeclareSubStatement,
                        SyntaxKind.DeclareFunctionStatement
                        Return DirectCast(member, DeclareStatementSyntax).DeclarationKeyword
                    Case SyntaxKind.DelegateSubStatement,
                        SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(member, DelegateStatementSyntax).DeclarationKeyword
                    Case SyntaxKind.EventBlock
                        Return DirectCast(member, EventBlockSyntax).EventStatement.EventKeyword
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).EventKeyword
                    Case SyntaxKind.FunctionBlock,
                        SyntaxKind.SubBlock
                        Return DirectCast(DirectCast(member, MethodBlockSyntax).BlockStatement, MethodStatementSyntax).DeclarationKeyword
                    Case SyntaxKind.FunctionStatement,
                        SyntaxKind.SubStatement
                        Return DirectCast(member, MethodStatementSyntax).DeclarationKeyword
                    Case SyntaxKind.OperatorBlock
                        Return DirectCast(DirectCast(member, OperatorBlockSyntax).BlockStatement, OperatorStatementSyntax).OperatorKeyword
                    Case SyntaxKind.PropertyBlock
                        Return DirectCast(member, PropertyBlockSyntax).PropertyStatement.PropertyKeyword
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).PropertyKeyword
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetParameterList(member As StatementSyntax) As ParameterListSyntax
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.SubBlock,
                        SyntaxKind.FunctionBlock,
                        SyntaxKind.ConstructorBlock,
                        SyntaxKind.OperatorBlock,
                        SyntaxKind.GetAccessorBlock,
                        SyntaxKind.SetAccessorBlock,
                        SyntaxKind.AddHandlerAccessorBlock,
                        SyntaxKind.RemoveHandlerAccessorBlock,
                        SyntaxKind.RaiseEventAccessorBlock
                        Return DirectCast(member, MethodBlockBaseSyntax).BlockStatement.ParameterList
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
                        Return DirectCast(member, MethodBaseSyntax).ParameterList
                    Case SyntaxKind.PropertyBlock
                        Return DirectCast(member, PropertyBlockSyntax).PropertyStatement.ParameterList
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).ParameterList
                    Case SyntaxKind.EventBlock
                        Return DirectCast(member, EventBlockSyntax).EventStatement.ParameterList
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).ParameterList
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetAsClause(member As StatementSyntax) As AsClauseSyntax
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.FunctionBlock
                        Return DirectCast(member, MethodBlockSyntax).SubOrFunctionStatement.AsClause
                    Case SyntaxKind.OperatorBlock
                        Return DirectCast(member, OperatorBlockSyntax).OperatorStatement.AsClause
                    Case SyntaxKind.FunctionStatement
                        Return DirectCast(member, MethodStatementSyntax).AsClause
                    Case SyntaxKind.OperatorStatement
                        Return DirectCast(member, OperatorStatementSyntax).AsClause
                    Case SyntaxKind.DeclareFunctionStatement
                        Return DirectCast(member, DeclareStatementSyntax).AsClause
                    Case SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(member, DelegateStatementSyntax).AsClause
                    Case SyntaxKind.PropertyBlock
                        Return DirectCast(member, PropertyBlockSyntax).PropertyStatement.AsClause
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).AsClause
                    Case SyntaxKind.EventBlock
                        Return DirectCast(member, EventBlockSyntax).EventStatement.AsClause
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).AsClause
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetNameToken(member As StatementSyntax) As SyntaxToken
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.ClassBlock,
                        SyntaxKind.InterfaceBlock,
                        SyntaxKind.ModuleBlock,
                        SyntaxKind.StructureBlock
                        Return DirectCast(member, TypeBlockSyntax).BlockStatement.Identifier
                    Case SyntaxKind.EnumBlock
                        Return DirectCast(member, EnumBlockSyntax).EnumStatement.Identifier
                    Case SyntaxKind.ClassStatement,
                        SyntaxKind.InterfaceStatement,
                        SyntaxKind.ModuleStatement,
                        SyntaxKind.StructureStatement
                        Return DirectCast(member, TypeStatementSyntax).Identifier
                    Case SyntaxKind.EnumStatement
                        Return DirectCast(member, EnumStatementSyntax).Identifier
                    Case SyntaxKind.FieldDeclaration
                        Return DirectCast(member, FieldDeclarationSyntax).Declarators.First().Names.First().Identifier
                    Case SyntaxKind.EventBlock
                        Return DirectCast(member, EventBlockSyntax).EventStatement.Identifier
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).Identifier
                    Case SyntaxKind.PropertyBlock
                        Return DirectCast(member, PropertyBlockSyntax).PropertyStatement.Identifier
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).Identifier
                    Case SyntaxKind.FunctionBlock,
                        SyntaxKind.SubBlock
                        Return DirectCast(DirectCast(member, MethodBlockSyntax).BlockStatement, MethodStatementSyntax).Identifier
                    Case SyntaxKind.ConstructorBlock
                        Return DirectCast(DirectCast(member, ConstructorBlockSyntax).BlockStatement, SubNewStatementSyntax).NewKeyword
                    Case SyntaxKind.OperatorBlock
                        Return DirectCast(DirectCast(member, OperatorBlockSyntax).BlockStatement, OperatorStatementSyntax).OperatorToken
                    Case SyntaxKind.SubStatement,
                        SyntaxKind.FunctionStatement
                        Return DirectCast(member, MethodStatementSyntax).Identifier
                    Case SyntaxKind.DeclareSubStatement,
                        SyntaxKind.DeclareFunctionStatement
                        Return DirectCast(member, DeclareStatementSyntax).Identifier
                    Case SyntaxKind.DelegateSubStatement,
                        SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(member, DelegateStatementSyntax).Identifier
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetTypeParameterList(member As StatementSyntax) As TypeParameterListSyntax
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.ClassBlock,
                        SyntaxKind.InterfaceBlock,
                        SyntaxKind.StructureBlock
                        Return DirectCast(member, TypeBlockSyntax).BlockStatement.TypeParameterList
                    Case SyntaxKind.ClassStatement,
                        SyntaxKind.InterfaceStatement,
                        SyntaxKind.StructureStatement
                        Return DirectCast(member, TypeStatementSyntax).TypeParameterList
                    Case SyntaxKind.DelegateSubStatement,
                        SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(member, DelegateStatementSyntax).TypeParameterList
                    Case SyntaxKind.SubBlock,
                        SyntaxKind.FunctionBlock
                        Return DirectCast(DirectCast(member, MethodBlockSyntax).BlockStatement, MethodStatementSyntax).TypeParameterList
                    Case SyntaxKind.SubStatement,
                        SyntaxKind.FunctionStatement
                        Return DirectCast(member, MethodStatementSyntax).TypeParameterList
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetNextStatement(statement As StatementSyntax) As StatementSyntax
            If statement IsNot Nothing Then
                ' In VB a statement can be followed by another statement in one of two ways.  It can
                ' follow on the same line (in which case there will be a statement terminator
                ' token), or it can be on the same line with a colon between the two of them.
                Dim nextToken = statement.GetLastToken().GetNextToken()

                Dim outerStatement = statement.GetAncestors(Of StatementSyntax)().Where(Function(s) s.SpanStart <> statement.SpanStart).FirstOrDefault()
                Return nextToken.GetAncestors(Of StatementSyntax)().FirstOrDefault(Function(s) s.GetAncestors(Of StatementSyntax)().Contains(outerStatement))
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetReturnType(member As StatementSyntax) As TypeSyntax
            Dim asClause = member.GetAsClause()
            Return If(asClause IsNot Nothing, asClause.Type, Nothing)
        End Function

        <Extension()>
        Public Function HasReturnType(member As StatementSyntax) As Boolean
            Return member.GetReturnType() IsNot Nothing
        End Function

        <Extension()>
        Public Function GetModifiers(member As SyntaxNode) As SyntaxTokenList
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.ClassBlock,
                        SyntaxKind.InterfaceBlock,
                        SyntaxKind.ModuleBlock,
                        SyntaxKind.StructureBlock
                        Return DirectCast(member, TypeBlockSyntax).BlockStatement.Modifiers
                    Case SyntaxKind.EnumBlock
                        Return DirectCast(member, EnumBlockSyntax).EnumStatement.Modifiers
                    Case SyntaxKind.ClassStatement,
                        SyntaxKind.InterfaceStatement,
                        SyntaxKind.ModuleStatement,
                        SyntaxKind.StructureStatement
                        Return DirectCast(member, TypeStatementSyntax).Modifiers
                    Case SyntaxKind.EnumStatement
                        Return DirectCast(member, EnumStatementSyntax).Modifiers
                    Case SyntaxKind.FieldDeclaration
                        Return DirectCast(member, FieldDeclarationSyntax).Modifiers
                    Case SyntaxKind.EventBlock
                        Return DirectCast(member, EventBlockSyntax).EventStatement.Modifiers
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).Modifiers
                    Case SyntaxKind.PropertyBlock
                        Return DirectCast(member, PropertyBlockSyntax).PropertyStatement.Modifiers
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).Modifiers
                    Case SyntaxKind.FunctionBlock,
                        SyntaxKind.SubBlock,
                        SyntaxKind.ConstructorBlock,
                        SyntaxKind.OperatorBlock,
                        SyntaxKind.GetAccessorBlock,
                        SyntaxKind.SetAccessorBlock,
                        SyntaxKind.AddHandlerAccessorBlock,
                        SyntaxKind.RemoveHandlerAccessorBlock,
                        SyntaxKind.RaiseEventAccessorBlock
                        Return DirectCast(member, MethodBlockBaseSyntax).BlockStatement.Modifiers
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
                        Return DirectCast(member, MethodBaseSyntax).Modifiers
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function WithModifiers(Of TNode As SyntaxNode)(member As TNode, modifiers As SyntaxTokenList) As TNode
            Return DirectCast(WithModifiersHelper(member, modifiers), TNode)
        End Function

        Private Function WithModifiersHelper(member As SyntaxNode, modifiers As SyntaxTokenList) As SyntaxNode
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.ClassBlock
                        Dim classBlock = DirectCast(member, ClassBlockSyntax)
                        Return classBlock.WithClassStatement(classBlock.ClassStatement.WithModifiers(modifiers))
                    Case SyntaxKind.InterfaceBlock
                        Dim interfaceBlock = DirectCast(member, InterfaceBlockSyntax)
                        Return interfaceBlock.WithInterfaceStatement(interfaceBlock.InterfaceStatement.WithModifiers(modifiers))
                    Case SyntaxKind.ModuleBlock
                        Dim moduleBlock = DirectCast(member, ModuleBlockSyntax)
                        Return moduleBlock.WithModuleStatement(moduleBlock.ModuleStatement.WithModifiers(modifiers))
                    Case SyntaxKind.StructureBlock
                        Dim structureBlock = DirectCast(member, StructureBlockSyntax)
                        Return structureBlock.WithStructureStatement(structureBlock.StructureStatement.WithModifiers(modifiers))
                    Case SyntaxKind.EnumBlock
                        Dim enumBlock = DirectCast(member, EnumBlockSyntax)
                        Return enumBlock.WithEnumStatement(enumBlock.EnumStatement.WithModifiers(modifiers))
                    Case SyntaxKind.ClassStatement
                        Return DirectCast(member, ClassStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.InterfaceStatement
                        Return DirectCast(member, InterfaceStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.ModuleStatement
                        Return DirectCast(member, ModuleStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.StructureStatement
                        Return DirectCast(member, StructureStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.EnumStatement
                        Return DirectCast(member, EnumStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.FieldDeclaration
                        Return DirectCast(member, FieldDeclarationSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.EventBlock
                        Dim eventBlock = DirectCast(member, EventBlockSyntax)
                        Return eventBlock.WithEventStatement(eventBlock.EventStatement.WithModifiers(modifiers))
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.PropertyBlock
                        Dim propertyBlock = DirectCast(member, PropertyBlockSyntax)
                        Return propertyBlock.WithPropertyStatement(propertyBlock.PropertyStatement.WithModifiers(modifiers))
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.FunctionBlock,
                        SyntaxKind.SubBlock
                        Dim methodBlock = DirectCast(member, MethodBlockSyntax)
                        Return methodBlock.WithSubOrFunctionStatement(DirectCast(methodBlock.SubOrFunctionStatement.WithModifiers(modifiers), MethodStatementSyntax))
                    Case SyntaxKind.ConstructorBlock
                        Dim methodBlock = DirectCast(member, ConstructorBlockSyntax)
                        Return methodBlock.WithSubNewStatement(DirectCast(methodBlock.SubNewStatement.WithModifiers(modifiers), SubNewStatementSyntax))
                    Case SyntaxKind.OperatorBlock
                        Dim methodBlock = DirectCast(member, OperatorBlockSyntax)
                        Return methodBlock.WithOperatorStatement(DirectCast(methodBlock.OperatorStatement.WithModifiers(modifiers), OperatorStatementSyntax))
                    Case SyntaxKind.GetAccessorBlock,
                        SyntaxKind.SetAccessorBlock,
                        SyntaxKind.AddHandlerAccessorBlock,
                        SyntaxKind.RemoveHandlerAccessorBlock,
                        SyntaxKind.RaiseEventAccessorBlock
                        Dim methodBlock = DirectCast(member, AccessorBlockSyntax)
                        Return methodBlock.WithAccessorStatement(DirectCast(methodBlock.AccessorStatement.WithModifiers(modifiers), AccessorStatementSyntax))
                    Case SyntaxKind.SubStatement,
                        SyntaxKind.FunctionStatement
                        Return DirectCast(member, MethodStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.SubNewStatement
                        Return DirectCast(member, SubNewStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.OperatorStatement
                        Return DirectCast(member, OperatorStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.GetAccessorStatement,
                        SyntaxKind.SetAccessorStatement,
                        SyntaxKind.AddHandlerAccessorStatement,
                        SyntaxKind.RemoveHandlerAccessorStatement,
                        SyntaxKind.RaiseEventAccessorStatement
                        Return DirectCast(member, AccessorStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.DeclareSubStatement,
                        SyntaxKind.DeclareFunctionStatement
                        Return DirectCast(member, DeclareStatementSyntax).WithModifiers(modifiers)
                    Case SyntaxKind.DelegateSubStatement,
                        SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(member, DelegateStatementSyntax).WithModifiers(modifiers)
                End Select
            End If

            Return Nothing
        End Function

    End Module
End Namespace
