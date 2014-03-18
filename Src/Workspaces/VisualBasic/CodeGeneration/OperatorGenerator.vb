' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class OperatorGenerator
        Inherits AbstractVisualBasicCodeGenerator

        Friend Shared Function AddOperatorTo(destination As TypeBlockSyntax,
                            method As IMethodSymbol,
                            options As CodeGenerationOptions,
                            availableIndices As IList(Of Boolean)) As TypeBlockSyntax
            Dim methodDeclaration = GenerateOperatorDeclaration(method, GetDestination(destination), options)

            Dim members = Insert(destination.Members, methodDeclaration, options, availableIndices,
                                 after:=AddressOf LastOperator)

            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Shared Function GenerateOperatorDeclaration(method As IMethodSymbol,
                                                         destination As CodeGenerationDestination,
                                                         options As CodeGenerationOptions) As StatementSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of StatementSyntax)(method, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim declaration = GenerateOperatorDeclarationWorker(method, destination, options)

            Return AddAnnotationsTo(method,
                AddCleanupAnnotationsTo(
                    ConditionallyAddDocumentationCommentTo(declaration, method, options)))
        End Function

        Private Shared Function GenerateOperatorDeclarationWorker(method As IMethodSymbol,
                                                                destination As CodeGenerationDestination,
                                                                options As CodeGenerationOptions) As StatementSyntax
            Dim operatorSyntaxKind = SyntaxFacts.GetOperatorKind(method.MetadataName)
            If operatorSyntaxKind = SyntaxKind.None Then
                Throw New ArgumentException(String.Format(WorkspacesResources.CannotCodeGenUnsupportedOperator, method.Name), "method")
            End If

            Dim begin = SyntaxFactory.OperatorStatement(
                AttributeGenerator.GenerateAttributeBlocks(method.GetAttributes(), options),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.SharedKeyword)),
                SyntaxFactory.Token(operatorSyntaxKind),
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
                end:=SyntaxFactory.EndOperatorStatement())
        End Function
    End Class
End Namespace