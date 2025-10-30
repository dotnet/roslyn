' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module OperatorGenerator

        Friend Function AddOperatorTo(destination As TypeBlockSyntax,
                            method As IMethodSymbol,
                            options As CodeGenerationContextInfo,
                            availableIndices As IList(Of Boolean)) As TypeBlockSyntax
            Dim methodDeclaration = GenerateOperatorDeclaration(method, options)

            Dim members = Insert(destination.Members, methodDeclaration, options, availableIndices,
                                 after:=AddressOf LastOperator)

            Return FixTerminators(destination.WithMembers(members))
        End Function

        Public Function GenerateOperatorDeclaration(method As IMethodSymbol,
                                                    options As CodeGenerationContextInfo) As StatementSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of StatementSyntax)(method, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim declaration = GenerateOperatorDeclarationWorker(method, options)

            Return AddAnnotationsTo(method,
                AddFormatterAndCodeGeneratorAnnotationsTo(
                    ConditionallyAddDocumentationCommentTo(declaration, method, options)))
        End Function

        Private Function GenerateOperatorDeclarationWorker(method As IMethodSymbol,
                                                           options As CodeGenerationContextInfo) As StatementSyntax
            Dim operatorSyntaxKind = SyntaxFacts.GetOperatorKind(method.MetadataName)
            If operatorSyntaxKind = SyntaxKind.None Then
                Throw New ArgumentException(String.Format(WorkspaceExtensionsResources.Cannot_generate_code_for_unsupported_operator_0, method.Name), NameOf(method))
            End If

            Dim begin = SyntaxFactory.OperatorStatement(
                AttributeGenerator.GenerateAttributeBlocks(method.GetAttributes(), options),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.SharedKeyword)),
                SyntaxFactory.Token(operatorSyntaxKind),
                ParameterGenerator.GenerateParameterList(method.Parameters, options),
                SyntaxFactory.SimpleAsClause(method.ReturnType.GenerateTypeSyntax()))

            Dim hasNoBody = Not options.Context.GenerateMethodBodies OrElse
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
