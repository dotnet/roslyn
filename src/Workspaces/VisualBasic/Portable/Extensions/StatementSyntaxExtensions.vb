' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
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
        Public Function GetModifiers(member As StatementSyntax) As IEnumerable(Of SyntaxToken)
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

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxToken)()
        End Function

        <Extension()>
        Public Function WithModifiers(member As StatementSyntax, modifiers As SyntaxTokenList) As StatementSyntax
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

        <Extension()>
        Public Function GetNameToken(member As DeclarationStatementSyntax) As SyntaxToken
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
        Public Function GetArity(member As StatementSyntax) As Integer
            Dim list = GetTypeParameterList(member)
            Return If(list Is Nothing, 0, list.Parameters.Count)
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
        Public Function GetReturnType(member As StatementSyntax) As TypeSyntax
            Dim asClause = member.GetAsClause()
            Return If(asClause IsNot Nothing, asClause.Type, Nothing)
        End Function

        <Extension()>
        Public Function HasReturnType(member As StatementSyntax) As Boolean
            Return member.GetReturnType() IsNot Nothing
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
                ' if foo
                '
                ' This is a MultiLineIfBlock (a statement), with an IfPart (not a statement) with an
                ' IfStatement (a statement).
                '
                ' We want to find the thing that contains the 'if foo' statement that really isn't
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
