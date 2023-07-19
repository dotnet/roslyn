' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Class VisualBasicDeclarationComputer
        Inherits DeclarationComputer

        Public Shared Sub ComputeDeclarationsInSpan(model As SemanticModel,
                                                    span As TextSpan,
                                                    getSymbol As Boolean,
                                                    builder As ArrayBuilder(Of DeclarationInfo),
                                                    cancellationToken As CancellationToken)
            ComputeDeclarationsCore(model, model.SyntaxTree.GetRoot(cancellationToken),
                                    Function(node, level) Not node.Span.OverlapsWith(span) OrElse InvalidLevel(level),
                                    getSymbol, builder, Nothing, cancellationToken)
        End Sub

        Public Shared Sub ComputeDeclarationsInNode(model As SemanticModel,
                                                    node As SyntaxNode,
                                                    getSymbol As Boolean,
                                                    builder As ArrayBuilder(Of DeclarationInfo),
                                                    cancellationToken As CancellationToken, Optional levelsToCompute As Integer? = Nothing)
            ComputeDeclarationsCore(model, node, Function(n, level) InvalidLevel(level), getSymbol, builder, levelsToCompute, cancellationToken)
        End Sub

        Private Shared Function InvalidLevel(level As Integer?) As Boolean
            Return level.HasValue AndAlso level.Value <= 0
        End Function

        Private Shared Function DecrementLevel(level As Integer?) As Integer?
            Return If(level.HasValue, level.Value - 1, level)
        End Function

        Private Shared Sub ComputeDeclarationsCore(model As SemanticModel,
                                                   node As SyntaxNode,
                                                   shouldSkip As Func(Of SyntaxNode, Integer?, Boolean),
                                                   getSymbol As Boolean,
                                                   builder As ArrayBuilder(Of DeclarationInfo),
                                                   levelsToCompute As Integer?,
                                                   cancellationToken As CancellationToken)
            cancellationToken.ThrowIfCancellationRequested()

            If shouldSkip(node, levelsToCompute) Then
                Return
            End If

            Dim newLevel = DecrementLevel(levelsToCompute)

            Select Case node.Kind()
                Case SyntaxKind.NamespaceBlock
                    Dim ns = DirectCast(node, NamespaceBlockSyntax)
                    For Each decl In ns.Members
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim declInfo = GetDeclarationInfo(model, node, getSymbol, cancellationToken)
                    builder.Add(declInfo)

                    Dim name = ns.NamespaceStatement.Name
                    Dim nsSymbol = declInfo.DeclaredSymbol
                    While (name.Kind() = SyntaxKind.QualifiedName)
                        name = (DirectCast(name, QualifiedNameSyntax)).Left
                        Dim declaredSymbol = If(getSymbol, nsSymbol?.ContainingNamespace, Nothing)
                        builder.Add(New DeclarationInfo(name, ImmutableArray(Of SyntaxNode).Empty, declaredSymbol))
                        nsSymbol = declaredSymbol
                    End While

                    Return
                Case SyntaxKind.EnumBlock
                    Dim t = DirectCast(node, EnumBlockSyntax)
                    For Each decl In t.Members
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim attributes = GetAttributes(t.EnumStatement.AttributeLists)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                    Return
                Case SyntaxKind.EnumStatement
                    Dim t = DirectCast(node, EnumStatementSyntax)
                    Dim attributes = GetAttributes(t.AttributeLists)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                    Return
                Case SyntaxKind.EnumMemberDeclaration
                    Dim t = DirectCast(node, EnumMemberDeclarationSyntax)
                    Dim attributes = GetAttributes(t.AttributeLists)
                    Dim codeBlocks = SpecializedCollections.SingletonEnumerable(Of SyntaxNode)(t.Initializer).Concat(attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.EventBlock
                    Dim t = DirectCast(node, EventBlockSyntax)
                    For Each decl In t.Accessors
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim codeBlocks = GetMethodBaseCodeBlocks(t.EventStatement)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.FieldDeclaration
                    Dim t = DirectCast(node, FieldDeclarationSyntax)
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
                    Dim t = DirectCast(node, PropertyBlockSyntax)
                    For Each decl In t.Accessors
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim codeBlocks = GetPropertyStatementCodeBlocks(t.PropertyStatement)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.PropertyStatement
                    Dim t = DirectCast(node, PropertyStatementSyntax)
                    Dim codeBlocks = GetPropertyStatementCodeBlocks(t)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    Return
                Case SyntaxKind.CompilationUnit
                    Dim t = DirectCast(node, CompilationUnitSyntax)
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
                        Dim codeBlocks = SpecializedCollections.SingletonEnumerable(Of SyntaxNode)(methodBlock).
                            Concat(GetMethodBaseCodeBlocks(methodBlock.BlockStatement))
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                        Return
                    End If

                    Dim methodStatement = TryCast(node, MethodBaseSyntax)
                    If methodStatement IsNot Nothing Then
                        Dim codeBlocks = GetMethodBaseCodeBlocks(methodStatement)
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

        Private Shared Function GetPropertyStatementCodeBlocks(propertyStatement As PropertyStatementSyntax) As IEnumerable(Of SyntaxNode)
            Dim initializer As SyntaxNode = propertyStatement.Initializer
            If initializer Is Nothing Then
                initializer = GetAsNewClauseInitializer(propertyStatement.AsClause)
            End If
            Dim codeBlocks = GetMethodBaseCodeBlocks(propertyStatement)
            Return If(initializer IsNot Nothing,
                SpecializedCollections.SingletonEnumerable(initializer).Concat(codeBlocks),
                codeBlocks)
        End Function

        Private Shared Function GetMethodBaseCodeBlocks(methodBase As MethodBaseSyntax) As IEnumerable(Of SyntaxNode)
            Dim paramInitializers = GetParameterListInitializersAndAttributes(methodBase.ParameterList)
            Dim attributes = GetAttributes(methodBase.AttributeLists).Concat(GetReturnTypeAttributes(GetAsClause(methodBase)))
            Return paramInitializers.Concat(attributes)
        End Function

        Private Shared Function GetAsClause(methodBase As MethodBaseSyntax) As AsClauseSyntax
            Select Case methodBase.Kind
                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    Return DirectCast(methodBase, MethodStatementSyntax).AsClause

                Case SyntaxKind.SubLambdaHeader, SyntaxKind.FunctionLambdaHeader
                    Return DirectCast(methodBase, LambdaHeaderSyntax).AsClause

                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(methodBase, DeclareStatementSyntax).AsClause

                Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(methodBase, DelegateStatementSyntax).AsClause

                Case SyntaxKind.EventStatement
                    Return DirectCast(methodBase, EventStatementSyntax).AsClause

                Case SyntaxKind.OperatorStatement
                    Return DirectCast(methodBase, OperatorStatementSyntax).AsClause

                Case SyntaxKind.PropertyStatement
                    Return DirectCast(methodBase, PropertyStatementSyntax).AsClause

                Case SyntaxKind.SubNewStatement,
                        SyntaxKind.GetAccessorStatement,
                        SyntaxKind.SetAccessorStatement,
                        SyntaxKind.AddHandlerAccessorStatement,
                        SyntaxKind.RemoveHandlerAccessorStatement,
                        SyntaxKind.RaiseEventAccessorStatement
                    Return Nothing

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(methodBase.Kind)
            End Select
        End Function

        Private Shared Function GetReturnTypeAttributes(asClause As AsClauseSyntax) As IEnumerable(Of SyntaxNode)
            Return If(asClause IsNot Nothing AndAlso Not asClause.Attributes.IsEmpty,
                GetAttributes(asClause.Attributes),
                SpecializedCollections.EmptyEnumerable(Of SyntaxNode))
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

        Private Shared Function GetInitializerNode(variableDeclarator As VariableDeclaratorSyntax) As SyntaxNode
            Dim initializer As SyntaxNode = variableDeclarator.Initializer
            If initializer Is Nothing Then
                initializer = GetAsNewClauseInitializer(variableDeclarator.AsClause)
            End If

            Return initializer
        End Function

        Private Shared Function GetAsNewClauseInitializer(asClause As AsClauseSyntax) As SyntaxNode
            ' The As New clause itself is necessary rather than the embedded New expression, so that the
            ' code block associated with the declaration appears as an initializer for the purposes
            ' of executing analyzer actions.
            Return If(asClause.IsKind(SyntaxKind.AsNewClause), asClause, Nothing)
        End Function
    End Class
End Namespace
