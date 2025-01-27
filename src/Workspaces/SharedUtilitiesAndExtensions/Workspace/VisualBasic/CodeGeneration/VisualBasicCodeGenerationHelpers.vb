' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module VisualBasicCodeGenerationHelpers

        Friend Sub AddAccessibilityModifiers(
                accessibility As Accessibility,
                tokens As ArrayBuilder(Of SyntaxToken),
                destination As CodeGenerationDestination,
                options As CodeGenerationContextInfo,
                nonStructureAccessibility As Accessibility)
            If Not options.Context.GenerateDefaultAccessibility Then
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

                Case Accessibility.Internal
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword))

                Case Accessibility.ProtectedAndInternal
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))

                Case Accessibility.ProtectedOrInternal
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword))

            End Select

        End Sub

        Public Function InsertAtIndex(members As SyntaxList(Of StatementSyntax),
                                                member As StatementSyntax,
                                                index As Integer) As SyntaxList(Of StatementSyntax)
            Dim result = New List(Of StatementSyntax)(members)

            ' then insert the new member.
            result.Insert(index, member)

            Return SyntaxFactory.List(result)
        End Function

        Public Function GenerateImplementsClause(explicitInterfaceOpt As ISymbol) As ImplementsClauseSyntax
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

        Public Function EnsureLastElasticTrivia(Of T As StatementSyntax)(statement As T) As T
            Dim lastToken = statement.GetLastToken(includeZeroWidth:=True)
            If lastToken.TrailingTrivia.Any(Function(trivia) trivia.IsElastic()) Then
                Return statement
            End If

            Return statement.WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)
        End Function

        Public Function FirstMember(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.FirstOrDefault()
        End Function

        Public Function FirstMethod(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) TypeOf m Is MethodBlockBaseSyntax OrElse TypeOf m Is MethodStatementSyntax)
        End Function

        Public Function LastField(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) m.Kind = SyntaxKind.FieldDeclaration)
        End Function

        Public Function LastConstructor(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) m.Kind = SyntaxKind.ConstructorBlock OrElse m.Kind = SyntaxKind.SubNewStatement)
        End Function

        Public Function LastMethod(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) TypeOf m Is MethodBlockBaseSyntax OrElse TypeOf m Is MethodStatementSyntax)
        End Function

        Public Function LastOperator(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return members.LastOrDefault(Function(m) m.Kind = SyntaxKind.OperatorBlock OrElse m.Kind = SyntaxKind.OperatorStatement)
        End Function

        Private Function AfterDeclaration(Of TDeclaration As SyntaxNode)(
            [next] As Func(Of SyntaxList(Of TDeclaration), TDeclaration)) As Func(Of SyntaxList(Of TDeclaration), TDeclaration)

            Return Function(list) [next]?(list)
        End Function

        Private Function BeforeDeclaration(Of TDeclaration As SyntaxNode)(
            [next] As Func(Of SyntaxList(Of TDeclaration), TDeclaration)) As Func(Of SyntaxList(Of TDeclaration), TDeclaration)

            Return Function(list) [next]?(list)
        End Function

        Public Function Insert(Of TDeclaration As SyntaxNode)(
            declarationList As SyntaxList(Of TDeclaration),
            declaration As TDeclaration,
            options As CodeGenerationContextInfo,
            availableIndices As IList(Of Boolean),
            Optional after As Func(Of SyntaxList(Of TDeclaration), TDeclaration) = Nothing,
            Optional before As Func(Of SyntaxList(Of TDeclaration), TDeclaration) = Nothing) As SyntaxList(Of TDeclaration)

            after = AfterDeclaration(after)
            before = BeforeDeclaration(before)

            Dim index = GetInsertionIndex(
                declarationList, declaration, options, availableIndices,
                VisualBasicDeclarationComparer.WithoutNamesInstance,
                VisualBasicDeclarationComparer.WithNamesInstance,
                after, before)

            If availableIndices IsNot Nothing Then
                availableIndices.Insert(index, True)
            End If

            Return declarationList.Insert(index, declaration)
        End Function

        Public Function GetDestination(destination As SyntaxNode) As CodeGenerationDestination
            If destination IsNot Nothing Then
                Select Case destination.Kind
                    Case SyntaxKind.ClassBlock
                        Return CodeGenerationDestination.ClassType
                    Case SyntaxKind.CompilationUnit
                        Return CodeGenerationDestination.CompilationUnit
                    Case SyntaxKind.EnumBlock
                        Return CodeGenerationDestination.EnumType
                    Case SyntaxKind.InterfaceBlock
                        Return CodeGenerationDestination.InterfaceType
                    Case SyntaxKind.ModuleBlock
                        Return CodeGenerationDestination.ModuleType
                    Case SyntaxKind.NamespaceBlock
                        Return CodeGenerationDestination.Namespace
                    Case SyntaxKind.StructureBlock
                        Return CodeGenerationDestination.StructType
                    Case Else
                        Return CodeGenerationDestination.Unspecified
                End Select
            End If

            Return CodeGenerationDestination.Unspecified
        End Function

        Public Function ConditionallyAddDocumentationCommentTo(Of TSyntaxNode As SyntaxNode)(
            node As TSyntaxNode,
            symbol As ISymbol,
            options As CodeGenerationContextInfo,
            Optional cancellationToken As CancellationToken = Nothing) As TSyntaxNode

            If Not options.Context.GenerateDocumentationComments OrElse node.GetLeadingTrivia().Any(Function(t) t.IsKind(SyntaxKind.DocumentationCommentTrivia)) Then
                Return node
            End If

            Dim comment As String = Nothing
            Dim result = If(TryGetDocumentationComment(symbol, "'''", comment, cancellationToken),
                            node.WithPrependedLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(comment)) _
                                .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker),
                            node)
            Return result
        End Function

        ''' <summary>
        ''' Try use the existing syntax node and generate a new syntax node for the given <param name="symbol"/>.
        ''' Note: the returned syntax node might be modified, which means its parent information might be missing.
        ''' </summary>
        Public Function GetReuseableSyntaxNodeForSymbol(Of T As SyntaxNode)(symbol As ISymbol, options As CodeGenerationContextInfo) As T
            ThrowIfNull(symbol)

            If options.Context.ReuseSyntax AndAlso symbol.DeclaringSyntaxReferences.Length = 1 Then
                Dim reusableNode = symbol.DeclaringSyntaxReferences(0).GetSyntax()

                ' For VB method like symbol (Function, Sub, Property & Event), DeclaringSyntaxReferences will fetch
                ' the first line of the member's block. But what we want to reuse is the whole member's block
                If symbol.IsKind(SymbolKind.Method) OrElse symbol.IsKind(SymbolKind.Property) OrElse symbol.IsKind(SymbolKind.Event) Then
                    Dim declarationStatementNode = TryCast(reusableNode, DeclarationStatementSyntax)
                    If declarationStatementNode IsNot Nothing Then
                        Dim declarationBlockFromBegin = declarationStatementNode.GetDeclarationBlockFromBegin()
                        Return TryCast(RemoveLeadingDirectiveTrivia(declarationBlockFromBegin), T)
                    End If
                End If

                Dim modifiedIdentifierNode = TryCast(reusableNode, ModifiedIdentifierSyntax)
                If modifiedIdentifierNode IsNot Nothing AndAlso symbol.IsKind(SymbolKind.Field) AndAlso GetType(T) Is GetType(FieldDeclarationSyntax) Then
                    Dim variableDeclarator = TryCast(modifiedIdentifierNode.Parent, VariableDeclaratorSyntax)
                    If variableDeclarator IsNot Nothing Then
                        Dim fieldDecl = TryCast(variableDeclarator.Parent, FieldDeclarationSyntax)
                        If fieldDecl IsNot Nothing Then
                            Dim names = SyntaxFactory.SingletonSeparatedList(modifiedIdentifierNode)
                            Dim newVariableDeclarator = variableDeclarator.WithNames(names)
                            Return TryCast(RemoveLeadingDirectiveTrivia(
                                fieldDecl.WithDeclarators(SyntaxFactory.SingletonSeparatedList(newVariableDeclarator))), T)
                        End If
                    End If
                End If

                Return TryCast(RemoveLeadingDirectiveTrivia(reusableNode), T)
            End If
            Return Nothing
        End Function
    End Module
End Namespace
