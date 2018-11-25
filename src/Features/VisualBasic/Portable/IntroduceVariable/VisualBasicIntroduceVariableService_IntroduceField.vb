' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    Friend Partial Class VisualBasicIntroduceVariableService
        Protected Overrides Async Function IntroduceFieldAsync(
                document As SemanticDocument,
                expression As ExpressionSyntax,
                allOccurrences As Boolean,
                isConstant As Boolean,
                cancellationToken As CancellationToken) As Task(Of Tuple(Of Document, SyntaxNode, Integer))

            Dim oldTypeDeclaration = expression.GetAncestorOrThis(Of TypeBlockSyntax)()
            Dim oldType = If(oldTypeDeclaration IsNot Nothing,
                document.SemanticModel.GetDeclaredSymbol(oldTypeDeclaration.BlockStatement, cancellationToken),
                Nothing)

            Dim newNameToken = GenerateUniqueLocalName(
                document, expression, isConstant, containerOpt:=Nothing,
                cancellationToken:=cancellationToken)

            Dim newQualifiedName = SyntaxFactory.SimpleMemberAccessExpression(
                expression:=SyntaxFactory.ParseName(oldType.ToNameDisplayString()),
                operatorToken:=SyntaxFactory.Token(SyntaxKind.DotToken),
                name:=SyntaxFactory.IdentifierName(newNameToken)).WithAdditionalAnnotations(Simplifier.Annotation)

            If oldType IsNot Nothing Then
                Return Await IntroduceFieldIntoTypeAsync(
                    document, expression, newQualifiedName, oldTypeDeclaration, oldType,
                    newNameToken, allOccurrences, isConstant, cancellationToken).ConfigureAwait(False)
            Else
                Dim oldCompilationUnit = DirectCast(document.Root, CompilationUnitSyntax)
                Dim newCompilationUnit = Rewrite(document, expression, newQualifiedName, document, oldCompilationUnit, allOccurrences, cancellationToken)
                Dim newFieldDeclaration = CreateFieldDeclaration(
                    document, oldTypeDeclaration, newNameToken, expression, allOccurrences, isConstant, cancellationToken)

                Dim insertionIndex = If(isConstant, DetermineConstantInsertPosition(oldCompilationUnit.Members, newCompilationUnit.Members), DetermineFieldInsertPosition(oldCompilationUnit.Members, newCompilationUnit.Members))
                Dim destination As SyntaxNode = oldCompilationUnit

                Dim newRoot = newCompilationUnit.WithMembers(
                    newCompilationUnit.Members.Insert(insertionIndex, newFieldDeclaration))

                Return Tuple.Create(document.Document.WithSyntaxRoot(newRoot), destination, insertionIndex)
            End If
        End Function

        Private Async Function IntroduceFieldIntoTypeAsync(
                document As SemanticDocument,
                expression As ExpressionSyntax,
                newQualifiedName As MemberAccessExpressionSyntax,
                oldTypeBlock As TypeBlockSyntax,
                oldType As INamedTypeSymbol,
                newNameToken As SyntaxToken,
                allOccurrences As Boolean,
                isConstant As Boolean,
                cancellationToken As CancellationToken) As Task(Of Tuple(Of Document, SyntaxNode, Integer))

            Dim oldToNewTypeBlockMap = New Dictionary(Of TypeBlockSyntax, TypeBlockSyntax)

            For Each declNode In oldType.DeclaringSyntaxReferences.Select(Function(r) r.GetSyntax().Parent).OfType(Of TypeBlockSyntax)()
                Dim currentDocument = Await SemanticDocument.CreateAsync(document.Project.Solution.GetDocument(declNode.SyntaxTree), cancellationToken).ConfigureAwait(False)
                Dim newDeclNode = Rewrite(document, expression, newQualifiedName, currentDocument, declNode, allOccurrences, cancellationToken)

                oldToNewTypeBlockMap(declNode) = newDeclNode
            Next

            Dim newTypeDeclaration = oldToNewTypeBlockMap(oldTypeBlock)

            Dim insertionIndex = If(isConstant,
                                DetermineConstantInsertPosition(oldTypeBlock.Members, newTypeDeclaration.Members),
                                DetermineFieldInsertPosition(oldTypeBlock.Members, newTypeDeclaration.Members))
            Dim destination As SyntaxNode = oldTypeBlock

            Dim newFieldDeclaration = CreateFieldDeclaration(
                document, oldTypeBlock, newNameToken, expression, allOccurrences, isConstant, cancellationToken)
            Dim finalTypeDeclaration = InsertMember(newTypeDeclaration, newFieldDeclaration, insertionIndex)

            oldToNewTypeBlockMap(oldTypeBlock) = finalTypeDeclaration

            Dim typeBlocksGroupedByTree = oldToNewTypeBlockMap.GroupBy(Function(kvp) kvp.Key.SyntaxTree)
            Dim updatedDocument = document.Document

            For Each group In typeBlocksGroupedByTree
                Dim syntaxTree = group.Key
                Dim currentDocument = document.Project.Solution.GetDocument(syntaxTree)

                Dim oldRoot = syntaxTree.GetRoot(cancellationToken)
                Dim newRoot = oldRoot.ReplaceNodes(
                    group.Select(Function(kvp) kvp.Key),
                    Function(n1, n2)
                        Return oldToNewTypeBlockMap(n1)
                    End Function)

                updatedDocument = updatedDocument.Project.Solution.GetDocument(currentDocument.Id).WithSyntaxRoot(newRoot)
            Next

            Return Tuple.Create(updatedDocument, destination, insertionIndex)
        End Function

        Protected Shared Function DetermineConstantInsertPosition(oldMembers As SyntaxList(Of StatementSyntax),
                                                                  newMembers As SyntaxList(Of StatementSyntax)) As Integer
            ' 1) Place the constant after the last constant.
            '
            ' 2) If there is no constant, place it before the first field
            '
            ' 3) If the first change is before either of those, then place before the first
            ' change
            '
            ' 4) Otherwise, place it at the start.
            Dim index = 0
            Dim lastConstantIndex = oldMembers.LastIndexOf(AddressOf IsConstantField)
            If lastConstantIndex >= 0 Then
                index = lastConstantIndex + 1
            Else
                Dim firstFieldIndex = oldMembers.IndexOf(Function(member) TypeOf member Is FieldDeclarationSyntax)
                If firstFieldIndex >= 0 Then
                    index = firstFieldIndex
                End If

            End If

            Dim firstChangeIndex = DetermineFirstChange(oldMembers, newMembers)
            If firstChangeIndex >= 0 Then
                index = Math.Min(index, firstChangeIndex)
            End If

            Return index
        End Function

        Protected Shared Function DetermineFieldInsertPosition(oldMembers As SyntaxList(Of StatementSyntax),
                                                               newMembers As SyntaxList(Of StatementSyntax)) As Integer
            ' 1) Place the constant after the last field.
            '
            ' 2) If there is no field, place it after the last constant
            '
            ' 3) If the first change is before either of those, then place before the first
            ' change
            '
            ' 4) Otherwise, place it at the start.
            Dim index = 0
            Dim lastFieldIndex = oldMembers.LastIndexOf(Function(member) TypeOf member Is FieldDeclarationSyntax)
            If lastFieldIndex >= 0 Then
                index = lastFieldIndex + 1
            Else
                Dim lastConstantIndex = oldMembers.LastIndexOf(AddressOf IsConstantField)
                If lastConstantIndex >= 0 Then
                    index = lastConstantIndex + 1
                End If

            End If

            Dim firstChangeIndex = DetermineFirstChange(oldMembers, newMembers)
            If firstChangeIndex >= 0 Then
                index = Math.Min(index, firstChangeIndex)
            End If

            Return index
        End Function

        Private Shared Function IsConstantField(member As StatementSyntax) As Boolean
            Dim field = TryCast(member, FieldDeclarationSyntax)
            Return field IsNot Nothing AndAlso field.Modifiers.Any(SyntaxKind.ConstKeyword)
        End Function

        Protected Shared Function DetermineFirstChange(oldMembers As SyntaxList(Of StatementSyntax),
                                                       newMembers As SyntaxList(Of StatementSyntax)) As Integer
            Dim i As Integer = 0

            While i < oldMembers.Count
                If Not SyntaxFactory.AreEquivalent(oldMembers(i), newMembers(i), topLevel:=False) Then
                    Return i
                End If

                i = i + 1
            End While

            Return -1
        End Function

        Private Function CreateFieldDeclaration(
                document As SemanticDocument,
                oldTypeDeclaration As TypeBlockSyntax,
                newNameToken As SyntaxToken,
                expression As ExpressionSyntax,
                allOccurrences As Boolean,
                isConstant As Boolean,
                cancellationToken As CancellationToken) As FieldDeclarationSyntax

            Dim matches = FindMatches(document, expression, document, oldTypeDeclaration, allOccurrences, cancellationToken)

            Dim trimmedExpression = expression.WithoutTrailingTrivia().WithoutLeadingTrivia()
            Return SyntaxFactory.FieldDeclaration(
                    Nothing,
                    MakeFieldModifiers(matches, isConstant, inScript:=oldTypeDeclaration Is Nothing, inModule:=oldTypeDeclaration.Kind = SyntaxKind.ModuleBlock),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(newNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()))),
                            asClause:=SyntaxFactory.SimpleAsClause(GetTypeSymbol(document, expression, cancellationToken).GenerateTypeSyntax()),
                            initializer:=SyntaxFactory.EqualsValue(value:=trimmedExpression)))) _
                    .WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker) _
                    .WithAdditionalAnnotations(Formatter.Annotation)

        End Function

        Private Function MakeFieldModifiers(expressions As IEnumerable(Of ExpressionSyntax),
                                            isConstant As Boolean,
                                            inScript As Boolean,
                                            inModule As Boolean) As SyntaxTokenList
            Dim modifiers = New List(Of SyntaxToken)

            Dim inTypeAttribute = expressions.Select(Function(e) e.GetAncestor(Of AttributeListSyntax)()).
                                              WhereNotNull().
                                              Any(Function(a) TypeOf a.Parent Is TypeStatementSyntax)

            If inTypeAttribute Then
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword))
            Else
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
            End If

            If isConstant Then
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.ConstKeyword))
            ElseIf inScript Then
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            ElseIf inModule Then
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            Else
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword))
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            End If

            Return SyntaxFactory.TokenList(modifiers)
        End Function

        Protected Shared Function InsertMember(typeDeclaration As TypeBlockSyntax,
                                               memberDeclaration As StatementSyntax,
                                               index As Integer) As TypeBlockSyntax
            Return typeDeclaration.WithMembers(
                typeDeclaration.Members.Insert(index, memberDeclaration))
        End Function
    End Class
End Namespace
