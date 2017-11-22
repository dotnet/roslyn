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
                    Dim attributes = GetAttributes(t.EnumStatement.AttributeLists)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                    Return
                Case SyntaxKind.EnumStatement
                    Dim t = CType(node, EnumStatementSyntax)
                    Dim attributes = GetAttributes(t.AttributeLists)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                    Return
                Case SyntaxKind.EnumMemberDeclaration
                    Dim t = CType(node, EnumMemberDeclarationSyntax)
                    Dim attributes = GetAttributes(t.AttributeLists)
                    Dim codeBlocks = SpecializedCollections.SingletonEnumerable(Of SyntaxNode)(t.Initializer).Concat(attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                    Dim t = CType(node, DelegateStatementSyntax)
                    Dim paramInitializers As IEnumerable(Of SyntaxNode) = GetParameterListInitializersAndAttributes(t.ParameterList)
                    Dim attributes = GetAttributes(t.AttributeLists)
                    Dim codeBlocks = paramInitializers.Concat(attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.EventBlock
                    Dim t = CType(node, EventBlockSyntax)
                    For Each decl In t.Accessors
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim eventInitializers = GetParameterListInitializersAndAttributes(t.EventStatement.ParameterList)
                    Dim attributes = GetAttributes(t.EventStatement.AttributeLists)
                    Dim codeBlocks = eventInitializers.Concat(attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.EventStatement
                    Dim t = CType(node, EventStatementSyntax)
                    Dim paramInitializers = GetParameterListInitializersAndAttributes(t.ParameterList)
                    Dim attributes = GetAttributes(t.AttributeLists)
                    Dim codeBlocks = paramInitializers.Concat(attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.FieldDeclaration
                    Dim t = CType(node, FieldDeclarationSyntax)
                    Dim attributes = GetAttributes(t.AttributeLists)
                    For Each decl In t.Declarators
                        Dim initializer = GetInitializerNode(decl)
                        Dim codeBlocks = SpecializedCollections.SingletonEnumerable(initializer).Concat(attributes)
                        For Each identifier In decl.Names
                            builder.Add(GetDeclarationInfo(model, identifier, getSymbol, codeBlocks, cancellationToken))
                        Next
                    Next
                    Return
                Case SyntaxKind.PropertyBlock
                    Dim t = CType(node, PropertyBlockSyntax)
                    For Each decl In t.Accessors
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim propertyInitializers = GetInitializerNodes(t.PropertyStatement)
                    Dim attributes = GetAttributes(t.PropertyStatement.AttributeLists)
                    Dim codeBlocks = propertyInitializers.Concat(attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.PropertyStatement
                    Dim t = CType(node, PropertyStatementSyntax)
                    Dim propertyInitializers = GetInitializerNodes(t)
                    Dim attributes = GetAttributes(t.AttributeLists)
                    Dim codeBlocks = propertyInitializers.Concat(attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.CompilationUnit
                    Dim t = CType(node, CompilationUnitSyntax)
                    For Each decl In t.Members
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next

                    If Not t.Attributes.IsEmpty Then
                        Dim attributes = GetAttributes(t.Attributes)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                    End If
                    Return
                Case Else
                    Dim typeBlock = TryCast(node, TypeBlockSyntax)
                    If typeBlock IsNot Nothing Then
                        For Each decl In typeBlock.Members
                            ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                        Next
                        Dim attributes = GetAttributes(typeBlock.BlockStatement.AttributeLists)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                        Return
                    End If

                    Dim typeStatement = TryCast(node, TypeStatementSyntax)
                    If typeStatement IsNot Nothing Then
                        Dim attributes = GetAttributes(typeStatement.AttributeLists)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                        Return
                    End If

                    Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
                    If methodBlock IsNot Nothing Then
                        Dim paramInitializers = GetParameterListInitializersAndAttributes(methodBlock.BlockStatement.ParameterList)
                        Dim attributes = GetAttributes(methodBlock.BlockStatement.AttributeLists)
                        Dim codeBlocks = paramInitializers.Concat(methodBlock).Concat(attributes)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                        Return
                    End If

                    Dim methodStatement = TryCast(node, MethodBaseSyntax)
                    If methodStatement IsNot Nothing Then
                        Dim paramInitializers = GetParameterListInitializersAndAttributes(methodStatement.ParameterList)
                        Dim attributes = GetAttributes(methodStatement.AttributeLists)
                        Dim codeBlocks = paramInitializers.Concat(attributes)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                        Return
                    End If

                    Return
            End Select
        End Sub

        Private Shared Function GetAttributes(attributeStatements As SyntaxList(Of AttributesStatementSyntax)) As IEnumerable(Of SyntaxNode)
            Dim attributes = SpecializedCollections.EmptyEnumerable(Of SyntaxNode)
            For Each attributeStatement In attributeStatements
                attributes = attributes.Concat(GetAttributes(attributeStatement.AttributeLists))
            Next

            Return attributes
        End Function

        Private Shared Iterator Function GetAttributes(attributeLists As SyntaxList(Of AttributeListSyntax)) As IEnumerable(Of SyntaxNode)
            For Each attributeList In attributeLists
                For Each attribute In attributeList.Attributes
                    Yield attribute
                Next
            Next
        End Function

        Private Shared Function GetParameterListInitializersAndAttributes(parameterList As ParameterListSyntax) As IEnumerable(Of SyntaxNode)
            Return If(parameterList IsNot Nothing,
                parameterList.Parameters.SelectMany(Function(p) GetParameterInitializersAndAttributes(p)),
                SpecializedCollections.EmptyEnumerable(Of SyntaxNode))
        End Function

        Private Shared Function GetParameterInitializersAndAttributes(parameter As ParameterSyntax) As IEnumerable(Of SyntaxNode)
            Return SpecializedCollections.SingletonEnumerable(Of SyntaxNode)(parameter.Default).Concat(GetAttributes(parameter.AttributeLists))
        End Function

        Private Shared Function GetInitializerNodes(propertyStatement As PropertyStatementSyntax) As IEnumerable(Of SyntaxNode)
            Dim parameterInitializers = GetParameterListInitializersAndAttributes(propertyStatement.ParameterList)
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
