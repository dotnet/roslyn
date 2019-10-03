' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class MethodGenerator

        Friend Shared Function AddMethodTo(destination As NamespaceBlockSyntax,
                                           method As IMethodSymbol,
                                           options As CodeGenerationOptions,
                                           availableIndices As IList(Of Boolean)) As NamespaceBlockSyntax

            Dim declaration = GenerateMethodDeclaration(method, CodeGenerationDestination.Namespace, options)

            Dim members = Insert(destination.Members, declaration, options, availableIndices,
                                 after:=AddressOf LastMethod)

            Return destination.WithMembers(SyntaxFactory.List(members))
        End Function

        Friend Shared Function AddMethodTo(destination As CompilationUnitSyntax,
                                           method As IMethodSymbol,
                                           options As CodeGenerationOptions,
                                           availableIndices As IList(Of Boolean)) As CompilationUnitSyntax

            Dim declaration = GenerateMethodDeclaration(method, CodeGenerationDestination.Namespace, options)

            Dim members = Insert(destination.Members, declaration, options, availableIndices,
                                 after:=AddressOf LastMethod)

            Return destination.WithMembers(SyntaxFactory.List(members))
        End Function

        Friend Shared Function AddMethodTo(destination As TypeBlockSyntax,
                                           method As IMethodSymbol,
                                           options As CodeGenerationOptions,
                                           availableIndices As IList(Of Boolean)) As TypeBlockSyntax

            Dim methodDeclaration = GenerateMethodDeclaration(method, GetDestination(destination), options)

            Dim members = Insert(destination.Members, methodDeclaration, options, availableIndices,
                                 after:=AddressOf LastMethod)

            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Shared Function GenerateMethodDeclaration(method As IMethodSymbol,
                                                         destination As CodeGenerationDestination,
                                                         options As CodeGenerationOptions) As StatementSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of StatementSyntax)(method, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim declaration = GenerateMethodDeclarationWorker(method, destination, options)

            Return AddAnnotationsTo(method,
                AddFormatterAndCodeGeneratorAnnotationsTo(
                    ConditionallyAddDocumentationCommentTo(declaration, method, options)))
        End Function

        Private Shared Function GenerateMethodDeclarationWorker(method As IMethodSymbol,
                                                                destination As CodeGenerationDestination,
                                                                options As CodeGenerationOptions) As StatementSyntax
            Dim isSub = method.ReturnType.SpecialType = SpecialType.System_Void
            Dim kind = If(isSub, SyntaxKind.SubStatement, SyntaxKind.FunctionStatement)
            Dim keyword = If(isSub, SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword)
            Dim asClauseOpt = GenerateAsClause(method, isSub, options)
            Dim implementsClauseOpt = GenerateImplementsClause(method.ExplicitInterfaceImplementations.FirstOrDefault())
            Dim handlesClauseOpt = GenerateHandlesClause(CodeGenerationMethodInfo.GetHandlesExpressions(method))

            Dim begin =
                SyntaxFactory.MethodStatement(kind, subOrFunctionKeyword:=SyntaxFactory.Token(keyword), identifier:=method.Name.ToIdentifierToken).
                    WithAttributeLists(AttributeGenerator.GenerateAttributeBlocks(method.GetAttributes(), options)).
                    WithModifiers(GenerateModifiers(method, destination, options)).
                    WithTypeParameterList(GenerateTypeParameterList(method)).
                    WithParameterList(ParameterGenerator.GenerateParameterList(method.Parameters, options)).
                    WithAsClause(asClauseOpt).
                    WithImplementsClause(implementsClauseOpt).
                    WithHandlesClause(handlesClauseOpt)

            Dim hasNoBody = Not options.GenerateMethodBodies OrElse
                            method.IsAbstract OrElse
                            destination = CodeGenerationDestination.InterfaceType

            If hasNoBody Then
                Return begin
            End If

            Dim endConstruct = If(isSub, SyntaxFactory.EndSubStatement(), SyntaxFactory.EndFunctionStatement())
            Return SyntaxFactory.MethodBlock(
                If(isSub, SyntaxKind.SubBlock, SyntaxKind.FunctionBlock),
                begin,
                statements:=StatementGenerator.GenerateStatements(method),
                endSubOrFunctionStatement:=endConstruct)
        End Function

        Private Shared Function GenerateAsClause(method As IMethodSymbol, isSub As Boolean, options As CodeGenerationOptions) As SimpleAsClauseSyntax
            If isSub Then
                Return Nothing
            End If

            Return SyntaxFactory.SimpleAsClause(
                AttributeGenerator.GenerateAttributeBlocks(method.GetReturnTypeAttributes(), options),
                method.ReturnType.GenerateTypeSyntax())
        End Function

        Private Shared Function GenerateHandlesClause(expressions As IList(Of SyntaxNode)) As HandlesClauseSyntax
            Dim memberAccessExpressions = expressions.OfType(Of MemberAccessExpressionSyntax).ToList()

            Dim items = From def In memberAccessExpressions
                        Let expr1 = def.Expression
                        Where expr1 IsNot Nothing
                        Let expr2 = If(TypeOf expr1 Is ParenthesizedExpressionSyntax, DirectCast(expr1, ParenthesizedExpressionSyntax).Expression, expr1)
                        Let children = expr2.ChildNodesAndTokens()
                        Where children.Count = 1 AndAlso children(0).IsToken
                        Let token = children(0).AsToken()
                        Where token.Kind = SyntaxKind.IdentifierToken OrElse
                              token.Kind = SyntaxKind.MyBaseKeyword OrElse
                              token.Kind = SyntaxKind.MyClassKeyword OrElse
                              token.Kind = SyntaxKind.MeKeyword
                        Where TypeOf def.Name Is IdentifierNameSyntax
                        Let identifier = def.Name.Identifier.ValueText.ToIdentifierName()
                        Select SyntaxFactory.HandlesClauseItem(If(token.Kind = SyntaxKind.IdentifierToken,
                                                           DirectCast(SyntaxFactory.WithEventsEventContainer(token.ValueText.ToIdentifierToken()), EventContainerSyntax),
                                                           SyntaxFactory.KeywordEventContainer(token)), identifier)

            Dim array = items.ToArray()
            Return If(array.Length = 0, Nothing, SyntaxFactory.HandlesClause(array))
        End Function

        Private Overloads Shared Function GenerateTypeParameterList(method As IMethodSymbol) As TypeParameterListSyntax
            Return TypeParameterGenerator.GenerateTypeParameterList(method.TypeParameters)
        End Function

        Private Shared Function GenerateModifiers(method As IMethodSymbol,
                                                  destination As CodeGenerationDestination,
                                                  options As CodeGenerationOptions) As SyntaxTokenList
            Dim result = New List(Of SyntaxToken)()
            If destination <> CodeGenerationDestination.InterfaceType Then
                AddAccessibilityModifiers(method.DeclaredAccessibility, result, destination, options, Accessibility.Public)

                If method.IsAbstract Then
                    result.Add(SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword))
                End If

                If method.IsSealed Then
                    result.Add(SyntaxFactory.Token(SyntaxKind.NotOverridableKeyword))
                End If

                If method.IsVirtual Then
                    result.Add(SyntaxFactory.Token(SyntaxKind.OverridableKeyword))
                End If

                If method.IsOverride Then
                    result.Add(SyntaxFactory.Token(SyntaxKind.OverridesKeyword))
                End If

                If method.IsStatic AndAlso destination <> CodeGenerationDestination.ModuleType Then
                    result.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword))
                End If

                If CodeGenerationMethodInfo.GetIsNew(method) Then
                    result.Add(SyntaxFactory.Token(SyntaxKind.ShadowsKeyword))
                End If

                If CodeGenerationMethodInfo.GetIsAsync(method) Then
                    result.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                End If
            End If

            Return SyntaxFactory.TokenList(result)
        End Function
    End Class
End Namespace
