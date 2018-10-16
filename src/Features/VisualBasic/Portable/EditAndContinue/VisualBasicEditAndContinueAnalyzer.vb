﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    <ExportLanguageService(GetType(IEditAndContinueAnalyzer), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicEditAndContinueAnalyzer
        Inherits AbstractEditAndContinueAnalyzer

#Region "Syntax Analysis"

        ''' <returns>
        ''' <see cref="MethodBlockBaseSyntax"/> for methods, constructors, operators and accessors.
        ''' <see cref="PropertyStatementSyntax"/> for auto-properties.
        ''' <see cref="VariableDeclaratorSyntax"/> for fields with simple initialization "Dim a = 1", or "Dim a As New C"
        ''' <see cref="ModifiedIdentifierSyntax"/> for fields with shared AsNew initialization "Dim a, b As New C" or array initializer "Dim a(n), b(n)".
        ''' A null reference otherwise.
        ''' </returns>
        Friend Overrides Function FindMemberDeclaration(rootOpt As SyntaxNode, node As SyntaxNode) As SyntaxNode
            While node IsNot rootOpt
                Select Case node.Kind
                    Case SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.ConstructorBlock,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock
                        Return node

                    Case SyntaxKind.PropertyStatement
                        ' Property [|a As Integer = 1|]
                        ' Property [|a As New C()|]
                        If Not node.Parent.IsKind(SyntaxKind.PropertyBlock) Then
                            Return node
                        End If

                    Case SyntaxKind.VariableDeclarator
                        If node.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                            ' Dim [|a = 0|]
                            ' Dim [|a = 0|], [|b = 0|]
                            ' Dim [|b as Integer = 0|]
                            ' Dim [|v1 As New C|]
                            ' Dim v1, v2 As New C(Sub [|Goo()|])
                            Return node
                        End If

                    Case SyntaxKind.ModifiedIdentifier
                        ' Dim [|a(n)|], [|b(n)|] As Integer
                        ' Dim [|v1|], [|v2|] As New C
                        If Not node.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                            Exit Select
                        End If

                        If DirectCast(node, ModifiedIdentifierSyntax).ArrayBounds IsNot Nothing OrElse
                           DirectCast(node.Parent, VariableDeclaratorSyntax).Names.Count > 1 Then
                            Return node
                        End If
                End Select

                node = node.Parent
            End While

            Return Nothing
        End Function

        ''' <returns>
        ''' Given a node representing a declaration (<paramref name="isMember"/> = true) or a top-level edit node (<paramref name="isMember"/> = false) returns:
        ''' - <see cref="MethodBlockBaseSyntax"/> for methods, constructors, operators and accessors.
        ''' - <see cref="ExpressionSyntax"/> for auto-properties and fields with initializer or AsNew clause.
        ''' - <see cref="ArgumentListSyntax"/> for fields with array initializer, e.g. "Dim a(1) As Integer".
        ''' A null reference otherwise.
        ''' </returns>
        Friend Overrides Function TryGetDeclarationBody(node As SyntaxNode, isMember As Boolean) As SyntaxNode
            Select Case node.Kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    ' the body is the Statements list of the block
                    Return node

                Case SyntaxKind.PropertyStatement
                    ' the body is the initializer expression/new expression (if any)

                    Dim propertyStatement = DirectCast(node, PropertyStatementSyntax)
                    If propertyStatement.Initializer IsNot Nothing Then
                        Return propertyStatement.Initializer.Value
                    End If

                    If HasAsNewClause(propertyStatement) Then
                        Return DirectCast(propertyStatement.AsClause, AsNewClauseSyntax).NewExpression
                    End If

                    Return Nothing

                Case SyntaxKind.VariableDeclarator
                    If Not node.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                        Return Nothing
                    End If

                    ' Dim a = initializer
                    Dim variableDeclarator = DirectCast(node, VariableDeclaratorSyntax)
                    If variableDeclarator.Initializer IsNot Nothing Then
                        Return variableDeclarator.Initializer.Value
                    End If

                    If HasAsNewClause(variableDeclarator) Then
                        ' Dim a As New C()
                        ' Dim a, b As New C(), but only if the specified node isn't already a member declaration representative.
                        ' -- This is to handle an edit in AsNew clause because such an edit doesn't affect the modified identifier that would otherwise represent the member.
                        If variableDeclarator.Names.Count = 1 OrElse Not isMember Then
                            Return DirectCast(variableDeclarator.AsClause, AsNewClauseSyntax).NewExpression
                        End If
                    End If

                    Return Nothing

                Case SyntaxKind.ModifiedIdentifier
                    If Not node.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                        Return Nothing
                    End If

                    ' Dim a, b As New C()
                    Dim variableDeclarator = DirectCast(node.Parent, VariableDeclaratorSyntax)
                    If HasMultiAsNewInitializer(variableDeclarator) Then
                        Return DirectCast(variableDeclarator.AsClause, AsNewClauseSyntax).NewExpression
                    End If

                    ' Dim a(n)
                    ' Dim a(n), b(n) As Integer
                    ' Dim a(n1, n2, n3) As Integer
                    Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)
                    If modifiedIdentifier.ArrayBounds IsNot Nothing Then
                        Return modifiedIdentifier.ArrayBounds
                    End If

                    Return Nothing

                Case Else
                    ' Note: A method without body is represented by a SubStatement.
                    Return Nothing
            End Select
        End Function

        Protected Overrides Function GetCapturedVariables(model As SemanticModel, memberBody As SyntaxNode) As ImmutableArray(Of ISymbol)
            Dim methodBlock = TryCast(memberBody, MethodBlockBaseSyntax)
            If methodBlock IsNot Nothing Then
                If methodBlock.Statements.IsEmpty Then
                    Return ImmutableArray(Of ISymbol).Empty
                End If

                Return model.AnalyzeDataFlow(methodBlock.Statements.First, methodBlock.Statements.Last).Captured
            End If

            Dim expression = TryCast(memberBody, ExpressionSyntax)
            If expression IsNot Nothing Then
                Return model.AnalyzeDataFlow(expression).Captured
            End If

            ' Edge case, no need to be efficient, currently there can either be no captured variables or just "Me".
            ' Dim a((Function(n) n + 1).Invoke(1), (Function(n) n + 2).Invoke(2)) As Integer
            Dim arrayBounds = TryCast(memberBody, ArgumentListSyntax)
            If arrayBounds IsNot Nothing Then
                Return ImmutableArray.CreateRange(
                    arrayBounds.Arguments.
                        SelectMany(AddressOf GetArgumentExpressions).
                        SelectMany(Function(expr) model.AnalyzeDataFlow(expr).Captured).
                        Distinct())
            End If

            Throw ExceptionUtilities.UnexpectedValue(memberBody)
        End Function

        Private Shared Iterator Function GetArgumentExpressions(argument As ArgumentSyntax) As IEnumerable(Of ExpressionSyntax)
            Select Case argument.Kind
                Case SyntaxKind.SimpleArgument
                    Yield DirectCast(argument, SimpleArgumentSyntax).Expression

                Case SyntaxKind.RangeArgument
                    Dim range = DirectCast(argument, RangeArgumentSyntax)
                    Yield range.LowerBound
                    Yield range.UpperBound

                Case SyntaxKind.OmittedArgument

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(argument.Kind)
            End Select
        End Function

        Friend Overrides Function HasParameterClosureScope(member As ISymbol) As Boolean
            Return False
        End Function

        Protected Overrides Function GetVariableUseSites(roots As IEnumerable(Of SyntaxNode), localOrParameter As ISymbol, model As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode)
            Debug.Assert(TypeOf localOrParameter Is IParameterSymbol OrElse TypeOf localOrParameter Is ILocalSymbol OrElse TypeOf localOrParameter Is IRangeVariableSymbol)

            ' Not supported (it's non trivial to find all places where "this" is used):
            Debug.Assert(Not localOrParameter.IsThisParameter())

            Return From root In roots
                   From node In root.DescendantNodesAndSelf()
                   Where node.IsKind(SyntaxKind.IdentifierName)
                   Let identifier = DirectCast(node, IdentifierNameSyntax)
                   Where String.Equals(DirectCast(identifier.Identifier.Value, String), localOrParameter.Name, StringComparison.OrdinalIgnoreCase) AndAlso
                         If(model.GetSymbolInfo(identifier, cancellationToken).Symbol?.Equals(localOrParameter), False)
                   Select node
        End Function

        Private Shared Function HasSimpleAsNewInitializer(variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return variableDeclarator.Names.Count = 1 AndAlso HasAsNewClause(variableDeclarator)
        End Function

        Private Shared Function HasMultiAsNewInitializer(variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return variableDeclarator.Names.Count > 1 AndAlso HasAsNewClause(variableDeclarator)
        End Function

        Private Shared Function HasAsNewClause(variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return variableDeclarator.AsClause IsNot Nothing AndAlso variableDeclarator.AsClause.IsKind(SyntaxKind.AsNewClause)
        End Function

        Private Shared Function HasAsNewClause(propertyStatement As PropertyStatementSyntax) As Boolean
            Return propertyStatement.AsClause IsNot Nothing AndAlso propertyStatement.AsClause.IsKind(SyntaxKind.AsNewClause)
        End Function

        ''' <returns>
        ''' Methods, operators, constructors, property and event accessors:
        ''' - We need to return the entire block declaration since the Begin and End statements are covered by breakpoint spans.
        ''' Field declarations in form of "Dim a, b, c As New C()" 
        ''' - Breakpoint spans cover "a", "b" and "c" and also "New C()" since the expression may contain lambdas.
        '''   For simplicity we don't allow moving the new expression independently of the field name. 
        ''' Field declarations with array initializers "Dim a(n), b(n) As Integer" 
        ''' - Breakpoint spans cover "a(n)" and "b(n)".
        ''' </returns>
        Friend Overrides Function TryGetActiveTokens(node As SyntaxNode) As IEnumerable(Of SyntaxToken)
            Select Case node.Kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    ' the body is the Statements list of the block
                    Return node.DescendantTokens()

                Case SyntaxKind.PropertyStatement
                    ' Property: Attributes Modifiers [|Identifier AsClause Initializer|] ImplementsClause
                    ' Property: Attributes Modifiers [|Identifier$ Initializer|] ImplementsClause
                    Dim propertyStatement = DirectCast(node, PropertyStatementSyntax)
                    If propertyStatement.Initializer IsNot Nothing Then
                        Return {propertyStatement.Identifier}.Concat(If(propertyStatement.AsClause?.DescendantTokens(),
                                                                     Array.Empty(Of SyntaxToken))).Concat(propertyStatement.Initializer.DescendantTokens())
                    End If

                    If HasAsNewClause(propertyStatement) Then
                        Return {propertyStatement.Identifier}.Concat(propertyStatement.AsClause.DescendantTokens())
                    End If

                    Return Nothing

                Case SyntaxKind.VariableDeclarator
                    If Not node.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                        Return Nothing
                    End If

                    ' Field: Attributes Modifiers Declarators
                    Dim fieldDeclaration = DirectCast(node.Parent, FieldDeclarationSyntax)
                    If fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword) Then
                        Return Nothing
                    End If

                    ' Dim a = initializer
                    Dim variableDeclarator = DirectCast(node, VariableDeclaratorSyntax)
                    If variableDeclarator.Initializer IsNot Nothing Then
                        Return variableDeclarator.DescendantTokens()
                    End If

                    ' Dim a As New C()
                    If HasSimpleAsNewInitializer(variableDeclarator) Then
                        Return variableDeclarator.DescendantTokens()
                    End If

                    Return Nothing

                Case SyntaxKind.ModifiedIdentifier
                    If Not node.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                        Return Nothing
                    End If

                    ' Dim a, b As New C()
                    Dim variableDeclarator = DirectCast(node.Parent, VariableDeclaratorSyntax)
                    If HasMultiAsNewInitializer(variableDeclarator) Then
                        Return node.DescendantTokens().Concat(DirectCast(variableDeclarator.AsClause, AsNewClauseSyntax).NewExpression.DescendantTokens())
                    End If

                    ' Dim a(n)
                    ' Dim a(n), b(n) As Integer
                    Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)
                    If modifiedIdentifier.ArrayBounds IsNot Nothing Then
                        Return node.DescendantTokens()
                    End If

                    Return Nothing

                Case Else
                    Return Nothing
            End Select
        End Function

        Protected Overrides Function GetEncompassingAncestorImpl(bodyOrMatchRoot As SyntaxNode) As SyntaxNode
            ' AsNewClause is a match root for field/property As New initializer 
            ' EqualsClause is a match root for field/property initializer
            If bodyOrMatchRoot.IsKind(SyntaxKind.AsNewClause) OrElse bodyOrMatchRoot.IsKind(SyntaxKind.EqualsValue) Then
                Debug.Assert(bodyOrMatchRoot.Parent.IsKind(SyntaxKind.VariableDeclarator) OrElse
                             bodyOrMatchRoot.Parent.IsKind(SyntaxKind.PropertyStatement))
                Return bodyOrMatchRoot.Parent
            End If

            ' ArgumentList is a match root for an array initialized field
            If bodyOrMatchRoot.IsKind(SyntaxKind.ArgumentList) Then
                Debug.Assert(bodyOrMatchRoot.Parent.IsKind(SyntaxKind.ModifiedIdentifier))
                Return bodyOrMatchRoot.Parent
            End If

            ' The following active nodes are outside of the initializer body,
            ' we need to return a node that encompasses them.
            ' Dim [|a = <<Body>>|]
            ' Dim [|a As Integer = <<Body>>|]
            ' Dim [|a As <<Body>>|]
            ' Dim [|a|], [|b|], [|c|] As <<Body>> 
            ' Property [|P As Integer = <<Body>>|]
            ' Property [|P As <<Body>>|]
            If bodyOrMatchRoot.Parent.IsKind(SyntaxKind.AsNewClause) OrElse
               bodyOrMatchRoot.Parent.IsKind(SyntaxKind.EqualsValue) Then
                Return bodyOrMatchRoot.Parent.Parent
            End If

            Return bodyOrMatchRoot
        End Function

        Protected Overrides Function FindStatementAndPartner(declarationBody As SyntaxNode,
                                                             position As Integer,
                                                             partnerDeclarationBodyOpt As SyntaxNode,
                                                             <Out> ByRef partnerOpt As SyntaxNode,
                                                             <Out> ByRef statementPart As Integer) As SyntaxNode
            SyntaxUtilities.AssertIsBody(declarationBody, allowLambda:=False)
            Debug.Assert(partnerDeclarationBodyOpt Is Nothing OrElse partnerDeclarationBodyOpt.RawKind = declarationBody.RawKind)

            ' Only field and property initializers may have an [|active statement|] starting outside of the <<body>>.
            ' Simple field initializers:         Dim [|a = <<expr>>|]
            '                                    Dim [|a As Integer = <<expr>>|]
            '                                    Dim [|a = <<expr>>|], [|b = <<expr>>|], [|c As Integer = <<expr>>|]
            '                                    Dim [|a As <<New C>>|] 
            ' Array initialized fields:          Dim [|a<<(array bounds)>>|] As Integer
            ' Shared initializers:               Dim [|a|], [|b|] As <<New C(Function() [|...|])>>
            ' Property initializers:             Property [|p As Integer = <<body>>|]
            '                                    Property [|p As <<New C()>>|]
            If position < declarationBody.SpanStart Then
                If declarationBody.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                    ' Property [|p As Integer = <<body>>|]
                    ' Property [|p As <<New C()>>|]

                    If partnerDeclarationBodyOpt IsNot Nothing Then
                        partnerOpt = partnerDeclarationBodyOpt.Parent.Parent
                    End If

                    Debug.Assert(declarationBody.Parent.Parent.IsKind(SyntaxKind.PropertyStatement))
                    Return declarationBody.Parent.Parent
                End If

                If declarationBody.IsKind(SyntaxKind.ArgumentList) Then
                    ' Dim a<<ArgumentList>> As Integer
                    If partnerDeclarationBodyOpt IsNot Nothing Then
                        partnerOpt = partnerDeclarationBodyOpt.Parent
                    End If

                    Debug.Assert(declarationBody.Parent.IsKind(SyntaxKind.ModifiedIdentifier))
                    Return declarationBody.Parent
                End If

                If declarationBody.Parent.IsKind(SyntaxKind.AsNewClause) Then
                    Dim variableDeclarator = DirectCast(declarationBody.Parent.Parent, VariableDeclaratorSyntax)
                    If variableDeclarator.Names.Count > 1 Then
                        ' Dim a, b, c As <<NewExpression>>
                        Dim nameIndex = GetItemIndexByPosition(variableDeclarator.Names, position)

                        If partnerDeclarationBodyOpt IsNot Nothing Then
                            partnerOpt = DirectCast(partnerDeclarationBodyOpt.Parent.Parent, VariableDeclaratorSyntax).Names(nameIndex)
                        End If

                        Return variableDeclarator.Names(nameIndex)
                    Else
                        If partnerDeclarationBodyOpt IsNot Nothing Then
                            partnerOpt = partnerDeclarationBodyOpt.Parent.Parent
                        End If

                        ' Dim a As <<NewExpression>>
                        Return variableDeclarator
                    End If
                End If

                If declarationBody.Parent.IsKind(SyntaxKind.EqualsValue) Then
                    Debug.Assert(declarationBody.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator) AndAlso
                                 declarationBody.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))

                    If partnerDeclarationBodyOpt IsNot Nothing Then
                        partnerOpt = partnerDeclarationBodyOpt.Parent.Parent
                    End If

                    Return declarationBody.Parent.Parent
                End If
            End If

            If Not declarationBody.FullSpan.Contains(position) Then
                ' invalid position, let's find a labeled node that encompasses the body:
                position = declarationBody.SpanStart
            End If

            Dim node As SyntaxNode = Nothing
            If partnerDeclarationBodyOpt IsNot Nothing Then
                SyntaxUtilities.FindLeafNodeAndPartner(declarationBody, position, partnerDeclarationBodyOpt, node, partnerOpt)
            Else
                node = declarationBody.FindToken(position).Parent
                partnerOpt = Nothing
            End If

            Debug.Assert(node IsNot Nothing)

            While node IsNot declarationBody AndAlso
                  Not StatementSyntaxComparer.HasLabel(node) AndAlso
                  Not LambdaUtilities.IsLambdaBodyStatementOrExpression(node)

                node = node.Parent
                If partnerOpt IsNot Nothing Then
                    partnerOpt = partnerOpt.Parent
                End If
            End While

            ' In case of local variable declaration an active statement may start with a modified identifier.
            ' If it is a declaration with a simple initializer we want the resulting statement to be the declaration, 
            ' not the identifier.
            If node.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso
               node.Parent.IsKind(SyntaxKind.VariableDeclarator) AndAlso
               DirectCast(node.Parent, VariableDeclaratorSyntax).Names.Count = 1 Then
                node = node.Parent
            End If

            Return node
        End Function

        Friend Overrides Function FindPartnerInMemberInitializer(leftModel As SemanticModel, leftType As INamedTypeSymbol, leftNode As SyntaxNode, rightType As INamedTypeSymbol, cancellationToken As CancellationToken) As SyntaxNode
            Dim leftInitializer = leftNode.FirstAncestorOrSelf(Of SyntaxNode)(
                Function(node)
                    Return node.IsKind(SyntaxKind.EqualsValue) AndAlso (node.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) OrElse node.Parent.IsKind(SyntaxKind.PropertyStatement)) OrElse
                           node.IsKind(SyntaxKind.AsNewClause) AndAlso node.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) OrElse
                           IsArrayBoundsArgument(node)
                End Function)

            If leftInitializer Is Nothing Then
                Return Nothing
            End If

            Dim rightInitializer As SyntaxNode
            If leftInitializer.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                ' property initializer
                Dim leftDeclaration = DirectCast(leftInitializer.Parent, PropertyStatementSyntax)
                Dim leftSymbol = leftModel.GetDeclaredSymbol(leftDeclaration, cancellationToken)
                Debug.Assert(leftSymbol IsNot Nothing)

                Dim rightProperty = rightType.GetMembers(leftSymbol.Name).Single()
                Dim rightDeclaration = DirectCast(GetSymbolSyntax(rightProperty, cancellationToken), PropertyStatementSyntax)

                rightInitializer = rightDeclaration.Initializer
            ElseIf leftInitializer.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration)
                ' field initializer or AsNewClause
                Dim leftDeclarator = DirectCast(leftInitializer.Parent, VariableDeclaratorSyntax)

                Dim leftSymbol = leftModel.GetDeclaredSymbol(leftDeclarator.Names.First(), cancellationToken)
                Debug.Assert(leftSymbol IsNot Nothing)

                Dim rightSymbol = rightType.GetMembers(leftSymbol.Name).Single()
                Dim rightDeclarator = DirectCast(GetSymbolSyntax(rightSymbol, cancellationToken).Parent, VariableDeclaratorSyntax)

                rightInitializer = If(leftInitializer.IsKind(SyntaxKind.EqualsValue), rightDeclarator.Initializer, DirectCast(rightDeclarator.AsClause, SyntaxNode))
            Else
                ' ArrayBounds argument
                Dim leftArguments = DirectCast(leftInitializer.Parent, ArgumentListSyntax)
                Dim argumentIndex = GetItemIndexByPosition(leftArguments.Arguments, leftInitializer.Span.Start)

                Dim leftIdentifier = leftArguments.Parent
                Debug.Assert(leftIdentifier.IsKind(SyntaxKind.ModifiedIdentifier))

                Dim leftSymbol = leftModel.GetDeclaredSymbol(leftIdentifier, cancellationToken)
                Debug.Assert(leftSymbol IsNot Nothing)

                Dim rightSymbol = rightType.GetMembers(leftSymbol.Name).Single()
                Dim rightIdentifier = DirectCast(GetSymbolSyntax(rightSymbol, cancellationToken), ModifiedIdentifierSyntax)

                rightInitializer = rightIdentifier.ArrayBounds.Arguments(argumentIndex)
            End If

            If rightInitializer Is Nothing Then
                Return Nothing
            End If

            Return FindPartner(leftInitializer, rightInitializer, leftNode)
        End Function

        Friend Overrides Function FindPartner(leftRoot As SyntaxNode, rightRoot As SyntaxNode, leftNode As SyntaxNode) As SyntaxNode
            Return SyntaxUtilities.FindPartner(leftRoot, rightRoot, leftNode)
        End Function

        Private Shared Function IsArrayBoundsArgument(node As SyntaxNode) As Boolean
            Dim argumentSyntax = TryCast(node, ArgumentSyntax)

            If argumentSyntax IsNot Nothing Then
                Debug.Assert(argumentSyntax.Parent.IsKind(SyntaxKind.ArgumentList))
                Dim identifier = argumentSyntax.Parent.Parent
                Return identifier.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso identifier.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration)
            End If

            Return False
        End Function

        Friend Overrides Function IsClosureScope(node As SyntaxNode) As Boolean
            Return LambdaUtilities.IsClosureScope(node)
        End Function

        Protected Overrides Function FindEnclosingLambdaBody(containerOpt As SyntaxNode, node As SyntaxNode) As SyntaxNode
            Dim root As SyntaxNode = GetEncompassingAncestor(containerOpt)

            While node IsNot root And node IsNot Nothing
                Dim body As SyntaxNode = Nothing
                If LambdaUtilities.IsLambdaBodyStatementOrExpression(node, body) Then
                    Return body
                End If

                node = node.Parent
            End While

            Return Nothing
        End Function

        Protected Overrides Function TryGetPartnerLambdaBody(oldBody As SyntaxNode, newLambda As SyntaxNode) As SyntaxNode
            Return LambdaUtilities.GetCorrespondingLambdaBody(oldBody, newLambda)
        End Function

        Protected Overrides Function ComputeTopLevelMatch(oldCompilationUnit As SyntaxNode, newCompilationUnit As SyntaxNode) As Match(Of SyntaxNode)
            Return TopSyntaxComparer.Instance.ComputeMatch(oldCompilationUnit, newCompilationUnit)
        End Function

        Protected Overrides Function ComputeBodyMatch(oldBody As SyntaxNode, newBody As SyntaxNode, knownMatches As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))) As Match(Of SyntaxNode)
            SyntaxUtilities.AssertIsBody(oldBody, allowLambda:=True)
            SyntaxUtilities.AssertIsBody(newBody, allowLambda:=True)

            Debug.Assert((TypeOf oldBody.Parent Is LambdaExpressionSyntax) = (TypeOf oldBody.Parent Is LambdaExpressionSyntax))
            Debug.Assert((TypeOf oldBody Is ExpressionSyntax) = (TypeOf newBody Is ExpressionSyntax))
            Debug.Assert((TypeOf oldBody Is ArgumentListSyntax) = (TypeOf newBody Is ArgumentListSyntax))

            If TypeOf oldBody.Parent Is LambdaExpressionSyntax Then
                ' The root is a single/multi line sub/function lambda.
                Return New StatementSyntaxComparer(oldBody.Parent, oldBody.Parent.ChildNodes(), newBody.Parent, newBody.Parent.ChildNodes(), matchingLambdas:=True).
                       ComputeMatch(oldBody.Parent, newBody.Parent, knownMatches)
            End If

            If TypeOf oldBody Is ExpressionSyntax Then
                ' Dim a = <Expression>
                ' Dim a As <NewExpression>
                ' Dim a, b, c As <NewExpression>
                ' Queries: The root is a query clause, the body is the expression.
                Return New StatementSyntaxComparer(oldBody.Parent, {oldBody}, newBody.Parent, {newBody}, matchingLambdas:=False).
                       ComputeMatch(oldBody.Parent, newBody.Parent, knownMatches)
            End If

            ' Method, accessor, operator, etc. bodies are represented by the declaring block, which is also the root.
            ' The body of an array initialized fields is an ArgumentListSyntax, which is the match root.
            Return StatementSyntaxComparer.Default.ComputeMatch(oldBody, newBody, knownMatches)
        End Function

        Protected Overrides Function TryMatchActiveStatement(oldStatement As SyntaxNode,
                                                             statementPart As Integer,
                                                             oldBody As SyntaxNode,
                                                             newBody As SyntaxNode,
                                                             <Out> ByRef newStatement As SyntaxNode) As Boolean
            SyntaxUtilities.AssertIsBody(oldBody, allowLambda:=True)
            SyntaxUtilities.AssertIsBody(newBody, allowLambda:=True)

            ' only statements in bodies of the same kind can be matched
            Debug.Assert((TypeOf oldBody Is MethodBlockBaseSyntax) = (TypeOf newBody Is MethodBlockBaseSyntax))
            Debug.Assert((TypeOf oldBody Is ExpressionSyntax) = (TypeOf newBody Is ExpressionSyntax))
            Debug.Assert((TypeOf oldBody Is ArgumentListSyntax) = (TypeOf newBody Is ArgumentListSyntax))
            Debug.Assert((TypeOf oldBody Is LambdaHeaderSyntax) = (TypeOf newBody Is LambdaHeaderSyntax))
            Debug.Assert(oldBody.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) = newBody.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
            Debug.Assert(oldBody.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) = newBody.Parent.Parent.IsKind(SyntaxKind.PropertyStatement))

            ' methods
            If TypeOf oldBody Is MethodBlockBaseSyntax Then
                newStatement = Nothing
                Return False
            End If

            ' lambdas
            If oldBody.IsKind(SyntaxKind.FunctionLambdaHeader) OrElse oldBody.IsKind(SyntaxKind.SubLambdaHeader) Then
                Dim oldSingleLineLambda = TryCast(oldBody.Parent, SingleLineLambdaExpressionSyntax)
                Dim newSingleLineLambda = TryCast(newBody.Parent, SingleLineLambdaExpressionSyntax)

                If oldSingleLineLambda IsNot Nothing AndAlso
                   newSingleLineLambda IsNot Nothing AndAlso
                   oldStatement Is oldSingleLineLambda.Body Then

                    newStatement = newSingleLineLambda.Body
                    Return True
                End If

                newStatement = Nothing
                Return False
            End If

            ' array initialized fields
            If newBody.IsKind(SyntaxKind.ArgumentList) Then
                ' the parent ModifiedIdentifier is the active statement
                If oldStatement Is oldBody.Parent Then
                    newStatement = newBody.Parent
                    Return True
                End If

                newStatement = Nothing
                Return False
            End If

            ' field and property initializers
            If TypeOf newBody Is ExpressionSyntax Then
                If newBody.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                    ' field
                    Dim newDeclarator = DirectCast(newBody.Parent.Parent, VariableDeclaratorSyntax)

                    Dim oldName As SyntaxToken
                    If oldStatement.IsKind(SyntaxKind.VariableDeclarator) Then
                        oldName = DirectCast(oldStatement, VariableDeclaratorSyntax).Names.Single.Identifier
                    Else
                        oldName = DirectCast(oldStatement, ModifiedIdentifierSyntax).Identifier
                    End If

                    For Each newName In newDeclarator.Names
                        If SyntaxFactory.AreEquivalent(newName.Identifier, oldName) Then
                            newStatement = newName
                            Return True
                        End If
                    Next

                    newStatement = Nothing
                    Return False
                ElseIf newBody.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                    ' property
                    If oldStatement Is oldBody.Parent.Parent Then
                        newStatement = newBody.Parent.Parent
                        Return True
                    End If

                    newStatement = newBody
                    Return True
                End If
            End If

            ' queries
            If oldStatement Is oldBody Then
                newStatement = newBody
                Return True
            End If

            newStatement = Nothing
            Return False
        End Function
#End Region

#Region "Syntax And Semantic Utils"

        Protected Overrides Function GetSyntaxSequenceEdits(oldNodes As ImmutableArray(Of SyntaxNode), newNodes As ImmutableArray(Of SyntaxNode)) As IEnumerable(Of SequenceEdit)
            Return SyntaxComparer.GetSequenceEdits(oldNodes, newNodes)
        End Function

        Friend Overrides ReadOnly Property EmptyCompilationUnit As SyntaxNode
            Get
                Return SyntaxFactory.CompilationUnit()
            End Get
        End Property

        Friend Overrides Function ExperimentalFeaturesEnabled(tree As SyntaxTree) As Boolean
            ' There are no experimental features at this time.
            Return False
        End Function

        Protected Overrides Function StatementLabelEquals(node1 As SyntaxNode, node2 As SyntaxNode) As Boolean
            Return StatementSyntaxComparer.GetLabelImpl(node1) = StatementSyntaxComparer.GetLabelImpl(node2)
        End Function

        Private Shared Function GetItemIndexByPosition(Of TNode As SyntaxNode)(list As SeparatedSyntaxList(Of TNode), position As Integer) As Integer
            For i = list.SeparatorCount - 1 To 0 Step -1
                If position > list.GetSeparator(i).SpanStart Then
                    Return i + 1
                End If
            Next

            Return 0
        End Function

        Private Shared Function ChildrenCompiledInBody(node As SyntaxNode) As Boolean
            Return Not node.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) AndAlso
                   Not node.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) AndAlso
                   Not node.IsKind(SyntaxKind.MultiLineSubLambdaExpression) AndAlso
                   Not node.IsKind(SyntaxKind.SingleLineSubLambdaExpression)
        End Function

        Protected Overrides Function TryGetEnclosingBreakpointSpan(root As SyntaxNode, position As Integer, <Out> ByRef span As TextSpan) As Boolean
            Return BreakpointSpans.TryGetEnclosingBreakpointSpan(root, position, span)
        End Function

        Protected Overrides Function TryGetActiveSpan(node As SyntaxNode, statementPart As Integer, <Out> ByRef span As TextSpan) As Boolean
            Return BreakpointSpans.TryGetEnclosingBreakpointSpan(node, node.SpanStart, span)
        End Function

        Protected Overrides Iterator Function EnumerateNearStatements(statement As SyntaxNode) As IEnumerable(Of KeyValuePair(Of SyntaxNode, Integer))
            Dim direction As Integer = +1
            Dim nodeOrToken As SyntaxNodeOrToken = statement
            Dim propertyOrFieldModifiers As SyntaxTokenList? = GetFieldOrPropertyModifiers(statement)

            While True
                ' If the current statement is the last statement of if-block or try-block statements 
                ' pretend there are no siblings following it.
                Dim lastBlockStatement As SyntaxNode = Nothing
                If nodeOrToken.Parent IsNot Nothing Then
                    If nodeOrToken.Parent.IsKind(SyntaxKind.MultiLineIfBlock) Then
                        lastBlockStatement = DirectCast(nodeOrToken.Parent, MultiLineIfBlockSyntax).Statements.LastOrDefault()
                    ElseIf nodeOrToken.Parent.IsKind(SyntaxKind.SingleLineIfStatement) Then
                        lastBlockStatement = DirectCast(nodeOrToken.Parent, SingleLineIfStatementSyntax).Statements.LastOrDefault()
                    ElseIf nodeOrToken.Parent.IsKind(SyntaxKind.TryBlock) Then
                        lastBlockStatement = DirectCast(nodeOrToken.Parent, TryBlockSyntax).Statements.LastOrDefault()
                    End If
                End If

                If direction > 0 Then
                    If lastBlockStatement IsNot Nothing AndAlso nodeOrToken.AsNode() Is lastBlockStatement Then
                        nodeOrToken = Nothing
                    Else
                        nodeOrToken = nodeOrToken.GetNextSibling()
                    End If
                Else
                    nodeOrToken = nodeOrToken.GetPreviousSibling()
                    If lastBlockStatement IsNot Nothing AndAlso nodeOrToken.AsNode() Is lastBlockStatement Then
                        nodeOrToken = Nothing
                    End If
                End If

                If nodeOrToken.RawKind = 0 Then
                    Dim parent = statement.Parent
                    If parent Is Nothing Then
                        Return
                    End If

                    If direction > 0 Then
                        nodeOrToken = statement
                        direction = -1
                        Continue While
                    End If

                    If propertyOrFieldModifiers.HasValue Then
                        Yield KeyValuePairUtil.Create(statement, -1)
                    End If

                    nodeOrToken = parent
                    statement = parent
                    propertyOrFieldModifiers = GetFieldOrPropertyModifiers(statement)
                    direction = +1
                End If

                Dim node = nodeOrToken.AsNode()
                If node Is Nothing Then
                    Continue While
                End If

                If propertyOrFieldModifiers.HasValue Then
                    Dim nodeModifiers = GetFieldOrPropertyModifiers(node)

                    If Not nodeModifiers.HasValue OrElse
                       propertyOrFieldModifiers.Value.Any(SyntaxKind.SharedKeyword) <> nodeModifiers.Value.Any(SyntaxKind.SharedKeyword) Then
                        Continue While
                    End If
                End If

                Yield KeyValuePairUtil.Create(node, 0)
            End While
        End Function

        Private Shared Function GetFieldOrPropertyModifiers(node As SyntaxNode) As SyntaxTokenList?
            If node.IsKind(SyntaxKind.FieldDeclaration) Then
                Return DirectCast(node, FieldDeclarationSyntax).Modifiers
            ElseIf node.IsKind(SyntaxKind.PropertyStatement) Then
                Return DirectCast(node, PropertyStatementSyntax).Modifiers
            Else
                Return Nothing
            End If
        End Function

        Protected Overrides Function AreEquivalent(left As SyntaxNode, right As SyntaxNode) As Boolean
            Return SyntaxFactory.AreEquivalent(left, right)
        End Function

        Private Shared Function AreEquivalentIgnoringLambdaBodies(left As SyntaxNode, right As SyntaxNode) As Boolean
            ' usual case
            If SyntaxFactory.AreEquivalent(left, right) Then
                Return True
            End If

            Return LambdaUtilities.AreEquivalentIgnoringLambdaBodies(left, right)
        End Function

        Protected Overrides Function AreEquivalentActiveStatements(oldStatement As SyntaxNode, newStatement As SyntaxNode, statementPart As Integer) As Boolean
            If oldStatement.RawKind <> newStatement.RawKind Then
                Return False
            End If

            ' Dim a,b,c As <NewExpression>
            ' We need to check the actual initializer expression in addition to the identifier.
            If HasMultiInitializer(oldStatement) Then
                Return AreEquivalentIgnoringLambdaBodies(oldStatement, newStatement) AndAlso
                       AreEquivalentIgnoringLambdaBodies(DirectCast(oldStatement.Parent, VariableDeclaratorSyntax).AsClause,
                                                         DirectCast(newStatement.Parent, VariableDeclaratorSyntax).AsClause)
            End If

            Select Case oldStatement.Kind
                Case SyntaxKind.SubNewStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    ' Header statements are nops. Changes in the header statements are changes in top-level surface
                    ' which should not be reported as active statement rude edits.
                    Return True

                Case Else
                    Return AreEquivalentIgnoringLambdaBodies(oldStatement, newStatement)
            End Select
        End Function

        Private Shared Function HasMultiInitializer(modifiedIdentifier As SyntaxNode) As Boolean
            Return modifiedIdentifier.Parent.IsKind(SyntaxKind.VariableDeclarator) AndAlso
                   DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax).Names.Count > 1
        End Function

        Friend Overrides Function IsMethod(declaration As SyntaxNode) As Boolean
            Return SyntaxUtilities.IsMethod(declaration)
        End Function

        Friend Overrides Function TryGetContainingTypeDeclaration(memberDeclaration As SyntaxNode) As SyntaxNode
            Return memberDeclaration.Parent.FirstAncestorOrSelf(Of TypeBlockSyntax)()
        End Function

        Friend Overrides Function HasBackingField(propertyDeclaration As SyntaxNode) As Boolean
            Return SyntaxUtilities.HasBackingField(propertyDeclaration)
        End Function

        Friend Overrides Function IsDeclarationWithInitializer(declaration As SyntaxNode) As Boolean
            Select Case declaration.Kind
                Case SyntaxKind.VariableDeclarator
                    Dim declarator = DirectCast(declaration, VariableDeclaratorSyntax)
                    Return GetInitializerExpression(declarator.Initializer, declarator.AsClause) IsNot Nothing

                Case SyntaxKind.ModifiedIdentifier
                    Debug.Assert(declaration.Parent.IsKind(SyntaxKind.VariableDeclarator) OrElse
                                 declaration.Parent.IsKind(SyntaxKind.Parameter))

                    If Not declaration.Parent.IsKind(SyntaxKind.VariableDeclarator) Then
                        Return False
                    End If

                    Dim declarator = DirectCast(declaration.Parent, VariableDeclaratorSyntax)

                    Dim identifier = DirectCast(declaration, ModifiedIdentifierSyntax)
                    Return identifier.ArrayBounds IsNot Nothing OrElse
                           GetInitializerExpression(declarator.Initializer, declarator.AsClause) IsNot Nothing

                Case SyntaxKind.PropertyStatement
                    Dim propertyStatement = DirectCast(declaration, PropertyStatementSyntax)
                    Return GetInitializerExpression(propertyStatement.Initializer, propertyStatement.AsClause) IsNot Nothing

                Case Else
                    Return False
            End Select
        End Function

        Private Shared Function GetInitializerExpression(equalsValue As EqualsValueSyntax, asClause As AsClauseSyntax) As ExpressionSyntax
            If equalsValue IsNot Nothing Then
                Return equalsValue.Value
            End If

            If asClause IsNot Nothing AndAlso asClause.IsKind(SyntaxKind.AsNewClause) Then
                Return DirectCast(asClause, AsNewClauseSyntax).NewExpression
            End If

            Return Nothing
        End Function

        Friend Overrides Function IsConstructorWithMemberInitializers(declaration As SyntaxNode) As Boolean
            Dim ctor = TryCast(declaration, ConstructorBlockSyntax)
            If ctor Is Nothing Then
                Return False
            End If

            ' Constructor includes field initializers if the first statement 
            ' isn't a call to another constructor of the declaring class or module.

            If ctor.Statements.Count = 0 Then
                Return True
            End If

            Dim firstStatement = ctor.Statements.First
            If Not firstStatement.IsKind(SyntaxKind.ExpressionStatement) Then
                Return True
            End If

            Dim expressionStatement = DirectCast(firstStatement, ExpressionStatementSyntax)
            If Not expressionStatement.Expression.IsKind(SyntaxKind.InvocationExpression) Then
                Return True
            End If

            Dim invocation = DirectCast(expressionStatement.Expression, InvocationExpressionSyntax)
            If Not invocation.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Return True
            End If

            Dim memberAccess = DirectCast(invocation.Expression, MemberAccessExpressionSyntax)
            If Not memberAccess.Name.IsKind(SyntaxKind.IdentifierName) OrElse
               Not memberAccess.Name.Identifier.IsKind(SyntaxKind.IdentifierToken) Then
                Return True
            End If

            ' Note that ValueText returns "New" for both New and [New]
            If Not String.Equals(memberAccess.Name.Identifier.ToString(), "New", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If

            Return memberAccess.Expression.IsKind(SyntaxKind.MyBaseKeyword)
        End Function

        Friend Overrides Function IsPartial(type As INamedTypeSymbol) As Boolean
            Dim syntaxRefs = type.DeclaringSyntaxReferences
            Return syntaxRefs.Length > 1 OrElse
                   DirectCast(syntaxRefs.Single().GetSyntax(), TypeStatementSyntax).Modifiers.Any(SyntaxKind.PartialKeyword)
        End Function

        Protected Overrides Function GetSymbolForEdit(model As SemanticModel, node As SyntaxNode, editKind As EditKind, editMap As Dictionary(Of SyntaxNode, EditKind), cancellationToken As CancellationToken) As ISymbol
            ' Avoid duplicate semantic edits - don't return symbols for statements within blocks.
            Select Case node.Kind()
                Case SyntaxKind.OperatorStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.ClassStatement,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.ModuleStatement,
                     SyntaxKind.EnumStatement,
                     SyntaxKind.NamespaceStatement
                    Return Nothing

                Case SyntaxKind.EventStatement
                    If node.Parent.IsKind(SyntaxKind.EventBlock) Then
                        Return Nothing
                    End If

                Case SyntaxKind.PropertyStatement  ' autoprop or interface property
                    If node.Parent.IsKind(SyntaxKind.PropertyBlock) Then
                        Return Nothing
                    End If

                Case SyntaxKind.SubStatement       ' interface method
                    If node.Parent.IsKind(SyntaxKind.SubBlock) Then
                        Return Nothing
                    End If

                Case SyntaxKind.FunctionStatement  ' interface method
                    If node.Parent.IsKind(SyntaxKind.FunctionBlock) Then
                        Return Nothing
                    End If

                Case SyntaxKind.Parameter
                    Return Nothing

                Case SyntaxKind.ModifiedIdentifier
                    If node.Parent.IsKind(SyntaxKind.Parameter) Then
                        Return Nothing
                    End If

                Case SyntaxKind.VariableDeclarator
                    ' An update to a field variable declarator might either be
                    ' 1) variable declarator update (an initializer is changes)
                    ' 2) modified identifier update (an array bound changes)
                    ' Handle the first one here. 
                    If editKind = EditKind.Update AndAlso node.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                        ' If multiple fields are defined by this declaration pick the first one.
                        ' We want to analyze the associated initializer just once. Any of the fields is good.
                        node = DirectCast(node, VariableDeclaratorSyntax).Names.First()
                    End If
            End Select

            Return model.GetDeclaredSymbol(node, cancellationToken)
        End Function

        Friend Overrides Function ContainsLambda(declaration As SyntaxNode) As Boolean
            Return declaration.DescendantNodes().Any(AddressOf LambdaUtilities.IsLambda)
        End Function

        Friend Overrides Function IsLambda(node As SyntaxNode) As Boolean
            Return LambdaUtilities.IsLambda(node)
        End Function

        Friend Overrides Function IsNestedFunction(node As SyntaxNode) As Boolean
            Return TypeOf node Is LambdaExpressionSyntax
        End Function

        Friend Overrides Function TryGetLambdaBodies(node As SyntaxNode, ByRef body1 As SyntaxNode, ByRef body2 As SyntaxNode) As Boolean
            Return LambdaUtilities.TryGetLambdaBodies(node, body1, body2)
        End Function

        Friend Overrides Function GetLambda(lambdaBody As SyntaxNode) As SyntaxNode
            Return LambdaUtilities.GetLambda(lambdaBody)
        End Function

        Protected Overrides Function GetLambdaBodyExpressionsAndStatements(lambdaBody As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Return LambdaUtilities.GetLambdaBodyExpressionsAndStatements(lambdaBody)
        End Function

        Friend Overrides Function GetLambdaExpressionSymbol(model As SemanticModel, lambdaExpression As SyntaxNode, cancellationToken As CancellationToken) As IMethodSymbol
            Dim lambdaExpressionSyntax = DirectCast(lambdaExpression, LambdaExpressionSyntax)

            ' The semantic model only returns the lambda symbol for positions that are within the body of the lambda (not the header)
            Return DirectCast(model.GetEnclosingSymbol(lambdaExpressionSyntax.SubOrFunctionHeader.Span.End, cancellationToken), IMethodSymbol)
        End Function

        Friend Overrides Function GetContainingQueryExpression(node As SyntaxNode) As SyntaxNode
            Return node.FirstAncestorOrSelf(Of QueryExpressionSyntax)
        End Function

        Friend Overrides Function QueryClauseLambdasTypeEquivalent(oldModel As SemanticModel, oldNode As SyntaxNode, newModel As SemanticModel, newNode As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Select Case oldNode.Kind
                Case SyntaxKind.AggregateClause
                    Dim oldInfo = oldModel.GetAggregateClauseSymbolInfo(DirectCast(oldNode, AggregateClauseSyntax), cancellationToken)
                    Dim newInfo = newModel.GetAggregateClauseSymbolInfo(DirectCast(newNode, AggregateClauseSyntax), cancellationToken)
                    Return MemberSignaturesEquivalent(oldInfo.Select1.Symbol, newInfo.Select1.Symbol) AndAlso
                           MemberSignaturesEquivalent(oldInfo.Select2.Symbol, newInfo.Select2.Symbol)

                Case SyntaxKind.CollectionRangeVariable
                    Dim oldInfo = oldModel.GetCollectionRangeVariableSymbolInfo(DirectCast(oldNode, CollectionRangeVariableSyntax), cancellationToken)
                    Dim newInfo = newModel.GetCollectionRangeVariableSymbolInfo(DirectCast(newNode, CollectionRangeVariableSyntax), cancellationToken)
                    Return MemberSignaturesEquivalent(oldInfo.AsClauseConversion.Symbol, newInfo.AsClauseConversion.Symbol) AndAlso
                           MemberSignaturesEquivalent(oldInfo.SelectMany.Symbol, newInfo.SelectMany.Symbol) AndAlso
                           MemberSignaturesEquivalent(oldInfo.ToQueryableCollectionConversion.Symbol, newInfo.ToQueryableCollectionConversion.Symbol)

                Case SyntaxKind.FunctionAggregation
                    Dim oldInfo = oldModel.GetSymbolInfo(DirectCast(oldNode, FunctionAggregationSyntax), cancellationToken)
                    Dim newInfo = newModel.GetSymbolInfo(DirectCast(newNode, FunctionAggregationSyntax), cancellationToken)
                    Return MemberSignaturesEquivalent(oldInfo.Symbol, newInfo.Symbol)

                Case SyntaxKind.ExpressionRangeVariable
                    Dim oldInfo = oldModel.GetSymbolInfo(DirectCast(oldNode, ExpressionRangeVariableSyntax), cancellationToken)
                    Dim newInfo = newModel.GetSymbolInfo(DirectCast(newNode, ExpressionRangeVariableSyntax), cancellationToken)
                    Return MemberSignaturesEquivalent(oldInfo.Symbol, newInfo.Symbol)

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Dim oldInfo = oldModel.GetSymbolInfo(DirectCast(oldNode, OrderingSyntax), cancellationToken)
                    Dim newInfo = newModel.GetSymbolInfo(DirectCast(newNode, OrderingSyntax), cancellationToken)
                    Return MemberSignaturesEquivalent(oldInfo.Symbol, newInfo.Symbol)

                Case SyntaxKind.FromClause,
                     SyntaxKind.WhereClause,
                     SyntaxKind.SkipClause,
                     SyntaxKind.TakeClause,
                     SyntaxKind.SkipWhileClause,
                     SyntaxKind.TakeWhileClause,
                     SyntaxKind.GroupByClause,
                     SyntaxKind.SimpleJoinClause,
                     SyntaxKind.GroupJoinClause,
                     SyntaxKind.SelectClause
                    Dim oldInfo = oldModel.GetSymbolInfo(DirectCast(oldNode, QueryClauseSyntax), cancellationToken)
                    Dim newInfo = newModel.GetSymbolInfo(DirectCast(newNode, QueryClauseSyntax), cancellationToken)
                    Return MemberSignaturesEquivalent(oldInfo.Symbol, newInfo.Symbol)

                Case Else
                    Return True
            End Select
        End Function
#End Region

#Region "Diagnostic Info"
        Protected Overrides ReadOnly Property ErrorDisplayFormat As SymbolDisplayFormat
            Get
                Return SymbolDisplayFormat.VisualBasicShortErrorMessageFormat
            End Get
        End Property

        Protected Overrides Function GetDiagnosticSpan(node As SyntaxNode, editKind As EditKind) As TextSpan
            Return GetDiagnosticSpanImpl(node, editKind)
        End Function

        Private Shared Function GetDiagnosticSpanImpl(node As SyntaxNode, editKind As EditKind) As TextSpan
            Return GetDiagnosticSpanImpl(node.Kind, node, editKind)
        End Function

        ' internal for testing; kind is passed explicitly for testing as well
        Friend Shared Function GetDiagnosticSpanImpl(kind As SyntaxKind, node As SyntaxNode, editKind As EditKind) As TextSpan
            Select Case kind
                Case SyntaxKind.CompilationUnit
                    Return Nothing

                Case SyntaxKind.OptionStatement,
                     SyntaxKind.ImportsStatement
                    Return node.Span

                Case SyntaxKind.NamespaceBlock
                    Return GetDiagnosticSpan(DirectCast(node, NamespaceBlockSyntax).NamespaceStatement)

                Case SyntaxKind.NamespaceStatement
                    Return GetDiagnosticSpan(DirectCast(node, NamespaceStatementSyntax))

                Case SyntaxKind.ClassBlock,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ModuleBlock
                    Return GetDiagnosticSpan(DirectCast(node, TypeBlockSyntax).BlockStatement)

                Case SyntaxKind.ClassStatement,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.ModuleStatement
                    Return GetDiagnosticSpan(DirectCast(node, TypeStatementSyntax))

                Case SyntaxKind.EnumBlock
                    Return GetDiagnosticSpanImpl(DirectCast(node, EnumBlockSyntax).EnumStatement, editKind)

                Case SyntaxKind.EnumStatement
                    Dim enumStatement = DirectCast(node, EnumStatementSyntax)
                    Return GetDiagnosticSpan(enumStatement.Modifiers, enumStatement.EnumKeyword, enumStatement.Identifier)

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.EventBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return GetDiagnosticSpan(DirectCast(node, MethodBlockBaseSyntax).BlockStatement)

                Case SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.EventStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DelegateSubStatement,
                    SyntaxKind.DelegateFunctionStatement
                    Return GetDiagnosticSpan(DirectCast(node, MethodBaseSyntax))

                Case SyntaxKind.PropertyBlock
                    Return GetDiagnosticSpan(DirectCast(node, PropertyBlockSyntax).PropertyStatement)

                Case SyntaxKind.PropertyStatement
                    Return GetDiagnosticSpan(DirectCast(node, PropertyStatementSyntax))

                Case SyntaxKind.FieldDeclaration
                    Dim fieldDeclaration = DirectCast(node, FieldDeclarationSyntax)
                    Return GetDiagnosticSpan(fieldDeclaration.Modifiers, fieldDeclaration.Declarators.First, fieldDeclaration.Declarators.Last)

                Case SyntaxKind.VariableDeclarator,
                     SyntaxKind.ModifiedIdentifier,
                     SyntaxKind.EnumMemberDeclaration,
                     SyntaxKind.TypeParameterSingleConstraintClause,
                     SyntaxKind.TypeParameterMultipleConstraintClause,
                     SyntaxKind.ClassConstraint,
                     SyntaxKind.StructureConstraint,
                     SyntaxKind.NewConstraint,
                     SyntaxKind.TypeConstraint
                    Return node.Span

                Case SyntaxKind.TypeParameter
                    Return DirectCast(node, TypeParameterSyntax).Identifier.Span

                Case SyntaxKind.TypeParameterList,
                     SyntaxKind.ParameterList,
                     SyntaxKind.AttributeList,
                     SyntaxKind.SimpleAsClause
                    If editKind = EditKind.Delete Then
                        Return GetDiagnosticSpanImpl(node.Parent, editKind)
                    Else
                        Return node.Span
                    End If

                Case SyntaxKind.AttributesStatement,
                     SyntaxKind.Attribute
                    Return node.Span

                Case SyntaxKind.Parameter
                    Dim parameter = DirectCast(node, ParameterSyntax)
                    Return GetDiagnosticSpan(parameter.Modifiers, parameter.Identifier, parameter)

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return GetDiagnosticSpan(DirectCast(node, LambdaExpressionSyntax).SubOrFunctionHeader)

                Case SyntaxKind.MultiLineIfBlock
                    Dim ifStatement = DirectCast(node, MultiLineIfBlockSyntax).IfStatement
                    Return GetDiagnosticSpan(ifStatement.IfKeyword, ifStatement.Condition, ifStatement.ThenKeyword)

                Case SyntaxKind.ElseIfBlock
                    Dim elseIfStatement = DirectCast(node, ElseIfBlockSyntax).ElseIfStatement
                    Return GetDiagnosticSpan(elseIfStatement.ElseIfKeyword, elseIfStatement.Condition, elseIfStatement.ThenKeyword)

                Case SyntaxKind.SingleLineIfStatement
                    Dim ifStatement = DirectCast(node, SingleLineIfStatementSyntax)
                    Return GetDiagnosticSpan(ifStatement.IfKeyword, ifStatement.Condition, ifStatement.ThenKeyword)

                Case SyntaxKind.SingleLineElseClause
                    Return DirectCast(node, SingleLineElseClauseSyntax).ElseKeyword.Span

                Case SyntaxKind.TryBlock
                    Return DirectCast(node, TryBlockSyntax).TryStatement.TryKeyword.Span

                Case SyntaxKind.CatchBlock
                    Return DirectCast(node, CatchBlockSyntax).CatchStatement.CatchKeyword.Span

                Case SyntaxKind.FinallyBlock
                    Return DirectCast(node, FinallyBlockSyntax).FinallyStatement.FinallyKeyword.Span

                Case SyntaxKind.SyncLockBlock
                    Return DirectCast(node, SyncLockBlockSyntax).SyncLockStatement.Span

                Case SyntaxKind.WithBlock
                    Return DirectCast(node, WithBlockSyntax).WithStatement.Span

                Case SyntaxKind.UsingBlock
                    Return DirectCast(node, UsingBlockSyntax).UsingStatement.Span

                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock
                    Return DirectCast(node, DoLoopBlockSyntax).DoStatement.Span

                Case SyntaxKind.WhileBlock
                    Return DirectCast(node, WhileBlockSyntax).WhileStatement.Span

                Case SyntaxKind.ForEachBlock,
                     SyntaxKind.ForBlock
                    Return DirectCast(node, ForOrForEachBlockSyntax).ForOrForEachStatement.Span

                Case SyntaxKind.AwaitExpression
                    Return DirectCast(node, AwaitExpressionSyntax).AwaitKeyword.Span

                Case SyntaxKind.AnonymousObjectCreationExpression
                    Dim newWith = DirectCast(node, AnonymousObjectCreationExpressionSyntax)
                    Return TextSpan.FromBounds(newWith.NewKeyword.Span.Start,
                                               newWith.Initializer.WithKeyword.Span.End)

                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(node, LambdaExpressionSyntax).SubOrFunctionHeader.Span

                Case SyntaxKind.QueryExpression
                    Return GetDiagnosticSpanImpl(DirectCast(node, QueryExpressionSyntax).Clauses.First(), editKind)

                Case SyntaxKind.WhereClause
                    Return DirectCast(node, WhereClauseSyntax).WhereKeyword.Span

                Case SyntaxKind.SelectClause
                    Return DirectCast(node, SelectClauseSyntax).SelectKeyword.Span

                Case SyntaxKind.FromClause
                    Return DirectCast(node, FromClauseSyntax).FromKeyword.Span

                Case SyntaxKind.AggregateClause
                    Return DirectCast(node, AggregateClauseSyntax).AggregateKeyword.Span

                Case SyntaxKind.LetClause
                    Return DirectCast(node, LetClauseSyntax).LetKeyword.Span

                Case SyntaxKind.SimpleJoinClause
                    Return DirectCast(node, SimpleJoinClauseSyntax).JoinKeyword.Span

                Case SyntaxKind.GroupJoinClause
                    Dim groupJoin = DirectCast(node, GroupJoinClauseSyntax)
                    Return TextSpan.FromBounds(groupJoin.GroupKeyword.SpanStart, groupJoin.JoinKeyword.Span.End)

                Case SyntaxKind.GroupByClause
                    Return DirectCast(node, GroupByClauseSyntax).GroupKeyword.Span

                Case SyntaxKind.FunctionAggregation
                    Return node.Span

                Case SyntaxKind.CollectionRangeVariable,
                     SyntaxKind.ExpressionRangeVariable
                    Return GetDiagnosticSpanImpl(node.Parent, editKind)

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    Dim partition = DirectCast(node, PartitionWhileClauseSyntax)
                    Return TextSpan.FromBounds(partition.SkipOrTakeKeyword.SpanStart, partition.WhileKeyword.Span.End)

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return node.Span

                Case SyntaxKind.JoinCondition
                    Return DirectCast(node, JoinConditionSyntax).EqualsKeyword.Span

                Case Else
                    Return node.Span
            End Select
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(ifKeyword As SyntaxToken, condition As SyntaxNode, thenKeywordOpt As SyntaxToken) As TextSpan
            Return TextSpan.FromBounds(ifKeyword.Span.Start,
                                       If(thenKeywordOpt.RawKind <> 0, thenKeywordOpt.Span.End, condition.Span.End))
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(node As NamespaceStatementSyntax) As TextSpan
            Return TextSpan.FromBounds(node.NamespaceKeyword.SpanStart, node.Name.Span.End)
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(node As TypeStatementSyntax) As TextSpan
            Return GetDiagnosticSpan(node.Modifiers,
                                     node.DeclarationKeyword,
                                     If(node.TypeParameterList, CType(node.Identifier, SyntaxNodeOrToken)))
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(modifiers As SyntaxTokenList, start As SyntaxNodeOrToken, endNode As SyntaxNodeOrToken) As TextSpan
            Return TextSpan.FromBounds(If(modifiers.Count <> 0, modifiers.First.SpanStart, start.SpanStart),
                                       endNode.Span.End)
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(header As MethodBaseSyntax) As TextSpan
            Dim startToken As SyntaxToken
            Dim endToken As SyntaxToken

            If header.Modifiers.Count > 0 Then
                startToken = header.Modifiers.First
            Else
                Select Case header.Kind
                    Case SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement
                        startToken = DirectCast(header, DelegateStatementSyntax).DelegateKeyword

                    Case SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement
                        startToken = DirectCast(header, DeclareStatementSyntax).DeclareKeyword

                    Case Else
                        startToken = header.DeclarationKeyword
                End Select
            End If

            If header.ParameterList IsNot Nothing Then
                endToken = header.ParameterList.CloseParenToken
            Else
                Select Case header.Kind
                    Case SyntaxKind.SubStatement,
                         SyntaxKind.FunctionStatement
                        endToken = DirectCast(header, MethodStatementSyntax).Identifier

                    Case SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement
                        endToken = DirectCast(header, DeclareStatementSyntax).LibraryName.Token

                    Case SyntaxKind.OperatorStatement
                        endToken = DirectCast(header, OperatorStatementSyntax).OperatorToken

                    Case SyntaxKind.SubNewStatement
                        endToken = DirectCast(header, SubNewStatementSyntax).NewKeyword

                    Case SyntaxKind.PropertyStatement
                        endToken = DirectCast(header, PropertyStatementSyntax).Identifier

                    Case SyntaxKind.EventStatement
                        endToken = DirectCast(header, EventStatementSyntax).Identifier

                    Case SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement
                        endToken = DirectCast(header, DelegateStatementSyntax).Identifier

                    Case SyntaxKind.SetAccessorStatement,
                         SyntaxKind.GetAccessorStatement,
                         SyntaxKind.AddHandlerAccessorStatement,
                         SyntaxKind.RemoveHandlerAccessorStatement,
                         SyntaxKind.RaiseEventAccessorStatement
                        endToken = header.DeclarationKeyword

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(header.Kind)
                End Select
            End If

            Return TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End)
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(lambda As LambdaHeaderSyntax) As TextSpan
            Dim startToken = If(lambda.Modifiers.Count <> 0, lambda.Modifiers.First, lambda.DeclarationKeyword)
            Dim endToken As SyntaxToken

            If lambda.ParameterList IsNot Nothing Then
                endToken = lambda.ParameterList.CloseParenToken
            Else
                endToken = lambda.DeclarationKeyword
            End If

            Return TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End)
        End Function

        Friend Overrides Function GetLambdaParameterDiagnosticSpan(lambda As SyntaxNode, ordinal As Integer) As TextSpan
            Select Case lambda.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(lambda, LambdaExpressionSyntax).SubOrFunctionHeader.ParameterList.Parameters(ordinal).Identifier.Span

                Case Else
                    Return lambda.Span
            End Select
        End Function

        Protected Overrides Function GetTopLevelDisplayName(node As SyntaxNode, editKind As EditKind) As String
            Return GetTopLevelDisplayNameImpl(node)
        End Function

        Protected Overrides Function GetStatementDisplayName(node As SyntaxNode, editKind As EditKind) As String
            Return GetStatementDisplayNameImpl(node, editKind)
        End Function

        Protected Overrides Function GetLambdaDisplayName(lambda As SyntaxNode) As String
            Return GetStatementDisplayNameImpl(lambda, EditKind.Update)
        End Function

        ' internal for testing
        Friend Shared Function GetTopLevelDisplayNameImpl(node As SyntaxNode) As String
            Select Case node.Kind
                Case SyntaxKind.OptionStatement
                    Return VBFeaturesResources.option_

                Case SyntaxKind.ImportsStatement
                    Return VBFeaturesResources.import

                Case SyntaxKind.NamespaceBlock,
                     SyntaxKind.NamespaceStatement
                    Return FeaturesResources.namespace_

                Case SyntaxKind.ClassBlock,
                     SyntaxKind.ClassStatement
                    Return FeaturesResources.class_

                Case SyntaxKind.StructureBlock,
                     SyntaxKind.StructureStatement
                    Return VBFeaturesResources.structure_

                Case SyntaxKind.InterfaceBlock,
                     SyntaxKind.InterfaceStatement
                    Return FeaturesResources.interface_

                Case SyntaxKind.ModuleBlock,
                     SyntaxKind.ModuleStatement
                    Return VBFeaturesResources.module_

                Case SyntaxKind.EnumBlock,
                     SyntaxKind.EnumStatement
                    Return FeaturesResources.enum_

                Case SyntaxKind.DelegateSubStatement,
                     SyntaxKind.DelegateFunctionStatement
                    Return FeaturesResources.delegate_

                Case SyntaxKind.FieldDeclaration
                    Dim declaration = DirectCast(node, FieldDeclarationSyntax)
                    Return If(declaration.Modifiers.Any(SyntaxKind.WithEventsKeyword), VBFeaturesResources.WithEvents_field,
                           If(declaration.Modifiers.Any(SyntaxKind.ConstKeyword), FeaturesResources.const_field, FeaturesResources.field))

                Case SyntaxKind.VariableDeclarator,
                     SyntaxKind.ModifiedIdentifier
                    Return GetTopLevelDisplayNameImpl(node.Parent)

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement
                    Return FeaturesResources.method

                Case SyntaxKind.OperatorBlock,
                     SyntaxKind.OperatorStatement
                    Return FeaturesResources.operator_

                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.SubNewStatement
                    Return FeaturesResources.constructor

                Case SyntaxKind.PropertyBlock

                    Return FeaturesResources.property_

                Case SyntaxKind.PropertyStatement
                    Return If(node.IsParentKind(SyntaxKind.PropertyBlock),
                        FeaturesResources.property_,
                        FeaturesResources.auto_property)

                Case SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement
                    Return FeaturesResources.event_

                Case SyntaxKind.EnumMemberDeclaration
                    Return FeaturesResources.enum_value

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement
                    Return VBFeaturesResources.property_accessor

                Case SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return FeaturesResources.event_accessor

                Case SyntaxKind.TypeParameterSingleConstraintClause,
                     SyntaxKind.TypeParameterMultipleConstraintClause,
                     SyntaxKind.ClassConstraint,
                     SyntaxKind.StructureConstraint,
                     SyntaxKind.NewConstraint,
                     SyntaxKind.TypeConstraint
                    Return FeaturesResources.type_constraint

                Case SyntaxKind.SimpleAsClause
                    Return VBFeaturesResources.as_clause

                Case SyntaxKind.TypeParameterList
                    Return VBFeaturesResources.type_parameters

                Case SyntaxKind.TypeParameter
                    Return FeaturesResources.type_parameter

                Case SyntaxKind.ParameterList
                    Return VBFeaturesResources.parameters

                Case SyntaxKind.Parameter
                    Return FeaturesResources.parameter

                Case SyntaxKind.AttributeList,
                     SyntaxKind.AttributesStatement
                    Return VBFeaturesResources.attributes

                Case SyntaxKind.Attribute
                    Return FeaturesResources.attribute

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind())
            End Select
        End Function

        ' internal for testing
        Friend Shared Function GetStatementDisplayNameImpl(node As SyntaxNode, kind As EditKind) As String
            Select Case node.Kind
                Case SyntaxKind.TryBlock
                    Return VBFeaturesResources.Try_block

                Case SyntaxKind.CatchBlock
                    Return VBFeaturesResources.Catch_clause

                Case SyntaxKind.FinallyBlock
                    Return VBFeaturesResources.Finally_clause

                Case SyntaxKind.UsingBlock
                    Return If(kind = EditKind.Update, VBFeaturesResources.Using_statement, VBFeaturesResources.Using_block)

                Case SyntaxKind.WithBlock
                    Return If(kind = EditKind.Update, VBFeaturesResources.With_statement, VBFeaturesResources.With_block)

                Case SyntaxKind.SyncLockBlock
                    Return If(kind = EditKind.Update, VBFeaturesResources.SyncLock_statement, VBFeaturesResources.SyncLock_block)

                Case SyntaxKind.ForEachBlock
                    Return If(kind = EditKind.Update, VBFeaturesResources.For_Each_statement, VBFeaturesResources.For_Each_block)

                Case SyntaxKind.OnErrorGoToMinusOneStatement,
                     SyntaxKind.OnErrorGoToZeroStatement,
                     SyntaxKind.OnErrorResumeNextStatement,
                     SyntaxKind.OnErrorGoToLabelStatement
                    Return VBFeaturesResources.On_Error_statement

                Case SyntaxKind.ResumeStatement,
                     SyntaxKind.ResumeNextStatement,
                     SyntaxKind.ResumeLabelStatement
                    Return VBFeaturesResources.Resume_statement

                Case SyntaxKind.YieldStatement
                    Return VBFeaturesResources.Yield_statement

                Case SyntaxKind.AwaitExpression
                    Return VBFeaturesResources.Await_expression

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return VBFeaturesResources.Lambda

                Case SyntaxKind.WhereClause
                    Return VBFeaturesResources.Where_clause

                Case SyntaxKind.SelectClause
                    Return VBFeaturesResources.Select_clause

                Case SyntaxKind.FromClause
                    Return VBFeaturesResources.From_clause

                Case SyntaxKind.AggregateClause
                    Return VBFeaturesResources.Aggregate_clause

                Case SyntaxKind.LetClause
                    Return VBFeaturesResources.Let_clause

                Case SyntaxKind.SimpleJoinClause
                    Return VBFeaturesResources.Join_clause

                Case SyntaxKind.GroupJoinClause
                    Return VBFeaturesResources.Group_Join_clause

                Case SyntaxKind.GroupByClause
                    Return VBFeaturesResources.Group_By_clause

                Case SyntaxKind.FunctionAggregation
                    Return VBFeaturesResources.Function_aggregation

                Case SyntaxKind.CollectionRangeVariable,
                     SyntaxKind.ExpressionRangeVariable
                    Return GetStatementDisplayNameImpl(node.Parent, kind)

                Case SyntaxKind.TakeWhileClause
                    Return VBFeaturesResources.Take_While_clause

                Case SyntaxKind.SkipWhileClause
                    Return VBFeaturesResources.Skip_While_clause

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return VBFeaturesResources.Ordering_clause

                Case SyntaxKind.JoinCondition
                    Return VBFeaturesResources.Join_condition

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind())
            End Select
        End Function

#End Region

#Region "Top-level Syntactic Rude Edits"
        Private Structure EditClassifier

            Private ReadOnly _analyzer As VisualBasicEditAndContinueAnalyzer
            Private ReadOnly _diagnostics As List(Of RudeEditDiagnostic)
            Private ReadOnly _match As Match(Of SyntaxNode)
            Private ReadOnly _oldNode As SyntaxNode
            Private ReadOnly _newNode As SyntaxNode
            Private ReadOnly _kind As EditKind
            Private ReadOnly _span As TextSpan?

            Public Sub New(analyzer As VisualBasicEditAndContinueAnalyzer,
                           diagnostics As List(Of RudeEditDiagnostic),
                           oldNode As SyntaxNode,
                           newNode As SyntaxNode,
                           kind As EditKind,
                           Optional match As Match(Of SyntaxNode) = Nothing,
                           Optional span As TextSpan? = Nothing)

                Me._analyzer = analyzer
                Me._diagnostics = diagnostics
                Me._oldNode = oldNode
                Me._newNode = newNode
                Me._kind = kind
                Me._span = span
                Me._match = match
            End Sub

            Private Sub ReportError(kind As RudeEditKind)
                ReportError(kind, {GetDisplayName()})
            End Sub

            Private Sub ReportError(kind As RudeEditKind, args As String())
                _diagnostics.Add(New RudeEditDiagnostic(kind, GetSpan(), If(_newNode, _oldNode), args))
            End Sub

            Private Sub ReportError(kind As RudeEditKind, spanNode As SyntaxNode, displayNode As SyntaxNode)
                _diagnostics.Add(New RudeEditDiagnostic(kind, GetDiagnosticSpanImpl(spanNode, Me._kind), displayNode, {GetTopLevelDisplayNameImpl(displayNode)}))
            End Sub

            Private Function GetSpan() As TextSpan
                If _span.HasValue Then
                    Return _span.Value
                End If

                If _newNode Is Nothing Then
                    Return _analyzer.GetDeletedNodeDiagnosticSpan(_match.Matches, _oldNode)
                Else
                    Return GetDiagnosticSpanImpl(_newNode, _kind)
                End If
            End Function

            Private Function GetDisplayName() As String
                Return GetTopLevelDisplayNameImpl(If(_newNode, _oldNode))
            End Function

            Public Sub ClassifyEdit()
                Select Case _kind
                    Case EditKind.Delete
                        ClassifyDelete(_oldNode)
                        Return

                    Case EditKind.Update
                        ClassifyUpdate(_oldNode, _newNode)
                        Return

                    Case EditKind.Move
                        ClassifyMove(_oldNode, _newNode)
                        Return

                    Case EditKind.Insert
                        ClassifyInsert(_newNode)
                        Return

                    Case EditKind.Reorder
                        ClassifyReorder(_oldNode, _newNode)
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(_kind)
                End Select
            End Sub

#Region "Move and Reorder"
            Private Sub ClassifyMove(oldNode As SyntaxNode, newNode As SyntaxNode)
                ReportError(RudeEditKind.Move)
            End Sub

            Private Sub ClassifyReorder(oldNode As SyntaxNode, newNode As SyntaxNode)
                Select Case newNode.Kind
                    Case SyntaxKind.OptionStatement,
                         SyntaxKind.ImportsStatement,
                         SyntaxKind.AttributesStatement,
                         SyntaxKind.NamespaceBlock,
                         SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.EnumBlock,
                         SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement,
                         SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement,
                         SyntaxKind.ConstructorBlock,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.PropertyBlock,
                         SyntaxKind.EventBlock,
                         SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock,
                         SyntaxKind.ClassConstraint,
                         SyntaxKind.StructureConstraint,
                         SyntaxKind.NewConstraint,
                         SyntaxKind.TypeConstraint,
                         SyntaxKind.AttributeList,
                         SyntaxKind.Attribute
                        ' We'll ignore these edits. A general policy is to ignore edits that are only discoverable via reflection.
                        Return

                    Case SyntaxKind.SubStatement,
                         SyntaxKind.FunctionStatement
                        ' Interface methods. We could allow reordering of non-COM interface methods.
                        Debug.Assert(oldNode.Parent.IsKind(SyntaxKind.InterfaceBlock) AndAlso newNode.Parent.IsKind(SyntaxKind.InterfaceBlock))
                        ReportError(RudeEditKind.Move)
                        Return

                    Case SyntaxKind.PropertyStatement,
                         SyntaxKind.FieldDeclaration,
                         SyntaxKind.EventStatement,
                         SyntaxKind.VariableDeclarator
                        ' Maybe we could allow changing order of field declarations unless the containing type layout is sequential,
                        ' and it's not a COM interface.
                        ReportError(RudeEditKind.Move)
                        Return

                    Case SyntaxKind.EnumMemberDeclaration
                        ' To allow this change we would need to check that values of all fields of the enum 
                        ' are preserved, or make sure we can update all method bodies that accessed those that changed.
                        ReportError(RudeEditKind.Move)
                        Return

                    Case SyntaxKind.TypeParameter,
                         SyntaxKind.Parameter
                        ReportError(RudeEditKind.Move)
                        Return

                    Case SyntaxKind.ModifiedIdentifier
                        ' Identifier can only moved from one VariableDeclarator to another if both are part of 
                        ' the same declaration. We could allow these moves if the order and types of variables 
                        ' didn't change.
                        ReportError(RudeEditKind.Move)
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(newNode.Kind)
                End Select
            End Sub

#End Region

#Region "Insert"
            Private Sub ClassifyInsert(node As SyntaxNode)
                Select Case node.Kind
                    Case SyntaxKind.OptionStatement,
                         SyntaxKind.ImportsStatement,
                         SyntaxKind.NamespaceBlock
                        ReportError(RudeEditKind.Insert)
                        Return

                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock
                        ClassifyTypeWithPossibleExternMembersInsert(DirectCast(node, TypeBlockSyntax))
                        Return

                    Case SyntaxKind.InterfaceBlock
                        ClassifyTypeInsert(DirectCast(node, TypeBlockSyntax).BlockStatement.Modifiers)
                        Return

                    Case SyntaxKind.EnumBlock
                        ClassifyTypeInsert(DirectCast(node, EnumBlockSyntax).EnumStatement.Modifiers)
                        Return

                    Case SyntaxKind.ModuleBlock
                        ' Modules can't be nested or private
                        ReportError(RudeEditKind.Insert)
                        Return

                    Case SyntaxKind.DelegateSubStatement,
                         SyntaxKind.DelegateFunctionStatement
                        ClassifyTypeInsert(DirectCast(node, DelegateStatementSyntax).Modifiers)
                        Return

                    Case SyntaxKind.SubStatement,               ' interface method
                         SyntaxKind.FunctionStatement           ' interface method
                        ReportError(RudeEditKind.Insert)
                        Return

                    Case SyntaxKind.PropertyBlock
                        ClassifyModifiedMemberInsert(DirectCast(node, PropertyBlockSyntax).PropertyStatement.Modifiers)
                        Return

                    Case SyntaxKind.PropertyStatement           ' autoprop or interface property
                        ' We don't need to check whether the container is an interface, since we disallow 
                        ' adding public methods And all methods in interface declarations are public.
                        ClassifyModifiedMemberInsert(DirectCast(node, PropertyStatementSyntax).Modifiers)
                        Return

                    Case SyntaxKind.EventBlock
                        ClassifyModifiedMemberInsert(DirectCast(node, EventBlockSyntax).EventStatement.Modifiers)
                        Return

                    Case SyntaxKind.EventStatement
                        ClassifyModifiedMemberInsert(DirectCast(node, EventStatementSyntax).Modifiers)
                        Return

                    Case SyntaxKind.OperatorBlock
                        ReportError(RudeEditKind.InsertOperator)
                        Return

                    Case SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock
                        ClassifyMethodInsert(DirectCast(node, MethodBlockSyntax).SubOrFunctionStatement)
                        Return

                    Case SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement
                        ' CLR doesn't support adding P/Invokes
                        ReportError(RudeEditKind.Insert)
                        Return

                    Case SyntaxKind.ConstructorBlock
                        If SyntaxUtilities.IsParameterlessConstructor(node) Then
                            Return
                        End If

                        ClassifyModifiedMemberInsert(DirectCast(node, MethodBlockBaseSyntax).BlockStatement.Modifiers)
                        Return

                    Case SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock
                        Return

                    Case SyntaxKind.FieldDeclaration
                        ClassifyFieldInsert(DirectCast(node, FieldDeclarationSyntax))
                        Return

                    Case SyntaxKind.VariableDeclarator
                        ' Ignore, errors will be reported for children (ModifiedIdentifier, AsClause)
                        Return

                    Case SyntaxKind.ModifiedIdentifier
                        ClassifyFieldInsert(DirectCast(node, ModifiedIdentifierSyntax))
                        Return

                    Case SyntaxKind.EnumMemberDeclaration,
                         SyntaxKind.TypeParameter,
                         SyntaxKind.StructureConstraint,
                         SyntaxKind.TypeParameterSingleConstraintClause,
                         SyntaxKind.TypeParameterMultipleConstraintClause,
                         SyntaxKind.ClassConstraint,
                         SyntaxKind.StructureConstraint,
                         SyntaxKind.NewConstraint,
                         SyntaxKind.TypeConstraint,
                         SyntaxKind.TypeParameterList,
                         SyntaxKind.Parameter,
                         SyntaxKind.Attribute,
                         SyntaxKind.AttributeList,
                         SyntaxKind.AttributesStatement,
                         SyntaxKind.SimpleAsClause
                        ReportError(RudeEditKind.Insert)
                        Return

                    Case SyntaxKind.ParameterList
                        ClassifyParameterInsert(DirectCast(node, ParameterListSyntax))
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind())
                End Select
            End Sub

            Private Function ClassifyModifiedMemberInsert(modifiers As SyntaxTokenList) As Boolean
                If modifiers.Any(SyntaxKind.OverridableKeyword) OrElse
                   modifiers.Any(SyntaxKind.MustOverrideKeyword) OrElse
                   modifiers.Any(SyntaxKind.OverridesKeyword) Then

                    ReportError(RudeEditKind.InsertVirtual)
                    Return False
                End If

                Return True
            End Function

            Private Function ClassifyTypeInsert(modifiers As SyntaxTokenList) As Boolean
                Return ClassifyModifiedMemberInsert(modifiers)
            End Function

            Private Sub ClassifyTypeWithPossibleExternMembersInsert(type As TypeBlockSyntax)
                If Not ClassifyTypeInsert(type.BlockStatement.Modifiers) Then
                    Return
                End If

                For Each member In type.Members
                    Dim modifiers As SyntaxTokenList = Nothing

                    Select Case member.Kind
                        Case SyntaxKind.DeclareFunctionStatement,
                             SyntaxKind.DeclareSubStatement
                            ReportError(RudeEditKind.Insert, member, member)

                        Case SyntaxKind.PropertyStatement
                            modifiers = DirectCast(member, PropertyStatementSyntax).Modifiers

                        Case SyntaxKind.SubBlock,
                             SyntaxKind.FunctionBlock
                            modifiers = DirectCast(member, MethodBlockBaseSyntax).BlockStatement.Modifiers
                    End Select

                    ' TODO: DllImport/Declare?
                    'If (modifiers.Any(SyntaxKind.MustOverrideKeyword)) Then
                    '    ReportError(RudeEditKind.InsertMustOverride, member, member)
                    'End If
                Next
            End Sub

            Private Sub ClassifyMethodInsert(method As MethodStatementSyntax)
                If method.TypeParameterList IsNot Nothing Then
                    ReportError(RudeEditKind.InsertGenericMethod)
                End If

                If method.HandlesClause IsNot Nothing Then
                    ReportError(RudeEditKind.InsertHandlesClause)
                End If

                ClassifyModifiedMemberInsert(method.Modifiers)
            End Sub

            Private Sub ClassifyFieldInsert(field As FieldDeclarationSyntax)
                ' Can't insert WithEvents field since it is effectively a virtual property.
                If field.Modifiers.Any(SyntaxKind.WithEventsKeyword) Then
                    ReportError(RudeEditKind.Insert)
                    Return
                End If

                Dim containingType = field.Parent
                If containingType.IsKind(SyntaxKind.ModuleBlock) Then
                    ReportError(RudeEditKind.Insert)
                    Return
                End If
            End Sub

            Private Sub ClassifyFieldInsert(fieldVariableName As ModifiedIdentifierSyntax)
                ClassifyFieldInsert(DirectCast(fieldVariableName.Parent.Parent, FieldDeclarationSyntax))
            End Sub

            Private Sub ClassifyParameterInsert(parameterList As ParameterListSyntax)
                ' Sub M -> Sub M() is ok
                If parameterList.Parameters.Count = 0 Then
                    Return
                End If

                ReportError(RudeEditKind.Insert)
            End Sub

#End Region

#Region "Delete"
            Private Sub ClassifyDelete(oldNode As SyntaxNode)
                Select Case oldNode.Kind
                    Case SyntaxKind.OptionStatement,
                         SyntaxKind.ImportsStatement,
                         SyntaxKind.AttributesStatement,
                         SyntaxKind.NamespaceBlock,
                         SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement,
                         SyntaxKind.EnumBlock,
                         SyntaxKind.FieldDeclaration,
                         SyntaxKind.ModifiedIdentifier,
                         SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.SubStatement,
                         SyntaxKind.FunctionStatement,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.PropertyBlock,
                         SyntaxKind.PropertyStatement,
                         SyntaxKind.EventBlock,
                         SyntaxKind.EventStatement,
                         SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement
                        ' To allow removal of declarations we would need to update method bodies that 
                        ' were previously binding to them but now are binding to another symbol that was previously hidden.
                        ReportError(RudeEditKind.Delete)
                        Return

                    Case SyntaxKind.ConstructorBlock
                        ' Allow deletion of a parameterless constructor.
                        ' Semantic analysis reports an error if the parameterless ctor isn't replaced by a default ctor.
                        If Not SyntaxUtilities.IsParameterlessConstructor(oldNode) Then
                            ReportError(RudeEditKind.Delete)
                        End If

                        Return

                    Case SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock
                        ' An accessor can be removed. Accessors are not hiding other symbols.
                        ' If the new compilation still uses the removed accessor a semantic error will be reported.
                        ' For simplicity though we disallow deletion of accessors for now.
                        ReportError(RudeEditKind.Delete)
                        Return

                    Case SyntaxKind.AttributeList,
                         SyntaxKind.Attribute
                        ' To allow removal of attributes we would need to check if the removed attribute
                        ' is a pseudo-custom attribute that CLR allows us to change, or if it is a compiler well-know attribute
                        ' that affects the generated IL.
                        ReportError(RudeEditKind.Delete)
                        Return

                    Case SyntaxKind.EnumMemberDeclaration
                        ' We could allow removing enum member if it didn't affect the values of other enum members.
                        ' If the updated compilation binds without errors it means that the enum value wasn't used.
                        ReportError(RudeEditKind.Delete)
                        Return

                    Case SyntaxKind.TypeParameter,
                         SyntaxKind.TypeParameterList,
                         SyntaxKind.Parameter,
                         SyntaxKind.TypeParameterSingleConstraintClause,
                         SyntaxKind.TypeParameterMultipleConstraintClause,
                         SyntaxKind.ClassConstraint,
                         SyntaxKind.StructureConstraint,
                         SyntaxKind.NewConstraint,
                         SyntaxKind.TypeConstraint,
                         SyntaxKind.SimpleAsClause
                        ReportError(RudeEditKind.Delete)
                        Return

                    Case SyntaxKind.ParameterList
                        ClassifyDelete(DirectCast(oldNode, ParameterListSyntax))

                    Case SyntaxKind.VariableDeclarator
                        ' Ignore, errors will be reported for children (ModifiedIdentifier, AsClause)
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(oldNode.Kind)
                End Select
            End Sub

            Private Sub ClassifyDelete(oldNode As ParameterListSyntax)
                ' Sub Goo() -> Sub Goo is ok
                If oldNode.Parameters.Count = 0 Then
                    Return
                End If

                ReportError(RudeEditKind.Delete)
            End Sub
#End Region

#Region "Update"
            Private Sub ClassifyUpdate(oldNode As SyntaxNode, newNode As SyntaxNode)
                Select Case newNode.Kind
                    Case SyntaxKind.OptionStatement
                        ReportError(RudeEditKind.Update)
                        Return

                    Case SyntaxKind.ImportsStatement
                        ReportError(RudeEditKind.Update)
                        Return

                    Case SyntaxKind.NamespaceBlock
                        Return

                    Case SyntaxKind.NamespaceStatement
                        ClassifyUpdate(DirectCast(oldNode, NamespaceStatementSyntax), DirectCast(newNode, NamespaceStatementSyntax))
                        Return

                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.ModuleBlock
                        ClassifyUpdate(DirectCast(oldNode, TypeBlockSyntax), DirectCast(newNode, TypeBlockSyntax))
                        Return

                    Case SyntaxKind.ClassStatement,
                         SyntaxKind.StructureStatement,
                         SyntaxKind.InterfaceStatement,
                         SyntaxKind.ModuleStatement
                        ClassifyUpdate(DirectCast(oldNode, TypeStatementSyntax), DirectCast(newNode, TypeStatementSyntax))
                        Return

                    Case SyntaxKind.EnumBlock
                        Return

                    Case SyntaxKind.EnumStatement
                        ClassifyUpdate(DirectCast(oldNode, EnumStatementSyntax), DirectCast(newNode, EnumStatementSyntax))
                        Return

                    Case SyntaxKind.DelegateSubStatement,
                         SyntaxKind.DelegateFunctionStatement
                        ClassifyUpdate(DirectCast(oldNode, DelegateStatementSyntax), DirectCast(newNode, DelegateStatementSyntax))
                        Return

                    Case SyntaxKind.FieldDeclaration
                        ClassifyUpdate(DirectCast(oldNode, FieldDeclarationSyntax), DirectCast(newNode, FieldDeclarationSyntax))
                        Return

                    Case SyntaxKind.VariableDeclarator
                        ClassifyUpdate(DirectCast(oldNode, VariableDeclaratorSyntax), DirectCast(newNode, VariableDeclaratorSyntax))
                        Return

                    Case SyntaxKind.ModifiedIdentifier
                        ClassifyUpdate(DirectCast(oldNode, ModifiedIdentifierSyntax), DirectCast(newNode, ModifiedIdentifierSyntax))
                        Return

                    Case SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock
                        ClassifyUpdate(DirectCast(oldNode, MethodBlockSyntax), DirectCast(newNode, MethodBlockSyntax))
                        Return

                    Case SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement
                        ClassifyUpdate(DirectCast(oldNode, DeclareStatementSyntax), DirectCast(newNode, DeclareStatementSyntax))
                        Return

                    Case SyntaxKind.SubStatement,
                         SyntaxKind.FunctionStatement
                        ClassifyUpdate(DirectCast(oldNode, MethodStatementSyntax), DirectCast(newNode, MethodStatementSyntax))
                        Return

                    Case SyntaxKind.SimpleAsClause
                        ClassifyUpdate(DirectCast(oldNode, SimpleAsClauseSyntax), DirectCast(newNode, SimpleAsClauseSyntax))
                        Return

                    Case SyntaxKind.OperatorBlock
                        ClassifyUpdate(DirectCast(oldNode, OperatorBlockSyntax), DirectCast(newNode, OperatorBlockSyntax))
                        Return

                    Case SyntaxKind.OperatorStatement
                        ClassifyUpdate(DirectCast(oldNode, OperatorStatementSyntax), DirectCast(newNode, OperatorStatementSyntax))
                        Return

                    Case SyntaxKind.ConstructorBlock
                        ClassifyUpdate(DirectCast(oldNode, ConstructorBlockSyntax), DirectCast(newNode, ConstructorBlockSyntax))
                        Return

                    Case SyntaxKind.SubNewStatement
                        ClassifyUpdate(DirectCast(oldNode, SubNewStatementSyntax), DirectCast(newNode, SubNewStatementSyntax))
                        Return

                    Case SyntaxKind.PropertyBlock
                        Return

                    Case SyntaxKind.PropertyStatement
                        ClassifyUpdate(DirectCast(oldNode, PropertyStatementSyntax), DirectCast(newNode, PropertyStatementSyntax))
                        Return

                    Case SyntaxKind.EventBlock
                        Return

                    Case SyntaxKind.EventStatement
                        ClassifyUpdate(DirectCast(oldNode, EventStatementSyntax), DirectCast(newNode, EventStatementSyntax))
                        Return

                    Case SyntaxKind.GetAccessorStatement,
                         SyntaxKind.SetAccessorStatement,
                         SyntaxKind.AddHandlerAccessorStatement,
                         SyntaxKind.RemoveHandlerAccessorStatement,
                         SyntaxKind.RaiseEventAccessorStatement
                        Return

                    Case SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock
                        ClassifyUpdate(DirectCast(oldNode, AccessorBlockSyntax), DirectCast(newNode, AccessorBlockSyntax))
                        Return

                    Case SyntaxKind.EnumMemberDeclaration
                        ClassifyUpdate(DirectCast(oldNode, EnumMemberDeclarationSyntax), DirectCast(newNode, EnumMemberDeclarationSyntax))
                        Return

                    Case SyntaxKind.StructureConstraint,
                         SyntaxKind.ClassConstraint,
                         SyntaxKind.NewConstraint
                        ReportError(RudeEditKind.ConstraintKindUpdate,
                                    {DirectCast(oldNode, SpecialConstraintSyntax).ConstraintKeyword.ValueText,
                                     DirectCast(newNode, SpecialConstraintSyntax).ConstraintKeyword.ValueText})
                        Return

                    Case SyntaxKind.TypeConstraint
                        ReportError(RudeEditKind.TypeUpdate)
                        Return

                    Case SyntaxKind.TypeParameterMultipleConstraintClause,
                         SyntaxKind.TypeParameterSingleConstraintClause
                        Return

                    Case SyntaxKind.TypeParameter
                        ClassifyUpdate(DirectCast(oldNode, TypeParameterSyntax), DirectCast(newNode, TypeParameterSyntax))
                        Return

                    Case SyntaxKind.Parameter
                        ClassifyUpdate(DirectCast(oldNode, ParameterSyntax), DirectCast(newNode, ParameterSyntax))
                        Return

                    Case SyntaxKind.Attribute
                        ReportError(RudeEditKind.Update)
                        Return

                    Case SyntaxKind.TypeParameterList,
                         SyntaxKind.ParameterList,
                         SyntaxKind.AttributeList
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(newNode.Kind)
                End Select
            End Sub

            Private Sub ClassifyUpdate(oldNode As NamespaceStatementSyntax, newNode As NamespaceStatementSyntax)
                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.Name, newNode.Name))
                ReportError(RudeEditKind.Renamed)
            End Sub

            Private Sub ClassifyUpdate(oldNode As TypeStatementSyntax, newNode As TypeStatementSyntax)
                If oldNode.RawKind <> newNode.RawKind Then
                    ReportError(RudeEditKind.TypeKindUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                ReportError(RudeEditKind.Renamed)
            End Sub

            Private Sub ClassifyUpdate(oldNode As TypeBlockSyntax, newNode As TypeBlockSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Inherits, newNode.Inherits) OrElse
                   Not SyntaxFactory.AreEquivalent(oldNode.Implements, newNode.Implements) Then
                    ReportError(RudeEditKind.BaseTypeOrInterfaceUpdate)
                End If

                ' type member list separators
            End Sub

            Private Sub ClassifyUpdate(oldNode As EnumStatementSyntax, newNode As EnumStatementSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.UnderlyingType, newNode.UnderlyingType))
                ReportError(RudeEditKind.EnumUnderlyingTypeUpdate)
            End Sub

            Private Sub ClassifyUpdate(oldNode As DelegateStatementSyntax, newNode As DelegateStatementSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                ' Function changed to Sub or vice versa. Note that Function doesn't need to have AsClause.
                If oldNode.RawKind <> newNode.RawKind Then
                    ReportError(RudeEditKind.TypeUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.AsClause, newNode.AsClause) Then
                    ReportError(RudeEditKind.TypeUpdate)
                    Return
                End If

                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                ReportError(RudeEditKind.Renamed)
            End Sub

            Private Sub ClassifyUpdate(oldNode As FieldDeclarationSyntax, newNode As FieldDeclarationSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                End If

                ' VariableDeclarator separators were modified
            End Sub

            Private Sub ClassifyUpdate(oldNode As ModifiedIdentifierSyntax, newNode As ModifiedIdentifierSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                ' TODO (tomat): We could be smarter and consider the following syntax changes to be legal:
                ' Dim a? As Integer <-> Dim a As Integer?
                ' Dim a() As Integer <-> Dim a As Integer()

                If Not SyntaxFactory.AreEquivalent(oldNode.ArrayRankSpecifiers, newNode.ArrayRankSpecifiers) OrElse
                   Not SyntaxFactory.AreEquivalent(oldNode.Nullable, newNode.Nullable) Then
                    ReportError(RudeEditKind.TypeUpdate)
                    Return
                End If

                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.ArrayBounds, newNode.ArrayBounds))

                If oldNode.ArrayBounds Is Nothing OrElse
                    newNode.ArrayBounds Is Nothing OrElse
                    oldNode.ArrayBounds.Arguments.Count <> newNode.ArrayBounds.Arguments.Count Then
                    ReportError(RudeEditKind.TypeUpdate)
                    Return
                End If

                ' Otherwise only the size of the array changed, which is a legal initializer update
                ' unless it contains lambdas, queries etc.
                ClassifyDeclarationBodyRudeUpdates(newNode)
            End Sub

            Private Sub ClassifyUpdate(oldNode As VariableDeclaratorSyntax, newNode As VariableDeclaratorSyntax)
                Dim typeDeclaration = DirectCast(oldNode.Parent.Parent, TypeBlockSyntax)
                If typeDeclaration.BlockStatement.Arity > 0 Then
                    ReportError(RudeEditKind.GenericTypeInitializerUpdate)
                    Return
                End If

                If ClassifyTypeAndInitializerUpdates(oldNode.Initializer,
                                                  oldNode.AsClause,
                                                  newNode.Initializer,
                                                  newNode.AsClause) Then
                    ' Check if a constant field is updated:
                    Dim fieldDeclaration = DirectCast(oldNode.Parent, FieldDeclarationSyntax)
                    If fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword) Then
                        ReportError(RudeEditKind.Update)
                        Return
                    End If
                End If
            End Sub

            Private Sub ClassifyUpdate(oldNode As PropertyStatementSyntax, newNode As PropertyStatementSyntax)
                If Not IncludesSignificantPropertyModifiers(oldNode.Modifiers, newNode.Modifiers) OrElse
                   Not IncludesSignificantPropertyModifiers(newNode.Modifiers, oldNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.ImplementsClause, newNode.ImplementsClause) Then
                    ReportError(RudeEditKind.ImplementsClauseUpdate)
                    Return
                End If

                If ClassifyTypeAndInitializerUpdates(oldNode.Initializer, oldNode.AsClause, newNode.Initializer, newNode.AsClause) Then
                    ' change in an initializer of an auto-property
                    Dim typeDeclaration = DirectCast(oldNode.Parent, TypeBlockSyntax)
                    If typeDeclaration.BlockStatement.Arity > 0 Then
                        ReportError(RudeEditKind.GenericTypeInitializerUpdate)
                        Return
                    End If
                End If
            End Sub

            Private Shared Function IncludesSignificantPropertyModifiers(subset As SyntaxTokenList, superset As SyntaxTokenList) As Boolean
                For Each modifier In subset
                    ' ReadOnly and WriteOnly keywords are redundant, it would be a semantic error if they Then didn't match the present accessors. 
                    ' We want to allow adding an accessor to a property, which requires change in the RO/WO modifiers.
                    If modifier.IsKind(SyntaxKind.ReadOnlyKeyword) OrElse
                       modifier.IsKind(SyntaxKind.WriteOnlyKeyword) Then
                        Continue For
                    End If

                    If Not superset.Any(modifier.Kind) Then
                        Return False
                    End If
                Next

                Return True
            End Function

            ' Returns true if the initializer has changed.
            Private Function ClassifyTypeAndInitializerUpdates(oldEqualsValue As EqualsValueSyntax,
                                                               oldClause As AsClauseSyntax,
                                                               newEqualsValue As EqualsValueSyntax,
                                                               newClause As AsClauseSyntax) As Boolean

                Dim oldInitializer = GetInitializerExpression(oldEqualsValue, oldClause)
                Dim newInitializer = GetInitializerExpression(newEqualsValue, newClause)

                If newInitializer IsNot Nothing AndAlso Not SyntaxFactory.AreEquivalent(oldInitializer, newInitializer) Then
                    ClassifyDeclarationBodyRudeUpdates(newInitializer)
                    Return True
                End If

                Return False
            End Function

            Private Sub ClassifyUpdate(oldNode As EventStatementSyntax, newNode As EventStatementSyntax)
                ' A custom event can't be matched with a field event and vice versa:
                Debug.Assert(SyntaxFactory.AreEquivalent(oldNode.CustomKeyword, newNode.CustomKeyword))

                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.ImplementsClause, newNode.ImplementsClause) Then
                    ReportError(RudeEditKind.ImplementsClauseUpdate)
                    Return
                End If

                Dim oldHasGeneratedType = oldNode.ParameterList IsNot Nothing
                Dim newHasGeneratedType = newNode.ParameterList IsNot Nothing

                Debug.Assert(oldHasGeneratedType <> newHasGeneratedType)
                ReportError(RudeEditKind.TypeUpdate)
            End Sub

            Private Sub ClassifyUpdate(oldNode As MethodBlockSyntax, newNode As MethodBlockSyntax)
                ClassifyMethodBodyRudeUpdate(oldNode,
                                             newNode,
                                             containingMethod:=newNode,
                                             containingType:=DirectCast(newNode.Parent, TypeBlockSyntax))
            End Sub

            Private Sub ClassifyUpdate(oldNode As MethodStatementSyntax, newNode As MethodStatementSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.DeclarationKeyword, newNode.DeclarationKeyword) Then
                    ReportError(RudeEditKind.MethodKindUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                If Not ClassifyMethodModifierUpdate(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                ' TODO (tomat): We can support this
                If Not SyntaxFactory.AreEquivalent(oldNode.HandlesClause, newNode.HandlesClause) Then
                    ReportError(RudeEditKind.HandlesClauseUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.ImplementsClause, newNode.ImplementsClause) Then
                    ReportError(RudeEditKind.ImplementsClauseUpdate)
                    Return
                End If
            End Sub

            Private Function ClassifyMethodModifierUpdate(oldModifiers As SyntaxTokenList, newModifiers As SyntaxTokenList) As Boolean
                Dim oldAsyncIndex = oldModifiers.IndexOf(SyntaxKind.AsyncKeyword)
                Dim newAsyncIndex = newModifiers.IndexOf(SyntaxKind.AsyncKeyword)

                If oldAsyncIndex >= 0 Then
                    oldModifiers = oldModifiers.RemoveAt(oldAsyncIndex)
                End If

                If newAsyncIndex >= 0 Then
                    newModifiers = newModifiers.RemoveAt(newAsyncIndex)
                End If

                ' 'async' keyword is allowed to add, but not to remove
                If oldAsyncIndex >= 0 AndAlso newAsyncIndex < 0 Then
                    Return False
                End If

                Dim oldIteratorIndex = oldModifiers.IndexOf(SyntaxKind.IteratorKeyword)
                Dim newIteratorIndex = newModifiers.IndexOf(SyntaxKind.IteratorKeyword)

                If oldIteratorIndex >= 0 Then
                    oldModifiers = oldModifiers.RemoveAt(oldIteratorIndex)
                End If

                If newIteratorIndex >= 0 Then
                    newModifiers = newModifiers.RemoveAt(newIteratorIndex)
                End If

                ' 'iterator' keyword is allowed to add, but not to remove
                If oldIteratorIndex >= 0 AndAlso newIteratorIndex < 0 Then
                    Return False
                End If

                Return SyntaxFactory.AreEquivalent(oldModifiers, newModifiers)
            End Function

            Private Sub ClassifyUpdate(oldNode As DeclareStatementSyntax, newNode As DeclareStatementSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.CharsetKeyword, newNode.CharsetKeyword) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.LibraryName, newNode.LibraryName) Then
                    ReportError(RudeEditKind.DeclareLibraryUpdate)
                    Return
                End If

                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.AliasName, newNode.AliasName))
                ReportError(RudeEditKind.DeclareAliasUpdate)
            End Sub

            Private Sub ClassifyUpdate(oldNode As OperatorBlockSyntax, newNode As OperatorBlockSyntax)
                ClassifyMethodBodyRudeUpdate(oldNode,
                                             newNode,
                                             containingMethod:=Nothing,
                                             containingType:=DirectCast(newNode.Parent, TypeBlockSyntax))
            End Sub

            Private Sub ClassifyUpdate(oldNode As OperatorStatementSyntax, newNode As OperatorStatementSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.OperatorToken, newNode.OperatorToken))
                ReportError(RudeEditKind.Renamed)
            End Sub

            Private Sub ClassifyUpdate(oldNode As AccessorBlockSyntax, newNode As AccessorBlockSyntax)
                Debug.Assert(newNode.Parent.IsKind(SyntaxKind.EventBlock) OrElse
                             newNode.Parent.IsKind(SyntaxKind.PropertyBlock))

                ClassifyMethodBodyRudeUpdate(oldNode,
                                             newNode,
                                             containingMethod:=Nothing,
                                             containingType:=DirectCast(newNode.Parent.Parent, TypeBlockSyntax))
            End Sub

            Private Sub ClassifyUpdate(oldNode As AccessorStatementSyntax, newNode As AccessorStatementSyntax)
                If oldNode.RawKind <> newNode.RawKind Then
                    ReportError(RudeEditKind.AccessorKindUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If
            End Sub

            Private Sub ClassifyUpdate(oldNode As EnumMemberDeclarationSyntax, newNode As EnumMemberDeclarationSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.Initializer, newNode.Initializer))
                ReportError(RudeEditKind.InitializerUpdate)
            End Sub

            Private Sub ClassifyUpdate(oldNode As ConstructorBlockSyntax, newNode As ConstructorBlockSyntax)
                ClassifyMethodBodyRudeUpdate(oldNode,
                                             newNode,
                                             containingMethod:=Nothing,
                                             containingType:=DirectCast(newNode.Parent, TypeBlockSyntax))
            End Sub

            Private Sub ClassifyUpdate(oldNode As SubNewStatementSyntax, newNode As SubNewStatementSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If
            End Sub

            Private Sub ClassifyUpdate(oldNode As SimpleAsClauseSyntax, newNode As SimpleAsClauseSyntax)
                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.Type, newNode.Type))
                ReportError(RudeEditKind.TypeUpdate, newNode.Parent, newNode.Parent)
            End Sub

            Private Sub ClassifyUpdate(oldNode As TypeParameterSyntax, newNode As TypeParameterSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                Debug.Assert(Not SyntaxFactory.AreEquivalent(oldNode.VarianceKeyword, newNode.VarianceKeyword))
                ReportError(RudeEditKind.VarianceUpdate)
            End Sub

            Private Sub ClassifyUpdate(oldNode As ParameterSyntax, newNode As ParameterSyntax)
                If Not SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier) Then
                    ReportError(RudeEditKind.Renamed)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers) Then
                    ReportError(RudeEditKind.ModifiersUpdate)
                    Return
                End If

                If Not SyntaxFactory.AreEquivalent(oldNode.Default, newNode.Default) Then
                    ReportError(RudeEditKind.InitializerUpdate)
                    Return
                End If

                If ClassifyTypeAndInitializerUpdates(oldNode.Default, oldNode.AsClause, newNode.Default, newNode.AsClause) Then
                    Return
                End If

                ClassifyUpdate(oldNode.Identifier, newNode.Identifier)
            End Sub

            Private Sub ClassifyMethodBodyRudeUpdate(oldBody As MethodBlockBaseSyntax,
                                                     newBody As MethodBlockBaseSyntax,
                                                     containingMethod As MethodBlockSyntax,
                                                     containingType As TypeBlockSyntax)

                If (oldBody.EndBlockStatement Is Nothing) <> (newBody.EndBlockStatement Is Nothing) Then
                    If oldBody.EndBlockStatement Is Nothing Then
                        ReportError(RudeEditKind.MethodBodyAdd)
                        Return
                    Else
                        ReportError(RudeEditKind.MethodBodyDelete)
                        Return
                    End If
                End If

                ' The method only gets called if there are no other changes to the method declaration.
                ' Since we got the update edit something has to be different in the body.
                Debug.Assert(newBody.EndBlockStatement IsNot Nothing)

                ClassifyMemberBodyRudeUpdate(containingMethod, containingType, isTriviaUpdate:=False)
                ClassifyDeclarationBodyRudeUpdates(newBody)
            End Sub

            Public Sub ClassifyMemberBodyRudeUpdate(containingMethodOpt As MethodBlockSyntax, containingTypeOpt As TypeBlockSyntax, isTriviaUpdate As Boolean)
                If containingMethodOpt?.SubOrFunctionStatement.TypeParameterList IsNot Nothing Then
                    ReportError(If(isTriviaUpdate, RudeEditKind.GenericMethodTriviaUpdate, RudeEditKind.GenericMethodUpdate))
                    Return
                End If

                If containingTypeOpt?.BlockStatement.Arity > 0 Then
                    ReportError(If(isTriviaUpdate, RudeEditKind.GenericTypeTriviaUpdate, RudeEditKind.GenericTypeUpdate))
                    Return
                End If
            End Sub

            Public Sub ClassifyDeclarationBodyRudeUpdates(newDeclarationOrBody As SyntaxNode)
                For Each node In newDeclarationOrBody.DescendantNodesAndSelf()
                    Select Case node.Kind
                        Case SyntaxKind.AggregateClause,
                             SyntaxKind.GroupByClause,
                             SyntaxKind.SimpleJoinClause,
                             SyntaxKind.GroupJoinClause
                            ReportError(RudeEditKind.RUDE_EDIT_COMPLEX_QUERY_EXPRESSION, node, Me._newNode)
                            Return

                        Case SyntaxKind.LocalDeclarationStatement
                            Dim declaration = DirectCast(node, LocalDeclarationStatementSyntax)
                            If declaration.Modifiers.Any(SyntaxKind.StaticKeyword) Then
                                ReportError(RudeEditKind.UpdateStaticLocal)
                            End If
                    End Select
                Next
            End Sub
#End Region
        End Structure

        Friend Overrides Sub ReportSyntacticRudeEdits(diagnostics As List(Of RudeEditDiagnostic),
                                                      match As Match(Of SyntaxNode),
                                                      edit As Edit(Of SyntaxNode),
                                                      editMap As Dictionary(Of SyntaxNode, EditKind))

            ' For most nodes we ignore Insert and Delete edits if their parent was also inserted or deleted, respectively.
            ' For ModifiedIdentifiers though we check the grandparent instead because variables can move across 
            ' VariableDeclarators. Moving a variable from a VariableDeclarator that only has a single variable results in
            ' deletion of that declarator. We don't want to report that delete. Similarly for moving to a new VariableDeclarator.

            If edit.Kind = EditKind.Delete AndAlso
               edit.OldNode.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso
               edit.OldNode.Parent.IsKind(SyntaxKind.VariableDeclarator) Then

                If HasEdit(editMap, edit.OldNode.Parent.Parent, EditKind.Delete) Then
                    Return
                End If

            ElseIf edit.Kind = EditKind.Insert AndAlso
                   edit.NewNode.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso
                   edit.NewNode.Parent.IsKind(SyntaxKind.VariableDeclarator) Then

                If HasEdit(editMap, edit.NewNode.Parent.Parent, EditKind.Insert) Then
                    Return
                End If

            ElseIf HasParentEdit(editMap, edit) Then
                Return
            End If

            Dim classifier = New EditClassifier(Me, diagnostics, edit.OldNode, edit.NewNode, edit.Kind, match)
            classifier.ClassifyEdit()
        End Sub

        Friend Overrides Sub ReportMemberUpdateRudeEdits(diagnostics As List(Of RudeEditDiagnostic), newMember As SyntaxNode, span As TextSpan?)
            Dim classifier = New EditClassifier(Me, diagnostics, Nothing, newMember, EditKind.Update, span:=span)

            classifier.ClassifyMemberBodyRudeUpdate(
                TryCast(newMember, MethodBlockSyntax),
                newMember.FirstAncestorOrSelf(Of TypeBlockSyntax)(),
                isTriviaUpdate:=True)

            classifier.ClassifyDeclarationBodyRudeUpdates(newMember)
        End Sub

#End Region

#Region "Semantic Rude Edits"
        Friend Overrides Sub ReportInsertedMemberSymbolRudeEdits(diagnostics As List(Of RudeEditDiagnostic), newSymbol As ISymbol)
            ' CLR doesn't support adding P/Invokes.

            ' VB needs to check if the type doesn't contain methods with DllImport attribute.
            If newSymbol.IsKind(SymbolKind.NamedType) Then
                For Each member In DirectCast(newSymbol, INamedTypeSymbol).GetMembers()
                    ReportDllImportInsertRudeEdit(diagnostics, member)
                Next
            Else
                ReportDllImportInsertRudeEdit(diagnostics, newSymbol)
            End If
        End Sub

        Private Shared Sub ReportDllImportInsertRudeEdit(diagnostics As List(Of RudeEditDiagnostic), member As ISymbol)
            If member.IsKind(SymbolKind.Method) AndAlso
               DirectCast(member, IMethodSymbol).GetDllImportData() IsNot Nothing Then

                diagnostics.Add(New RudeEditDiagnostic(RudeEditKind.InsertDllImport,
                                                       member.Locations.First().SourceSpan))
            End If
        End Sub
#End Region

#Region "Exception Handling Rude Edits"

        Protected Overrides Function GetExceptionHandlingAncestors(node As SyntaxNode, isNonLeaf As Boolean) As List(Of SyntaxNode)
            Dim result = New List(Of SyntaxNode)()
            Dim initialNode = node

            While node IsNot Nothing
                Dim kind = node.Kind

                Select Case kind
                    Case SyntaxKind.TryBlock
                        If isNonLeaf Then
                            result.Add(node)
                        End If

                    Case SyntaxKind.CatchBlock,
                         SyntaxKind.FinallyBlock
                        result.Add(node)
                        Debug.Assert(node.Parent.Kind = SyntaxKind.TryBlock)
                        node = node.Parent

                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock
                        ' stop at type declaration
                        Exit While
                End Select

                ' stop at lambda
                If LambdaUtilities.IsLambda(node) Then
                    Exit While
                End If

                node = node.Parent
            End While

            Return result
        End Function

        Friend Overrides Sub ReportEnclosingExceptionHandlingRudeEdits(diagnostics As List(Of RudeEditDiagnostic),
                                                                       exceptionHandlingEdits As IEnumerable(Of Edit(Of SyntaxNode)),
                                                                       oldStatement As SyntaxNode,
                                                                       newStatementSpan As TextSpan)
            For Each edit In exceptionHandlingEdits
                Debug.Assert(edit.Kind <> EditKind.Update OrElse edit.OldNode.RawKind = edit.NewNode.RawKind)

                If edit.Kind <> EditKind.Update OrElse Not AreExceptionHandlingPartsEquivalent(edit.OldNode, edit.NewNode) Then
                    AddRudeDiagnostic(diagnostics, edit.OldNode, edit.NewNode, newStatementSpan)
                End If
            Next
        End Sub

        Private Shared Function AreExceptionHandlingPartsEquivalent(oldNode As SyntaxNode, newNode As SyntaxNode) As Boolean
            Select Case oldNode.Kind
                Case SyntaxKind.TryBlock
                    Dim oldTryBlock = DirectCast(oldNode, TryBlockSyntax)
                    Dim newTryBlock = DirectCast(newNode, TryBlockSyntax)
                    Return SyntaxFactory.AreEquivalent(oldTryBlock.FinallyBlock, newTryBlock.FinallyBlock) AndAlso
                           SyntaxFactory.AreEquivalent(oldTryBlock.CatchBlocks, newTryBlock.CatchBlocks)

                Case SyntaxKind.CatchBlock,
                     SyntaxKind.FinallyBlock
                    Return SyntaxFactory.AreEquivalent(oldNode, newNode)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(oldNode.Kind)
            End Select
        End Function

        ''' <summary>
        ''' An active statement (leaf or not) inside a "Catch" makes the Catch part readonly.
        ''' An active statement (leaf or not) inside a "Finally" makes the whole Try/Catch/Finally part read-only.
        ''' An active statement (non leaf)    inside a "Try" makes the Catch/Finally part read-only.
        ''' </summary>
        Protected Overrides Function GetExceptionHandlingRegion(node As SyntaxNode, <Out> ByRef coversAllChildren As Boolean) As TextSpan
            Select Case node.Kind
                Case SyntaxKind.TryBlock
                    Dim tryBlock = DirectCast(node, TryBlockSyntax)
                    coversAllChildren = False

                    If tryBlock.CatchBlocks.Count = 0 Then
                        Debug.Assert(tryBlock.FinallyBlock IsNot Nothing)
                        Return TextSpan.FromBounds(tryBlock.FinallyBlock.SpanStart, tryBlock.EndTryStatement.Span.End)
                    End If

                    Return TextSpan.FromBounds(tryBlock.CatchBlocks.First().SpanStart, tryBlock.EndTryStatement.Span.End)

                Case SyntaxKind.CatchBlock
                    coversAllChildren = True
                    Return node.Span

                Case SyntaxKind.FinallyBlock
                    coversAllChildren = True
                    Return DirectCast(node.Parent, TryBlockSyntax).Span

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

#End Region

#Region "State Machines"

        Friend Overrides Function IsStateMachineMethod(declaration As SyntaxNode) As Boolean
            Return SyntaxUtilities.IsAsyncMethodOrLambda(declaration) OrElse
                   SyntaxUtilities.IsIteratorMethodOrLambda(declaration)
        End Function

        Protected Overrides Sub GetStateMachineInfo(body As SyntaxNode, ByRef suspensionPoints As ImmutableArray(Of SyntaxNode), ByRef kind As StateMachineKind)
            ' In VB declaration and body are represented by the same node for both lambdas and methods (unlike C#)
            If SyntaxUtilities.IsAsyncMethodOrLambda(body) Then
                suspensionPoints = SyntaxUtilities.GetAwaitExpressions(body)
                kind = StateMachineKind.Async
            ElseIf SyntaxUtilities.IsIteratorMethodOrLambda(body) Then
                suspensionPoints = SyntaxUtilities.GetYieldStatements(body)
                kind = StateMachineKind.Iterator
            Else
                suspensionPoints = ImmutableArray(Of SyntaxNode).Empty
                kind = StateMachineKind.None
            End If
        End Sub

        Friend Overrides Sub ReportStateMachineSuspensionPointRudeEdits(diagnostics As List(Of RudeEditDiagnostic), oldNode As SyntaxNode, newNode As SyntaxNode)
            ' TODO: changes around suspension points (foreach, lock, using, etc.)

            If newNode.IsKind(SyntaxKind.AwaitExpression) Then
                Dim oldContainingStatementPart = FindContainingStatementPart(oldNode)
                Dim newContainingStatementPart = FindContainingStatementPart(newNode)

                ' If the old statement has spilled state and the new doesn't, the edit is ok. We'll just not use the spilled state.
                If Not SyntaxFactory.AreEquivalent(oldContainingStatementPart, newContainingStatementPart) AndAlso
                   Not HasNoSpilledState(newNode, newContainingStatementPart) Then
                    diagnostics.Add(New RudeEditDiagnostic(RudeEditKind.AwaitStatementUpdate, newContainingStatementPart.Span))
                End If
            End If
        End Sub

        Private Shared Function FindContainingStatementPart(node As SyntaxNode) As SyntaxNode
            Dim statement = TryCast(node, StatementSyntax)

            While statement Is Nothing
                Select Case node.Parent.Kind()
                    Case SyntaxKind.ForStatement,
                         SyntaxKind.ForEachStatement,
                         SyntaxKind.IfStatement,
                         SyntaxKind.WhileStatement,
                         SyntaxKind.SimpleDoStatement,
                         SyntaxKind.SelectStatement,
                         SyntaxKind.UsingStatement
                        Return node
                End Select

                If LambdaUtilities.IsLambdaBodyStatementOrExpression(node) Then
                    Return node
                End If

                node = node.Parent
                statement = TryCast(node, StatementSyntax)
            End While

            Return statement
        End Function

        Private Shared Function HasNoSpilledState(awaitExpression As SyntaxNode, containingStatementPart As SyntaxNode) As Boolean
            Debug.Assert(awaitExpression.IsKind(SyntaxKind.AwaitExpression))

            ' There is nothing within the statement part surrounding the await expression.
            If containingStatementPart Is awaitExpression Then
                Return True
            End If

            Select Case containingStatementPart.Kind()
                Case SyntaxKind.ExpressionStatement,
                     SyntaxKind.ReturnStatement
                    Dim expression = GetExpressionFromStatementPart(containingStatementPart)

                    ' Await <expr>
                    ' Return Await <expr>
                    If expression Is awaitExpression Then
                        Return True
                    End If

                    ' <ident> = Await <expr>
                    ' Return <ident> = Await <expr>
                    Return IsSimpleAwaitAssignment(expression, awaitExpression)

                Case SyntaxKind.VariableDeclarator
                    ' <ident> = Await <expr> in using, for, etc
                    ' EqualsValue -> VariableDeclarator
                    Return awaitExpression.Parent.Parent Is containingStatementPart

                Case SyntaxKind.LoopUntilStatement,
                     SyntaxKind.LoopWhileStatement,
                     SyntaxKind.DoUntilStatement,
                     SyntaxKind.DoWhileStatement
                    ' Until Await <expr>
                    ' UntilClause -> LoopUntilStatement
                    Return awaitExpression.Parent.Parent Is containingStatementPart

                Case SyntaxKind.LocalDeclarationStatement
                    ' Dim <ident> = Await <expr>
                    ' EqualsValue -> VariableDeclarator -> LocalDeclarationStatement
                    Return awaitExpression.Parent.Parent.Parent Is containingStatementPart
            End Select

            Return IsSimpleAwaitAssignment(containingStatementPart, awaitExpression)
        End Function

        Private Shared Function GetExpressionFromStatementPart(statement As SyntaxNode) As ExpressionSyntax
            Select Case statement.Kind()
                Case SyntaxKind.ExpressionStatement
                    Return DirectCast(statement, ExpressionStatementSyntax).Expression

                Case SyntaxKind.ReturnStatement
                    Return DirectCast(statement, ReturnStatementSyntax).Expression

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(statement.Kind())
            End Select
        End Function

        Private Shared Function IsSimpleAwaitAssignment(node As SyntaxNode, awaitExpression As SyntaxNode) As Boolean
            If node.IsKind(SyntaxKind.SimpleAssignmentStatement) Then
                Dim assignment = DirectCast(node, AssignmentStatementSyntax)
                Return assignment.Left.IsKind(SyntaxKind.IdentifierName) AndAlso assignment.Right Is awaitExpression
            End If

            Return False
        End Function
#End Region

#Region "Rude Edits around Active Statement"

        Friend Overrides Sub ReportOtherRudeEditsAroundActiveStatement(diagnostics As List(Of RudeEditDiagnostic),
                                                                       match As Match(Of SyntaxNode),
                                                                       oldActiveStatement As SyntaxNode,
                                                                       newActiveStatement As SyntaxNode,
                                                                       isNonLeaf As Boolean)

            Dim onErrorOrResumeStatement = FindOnErrorOrResumeStatement(match.NewRoot)
            If onErrorOrResumeStatement IsNot Nothing Then
                AddRudeDiagnostic(diagnostics, oldActiveStatement, onErrorOrResumeStatement, newActiveStatement.Span)
            End If

            ReportRudeEditsForAncestorsDeclaringInterStatementTemps(diagnostics, match, oldActiveStatement, newActiveStatement)
        End Sub

        Private Shared Function FindOnErrorOrResumeStatement(newDeclarationOrBody As SyntaxNode) As SyntaxNode
            For Each node In newDeclarationOrBody.DescendantNodes(AddressOf ChildrenCompiledInBody)
                Select Case node.Kind
                    Case SyntaxKind.OnErrorGoToLabelStatement,
                         SyntaxKind.OnErrorGoToMinusOneStatement,
                         SyntaxKind.OnErrorGoToZeroStatement,
                         SyntaxKind.OnErrorResumeNextStatement,
                         SyntaxKind.ResumeStatement,
                         SyntaxKind.ResumeNextStatement,
                         SyntaxKind.ResumeLabelStatement
                        Return node
                End Select
            Next

            Return Nothing
        End Function

        Private Sub ReportRudeEditsForAncestorsDeclaringInterStatementTemps(diagnostics As List(Of RudeEditDiagnostic),
                                                                            match As Match(Of SyntaxNode),
                                                                            oldActiveStatement As SyntaxNode,
                                                                            newActiveStatement As SyntaxNode)

            ' Rude Edits for Using/SyncLock/With/ForEach statements that are added/updated around an active statement.
            ' Although such changes are technically possible, they might lead to confusion since 
            ' the temporary variables these statements generate won't be properly initialized.
            '
            ' We use a simple algorithm to match each New node with its old counterpart.
            ' If all nodes match this algorithm Is linear, otherwise it's quadratic.
            ' 
            ' Unlike exception regions matching where we use LCS, we allow reordering of the statements.

            ReportUnmatchedStatements(Of SyncLockBlockSyntax)(diagnostics, match, New Integer() {SyntaxKind.SyncLockBlock}, oldActiveStatement, newActiveStatement,
                areEquivalent:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(n1.SyncLockStatement.Expression, n2.SyncLockStatement.Expression),
                areSimilar:=Nothing)

            ReportUnmatchedStatements(Of WithBlockSyntax)(diagnostics, match, New Integer() {SyntaxKind.WithBlock}, oldActiveStatement, newActiveStatement,
                areEquivalent:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(n1.WithStatement.Expression, n2.WithStatement.Expression),
                areSimilar:=Nothing)

            ReportUnmatchedStatements(Of UsingBlockSyntax)(diagnostics, match, New Integer() {SyntaxKind.UsingBlock}, oldActiveStatement, newActiveStatement,
                areEquivalent:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(n1.UsingStatement.Expression, n2.UsingStatement.Expression),
                areSimilar:=Nothing)

            ReportUnmatchedStatements(Of ForOrForEachBlockSyntax)(diagnostics, match, New Integer() {SyntaxKind.ForEachBlock}, oldActiveStatement, newActiveStatement,
                areEquivalent:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(n1.ForOrForEachStatement, n2.ForOrForEachStatement),
                areSimilar:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(DirectCast(n1.ForOrForEachStatement, ForEachStatementSyntax).ControlVariable,
                                                                         DirectCast(n2.ForOrForEachStatement, ForEachStatementSyntax).ControlVariable))
        End Sub

#End Region
    End Class
End Namespace
