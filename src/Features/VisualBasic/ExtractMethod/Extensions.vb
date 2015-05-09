' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Friend Module Extensions

        <Extension()>
        Public Function GetUnparenthesizedExpression(node As SyntaxNode) As ExpressionSyntax
            Dim parenthesizedExpression = TryCast(node, ParenthesizedExpressionSyntax)
            If parenthesizedExpression Is Nothing Then
                Return DirectCast(node, ExpressionSyntax)
            End If

            Return GetUnparenthesizedExpression(parenthesizedExpression.Expression)
        End Function

        <Extension()>
        Public Function GetStatementContainer(node As SyntaxNode) As SyntaxNode
            Contract.ThrowIfNull(node)

            Dim statement = node.GetStatementUnderContainer()
            If statement Is Nothing Then
                Return Nothing
            End If

            Return statement.Parent
        End Function

        <Extension()>
        Public Function GetStatementUnderContainer(node As SyntaxNode) As ExecutableStatementSyntax
            Contract.ThrowIfNull(node)

            Do While node IsNot Nothing
                If node.Parent.IsStatementContainerNode() AndAlso
                   TypeOf node Is ExecutableStatementSyntax AndAlso
                   node.Parent.ContainStatement(DirectCast(node, ExecutableStatementSyntax)) Then
                    Return TryCast(node, ExecutableStatementSyntax)
                End If

                node = node.Parent
            Loop

            Return Nothing
        End Function

        <Extension()>
        Public Function ContainStatement(node As SyntaxNode, statement As StatementSyntax) As Boolean
            Contract.ThrowIfNull(node)
            Contract.ThrowIfNull(statement)

            If Not node.IsStatementContainerNode() Then
                Return False
            End If

            Return node.GetStatements().IndexOf(statement) >= 0
        End Function

        <Extension()>
        Public Function GetOutermostNodeWithSameSpan(initialNode As SyntaxNode, predicate As Func(Of SyntaxNode, Boolean)) As SyntaxNode
            If initialNode Is Nothing Then
                Return Nothing
            End If

            ' now try to find outmost node that has same span
            Dim firstContainingSpan = initialNode.Span

            Dim node = initialNode
            Dim lastNode = Nothing

            Do
                If predicate(node) Then
                    lastNode = node
                End If

                node = node.Parent
            Loop While node IsNot Nothing AndAlso node.Span.Equals(firstContainingSpan)

            Return CType(If(lastNode, initialNode), SyntaxNode)
        End Function

        <Extension()>
        Public Function PartOfConstantInitializerExpression(node As SyntaxNode) As Boolean
            Return node.PartOfConstantInitializerExpression(Of FieldDeclarationSyntax)(Function(n) n.Modifiers) OrElse
                   node.PartOfConstantInitializerExpression(Of LocalDeclarationStatementSyntax)(Function(n) n.Modifiers)
        End Function

        <Extension()>
        Private Function PartOfConstantInitializerExpression(Of T As SyntaxNode)(node As SyntaxNode, modifiersGetter As Func(Of T, SyntaxTokenList)) As Boolean
            Dim decl = node.GetAncestor(Of T)()
            If decl Is Nothing Then
                Return False
            End If

            If Not modifiersGetter(decl).Any(Function(m) m.Kind = SyntaxKind.ConstKeyword) Then
                Return False
            End If

            ' we are under decl with const modifier, check we are part of initializer expression
            Dim equal = node.GetAncestor(Of EqualsValueSyntax)()
            If equal Is Nothing Then
                Return False
            End If

            Return equal.Value IsNot Nothing AndAlso equal.Value.Span.Contains(node.Span)
        End Function

        <Extension()>
        Public Function IsArgumentForByRefParameter(node As SyntaxNode, model As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Dim argument = node.FirstAncestorOrSelf(Of ArgumentSyntax)()

            ' make sure we are the argument
            If argument Is Nothing OrElse node.Span <> argument.Span Then
                Return False
            End If

            ' now find invocation node
            Dim invocation = argument.FirstAncestorOrSelf(Of InvocationExpressionSyntax)()

            ' argument for something we are not interested in
            If invocation Is Nothing Then
                Return False
            End If

            ' find argument index
            Dim argumentIndex = invocation.ArgumentList.Arguments.IndexOf(argument)
            If argumentIndex < 0 Then
                Return False
            End If

            ' get all method symbols
            Dim methodSymbols = model.GetSymbolInfo(invocation, cancellationToken).GetAllSymbols().Where(Function(s) s.Kind = SymbolKind.Method).Cast(Of IMethodSymbol)()
            For Each method In methodSymbols
                ' not a right method
                If method.Parameters.Length <= argumentIndex Then
                    Continue For
                End If

                ' make sure there is no ref type
                Dim parameter = method.Parameters(argumentIndex)
                If parameter.RefKind <> RefKind.None Then
                    Return True
                End If
            Next

            Return False
        End Function

        <Extension()>
        Public Function ContainArgumentlessThrowWithoutEnclosingCatch(ByVal tokens As IEnumerable(Of SyntaxToken), ByVal textSpan As TextSpan) As Boolean
            For Each token In tokens
                If token.Kind <> SyntaxKind.ThrowKeyword Then
                    Continue For
                End If

                Dim throwStatement = TryCast(token.Parent, ThrowStatementSyntax)
                If throwStatement Is Nothing OrElse throwStatement.Expression IsNot Nothing Then
                    Continue For
                End If

                Dim catchBlock = token.GetAncestor(Of CatchBlockSyntax)()
                If catchBlock Is Nothing OrElse Not textSpan.Contains(catchBlock.Span) Then
                    Return True
                End If
            Next token

            Return False
        End Function

        <Extension()>
        Public Function ContainPreprocessorCrossOver(ByVal tokens As IEnumerable(Of SyntaxToken), ByVal textSpan As TextSpan) As Boolean
            Dim activeRegions As Integer = 0
            Dim activeIfs As Integer = 0

            For Each trivia In tokens.GetAllTrivia()
                If Not trivia.IsDirective Then
                    Continue For
                End If

                Dim directive = DirectCast(trivia.GetStructure(), DirectiveTriviaSyntax)
                If Not textSpan.Contains(directive.Span) Then
                    Continue For
                End If

                Select Case directive.Kind
                    Case SyntaxKind.RegionDirectiveTrivia
                        activeRegions += 1
                    Case SyntaxKind.EndRegionDirectiveTrivia
                        If activeRegions <= 0 Then Return True
                        activeRegions -= 1
                    Case SyntaxKind.IfDirectiveTrivia
                        activeIfs += 1
                    Case SyntaxKind.EndIfDirectiveTrivia
                        If activeIfs <= 0 Then Return True
                        activeIfs -= 1
                    Case SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ElseIfDirectiveTrivia
                        If activeIfs <= 0 Then Return True
                End Select
            Next trivia

            Return activeIfs <> 0 OrElse activeRegions <> 0
        End Function

        <Extension()>
        Public Function GetAllTrivia(ByVal tokens As IEnumerable(Of SyntaxToken)) As IEnumerable(Of SyntaxTrivia)
            Dim list = New List(Of SyntaxTrivia)()

            For Each token In tokens
                list.AddRange(token.LeadingTrivia)
                list.AddRange(token.TrailingTrivia)
            Next token

            Return list
        End Function

        <Extension()>
        Public Function ContainsFieldInitializer(node As SyntaxNode) As Boolean
            node = node.GetOutermostNodeWithSameSpan(Function(n) True)

            Return node.DescendantNodesAndSelf().Any(Function(n) TypeOf n Is FieldInitializerSyntax)
        End Function

        <Extension()>
        Public Function ContainsDotMemberAccess(node As SyntaxNode) As Boolean
            Dim predicate = Function(n As SyntaxNode)
                                Dim member = TryCast(n, MemberAccessExpressionSyntax)
                                If member Is Nothing Then
                                    Return False
                                End If

                                Return member.Expression Is Nothing AndAlso member.OperatorToken.Kind = SyntaxKind.DotToken
                            End Function

            Return node.DescendantNodesAndSelf().Any(predicate)
        End Function

        <Extension()>
        Public Function UnderWithBlockContext(token As SyntaxToken) As Boolean
            Dim withBlock = token.GetAncestor(Of WithBlockSyntax)()
            If withBlock Is Nothing Then
                Return False
            End If

            Dim withBlockSpan = TextSpan.FromBounds(withBlock.WithStatement.Span.End, withBlock.EndWithStatement.SpanStart)
            Return withBlockSpan.Contains(token.Span)
        End Function

        <Extension()>
        Public Function UnderObjectMemberInitializerContext(token As SyntaxToken) As Boolean
            Dim initializer = token.GetAncestor(Of ObjectMemberInitializerSyntax)()
            If initializer Is Nothing Then
                Return False
            End If

            Dim initializerSpan = TextSpan.FromBounds(initializer.WithKeyword.Span.End, initializer.Span.End)
            Return initializerSpan.Contains(token.Span)
        End Function

        <Extension()>
        Public Function UnderValidContext(token As SyntaxToken) As Boolean
            Dim predicate As Func(Of SyntaxNode, Boolean) =
                Function(n)
                    Dim range = TryCast(n, RangeArgumentSyntax)
                    If range IsNot Nothing Then
                        If range.UpperBound.Span.Contains(token.Span) AndAlso
                           range.GetAncestor(Of FieldDeclarationSyntax)() IsNot Nothing Then
                            Return True
                        End If
                    End If

                    Dim [property] = TryCast(n, PropertyStatementSyntax)
                    If [property] IsNot Nothing Then
                        Dim asNewClause = TryCast([property].AsClause, AsNewClauseSyntax)
                        If asNewClause IsNot Nothing AndAlso asNewClause.NewExpression IsNot Nothing Then
                            Dim span = TextSpan.FromBounds(asNewClause.NewExpression.NewKeyword.Span.End, asNewClause.NewExpression.Span.End)
                            Return span.Contains(token.Span)
                        End If
                    End If

                    If n.CheckTopLevel(token.Span) Then
                        Return True
                    End If

                    Return False
                End Function

            Return token.GetAncestors(Of SyntaxNode)().Any(predicate)
        End Function

        <Extension()>
        Public Function ContainsInMethodBlockBody(block As MethodBlockBaseSyntax, textSpan As TextSpan) As Boolean
            If block Is Nothing Then
                Return False
            End If

            Dim blockSpan = TextSpan.FromBounds(block.BlockStatement.Span.End, block.EndBlockStatement.SpanStart)
            Return blockSpan.Contains(textSpan)
        End Function

        <Extension()> _
        Public Function UnderValidContext(ByVal node As SyntaxNode) As Boolean
            Contract.ThrowIfNull(node)

            Dim predicate As Func(Of SyntaxNode, Boolean) =
                Function(n)
                    If TypeOf n Is MethodBlockBaseSyntax OrElse
                       TypeOf n Is MultiLineLambdaExpressionSyntax OrElse
                       TypeOf n Is SingleLineLambdaExpressionSyntax Then
                        Return True
                    End If

                    Return False
                End Function

            If Not node.GetAncestorsOrThis(Of SyntaxNode)().Any(predicate) Then
                Return False
            End If

            If node.FromScript() OrElse node.GetAncestor(Of TypeBlockSyntax)() IsNot Nothing Then
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsReturnableConstruct(node As SyntaxNode) As Boolean
            Return TypeOf node Is MethodBlockBaseSyntax OrElse
                   TypeOf node Is SingleLineLambdaExpressionSyntax OrElse
                   TypeOf node Is MultiLineLambdaExpressionSyntax
        End Function

        <Extension()>
        Public Function HasSyntaxAnnotation([set] As HashSet(Of SyntaxAnnotation), node As SyntaxNode) As Boolean
            Return [set].Any(Function(a) node.GetAnnotatedNodesAndTokens(a).Any())
        End Function

        <Extension()>
        Public Function IsFunctionValue(symbol As ISymbol) As Boolean
            Dim local = TryCast(symbol, ILocalSymbol)

            Return local IsNot Nothing AndAlso local.IsFunctionValue
        End Function

        <Extension()>
        Public Function ToSeparatedList(Of T As SyntaxNode)(nodes As IEnumerable(Of Tuple(Of T, SyntaxToken))) As SeparatedSyntaxList(Of T)
            Dim list = New List(Of SyntaxNodeOrToken)
            For Each tuple In nodes
                Contract.ThrowIfNull(tuple.Item1)
                list.Add(tuple.Item1)

                If tuple.Item2.Kind = SyntaxKind.None Then
                    Exit For
                End If
                list.Add(tuple.Item2)
            Next

            Return SyntaxFactory.SeparatedList(Of T)(list)
        End Function

        <Extension()>
        Public Function CreateAssignmentExpressionStatementWithValue(identifier As SyntaxToken, rvalue As ExpressionSyntax) As StatementSyntax
            Return SyntaxFactory.SimpleAssignmentStatement(SyntaxFactory.IdentifierName(identifier), SyntaxFactory.Token(SyntaxKind.EqualsToken), rvalue).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)
        End Function

        <Extension()>
        Public Function ProcessLocalDeclarationStatement(variableToRemoveMap As HashSet(Of SyntaxAnnotation),
                                                         declarationStatement As LocalDeclarationStatementSyntax,
                                                         expressionStatements As List(Of StatementSyntax),
                                                         variableDeclarators As List(Of VariableDeclaratorSyntax),
                                                         triviaList As List(Of SyntaxTrivia)) As Boolean
            ' go through each var decls in decl statement, and create new assignment if
            ' variable is initialized at decl.
            Dim hasChange As Boolean = False
            Dim leadingTriviaApplied As Boolean = False

            For Each variableDeclarator In declarationStatement.Declarators
                Dim identifierList = New List(Of ModifiedIdentifierSyntax)()
                Dim nameCount = variableDeclarator.Names.Count

                For i = 0 To nameCount - 1 Step 1
                    Dim variable = variableDeclarator.Names(i)
                    If variableToRemoveMap.HasSyntaxAnnotation(variable) Then
                        If variableDeclarator.Initializer IsNot Nothing AndAlso i = nameCount - 1 Then
                            ' move comments with the variable here
                            Dim identifier As SyntaxToken = variable.Identifier

                            ' The leading trivia from the declaration is applied to the first variable
                            ' There is not much value in appending the trailing trivia of the modifier
                            If i = 0 AndAlso Not leadingTriviaApplied AndAlso declarationStatement.HasLeadingTrivia Then
                                identifier = identifier.WithLeadingTrivia(declarationStatement.GetLeadingTrivia.AddRange(identifier.LeadingTrivia))
                                leadingTriviaApplied = True
                            End If

                            expressionStatements.Add(identifier.CreateAssignmentExpressionStatementWithValue(variableDeclarator.Initializer.Value))
                            Continue For
                        End If

                        ' we don't remove trivia around tokens we remove
                        triviaList.AddRange(variable.GetLeadingTrivia())
                        triviaList.AddRange(variable.GetTrailingTrivia())
                        Continue For
                    End If

                    If triviaList.Count > 0 Then
                        identifierList.Add(variable.WithPrependedLeadingTrivia(triviaList))
                        triviaList.Clear()
                        Continue For
                    End If

                    identifierList.Add(variable)
                Next

                If identifierList.Count = 0 Then
                    ' attach left over trivia to last expression statement
                    If triviaList.Count > 0 AndAlso expressionStatements.Count > 0 Then
                        Dim lastStatement = expressionStatements(expressionStatements.Count - 1)
                        lastStatement = lastStatement.WithPrependedLeadingTrivia(triviaList)
                        expressionStatements(expressionStatements.Count - 1) = lastStatement
                        triviaList.Clear()
                    End If

                    Continue For
                ElseIf identifierList.Count = variableDeclarator.Names.Count Then
                    variableDeclarators.Add(variableDeclarator)
                ElseIf identifierList.Count > 0 Then
                    variableDeclarators.Add(
                        variableDeclarator.WithNames(SyntaxFactory.SeparatedList(identifierList)).
                            WithPrependedLeadingTrivia(triviaList))
                    hasChange = True
                End If
            Next variableDeclarator

            Return hasChange OrElse declarationStatement.Declarators.Count <> variableDeclarators.Count
        End Function

        <Extension()>
        Public Function IsExpressionInCast(node As SyntaxNode) As Boolean
            Return TypeOf node Is ExpressionSyntax AndAlso TypeOf node.Parent Is CastExpressionSyntax
        End Function

        <Extension()>
        Public Function IsErrorType(type As ITypeSymbol) As Boolean
            Return type Is Nothing OrElse type.Kind = SymbolKind.ErrorType
        End Function

        <Extension()>
        Public Function IsObjectType(type As ITypeSymbol) As Boolean
            Return type Is Nothing OrElse type.SpecialType = SpecialType.System_Object
        End Function
    End Module
End Namespace
