' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module EventGenerator

        Private Function AfterMember(
                members As SyntaxList(Of StatementSyntax),
                eventDeclaration As StatementSyntax) As StatementSyntax
            If eventDeclaration.Kind = SyntaxKind.EventStatement Then
                ' Field style events go after the last field event, or after the last field.
                Dim lastEvent = members.LastOrDefault(Function(m) TypeOf m Is EventStatementSyntax)

                Return If(lastEvent, LastField(members))
            End If

            If eventDeclaration.Kind = SyntaxKind.EventBlock Then
                ' Property style events go after existing events, then after existing constructors.
                Dim lastEvent = members.LastOrDefault(Function(m) m.Kind = SyntaxKind.EventBlock)

                Return If(lastEvent, LastConstructor(members))
            End If

            Return Nothing
        End Function

        Private Function BeforeMember(
                members As SyntaxList(Of StatementSyntax),
                eventDeclaration As StatementSyntax) As StatementSyntax
            ' If it's a field style event, then it goes before everything else if we don't have any
            ' existing fields/events.
            If eventDeclaration.Kind = SyntaxKind.FieldDeclaration Then
                Return members.FirstOrDefault()
            End If

            ' Otherwise just place it before the methods.
            Return FirstMethod(members)
        End Function

        Friend Function AddEventTo(destination As TypeBlockSyntax,
                                    [event] As IEventSymbol,
                                    options As CodeGenerationContextInfo,
                                    availableIndices As IList(Of Boolean)) As TypeBlockSyntax
            Dim eventDeclaration = GenerateEventDeclaration([event], GetDestination(destination), options)

            Dim members = Insert(destination.Members, eventDeclaration, options, availableIndices,
                                 after:=Function(list) AfterMember(list, eventDeclaration),
                                 before:=Function(list) BeforeMember(list, eventDeclaration))

            ' Find the best place to put the field.  It should go after the last field if we already
            ' have fields, or at the beginning of the file if we don't.
            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Function GenerateEventDeclaration([event] As IEventSymbol,
                                                 destination As CodeGenerationDestination,
                                                 options As CodeGenerationContextInfo) As DeclarationStatementSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of DeclarationStatementSyntax)([event], options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim declaration = GenerateEventDeclarationWorker([event], destination, options)

            Return AddFormatterAndCodeGeneratorAnnotationsTo(ConditionallyAddDocumentationCommentTo(declaration, [event], options))
        End Function

        Private Function GenerateEventDeclarationWorker([event] As IEventSymbol,
                                                        destination As CodeGenerationDestination,
                                                        options As CodeGenerationContextInfo) As DeclarationStatementSyntax

            If options.Context.GenerateMethodBodies AndAlso
                ([event].AddMethod IsNot Nothing OrElse [event].RemoveMethod IsNot Nothing OrElse [event].RaiseMethod IsNot Nothing) Then
                Return GenerateCustomEventDeclarationWorker([event], destination, options)
            Else
                Return GenerateNotCustomEventDeclarationWorker([event], destination, options)
            End If
        End Function

        Private Function GenerateCustomEventDeclarationWorker(
                [event] As IEventSymbol,
                destination As CodeGenerationDestination,
                options As CodeGenerationContextInfo) As DeclarationStatementSyntax
            Dim addStatements = If(
                [event].AddMethod Is Nothing,
                New SyntaxList(Of StatementSyntax),
                GenerateStatements([event].AddMethod))
            Dim removeStatements = If(
                [event].RemoveMethod Is Nothing,
                New SyntaxList(Of StatementSyntax),
                GenerateStatements([event].RemoveMethod))
            Dim raiseStatements = If(
                [event].RaiseMethod Is Nothing,
                New SyntaxList(Of StatementSyntax),
                GenerateStatements([event].RaiseMethod))

            Dim generator As VisualBasicSyntaxGenerator = New VisualBasicSyntaxGenerator()

            Dim invoke = DirectCast([event].Type, INamedTypeSymbol)?.DelegateInvokeMethod
            Dim parameters = If(
                invoke IsNot Nothing,
                invoke.Parameters.Select(Function(p) generator.ParameterDeclaration(p)),
                Nothing)

            Dim result = DirectCast(generator.CustomEventDeclarationWithRaise(
                                        [event].Name,
                                        generator.TypeExpression([event].Type),
                                        [event].DeclaredAccessibility,
                                        DeclarationModifiers.From([event]),
                                        parameters,
                                        addStatements,
                                        removeStatements,
                                        raiseStatements), EventBlockSyntax)
            result = DirectCast(
                result.WithAttributeLists(GenerateAttributeBlocks([event].GetAttributes(), options)),
                EventBlockSyntax)
            result = DirectCast(result.WithModifiers(GenerateModifiers([event], destination, options)), EventBlockSyntax)
            Dim explicitInterface = [event].ExplicitInterfaceImplementations.FirstOrDefault()
            If (explicitInterface IsNot Nothing)
                result = result.WithEventStatement(
                    result.EventStatement.WithImplementsClause(GenerateImplementsClause(explicitInterface)))
            End If

            Return result
        End Function

        Private Function GenerateNotCustomEventDeclarationWorker(
                [event] As IEventSymbol,
                destination As CodeGenerationDestination,
                options As CodeGenerationContextInfo) As EventStatementSyntax
            Dim eventType = TryCast([event].Type, INamedTypeSymbol)
            If eventType.IsDelegateType() AndAlso eventType.AssociatedSymbol IsNot Nothing Then
                ' This is a declaration style event like "Event E(x As String)".  This event will
                ' have a type that is unmentionable.  So we should not generate it as "Event E() As
                ' SomeType", but should instead inline the delegate type into the event itself.
                Return SyntaxFactory.EventStatement(
                    attributeLists:=GenerateAttributeBlocks([event].GetAttributes(), options),
                    modifiers:=GenerateModifiers([event], destination, options),
                    identifier:=[event].Name.ToIdentifierToken,
                    parameterList:=ParameterGenerator.GenerateParameterList(eventType.DelegateInvokeMethod.Parameters.Select(Function(p) RemoveOptionalOrParamArray(p)).ToList(), options),
                    asClause:=Nothing,
                    implementsClause:=GenerateImplementsClause([event].ExplicitInterfaceImplementations.FirstOrDefault()))
            End If

            Return SyntaxFactory.EventStatement(
                attributeLists:=GenerateAttributeBlocks([event].GetAttributes(), options),
                modifiers:=GenerateModifiers([event], destination, options),
                identifier:=[event].Name.ToIdentifierToken,
                parameterList:=Nothing,
                asClause:=GenerateAsClause([event]),
                implementsClause:=GenerateImplementsClause([event].ExplicitInterfaceImplementations.FirstOrDefault()))
        End Function

        Private Function GenerateModifiers([event] As IEventSymbol,
                                                  destination As CodeGenerationDestination,
                                                  options As CodeGenerationContextInfo) As SyntaxTokenList
            Dim tokens As ArrayBuilder(Of SyntaxToken) = Nothing
            Using x = ArrayBuilder(Of SyntaxToken).GetInstance(tokens)

                If destination <> CodeGenerationDestination.InterfaceType Then
                    AddAccessibilityModifiers([event].DeclaredAccessibility, tokens, destination, options, Accessibility.Public)

                    If [event].IsStatic Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword))
                    End If

                    If [event].IsAbstract Then
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword))
                    End If
                End If

                Return SyntaxFactory.TokenList(tokens)
            End Using
        End Function

        Private Function GenerateAsClause([event] As IEventSymbol) As SimpleAsClauseSyntax
            ' TODO: Someday support events without as clauses (with parameter lists instead)
            Return SyntaxFactory.SimpleAsClause([event].Type.GenerateTypeSyntax())
        End Function

        Private Function RemoveOptionalOrParamArray(parameter As IParameterSymbol) As IParameterSymbol
            If Not parameter.IsOptional AndAlso Not parameter.IsParams Then
                Return parameter
            Else
                Return CodeGenerationSymbolFactory.CreateParameterSymbol(parameter.GetAttributes(), parameter.RefKind, isParams:=False, type:=parameter.Type, name:=parameter.Name, hasDefaultValue:=False)
            End If
        End Function
    End Module
End Namespace
