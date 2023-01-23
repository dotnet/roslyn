' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module ConstructorGenerator

        Private Function LastConstructorOrField(Of TDeclaration As SyntaxNode)(members As SyntaxList(Of TDeclaration)) As TDeclaration
            Return If(LastConstructor(members), LastField(members))
        End Function

        Friend Function AddConstructorTo(destination As TypeBlockSyntax,
                                                constructor As IMethodSymbol,
                                                options As CodeGenerationContextInfo,
                                                availableIndices As IList(Of Boolean)) As TypeBlockSyntax
            Dim constructorDeclaration = GenerateConstructorDeclaration(constructor, GetDestination(destination), options)

            Dim members = Insert(destination.Members, constructorDeclaration, options, availableIndices,
                                 after:=AddressOf LastConstructorOrField,
                                 before:=AddressOf FirstMember)

            Return FixTerminators(destination.WithMembers(members))
        End Function

        Friend Function GenerateConstructorDeclaration(constructor As IMethodSymbol,
                                                              destination As CodeGenerationDestination,
                                                              options As CodeGenerationContextInfo) As StatementSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of StatementSyntax)(constructor, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim constructorStatement =
                SyntaxFactory.SubNewStatement() _
                .WithAttributeLists(AttributeGenerator.GenerateAttributeBlocks(constructor.GetAttributes(), options)) _
                .WithModifiers(GenerateModifiers(constructor, destination, options)) _
                .WithParameterList(ParameterGenerator.GenerateParameterList(constructor.Parameters, options))

            Dim hasNoBody = Not options.Context.GenerateMethodBodies

            Dim declaration =
                If(hasNoBody,
                   DirectCast(constructorStatement, StatementSyntax),
                   SyntaxFactory.ConstructorBlock(
                      constructorStatement,
                      statements:=GenerateStatements(constructor),
                      endSubStatement:=SyntaxFactory.EndSubStatement()))

            Return AddAnnotationsTo(constructor, AddFormatterAndCodeGeneratorAnnotationsTo(
                ConditionallyAddDocumentationCommentTo(declaration, constructor, options)))
        End Function

        Private Function GenerateStatements(constructor As IMethodSymbol) As SyntaxList(Of StatementSyntax)
            If CodeGenerationConstructorInfo.GetStatements(constructor).IsDefault AndAlso
               CodeGenerationConstructorInfo.GetBaseConstructorArgumentsOpt(constructor).IsDefault AndAlso
               CodeGenerationConstructorInfo.GetThisConstructorArgumentsOpt(constructor).IsDefault Then
                Return Nothing
            End If

            Dim statements = New List(Of StatementSyntax)
            If Not CodeGenerationConstructorInfo.GetBaseConstructorArgumentsOpt(constructor).IsDefault Then
                statements.Add(CreateBaseConstructorCall(constructor))
            End If

            If Not CodeGenerationConstructorInfo.GetThisConstructorArgumentsOpt(constructor).IsDefault Then
                statements.Add(CreateThisConstructorCall(CodeGenerationConstructorInfo.GetThisConstructorArgumentsOpt(constructor)))
            End If

            If Not CodeGenerationConstructorInfo.GetStatements(constructor).IsDefault Then
                statements.AddRange(StatementGenerator.GenerateStatements(
                    CodeGenerationConstructorInfo.GetStatements(constructor)))
            End If

            Return SyntaxFactory.List(statements)
        End Function

        Private Function GenerateModifiers(constructor As IMethodSymbol, destination As CodeGenerationDestination, options As CodeGenerationContextInfo) As SyntaxTokenList
            Dim tokens As ArrayBuilder(Of SyntaxToken) = Nothing
            Using x = ArrayBuilder(Of SyntaxToken).GetInstance(tokens)
                If constructor.IsStatic Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword))
                Else
                    AddAccessibilityModifiers(constructor.DeclaredAccessibility, tokens, destination, options, Accessibility.Public)
                End If

                Return SyntaxFactory.TokenList(tokens)
            End Using
        End Function

        Private Function CreateBaseConstructorCall(constructor As IMethodSymbol) As StatementSyntax
            Return SyntaxFactory.ExpressionStatement(
                expression:=SyntaxFactory.InvocationExpression(
                    SyntaxFactory.SimpleMemberAccessExpression(
                        expression:=SyntaxFactory.MyBaseExpression(),
                        operatorToken:=SyntaxFactory.Token(SyntaxKind.DotToken),
                        name:=SyntaxFactory.IdentifierName("New")),
                argumentList:=SyntaxFactory.ArgumentList(
                    arguments:=SyntaxFactory.SeparatedList(constructor.Parameters.Select(Function(p) SyntaxFactory.SimpleArgument(SyntaxFactory.IdentifierName(p.Name))).OfType(Of ArgumentSyntax)))))
        End Function

        Private Function CreateThisConstructorCall(arguments As IList(Of SyntaxNode)) As StatementSyntax
            Return SyntaxFactory.ExpressionStatement(
                expression:=SyntaxFactory.InvocationExpression(
                    SyntaxFactory.SimpleMemberAccessExpression(
                        expression:=SyntaxFactory.MeExpression(),
                        operatorToken:=SyntaxFactory.Token(SyntaxKind.DotToken),
                        name:=SyntaxFactory.IdentifierName("New")),
                argumentList:=ArgumentGenerator.GenerateArgumentList(arguments)))
        End Function
    End Module
End Namespace
