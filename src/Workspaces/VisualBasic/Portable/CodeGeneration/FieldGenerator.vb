' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module FieldGenerator

        Private Function LastField(Of TDeclaration As SyntaxNode)(
            members As SyntaxList(Of TDeclaration),
            fieldDeclaration As FieldDeclarationSyntax) As TDeclaration
            Dim lastConst = members.Where(Function(m) TypeOf m Is FieldDeclarationSyntax AndAlso
                                              DirectCast(DirectCast(m, Object), FieldDeclarationSyntax).Modifiers.Any(SyntaxKind.ConstKeyword)).LastOrDefault()

            ' Place a const after the last existing const.
            If fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword) Then
                Return lastConst
            End If

            ' Place a field after the last field, or after the last const.
            Return If(VisualBasicCodeGenerationHelpers.LastField(members), lastConst)
        End Function

        Friend Function AddFieldTo(destination As CompilationUnitSyntax,
                            field As IFieldSymbol,
                            options As CodeGenerationOptions,
                            availableIndices As IList(Of Boolean)) As CompilationUnitSyntax
            Dim fieldDeclaration = GenerateFieldDeclaration(field, CodeGenerationDestination.CompilationUnit, options)

            Dim members = Insert(destination.Members, fieldDeclaration, options, availableIndices,
                                 after:=Function(m) LastField(m, fieldDeclaration), before:=AddressOf FirstMember)
            Return destination.WithMembers(members)
        End Function

        Friend Function AddFieldTo(destination As TypeBlockSyntax,
                                    field As IFieldSymbol,
                                    options As CodeGenerationOptions,
                                    availableIndices As IList(Of Boolean)) As TypeBlockSyntax
            Dim fieldDeclaration = GenerateFieldDeclaration(field, GetDestination(destination), options)

            Dim members = Insert(destination.Members, fieldDeclaration, options, availableIndices,
                                 after:=Function(m) LastField(m, fieldDeclaration), before:=AddressOf FirstMember)

            ' Find the best place to put the field.  It should go after the last field if we already
            ' have fields, or at the beginning of the file if we don't.
            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Function GenerateFieldDeclaration(field As IFieldSymbol,
                                                        destination As CodeGenerationDestination,
                                                        options As CodeGenerationOptions) As FieldDeclarationSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of ModifiedIdentifierSyntax)(field, options)
            If reusableSyntax IsNot Nothing Then
                Dim variableDeclarator = TryCast(reusableSyntax.Parent, VariableDeclaratorSyntax)
                If variableDeclarator IsNot Nothing Then
                    Dim names = (New SeparatedSyntaxList(Of ModifiedIdentifierSyntax)).Add(reusableSyntax)
                    Dim newVariableDeclarator = variableDeclarator.WithNames(names)
                    Dim fieldDecl = TryCast(variableDeclarator.Parent, FieldDeclarationSyntax)
                    If fieldDecl IsNot Nothing Then
                        Return fieldDecl.WithDeclarators((New SeparatedSyntaxList(Of VariableDeclaratorSyntax)).Add(newVariableDeclarator))
                    End If
                End If
            End If

            Dim initializer = GenerateEqualsValue(field)

            Dim fieldDeclaration =
                SyntaxFactory.FieldDeclaration(
                    AttributeGenerator.GenerateAttributeBlocks(field.GetAttributes(), options),
                    GenerateModifiers(field, destination, options),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.SingletonSeparatedList(field.Name.ToModifiedIdentifier),
                            SyntaxFactory.SimpleAsClause(field.Type.GenerateTypeSyntax()),
                            initializer)))

            Return AddCleanupAnnotationsTo(ConditionallyAddDocumentationCommentTo(EnsureLastElasticTrivia(fieldDeclaration), field, options))
        End Function

        Private Function GenerateEqualsValue(field As IFieldSymbol) As EqualsValueSyntax
            If field.HasConstantValue Then
                Dim canUseFieldReference = field.Type IsNot Nothing AndAlso Not field.Type.Equals(field.ContainingType)
                Return SyntaxFactory.EqualsValue(ExpressionGenerator.GenerateExpression(field.Type, field.ConstantValue, canUseFieldReference))
            End If

            Return Nothing
        End Function

        Private Function GenerateModifiers(field As IFieldSymbol,
                                                  destination As CodeGenerationDestination,
                                                  options As CodeGenerationOptions) As SyntaxTokenList
            Dim tokens = New List(Of SyntaxToken)()
            AddAccessibilityModifiers(field.DeclaredAccessibility, tokens, destination, options, Accessibility.Private)

            If field.IsConst Then
                tokens.Add(SyntaxFactory.Token(SyntaxKind.ConstKeyword))
            Else
                If field.IsStatic AndAlso destination <> CodeGenerationDestination.ModuleType Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword))
                End If

                If field.IsReadOnly Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                End If

                If CodeGenerationFieldInfo.GetIsWithEvents(field) Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.WithEventsKeyword))
                End If

                If tokens.Count = 0 Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.DimKeyword))
                End If
            End If

            Return SyntaxFactory.TokenList(tokens)
        End Function
    End Module
End Namespace
