﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module ConversionGenerator

        Friend Function AddConversionTo(destination As TypeBlockSyntax,
                            method As IMethodSymbol,
                            options As CodeGenerationOptions,
                            availableIndices As IList(Of Boolean)) As TypeBlockSyntax
            Dim methodDeclaration = GenerateConversionDeclaration(method, GetDestination(destination), options)

            Dim members = Insert(destination.Members, methodDeclaration, options, availableIndices,
                                 after:=AddressOf LastOperator)

            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Function GenerateConversionDeclaration(method As IMethodSymbol,
                                                         destination As CodeGenerationDestination,
                                                         options As CodeGenerationOptions) As StatementSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of StatementSyntax)(method, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim declaration = GenerateConversionDeclarationWorker(method, destination, options)

            Return AddAnnotationsTo(method,
                AddFormatterAndCodeGeneratorAnnotationsTo(
                    ConditionallyAddDocumentationCommentTo(declaration, method, options)))
        End Function

        Private Function GenerateConversionDeclarationWorker(method As IMethodSymbol,
                                                                destination As CodeGenerationDestination,
                                                                options As CodeGenerationOptions) As StatementSyntax
            Dim modifiers = New List(Of SyntaxToken) From {
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.SharedKeyword)
            }

            modifiers.Add(SyntaxFactory.Token(
                If(method.MetadataName = WellKnownMemberNames.ImplicitConversionName,
                    SyntaxKind.WideningKeyword,
                    SyntaxKind.NarrowingKeyword)))

            Dim begin = SyntaxFactory.OperatorStatement(
                AttributeGenerator.GenerateAttributeBlocks(method.GetAttributes(), options),
                SyntaxFactory.TokenList(modifiers),
                SyntaxFactory.Token(SyntaxKind.CTypeKeyword),
                ParameterGenerator.GenerateParameterList(method.Parameters, options),
                SyntaxFactory.SimpleAsClause(method.ReturnType.GenerateTypeSyntax()))

            Dim hasNoBody = Not options.GenerateMethodBodies OrElse
                            method.IsExtern

            If hasNoBody Then
                Return begin
            End If

            Return SyntaxFactory.OperatorBlock(
                begin,
                statements:=StatementGenerator.GenerateStatements(method),
                endOperatorStatement:=SyntaxFactory.EndOperatorStatement())
        End Function
    End Module
End Namespace
