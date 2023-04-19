' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Partial Friend Class VisualBasicCodeModelService

        Protected Overrides Function CreateNodeNameGenerator() As AbstractNodeNameGenerator
            Return New NodeNameGenerator()
        End Function

        Private Class NodeNameGenerator
            Inherits AbstractNodeNameGenerator

            Protected Overrides Function IsNameableNode(node As SyntaxNode) As Boolean
                Return VisualBasicCodeModelService.IsNameableNode(node)
            End Function

            Private Shared Sub AppendName(builder As StringBuilder, name As NameSyntax)
                If name.Kind = SyntaxKind.QualifiedName Then
                    AppendName(builder, DirectCast(name, QualifiedNameSyntax).Left)
                End If

                Select Case name.Kind
                    Case SyntaxKind.IdentifierName
                        AppendDotIfNeeded(builder)
                        builder.Append(DirectCast(name, IdentifierNameSyntax).Identifier.ValueText)

                    Case SyntaxKind.GenericName
                        Dim genericName = DirectCast(name, GenericNameSyntax)
                        AppendDotIfNeeded(builder)
                        builder.Append(genericName.Identifier.ValueText)
                        AppendArity(builder, genericName.Arity)

                    Case SyntaxKind.QualifiedName
                        AppendName(builder, DirectCast(name, QualifiedNameSyntax).Right)

                        ' TODO(DustinCa): Is SyntaxKind.GlobalName needed?
                End Select
            End Sub

            Private Shared Sub AppendTypeName(builder As StringBuilder, type As TypeSyntax)
                If TypeOf type Is NameSyntax Then
                    AppendName(builder, DirectCast(type, NameSyntax))
                Else
                    Select Case type.Kind
                        Case SyntaxKind.PredefinedType
                            builder.Append(DirectCast(type, PredefinedTypeSyntax).Keyword.ToString())

                        Case SyntaxKind.ArrayType
                            Dim arrayType = DirectCast(type, ArrayTypeSyntax)
                            AppendTypeName(builder, arrayType.ElementType)

                            Dim specifiers = arrayType.RankSpecifiers
                            For i = 0 To specifiers.Count - 1
                                builder.Append("("c)

                                Dim specifier = specifiers(i)
                                If specifier.CommaTokens.Count > 0 Then
                                    builder.Append(","c, specifier.CommaTokens.Count)
                                End If

                                builder.Append(")"c)
                            Next

                        Case SyntaxKind.NullableType
                            AppendTypeName(builder, DirectCast(type, NullableTypeSyntax).ElementType)
                            builder.Append("?"c)
                    End Select
                End If
            End Sub

            Private Shared Sub AppendParameterList(builder As StringBuilder, parameterList As ParameterListSyntax)
                builder.Append("("c)

                If parameterList IsNot Nothing Then
                    Dim firstSeen = False

                    For Each parameter In parameterList.Parameters
                        If firstSeen Then
                            builder.Append(", ")
                        End If

                        If parameter.Modifiers.Any(SyntaxKind.ByRefKeyword) Then
                            builder.Append("ByRef ")
                        ElseIf parameter.Modifiers.Any(SyntaxKind.ParamArrayKeyword) Then
                            builder.Append("ParamArray ")
                        End If

                        ' We include the name here because it might have type information
                        ' associated with it (e.g. rank specifiers or type suffix characters).
                        builder.Append(parameter.Identifier.ToString())

                        If parameter.AsClause IsNot Nothing Then
                            builder.Append(" As ")
                            AppendTypeName(builder, parameter.AsClause.Type)
                        End If

                        firstSeen = True
                    Next
                End If

                builder.Append(")"c)
            End Sub

            Private Shared Sub AppendOperatorName(builder As StringBuilder, kind As SyntaxKind)
                Dim name = "#op_" & kind.ToString()
                If name.EndsWith("Keyword", StringComparison.Ordinal) Then
                    name = name.Substring(0, name.Length - 7)
                ElseIf name.EndsWith("Token", StringComparison.Ordinal) Then
                    name = name.Substring(0, name.Length - 5)
                End If

                builder.Append(name)
            End Sub

            Protected Overrides Sub AppendNodeName(builder As StringBuilder, node As SyntaxNode)
                Debug.Assert(node IsNot Nothing)
                Debug.Assert(IsNameableNode(node))

                AppendDotIfNeeded(builder)

                Select Case node.Kind
                    Case SyntaxKind.NamespaceBlock
                        Dim namespaceBlock = DirectCast(node, NamespaceBlockSyntax)
                        AppendName(builder, namespaceBlock.NamespaceStatement.Name)

                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.ModuleBlock

                        Dim typeBlock = DirectCast(node, TypeBlockSyntax)
                        Dim typeStatement = typeBlock.BlockStatement
                        builder.Append(typeStatement.Identifier.ValueText)
                        If typeStatement.TypeParameterList IsNot Nothing Then
                            AppendArity(builder, typeStatement.TypeParameterList.Parameters.Count)
                        End If

                    Case SyntaxKind.EnumBlock

                        Dim enumBlock = DirectCast(node, EnumBlockSyntax)
                        builder.Append(enumBlock.EnumStatement.Identifier.ValueText)

                    Case SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement

                        Dim delegateStatement = DirectCast(node, DelegateStatementSyntax)
                        builder.Append(delegateStatement.Identifier.ValueText)
                        If delegateStatement.TypeParameterList IsNot Nothing Then
                            AppendArity(builder, delegateStatement.TypeParameterList.Parameters.Count)
                        End If

                    Case SyntaxKind.EnumMemberDeclaration
                        Dim enumMemberDeclaration = DirectCast(node, EnumMemberDeclarationSyntax)
                        builder.Append(enumMemberDeclaration.Identifier.ValueText)

                    Case SyntaxKind.ModifiedIdentifier
                        Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)
                        builder.Append(modifiedIdentifier.Identifier.ValueText)

                    Case SyntaxKind.FunctionBlock,
                         SyntaxKind.SubBlock

                        Dim methodBlock = DirectCast(node, MethodBlockSyntax)
                        Dim methodStatement = DirectCast(methodBlock.BlockStatement, MethodStatementSyntax)
                        builder.Append(methodStatement.Identifier.ValueText)
                        If methodStatement.TypeParameterList IsNot Nothing Then
                            AppendArity(builder, methodStatement.TypeParameterList.Parameters.Count)
                        End If

                        AppendParameterList(builder, methodStatement.ParameterList)

                    Case SyntaxKind.FunctionStatement,
                         SyntaxKind.SubStatement

                        Dim methodStatement = DirectCast(node, MethodStatementSyntax)
                        builder.Append(methodStatement.Identifier.ValueText)
                        If methodStatement.TypeParameterList IsNot Nothing Then
                            AppendArity(builder, methodStatement.TypeParameterList.Parameters.Count)
                        End If

                        AppendParameterList(builder, methodStatement.ParameterList)

                    Case SyntaxKind.DeclareFunctionStatement,
                         SyntaxKind.DeclareSubStatement

                        Dim declareStatement = DirectCast(node, DeclareStatementSyntax)
                        builder.Append(declareStatement.Identifier.ValueText)
                        AppendParameterList(builder, declareStatement.ParameterList)

                    Case SyntaxKind.OperatorBlock

                        Dim methodBlock = DirectCast(node, OperatorBlockSyntax)
                        Dim operatorStatement = DirectCast(methodBlock.BlockStatement, OperatorStatementSyntax)
                        AppendOperatorName(builder, operatorStatement.OperatorToken.Kind)
                        If operatorStatement.AsClause IsNot Nothing AndAlso
                          (operatorStatement.Modifiers.Any(SyntaxKind.NarrowingKeyword) OrElse
                           operatorStatement.Modifiers.Any(SyntaxKind.WideningKeyword)) Then

                            builder.Append("_"c)
                            AppendTypeName(builder, DirectCast(operatorStatement.AsClause, SimpleAsClauseSyntax).Type)
                        End If

                        AppendParameterList(builder, operatorStatement.ParameterList)

                    Case SyntaxKind.ConstructorBlock

                        Dim methodBlock = DirectCast(node, ConstructorBlockSyntax)
                        Dim constructorStatement = DirectCast(methodBlock.BlockStatement, SubNewStatementSyntax)
                        builder.Append(If(constructorStatement.Modifiers.Any(SyntaxKind.SharedKeyword), "#sctor", "#ctor"))

                    Case SyntaxKind.PropertyBlock
                        Dim propertyBlock = DirectCast(node, PropertyBlockSyntax)
                        Dim propertyStatement = propertyBlock.PropertyStatement
                        builder.Append(propertyStatement.Identifier.ValueText)

                        If propertyStatement.ParameterList IsNot Nothing Then
                            AppendParameterList(builder, propertyStatement.ParameterList)
                        End If

                        If propertyStatement.AsClause IsNot Nothing Then
                            builder.Append(" As ")
                            AppendTypeName(builder, propertyStatement.AsClause.Type())
                        End If

                    Case SyntaxKind.PropertyStatement
                        Dim propertyStatement = DirectCast(node, PropertyStatementSyntax)
                        builder.Append(propertyStatement.Identifier.ValueText)

                        If propertyStatement.ParameterList IsNot Nothing Then
                            AppendParameterList(builder, propertyStatement.ParameterList)
                        End If

                        If propertyStatement.AsClause IsNot Nothing Then
                            builder.Append(" As ")
                            AppendTypeName(builder, propertyStatement.AsClause.Type())
                        End If

                    Case SyntaxKind.EventBlock
                        Dim eventBlock = DirectCast(node, EventBlockSyntax)
                        Dim eventStatement = eventBlock.EventStatement
                        builder.Append(eventStatement.Identifier.ValueText)

                        If eventStatement.ParameterList IsNot Nothing Then
                            AppendParameterList(builder, eventStatement.ParameterList)
                        End If

                        If eventStatement.AsClause IsNot Nothing Then
                            builder.Append(" As ")
                            AppendTypeName(builder, eventStatement.AsClause.Type())
                        End If

                    Case SyntaxKind.EventStatement
                        Dim eventStatement = DirectCast(node, EventStatementSyntax)
                        builder.Append(eventStatement.Identifier.ValueText)

                        If eventStatement.ParameterList IsNot Nothing Then
                            AppendParameterList(builder, eventStatement.ParameterList)
                        End If

                        If eventStatement.AsClause IsNot Nothing Then
                            builder.Append(" As ")
                            AppendTypeName(builder, eventStatement.AsClause.Type())
                        End If
                End Select
            End Sub

        End Class
    End Class
End Namespace
