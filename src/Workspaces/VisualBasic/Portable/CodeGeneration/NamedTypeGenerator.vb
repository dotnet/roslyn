' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration

    Friend Module NamedTypeGenerator

        Public Function AddNamedTypeTo(service As ICodeGenerationService,
                                    destination As TypeBlockSyntax,
                                    namedType As INamedTypeSymbol,
                                    options As CodeGenerationOptions,
                                    availableIndices As IList(Of Boolean),
                                    cancellationToken As CancellationToken) As TypeBlockSyntax
            Dim declaration = GenerateNamedTypeDeclaration(service, namedType, options, cancellationToken)
            Dim members = Insert(destination.Members, declaration, options, availableIndices)
            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Function AddNamedTypeTo(service As ICodeGenerationService,
                                    destination As NamespaceBlockSyntax,
                                    namedType As INamedTypeSymbol,
                                    options As CodeGenerationOptions,
                                    availableIndices As IList(Of Boolean),
                                       cancellationToken As CancellationToken) As NamespaceBlockSyntax
            Dim declaration = GenerateNamedTypeDeclaration(service, namedType, options, cancellationToken)
            Dim members = Insert(destination.Members, declaration, options, availableIndices)
            Return destination.WithMembers(members)
        End Function

        Public Function AddNamedTypeTo(service As ICodeGenerationService,
                                    destination As CompilationUnitSyntax,
                                    namedType As INamedTypeSymbol,
                                    options As CodeGenerationOptions,
                                    availableIndices As IList(Of Boolean),
                                       cancellationToken As CancellationToken) As CompilationUnitSyntax
            Dim declaration = GenerateNamedTypeDeclaration(service, namedType, options, cancellationToken)
            Dim members = Insert(destination.Members, declaration, options, availableIndices)
            Return destination.WithMembers(members)
        End Function

        Public Function GenerateNamedTypeDeclaration(service As ICodeGenerationService,
                                    namedType As INamedTypeSymbol,
                                    options As CodeGenerationOptions,
                                    cancellationToken As CancellationToken) As StatementSyntax
            options = If(options, CodeGenerationOptions.Default)

            Dim declaration = GetDeclarationSyntaxWithoutMembers(namedType, options)

            declaration = If(options.GenerateMembers AndAlso namedType.TypeKind <> TypeKind.Delegate,
                service.AddMembers(declaration, GetMembers(namedType), options, cancellationToken),
                declaration)

            Return AddCleanupAnnotationsTo(ConditionallyAddDocumentationCommentTo(declaration, namedType, options))
        End Function

        Public Function UpdateNamedTypeDeclaration(service As ICodeGenerationService,
                                                          declaration As StatementSyntax,
                                                          newMembers As IList(Of ISymbol),
                                                          options As CodeGenerationOptions,
                                                          cancellationToken As CancellationToken) As StatementSyntax
            declaration = RemoveAllMembers(declaration)
            declaration = service.AddMembers(declaration, newMembers, options, cancellationToken)
            Return AddCleanupAnnotationsTo(declaration)
        End Function

        Private Function GetDeclarationSyntaxWithoutMembers(namedType As INamedTypeSymbol, options As CodeGenerationOptions) As StatementSyntax
            Dim reusableDeclarationSyntax = GetReuseableSyntaxNodeForSymbol(Of StatementSyntax)(namedType, options)
            If reusableDeclarationSyntax Is Nothing Then
                Return GenerateNamedTypeDeclarationWorker(namedType, options)
            End If

            Return RemoveAllMembers(reusableDeclarationSyntax)
        End Function

        Private Function RemoveAllMembers(declaration As StatementSyntax) As StatementSyntax
            Select Case declaration.Kind
                Case SyntaxKind.EnumBlock
                    Return DirectCast(declaration, EnumBlockSyntax).WithMembers(Nothing)

                Case SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ClassBlock
                    Return DirectCast(declaration, TypeBlockSyntax).WithMembers(Nothing)

                Case Else
                    Return declaration
            End Select
        End Function

        Private Function GenerateNamedTypeDeclarationWorker(namedType As INamedTypeSymbol, options As CodeGenerationOptions) As StatementSyntax
            ' TODO(cyrusn): Support enums/delegates.
            If namedType.TypeKind = TypeKind.Enum Then
                Return GenerateEnumDeclaration(namedType, options)
            ElseIf namedType.TypeKind = TypeKind.Delegate Then
                Return GenerateDelegateDeclaration(namedType, options)
            End If

            Dim isInterface = namedType.TypeKind = TypeKind.Interface
            Dim isStruct = namedType.TypeKind = TypeKind.Struct
            Dim isModule = namedType.TypeKind = TypeKind.Module

            Dim blockKind =
                If(isInterface, SyntaxKind.InterfaceBlock, If(isStruct, SyntaxKind.StructureBlock, If(isModule, SyntaxKind.ModuleBlock, SyntaxKind.ClassBlock)))

            Dim statementKind =
                If(isInterface, SyntaxKind.InterfaceStatement, If(isStruct, SyntaxKind.StructureStatement, If(isModule, SyntaxKind.ModuleStatement, SyntaxKind.ClassStatement)))

            Dim keywordKind =
                If(isInterface, SyntaxKind.InterfaceKeyword, If(isStruct, SyntaxKind.StructureKeyword, If(isModule, SyntaxKind.ModuleKeyword, SyntaxKind.ClassKeyword)))

            Dim endStatement =
                If(isInterface, SyntaxFactory.EndInterfaceStatement(), If(isStruct, SyntaxFactory.EndStructureStatement(), If(isModule, SyntaxFactory.EndModuleStatement, SyntaxFactory.EndClassStatement)))

            Dim typeDeclaration =
                SyntaxFactory.TypeBlock(
                    blockKind,
                    SyntaxFactory.TypeStatement(
                        statementKind,
                        attributes:=GenerateAttributes(namedType, options),
                        modifiers:=GenerateModifiers(namedType),
                        keyword:=SyntaxFactory.Token(keywordKind),
                        identifier:=namedType.Name.ToIdentifierToken(),
                        typeParameterList:=GenerateTypeParameterList(namedType)),
                    [inherits]:=GenerateInheritsStatements(namedType),
                    implements:=GenerateImplementsStatements(namedType),
                    end:=endStatement)

            Return typeDeclaration
        End Function

        Private Function GenerateDelegateDeclaration(namedType As INamedTypeSymbol, options As CodeGenerationOptions) As StatementSyntax
            Dim invokeMethod = namedType.DelegateInvokeMethod
            Return SyntaxFactory.DelegateStatement(
                kind:=If(invokeMethod.ReturnsVoid, SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement),
                attributeLists:=GenerateAttributes(namedType, options),
                modifiers:=GenerateModifiers(namedType),
                subOrFunctionKeyword:=If(invokeMethod.ReturnsVoid, SyntaxFactory.Token(SyntaxKind.SubKeyword), SyntaxFactory.Token(SyntaxKind.FunctionKeyword)),
                identifier:=namedType.Name.ToIdentifierToken(),
                typeParameterList:=GenerateTypeParameterList(namedType),
                parameterList:=ParameterGenerator.GenerateParameterList(invokeMethod.Parameters, options),
                asClause:=If(invokeMethod.ReturnsVoid, Nothing,
                             SyntaxFactory.SimpleAsClause(invokeMethod.ReturnType.GenerateTypeSyntax())))
        End Function

        Private Function GenerateEnumDeclaration(namedType As INamedTypeSymbol, options As CodeGenerationOptions) As StatementSyntax
            Dim underlyingType =
                If(namedType.EnumUnderlyingType IsNot Nothing AndAlso namedType.EnumUnderlyingType.SpecialType <> SpecialType.System_Int32,
                   SyntaxFactory.SimpleAsClause(namedType.EnumUnderlyingType.GenerateTypeSyntax()),
                   Nothing)
            Return SyntaxFactory.EnumBlock(
                SyntaxFactory.EnumStatement(
                    GenerateAttributes(namedType, options),
                    GenerateModifiers(namedType),
                    namedType.Name.ToIdentifierToken,
                    underlyingType))
        End Function

        Private Function GenerateAttributes(namedType As INamedTypeSymbol, options As CodeGenerationOptions) As SyntaxList(Of AttributeListSyntax)
            Return AttributeGenerator.GenerateAttributeBlocks(namedType.GetAttributes(), options)
        End Function

        Private Function GenerateModifiers(namedType As INamedTypeSymbol) As SyntaxTokenList
            Dim tokens = New List(Of SyntaxToken)

            Select Case namedType.DeclaredAccessibility
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
                Case Accessibility.Internal
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword))
                Case Else
            End Select

            If namedType.TypeKind = TypeKind.Class Then
                If namedType.IsSealed Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword))
                End If

                If namedType.IsAbstract Then
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.MustInheritKeyword))
                End If
            End If

            Return SyntaxFactory.TokenList(tokens)
        End Function

        Private Function GenerateTypeParameterList(namedType As INamedTypeSymbol) As TypeParameterListSyntax
            Return TypeParameterGenerator.GenerateTypeParameterList(namedType.TypeParameters)
        End Function

        Private Function GenerateInheritsStatements(namedType As INamedTypeSymbol) As SyntaxList(Of InheritsStatementSyntax)
            If namedType.TypeKind = TypeKind.Struct OrElse
               namedType.BaseType Is Nothing OrElse
               namedType.BaseType.SpecialType = SpecialType.System_Object Then
                Return Nothing
            End If

            Return SyntaxFactory.SingletonList(
                SyntaxFactory.InheritsStatement(types:=SyntaxFactory.SingletonSeparatedList(namedType.BaseType.GenerateTypeSyntax())))
        End Function

        Private Function GenerateImplementsStatements(namedType As INamedTypeSymbol) As SyntaxList(Of ImplementsStatementSyntax)
            If namedType.Interfaces.Length = 0 Then
                Return Nothing
            End If

            Dim types = namedType.Interfaces.Select(Function(t) t.GenerateTypeSyntax())
            Dim typeNodes = SyntaxFactory.SeparatedList(types)
            Return SyntaxFactory.SingletonList(SyntaxFactory.ImplementsStatement(types:=typeNodes))
        End Function
    End Module
End Namespace
