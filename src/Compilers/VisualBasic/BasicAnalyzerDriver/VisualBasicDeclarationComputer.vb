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
                    Dim attributes = ArrayBuilder(Of SyntaxNode).GetInstance()
                    AddAttributes(t.EnumStatement.AttributeLists, attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                    attributes.Free()
                    Return
                Case SyntaxKind.EnumStatement
                    Dim t = DirectCast(node, EnumStatementSyntax)
                    Dim attributes = ArrayBuilder(Of SyntaxNode).GetInstance()
                    AddAttributes(t.AttributeLists, attributes)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                    attributes.Free()
                    Return
                Case SyntaxKind.EnumMemberDeclaration
                    Dim t = DirectCast(node, EnumMemberDeclarationSyntax)
                    Dim codeBlocks = ArrayBuilder(Of SyntaxNode).GetInstance()
                    codeBlocks.Add(t.Initializer)
                    AddAttributes(t.AttributeLists, codeBlocks)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    codeBlocks.Free()
                    Return
                Case SyntaxKind.EventBlock
                    Dim t = DirectCast(node, EventBlockSyntax)
                    For Each decl In t.Accessors
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim codeBlocks = ArrayBuilder(Of SyntaxNode).GetInstance()
                    AddMethodBaseCodeBlocks(t.EventStatement, codeBlocks)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    codeBlocks.Free()
                    Return
                Case SyntaxKind.FieldDeclaration
                    Dim t = DirectCast(node, FieldDeclarationSyntax)
                    Dim attributes = ArrayBuilder(Of SyntaxNode).GetInstance()
                    AddAttributes(t.AttributeLists, attributes)
                    For Each decl In t.Declarators
                        Dim initializer = GetInitializerNode(decl)
                        Dim codeBlocks = ArrayBuilder(Of SyntaxNode).GetInstance()
                        codeBlocks.Add(initializer)
                        codeBlocks.AddRange(attributes)
                        For Each identifier In decl.Names
                            builder.Add(GetDeclarationInfo(model, identifier, getSymbol, codeBlocks, cancellationToken))
                        Next
                        codeBlocks.Free()
                    Next
                    attributes.Free()
                    Return
                Case SyntaxKind.PropertyBlock
                    Dim t = DirectCast(node, PropertyBlockSyntax)
                    For Each decl In t.Accessors
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next
                    Dim codeBlocks = ArrayBuilder(Of SyntaxNode).GetInstance()
                    AddPropertyStatementCodeBlocks(t.PropertyStatement, codeBlocks)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    codeBlocks.Free()
                    Return
                Case SyntaxKind.PropertyStatement
                    Dim t = DirectCast(node, PropertyStatementSyntax)
                    Dim codeBlocks = ArrayBuilder(Of SyntaxNode).GetInstance()
                    AddPropertyStatementCodeBlocks(t, codeBlocks)
                    builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                    codeBlocks.Free()
                    Return
                Case SyntaxKind.CompilationUnit
                    Dim t = DirectCast(node, CompilationUnitSyntax)
                    For Each decl In t.Members
                        ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                    Next

                    If Not t.Attributes.IsEmpty Then
                        Dim attributes = ArrayBuilder(Of SyntaxNode).GetInstance()
                        AddAttributes(t.Attributes, attributes)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                        attributes.Free()
                    End If
                    Return
                Case Else
                    Dim typeBlock = TryCast(node, TypeBlockSyntax)
                    If typeBlock IsNot Nothing Then
                        For Each decl In typeBlock.Members
                            ComputeDeclarationsCore(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken)
                        Next
                        Dim attributes = ArrayBuilder(Of SyntaxNode).GetInstance()
                        AddAttributes(typeBlock.BlockStatement.AttributeLists, attributes)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                        attributes.Free()
                        Return
                    End If

                    Dim typeStatement = TryCast(node, TypeStatementSyntax)
                    If typeStatement IsNot Nothing Then
                        Dim attributes = ArrayBuilder(Of SyntaxNode).GetInstance()
                        AddAttributes(typeStatement.AttributeLists, attributes)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken))
                        attributes.Free()
                        Return
                    End If

                    Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
                    If methodBlock IsNot Nothing Then
                        Dim codeBlocks = ArrayBuilder(Of SyntaxNode).GetInstance()
                        codeBlocks.Add(methodBlock)
                        AddMethodBaseCodeBlocks(methodBlock.BlockStatement, codeBlocks)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                        codeBlocks.Free()
                        Return
                    End If

                    Dim methodStatement = TryCast(node, MethodBaseSyntax)
                    If methodStatement IsNot Nothing Then
                        Dim codeBlocks = ArrayBuilder(Of SyntaxNode).GetInstance()
                        AddMethodBaseCodeBlocks(methodStatement, codeBlocks)
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken))
                        codeBlocks.Free()
                        Return
                    End If

                    Return
            End Select
        End Sub

        Private Shared Sub AddAttributes(attributeStatements As SyntaxList(Of AttributesStatementSyntax), builder As ArrayBuilder(Of SyntaxNode))
            For Each attributeStatement In attributeStatements
                AddAttributes(attributeStatement.AttributeLists, builder)
            Next
        End Sub

        Private Shared Sub AddPropertyStatementCodeBlocks(propertyStatement As PropertyStatementSyntax, builder As ArrayBuilder(Of SyntaxNode))
            Dim initializer As SyntaxNode = propertyStatement.Initializer
            If initializer Is Nothing Then
                initializer = GetAsNewClauseInitializer(propertyStatement.AsClause)
            End If

            If (initializer IsNot Nothing) Then
                builder.Add(initializer)
            End If

            AddMethodBaseCodeBlocks(propertyStatement, builder)
        End Sub

        Private Shared Sub AddMethodBaseCodeBlocks(methodBase As MethodBaseSyntax, builder As ArrayBuilder(Of SyntaxNode))
            AddParameterListInitializersAndAttributes(methodBase.ParameterList, builder)
            AddAttributes(methodBase.AttributeLists, builder)
            AddReturnTypeAttributes(GetAsClause(methodBase), builder)
        End Sub

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

        Private Shared Sub AddReturnTypeAttributes(asClause As AsClauseSyntax, builder As ArrayBuilder(Of SyntaxNode))
            If asClause IsNot Nothing AndAlso Not asClause.Attributes.IsEmpty Then
                AddAttributes(asClause.Attributes, builder)
            End If
        End Sub

        Private Shared Sub AddAttributes(attributeLists As SyntaxList(Of AttributeListSyntax), builder As ArrayBuilder(Of SyntaxNode))
            For Each attributeList In attributeLists
                For Each attribute In attributeList.Attributes
                    builder.Add(attribute)
                Next
            Next
        End Sub

        Private Shared Sub AddParameterListInitializersAndAttributes(parameterList As ParameterListSyntax, builder As ArrayBuilder(Of SyntaxNode))
            If parameterList IsNot Nothing Then
                For Each parameter In parameterList.Parameters
                    AddParameterInitializersAndAttributes(parameter, builder)
                Next
            End If
        End Sub

        Private Shared Sub AddParameterInitializersAndAttributes(parameter As ParameterSyntax, builder As ArrayBuilder(Of SyntaxNode))
            builder.Add(parameter.Default)
            AddAttributes(parameter.AttributeLists, builder)
        End Sub

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
