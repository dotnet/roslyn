' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module PropertyGenerator

        Private Function LastPropertyOrField(Of TDeclaration As SyntaxNode)(
                members As SyntaxList(Of TDeclaration)) As TDeclaration
            Dim lastProperty = members.LastOrDefault(Function(m) m.Kind = SyntaxKind.PropertyBlock OrElse m.Kind = SyntaxKind.PropertyStatement)
            Return If(lastProperty, LastField(members))
        End Function

        Friend Function AddPropertyTo(destination As CompilationUnitSyntax,
                            [property] As IPropertySymbol,
                            options As CodeGenerationContextInfo,
                            availableIndices As IList(Of Boolean)) As CompilationUnitSyntax
            Dim propertyDeclaration = GeneratePropertyDeclaration([property], CodeGenerationDestination.CompilationUnit, options)

            Dim members = Insert(destination.Members, propertyDeclaration, options, availableIndices,
                                 after:=AddressOf LastPropertyOrField, before:=AddressOf FirstMember)

            Return destination.WithMembers(SyntaxFactory.List(members))
        End Function

        Friend Function AddPropertyTo(destination As TypeBlockSyntax,
                                    [property] As IPropertySymbol,
                                    options As CodeGenerationContextInfo,
                                    availableIndices As IList(Of Boolean)) As TypeBlockSyntax
            Dim propertyDeclaration = GeneratePropertyDeclaration([property], GetDestination(destination), options)

            Dim members = Insert(destination.Members, propertyDeclaration, options, availableIndices,
                                 after:=AddressOf LastPropertyOrField, before:=AddressOf FirstMember)

            ' Find the best place to put the field.  It should go after the last field if we already
            ' have fields, or at the beginning of the file if we don't.
            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Function GeneratePropertyDeclaration([property] As IPropertySymbol,
                                                           destination As CodeGenerationDestination,
                                                           options As CodeGenerationContextInfo) As StatementSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of DeclarationStatementSyntax)([property], options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim declaration = GeneratePropertyDeclarationWorker([property], destination, options)

            Return AddAnnotationsTo([property],
                AddFormatterAndCodeGeneratorAnnotationsTo(
                    ConditionallyAddDocumentationCommentTo(declaration, [property], options)))
        End Function

        Private Function GeneratePropertyDeclarationWorker([property] As IPropertySymbol,
                                                                  destination As CodeGenerationDestination,
                                                                  options As CodeGenerationContextInfo) As StatementSyntax

            Dim implementsClauseOpt = GenerateImplementsClause([property].ExplicitInterfaceImplementations.FirstOrDefault())

            Dim parameterList = GeneratePropertyParameterList([property], options)
            Dim asClause = GenerateAsClause([property], options)
            Dim begin = SyntaxFactory.PropertyStatement(identifier:=[property].Name.ToIdentifierToken).
                WithAttributeLists(AttributeGenerator.GenerateAttributeBlocks([property].GetAttributes(), options)).
                WithModifiers(GenerateModifiers([property], destination, options, parameterList)).
                WithParameterList(parameterList).
                WithAsClause(asClause).
                WithImplementsClause(implementsClauseOpt)

            ' If it's abstract or an auto-prop without a backing field, then we make just a single
            ' statement.
            Dim getMethod = [property].GetMethod
            Dim setMethod = [property].SetMethod
            Dim hasStatements =
                (getMethod IsNot Nothing AndAlso Not getMethod.IsAbstract) OrElse
                (setMethod IsNot Nothing AndAlso Not setMethod.IsAbstract)

            Dim hasNoBody =
                Not options.Context.GenerateMethodBodies OrElse
                destination = CodeGenerationDestination.InterfaceType OrElse
                [property].IsAbstract OrElse
                Not hasStatements

            If hasNoBody Then
                Return begin
            End If

            Return SyntaxFactory.PropertyBlock(
                begin,
                accessors:=GenerateAccessorList([property], destination, options),
                endPropertyStatement:=SyntaxFactory.EndPropertyStatement())
        End Function

        Private Function GeneratePropertyParameterList([property] As IPropertySymbol, options As CodeGenerationContextInfo) As ParameterListSyntax
            If [property].Parameters.IsDefault OrElse [property].Parameters.Length = 0 Then
                Return Nothing
            End If

            Return ParameterGenerator.GenerateParameterList([property].Parameters, options)
        End Function

        Private Function GenerateAccessorList([property] As IPropertySymbol,
                                                     destination As CodeGenerationDestination,
                                                     options As CodeGenerationContextInfo) As SyntaxList(Of AccessorBlockSyntax)
            Dim accessors = New List(Of AccessorBlockSyntax) From {
                GenerateAccessor([property], [property].GetMethod, isGetter:=True, destination:=destination, options:=options),
                GenerateAccessor([property], [property].SetMethod, isGetter:=False, destination:=destination, options:=options)
            }

            Return SyntaxFactory.List(accessors.WhereNotNull())
        End Function

        Private Function GenerateAccessor([property] As IPropertySymbol,
                                                 accessor As IMethodSymbol,
                                                 isGetter As Boolean,
                                                 destination As CodeGenerationDestination,
                                                 options As CodeGenerationContextInfo) As AccessorBlockSyntax
            If accessor Is Nothing Then
                Return Nothing
            End If

            Dim statementKind = If(isGetter, SyntaxKind.GetAccessorStatement, SyntaxKind.SetAccessorStatement)
            Dim blockKind = If(isGetter, SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock)

            If isGetter Then
                Return SyntaxFactory.GetAccessorBlock(
                    SyntaxFactory.GetAccessorStatement().WithModifiers(GenerateAccessorModifiers([property], accessor, destination, options)),
                    GenerateAccessorStatements(accessor),
                    SyntaxFactory.EndGetStatement())
            Else
                Return SyntaxFactory.SetAccessorBlock(
                    SyntaxFactory.SetAccessorStatement() _
                          .WithModifiers(GenerateAccessorModifiers([property], accessor, destination, options)) _
                          .WithParameterList(SyntaxFactory.ParameterList(parameters:=SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Parameter(identifier:=SyntaxFactory.ModifiedIdentifier("value")).WithAsClause(SyntaxFactory.SimpleAsClause(type:=[property].Type.GenerateTypeSyntax()))))),
                    GenerateAccessorStatements(accessor),
                    SyntaxFactory.EndSetStatement())
            End If
        End Function

        Private Function GenerateAccessorStatements(accessor As IMethodSymbol) As SyntaxList(Of StatementSyntax)
            Dim statementsOpt = CodeGenerationMethodInfo.GetStatements(accessor)
            If Not statementsOpt.IsDefault Then
                Return SyntaxFactory.List(statementsOpt.OfType(Of StatementSyntax))
            Else
                Return Nothing
            End If
        End Function

        Private Function GenerateAccessorModifiers([property] As IPropertySymbol,
                                                           accessor As IMethodSymbol,
                                                           destination As CodeGenerationDestination,
                                                           options As CodeGenerationContextInfo) As SyntaxTokenList
            If accessor.DeclaredAccessibility = Accessibility.NotApplicable OrElse
               accessor.DeclaredAccessibility = [property].DeclaredAccessibility Then
                Return New SyntaxTokenList()
            End If

            Dim modifiers As ArrayBuilder(Of SyntaxToken) = Nothing
            Using x = ArrayBuilder(Of SyntaxToken).GetInstance(modifiers)

                AddAccessibilityModifiers(accessor.DeclaredAccessibility, modifiers, destination, options, Accessibility.Public)
                Return SyntaxFactory.TokenList(modifiers)
            End Using
        End Function

        Private Function GenerateModifiers(
                [property] As IPropertySymbol,
                destination As CodeGenerationDestination,
                options As CodeGenerationContextInfo,
                parameterList As ParameterListSyntax) As SyntaxTokenList
            Dim tokens As ArrayBuilder(Of SyntaxToken) = Nothing
            Using x = ArrayBuilder(Of SyntaxToken).GetInstance(tokens)

                If [property].IsIndexer Then
                    Dim hasRequiredParameter = parameterList IsNot Nothing AndAlso parameterList.Parameters.Any(AddressOf IsRequired)
                    If hasRequiredParameter Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                    End If
                End If

                If destination <> CodeGenerationDestination.InterfaceType Then
                    AddAccessibilityModifiers([property].DeclaredAccessibility, tokens, destination, options, Accessibility.Public)

                    If [property].IsStatic AndAlso destination <> CodeGenerationDestination.ModuleType Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword))
                    End If

                    If CodeGenerationPropertyInfo.GetIsNew([property]) Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.ShadowsKeyword))
                    End If

                    If [property].IsVirtual Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.OverridableKeyword))
                    End If

                    If [property].IsOverride Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.OverridesKeyword))
                    End If

                    If [property].IsAbstract Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword))
                    End If

                    If [property].IsSealed Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.NotOverridableKeyword))
                    End If
                End If

                If [property].GetMethod Is Nothing AndAlso
               [property].SetMethod IsNot Nothing Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.WriteOnlyKeyword))
                End If

                If [property].SetMethod Is Nothing AndAlso
               [property].GetMethod IsNot Nothing Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                End If

                Return SyntaxFactory.TokenList(tokens)
            End Using
        End Function

        Private Function IsRequired(parameter As ParameterSyntax) As Boolean
            Return parameter.Modifiers.Count = 0 OrElse
                parameter.Modifiers.Any(SyntaxKind.ByValKeyword) OrElse
                parameter.Modifiers.Any(SyntaxKind.ByRefKeyword)
        End Function

        Private Function GenerateAsClause([property] As IPropertySymbol, options As CodeGenerationContextInfo) As AsClauseSyntax
            Dim attributes = If([property].GetMethod IsNot Nothing,
                                AttributeGenerator.GenerateAttributeBlocks([property].GetMethod.GetReturnTypeAttributes(), options),
                                Nothing)
            Return SyntaxFactory.SimpleAsClause(attributes, [property].Type.GenerateTypeSyntax())
        End Function
    End Module
End Namespace
