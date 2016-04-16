' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Class VisualBasicDeclarationComputer
        Inherits DeclarationComputer

        Public Shared Sub ComputeDeclarationsInSpan(model As SemanticModel, span As TextSpan, getSymbol As Boolean, builder As List(Of DeclarationInfo), cancellationToken As CancellationToken)
            ComputeDeclarationsCore(model, model.SyntaxTree.GetRoot(),
                                    Function(node, level) Not node.Span.OverlapsWith(span) OrElse InvalidLevel(level),
                                    getSymbol, builder, Nothing, cancellationToken)
        End Sub

        Public Shared Sub ComputeDeclarationsInNode(model As SemanticModel, node As SyntaxNode, getSymbol As Boolean, builder As List(Of DeclarationInfo), cancellationToken As CancellationToken, Optional levelsToCompute As Integer? = Nothing)
            ComputeDeclarationsCore(model, node, Function(n, level) InvalidLevel(level), getSymbol, builder, levelsToCompute, cancellationToken)
        End Sub

        Private Shared Function InvalidLevel(level As Integer?) As Boolean
            Return level.HasValue AndAlso level.Value <= 0
        End Function


        Private Shared Function DecrementLevel(level As Integer?) As Integer?
            Return If(level.HasValue, level.Value - 1, level)
        End Function

        Private Shared Sub ComputeDeclarationsCore(model As SemanticModel, node As SyntaxNode, shouldSkip As Func(Of SyntaxNode, Integer?, Boolean), getSymbol As Boolean, builder As List(Of DeclarationInfo), levelsToCompute As Integer?, cancellationToken As CancellationToken)
            cancellationToken.ThrowIfCancellationRequested()

            If shouldSkip(node, levelsToCompute) Then
                Return
            End If

            Dim newLevel = DecrementLevel(levelsToCompute)

            Select Case node.Kind()
                Case SyntaxKind.NamespaceBlock
                    Dim ns = CType(node, NamespaceBlockSyntax)
                    For Each decl In ns.Members
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim declInfo = GetDeclarationInfo(model, node, getSymbol, cancellationToken)
                    builder.Add(declInfo)

                    Dim name = ns.NamespaceStatement.Name
                    Dim nsSymbol = declInfo.DeclaredSymbol
                    While (name.Kind() = SyntaxKind.QualifiedName)
                        name = (CType(name, QualifiedNameSyntax)).Left
                        Dim declaredSymbol = If(getSymbol, nsSymbol?.ContainingNamespace, Nothing)
                        builder.Add(New DeclarationInfo(name, ImmutableArray(Of SyntaxNode).Empty, declaredSymbol))
                        nsSymbol = declaredSymbol
                    End While

                    Return
                Case SyntaxKind.EnumBlock
                    Dim t = CType(node, EnumBlockSyntax)
                    For Each decl In t.Members
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken))
                    Return
                Case SyntaxKind.EnumMemberDeclaration
                    Dim t = CType(node, EnumMemberDeclarationSyntax)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, t.Initializer, cancellationToken))
                    Return
                Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                    Dim t = CType(node, DelegateStatementSyntax)
                    Dim paramInitializers As IEnumerable(Of SyntaxNode) = GetParameterInitializers(t.ParameterList)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, paramInitializers, cancellationToken))
                    Return
                Case SyntaxKind.EventBlock
                    Dim t = CType(node, EventBlockSyntax)
                    For Each decl In t.Accessors
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim eventInitializers = GetParameterInitializers(t.EventStatement.ParameterList)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, eventInitializers, cancellationToken))
                    Return
                Case SyntaxKind.EventStatement
                    Dim t = CType(node, EventStatementSyntax)
                    Dim paramInitializers = GetParameterInitializers(t.ParameterList)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, paramInitializers, cancellationToken))
                    Return
                Case SyntaxKind.FieldDeclaration
                    Dim t = CType(node, FieldDeclarationSyntax)
                    For Each decl In t.Declarators
                        Dim initializer = GetInitializerNode(decl)
                        For Each identifier In decl.Names
                            builder.Add(GetDeclarationInfo(model, identifier, getSymbol, initializer, cancellationToken))
                        Next
                    Next
                    Return
                Case SyntaxKind.PropertyBlock
                    Dim t = CType(node, PropertyBlockSyntax)
                    For Each decl In t.Accessors
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim propertyInitializers = GetInitializerNodes(t.PropertyStatement)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, propertyInitializers, cancellationToken))
                    Return
                Case SyntaxKind.PropertyStatement
                    Dim t = CType(node, PropertyStatementSyntax)
                    Dim propertyInitializers = GetInitializerNodes(t)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, propertyInitializers, cancellationToken))
                    Return
                Case SyntaxKind.CompilationUnit
                    Dim t = CType(node, CompilationUnitSyntax)
                    For Each decl In t.Members
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Return
                Case Else
                    Dim typeBlock = TryCast(node, TypeBlockSyntax)
                    If typeBlock IsNot Nothing Then
                        For Each decl In typeBlock.Members
                            ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                        Next
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken))
                        Return
                    End If

                    Dim typeStatement = TryCast(node, TypeStatementSyntax)
                    If typeStatement IsNot Nothing Then
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken))
                        Return
                    End If

                    Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
                    If methodBlock IsNot Nothing Then
                        Dim paramInitializers = GetParameterInitializers(methodBlock.BlockStatement.ParameterList)
                        Dim codeBlocks = paramInitializers.Concat(methodBlock.Statements).Concat(methodBlock.EndBlockStatement)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                        Return
                    End If

                    Dim methodStatement = TryCast(node, MethodBaseSyntax)
                    If methodStatement IsNot Nothing Then
                        Dim paramInitializers = GetParameterInitializers(methodStatement.ParameterList)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, paramInitializers, cancellationToken))
                        Return
                    End If

                    Return
            End Select
        End Sub

        Private Shared Function GetParameterInitializers(parameterList As ParameterListSyntax) As IEnumerable(Of SyntaxNode)
            Return If(parameterList IsNot Nothing,
                parameterList.Parameters.Select(Function(p) p.Default),
                SpecializedCollections.EmptyEnumerable(Of SyntaxNode))
        End Function

        Private Shared Function GetInitializerNodes(propertyStatement As PropertyStatementSyntax) As IEnumerable(Of SyntaxNode)
            Dim parameterInitializers = GetParameterInitializers(propertyStatement.ParameterList)
            Dim initializer As SyntaxNode = propertyStatement.Initializer
            If initializer Is Nothing Then
                initializer = GetAsNewClauseIntializer(propertyStatement.AsClause)
            End If
            Return parameterInitializers.Concat(initializer)
        End Function

        Private Shared Function GetInitializerNode(variableDeclarator As VariableDeclaratorSyntax) As SyntaxNode
            Dim initializer As SyntaxNode = variableDeclarator.Initializer
            If initializer Is Nothing Then
                initializer = GetAsNewClauseIntializer(variableDeclarator.AsClause)
            End If

            Return initializer
        End Function

        Private Shared Function GetAsNewClauseIntializer(asClause As AsClauseSyntax) As SyntaxNode
            ' The As New clause itself is necessary rather than the embedded New expression, so that the
            ' code block associated with the declaration appears as an initializer for the purposes
            ' of executing analyzer actions.
            Return If(asClause.IsKind(SyntaxKind.AsNewClause), asClause, Nothing)
        End Function
    End Class
End Namespace
