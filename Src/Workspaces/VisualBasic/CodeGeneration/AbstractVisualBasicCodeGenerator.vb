' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Partial Friend MustInherit Class AbstractVisualBasicCodeGenerator
        Inherits AbstractCodeGenerator

        Friend Shared Sub AddAccessibilityModifiers(accessibility As Accessibility,
                                                       tokens As IList(Of SyntaxToken),
                                                       destination As CodeGenerationDestination,
                                                       options As CodeGenerationOptions,
                                                       nonStructureAccessibility As Accessibility)
            options = If(options, CodeGenerationOptions.Default)
            If Not options.GenerateDefaultAccessibility Then
                If destination = CodeGenerationDestination.StructType AndAlso accessibility = Accessibility.Public Then
                    Return
                End If

                If destination <> CodeGenerationDestination.StructType AndAlso accessibility = nonStructureAccessibility Then
                    Return
                End If
            End If

            Select Case accessibility
                Case Accessibility.Public
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword))

                Case Accessibility.Protected
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))

                Case Accessibility.Private
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))

                Case Accessibility.ProtectedAndInternal, Accessibility.Internal
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword))

                Case Accessibility.ProtectedOrInternal
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword))

            End Select

        End Sub

        Protected Shared Function InsertAtIndex(members As SyntaxList(Of StatementSyntax),
                                                member As StatementSyntax,
                                                index As Integer) As SyntaxList(Of StatementSyntax)
            Dim result = New List(Of StatementSyntax)(members)

            ' then insert the new member.
            result.Insert(index, member)

            Return SyntaxFactory.List(result)
        End Function

        Protected Shared Function GenerateImplementsClause(explicitInterfaceOpt As ISymbol) As ImplementsClauseSyntax
            If explicitInterfaceOpt IsNot Nothing AndAlso explicitInterfaceOpt.ContainingType IsNot Nothing Then
                Dim type = explicitInterfaceOpt.ContainingType.GenerateTypeSyntax()

                If TypeOf type Is NameSyntax Then
                    Return SyntaxFactory.ImplementsClause(
                        interfaceMembers:=SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.QualifiedName(
                                DirectCast(type, NameSyntax), explicitInterfaceOpt.Name.ToIdentifierName())))
                End If
            End If

            Return Nothing
        End Function

        Protected Shared Function EnsureEndTokens(destinationType As TypeBlockSyntax) As EndBlockStatementSyntax
            If destinationType.End.IsMissing Then
                Select Case (destinationType.VisualBasicKind)
                    Case SyntaxKind.ClassBlock
                        Return AddCleanupAnnotationsTo(SyntaxFactory.EndClassStatement())
                    Case SyntaxKind.InterfaceBlock
                        Return AddCleanupAnnotationsTo(SyntaxFactory.EndInterfaceStatement())
                    Case SyntaxKind.StructureBlock
                        Return AddCleanupAnnotationsTo(SyntaxFactory.EndStructureStatement())
                End Select
            End If

            Return destinationType.End
        End Function

        Protected Shared Function EnsureLastElasticTrivia(Of T As StatementSyntax)(statement As T) As T
            Dim lastToken = statement.GetLastToken(includeZeroWidth:=True)
            If lastToken.TrailingTrivia.Any(Function(trivia) trivia.IsElastic()) Then
                Return statement
            End If

            Return statement.WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)
        End Function

        Friend Shared Function FixTerminators(destinationType As TypeBlockSyntax) As TypeBlockSyntax
            Return destinationType.WithInherits(EnsureProperInherits(destinationType)).
                                   WithImplements(EnsureProperImplements(destinationType)).
                                   WithBegin(EnsureProperBegin(destinationType)).
                                   WithEnd(EnsureEndTokens(destinationType))
        End Function

        Friend Shared Function EnsureProperImplements(destinationType As TypeBlockSyntax) As SyntaxList(Of ImplementsStatementSyntax)
            Dim allElements = destinationType.Implements
            If allElements.Count > 0 Then
                Return EnsureProperList(destinationType.Implements)
            End If

            Return destinationType.Implements
        End Function

        Friend Shared Function EnsureProperInherits(destinationType As TypeBlockSyntax) As SyntaxList(Of InheritsStatementSyntax)
            Dim allElements = destinationType.Inherits
            If allElements.Count > 0 AndAlso
               destinationType.Implements.Count = 0 Then
                Return EnsureProperList(destinationType.Inherits)
            End If

            Return destinationType.Inherits
        End Function

        Friend Shared Function EnsureProperList(Of TSyntax As SyntaxNode)(list As SyntaxList(Of TSyntax)) As SyntaxList(Of TSyntax)
            Dim allElements = list
            If Not allElements.Last().GetTrailingTrivia().Any(Function(t) t.VisualBasicKind = SyntaxKind.EndOfLineTrivia OrElse t.VisualBasicKind = SyntaxKind.ColonTrivia) Then
                Return SyntaxFactory.SingletonList(Of TSyntax)(
                    allElements.Last().WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
            ElseIf allElements.Last().GetTrailingTrivia().Any(Function(t) t.VisualBasicKind = SyntaxKind.ColonTrivia) Then
                Return SyntaxFactory.List(Of TSyntax)(
                    allElements.Take(allElements.Count - 1).Concat(ReplaceTrailingColonToEndOfLineTrivia(allElements.Last())))
            End If

            Return list
        End Function

        Friend Shared Function EnsureProperBegin(destinationType As TypeBlockSyntax) As TypeStatementSyntax
            If destinationType.Inherits.Count = 0 AndAlso
               destinationType.Implements.Count = 0 AndAlso
               destinationType.Begin.GetTrailingTrivia().Any(Function(t) t.VisualBasicKind = SyntaxKind.ColonTrivia) Then
                Return ReplaceTrailingColonToEndOfLineTrivia(destinationType.Begin)
            End If

            Return destinationType.Begin
        End Function

        Protected Shared Function FirstMember(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.FirstOrDefault()
        End Function

        Protected Shared Function FirstMethod(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) TypeOf m Is MethodBlockBaseSyntax OrElse TypeOf m Is MethodStatementSyntax)
        End Function

        Protected Shared Function LastField(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) m.VisualBasicKind = SyntaxKind.FieldDeclaration)
        End Function

        Protected Shared Function LastConstructor(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) m.VisualBasicKind = SyntaxKind.ConstructorBlock OrElse m.VisualBasicKind = SyntaxKind.SubNewStatement)
        End Function

        Protected Shared Function LastMethod(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) TypeOf m Is MethodBlockBaseSyntax OrElse TypeOf m Is MethodStatementSyntax)
        End Function

        Protected Shared Function LastOperator(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) m.VisualBasicKind = SyntaxKind.OperatorBlock OrElse m.VisualBasicKind = SyntaxKind.OperatorStatement)
        End Function

        Private Shared Function ReplaceTrailingColonToEndOfLineTrivia(Of TNode As SyntaxNode)(node As TNode) As TNode
            Return node.WithTrailingTrivia(node.GetTrailingTrivia().Select(Function(t) If(t.VisualBasicKind = SyntaxKind.ColonTrivia, SyntaxFactory.CarriageReturnLineFeed, t)))
        End Function

        Private Shared Function AfterDeclaration(Of TDeclaration As SyntaxNode)(
            declarationList As SyntaxList(Of TDeclaration),
            options As CodeGenerationOptions,
            [next] As Func(Of SyntaxList(Of TDeclaration), TDeclaration)) As Func(Of SyntaxList(Of TDeclaration), TDeclaration)

            options = If(options, CodeGenerationOptions.Default)

            Return Function(list)
                       If [next] IsNot Nothing Then
                           Return [next](list)
                       End If

                       Return Nothing
                   End Function
        End Function

        Private Shared Function BeforeDeclaration(Of TDeclaration As SyntaxNode)(
            declarationList As SyntaxList(Of TDeclaration),
            options As CodeGenerationOptions,
             [next] As Func(Of SyntaxList(Of TDeclaration), TDeclaration)) As Func(Of SyntaxList(Of TDeclaration), TDeclaration)

            options = If(options, CodeGenerationOptions.Default)

            Return Function(list)
                       If [next] IsNot Nothing Then
                           Return [next](list)
                       End If

                       Return Nothing
                   End Function
        End Function

        Protected Shared Function Insert(Of TDeclaration As SyntaxNode)(
            declarationList As SyntaxList(Of TDeclaration),
            declaration As TDeclaration,
            options As CodeGenerationOptions,
            availableIndices As IList(Of Boolean),
            Optional after As Func(Of SyntaxList(Of TDeclaration), TDeclaration) = Nothing,
            Optional before As Func(Of SyntaxList(Of TDeclaration), TDeclaration) = Nothing) As SyntaxList(Of TDeclaration)

            after = AfterDeclaration(declarationList, options, after)
            before = BeforeDeclaration(declarationList, options, before)

            Dim index = GetInsertionIndex(
                declarationList, declaration, options, availableIndices, after, before)

            If availableIndices IsNot Nothing Then
                availableIndices.Insert(index, True)
            End If

            Return declarationList.Insert(index, declaration)
        End Function

        Private Shared Function GetInsertionIndex(Of TDeclaration As SyntaxNode)(
            declarationList As SyntaxList(Of TDeclaration),
            declaration As TDeclaration,
            options As CodeGenerationOptions,
            availableIndices As IList(Of Boolean),
            after As Func(Of SyntaxList(Of TDeclaration), TDeclaration),
            before As Func(Of SyntaxList(Of TDeclaration), TDeclaration)) As Integer

            If options IsNot Nothing Then
                ' Try to use AfterThisLocation 
                If options.AfterThisLocation IsNot Nothing Then
                    Dim afterMember = declarationList.LastOrDefault(Function(m) m.SpanStart <= options.AfterThisLocation.SourceSpan.Start)
                    If afterMember IsNot Nothing Then
                        Dim index = declarationList.IndexOf(afterMember)
                        index = GetPreferredIndex(index + 1, availableIndices, forward:=True)
                        If index <> -1 Then
                            Return index
                        End If
                    End If
                End If

                ' Try to use BeforeThisLocation
                If options.BeforeThisLocation IsNot Nothing Then
                    Dim beforeMember = declarationList.FirstOrDefault(Function(m) m.Span.End >= options.BeforeThisLocation.SourceSpan.End)
                    If beforeMember IsNot Nothing Then
                        Dim index = declarationList.IndexOf(beforeMember)
                        index = GetPreferredIndex(index, availableIndices, forward:=False)
                        If index <> -1 Then
                            Return index
                        End If
                    End If
                End If

                If options.AutoInsertionLocation Then
                    Dim declarations = declarationList.ToArray()
                    If (declarations.Length = 0) Then
                        Return 0
                    ElseIf declarations.IsSorted(VisualBasicDeclarationComparer.Instance) Then
                        Dim result = Array.BinarySearch(Of TDeclaration)(declarations, declaration, VisualBasicDeclarationComparer.Instance)
                        Dim index = GetPreferredIndex(If(result < 0, Not result, result), availableIndices, forward:=True)
                        If index <> -1 Then
                            Return index
                        End If
                    End If

                    If after IsNot Nothing Then
                        Dim member = after(declarationList)
                        If member IsNot Nothing Then
                            Dim index = declarationList.IndexOf(member)
                            index = GetPreferredIndex(index + 1, availableIndices, forward:=True)
                            If index <> -1 Then
                                Return index
                            End If
                        End If
                    End If

                    If before IsNot Nothing Then
                        Dim member = before(declarationList)
                        If member IsNot Nothing Then
                            Dim index = declarationList.IndexOf(member)
                            index = GetPreferredIndex(index, availableIndices, forward:=False)
                            If index <> -1 Then
                                Return index
                            End If
                        End If
                    End If
                End If
            End If

            ' Otherwise, add the method to the end.
            Dim index1 = GetPreferredIndex(declarationList.Count, availableIndices, forward:=False)
            If index1 <> -1 Then
                Return index1
            End If

            Return declarationList.Count
        End Function

        Protected Shared Function GetDestination(destination As TypeBlockSyntax) As CodeGenerationDestination
            If destination IsNot Nothing Then
                Select Case destination.VisualBasicKind
                    Case SyntaxKind.ClassBlock
                        Return CodeGenerationDestination.ClassType
                    Case SyntaxKind.InterfaceBlock
                        Return CodeGenerationDestination.InterfaceType
                    Case SyntaxKind.ModuleBlock
                        Return CodeGenerationDestination.ModuleType
                    Case SyntaxKind.StructureBlock
                        Return CodeGenerationDestination.StructType
                End Select
            End If

            Return CodeGenerationDestination.Unspecified
        End Function

        Protected Overrides Function GetSyntaxFactory() As ISyntaxFactoryService
            Return New VisualBasicSyntaxFactory()
        End Function

        Protected Shared Function ConditionallyAddDocumentationCommentTo(Of TSyntaxNode As SyntaxNode)(
            node As TSyntaxNode,
            symbol As ISymbol,
            options As CodeGenerationOptions,
            Optional cancellationToken As CancellationToken = Nothing) As TSyntaxNode

            If Not options.GenerateDocumentationComments OrElse node.GetLeadingTrivia().Any(Function(t) t.IsKind(SyntaxKind.DocumentationCommentTrivia)) Then
                Return node
            End If

            Dim comment As String = Nothing
            Dim result = If(TryGetDocumentationComment(symbol, "'''", comment, cancellationToken),
                            node.WithPrependedLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(comment)) _
                                .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker),
                            node)
            Return result
        End Function

        Protected Shared Function GenerateExpression(typedConstant As TypedConstant) As ExpressionSyntax
            Return New ExpressionGenerator().GenerateExpression(typedConstant)
        End Function

        Protected Shared Function GenerateExpression(type As ITypeSymbol, value As Object, canUseFieldReference As Boolean) As ExpressionSyntax
            Return New ExpressionGenerator().GenerateExpression(type, value, canUseFieldReference)
        End Function
    End Class
End Namespace