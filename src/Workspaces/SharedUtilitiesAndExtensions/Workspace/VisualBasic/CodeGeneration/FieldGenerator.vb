' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module FieldGenerator

        Private Function LastField(Of TDeclaration As SyntaxNode)(
                members As SyntaxList(Of TDeclaration),
                fieldDeclaration As FieldDeclarationSyntax) As TDeclaration

            Dim lastConst = members.OfType(Of FieldDeclarationSyntax).
                                    Where(Function(f) f.Modifiers.Any(SyntaxKind.ConstKeyword)).
                                    LastOrDefault()

            ' Place a const after the last existing const.
            If fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword) Then
                Return DirectCast(DirectCast(lastConst, Object), TDeclaration)
            End If

            Dim lastReadOnly = members.OfType(Of FieldDeclarationSyntax)().
                                       Where(Function(f) f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)).
                                       LastOrDefault()

            Dim lastNormal = members.OfType(Of FieldDeclarationSyntax)().
                                     Where(Function(f) Not f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) AndAlso Not f.Modifiers.Any(SyntaxKind.ConstKeyword)).
                                     LastOrDefault()

            Dim result =
                If(fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword),
                    If(lastReadOnly, If(lastNormal, lastConst)),
                    If(lastNormal, If(lastReadOnly, lastConst)))

            Return DirectCast(DirectCast(result, Object), TDeclaration)
        End Function

        Friend Function AddFieldTo(destination As CompilationUnitSyntax,
                            field As IFieldSymbol,
                            options As CodeGenerationContextInfo,
                            availableIndices As IList(Of Boolean)) As CompilationUnitSyntax
            Dim fieldDeclaration = GenerateFieldDeclaration(field, CodeGenerationDestination.CompilationUnit, options)

            Dim members = Insert(destination.Members, fieldDeclaration, options, availableIndices,
                                 after:=Function(m) LastField(m, fieldDeclaration), before:=AddressOf FirstMember)
            Return destination.WithMembers(members)
        End Function

        Friend Function AddFieldTo(destination As TypeBlockSyntax,
                                    field As IFieldSymbol,
                                    options As CodeGenerationContextInfo,
                                    availableIndices As IList(Of Boolean)) As TypeBlockSyntax
            Dim fieldDeclaration = GenerateFieldDeclaration(field, GetDestination(destination), options)

            Dim members = Insert(destination.Members, fieldDeclaration, options, availableIndices,
                                 after:=Function(m) LastField(m, fieldDeclaration), before:=AddressOf FirstMember)

            ' Find the best place to put the field.  It should go after the last field if we already
            ' have fields, or at the beginning of the file if we don't.
            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Function GenerateFieldDeclaration(
                field As IFieldSymbol,
                destination As CodeGenerationDestination,
                options As CodeGenerationContextInfo) As FieldDeclarationSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of FieldDeclarationSyntax)(field, options)
            If reusableSyntax IsNot Nothing Then
                Return EnsureLastElasticTrivia(reusableSyntax)
            End If

            Dim initializerNode = TryCast(CodeGenerationFieldInfo.GetInitializer(field), ExpressionSyntax)
            Dim initializer = If(initializerNode IsNot Nothing, SyntaxFactory.EqualsValue(initializerNode), GenerateEqualsValue(options.Generator, field))

            Dim fieldDeclaration =
                SyntaxFactory.FieldDeclaration(
                    AttributeGenerator.GenerateAttributeBlocks(field.GetAttributes(), options),
                    GenerateModifiers(field, destination, options),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.SingletonSeparatedList(field.Name.ToModifiedIdentifier),
                            SyntaxFactory.SimpleAsClause(field.Type.GenerateTypeSyntax()),
                            initializer)))

            Return AddFormatterAndCodeGeneratorAnnotationsTo(ConditionallyAddDocumentationCommentTo(EnsureLastElasticTrivia(fieldDeclaration), field, options))
        End Function

        Private Function GenerateEqualsValue(generator As SyntaxGenerator, field As IFieldSymbol) As EqualsValueSyntax
            If field.HasConstantValue Then
                Dim canUseFieldReference = field.Type IsNot Nothing AndAlso Not field.Type.Equals(field.ContainingType)
                Return SyntaxFactory.EqualsValue(ExpressionGenerator.GenerateExpression(generator, field.Type, field.ConstantValue, canUseFieldReference))
            End If

            Return Nothing
        End Function

        Private Function GenerateModifiers(field As IFieldSymbol,
                                                  destination As CodeGenerationDestination,
                                                  options As CodeGenerationContextInfo) As SyntaxTokenList
            Dim tokens As ArrayBuilder(Of SyntaxToken) = Nothing
            Using x = ArrayBuilder(Of SyntaxToken).GetInstance(tokens)

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
            End Using
        End Function
    End Module
End Namespace
