' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module VisualBasicCodeGenerationHelpers

        Friend Sub AddAccessibilityModifiers(accessibility As Accessibility,
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

        Private Function BeforeDeclaration(Of TDeclaration As SyntaxNode)(
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

        Public Function Insert(Of TDeclaration As SyntaxNode)(
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

        Private Function GetInsertionIndex(Of TDeclaration As SyntaxNode)(
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
    End Module
End Namespace
