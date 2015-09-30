' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration

    Friend Module NamespaceGenerator

        Public Function AddNamespaceTo(service As ICodeGenerationService,
                                    destination As CompilationUnitSyntax,
                                    [namespace] As INamespaceSymbol,
                                    options As CodeGenerationOptions,
                                    availableIndices As IList(Of Boolean),
                                    cancellationToken As CancellationToken) As CompilationUnitSyntax
            Dim declaration = GenerateNamespaceDeclaration(service, [namespace], options, cancellationToken)
            If Not TypeOf declaration Is NamespaceBlockSyntax Then
                Throw New ArgumentException(VBWorkspaceResources.NamespaceCannotBeAdded)
            End If

            Dim members = Insert(destination.Members, DirectCast(declaration, StatementSyntax), options, availableIndices)
            Return destination.WithMembers(members)
        End Function

        Public Function AddNamespaceTo(service As ICodeGenerationService,
                                    destination As NamespaceBlockSyntax,
                                    [namespace] As INamespaceSymbol,
                                    options As CodeGenerationOptions,
                                    availableIndices As IList(Of Boolean),
                                    cancellationToken As CancellationToken) As NamespaceBlockSyntax
            Dim declaration = GenerateNamespaceDeclaration(service, [namespace], options, cancellationToken)
            If Not TypeOf declaration Is NamespaceBlockSyntax Then
                Throw New ArgumentException(VBWorkspaceResources.NamespaceCannotBeAdded)
            End If

            Dim members = Insert(destination.Members, DirectCast(declaration, StatementSyntax), options, availableIndices)
            Return destination.WithMembers(members)
        End Function

        Public Function GenerateNamespaceDeclaration(service As ICodeGenerationService, [namespace] As INamespaceSymbol, options As CodeGenerationOptions, cancellationToken As CancellationToken) As SyntaxNode
            Dim name As String = Nothing
            Dim innermostNamespace As INamespaceSymbol = Nothing
            options = If(options, CodeGenerationOptions.Default)
            GetNameAndInnermostNamespace([namespace], options, name, innermostNamespace)

            Dim declaration = GetDeclarationSyntaxWithoutMembers([namespace], innermostNamespace, name, options)

            declaration = If(options.GenerateMembers,
                service.AddMembers(declaration, innermostNamespace.GetMembers().AsEnumerable(), options, cancellationToken),
                declaration)

            Return AddCleanupAnnotationsTo(declaration)
        End Function

        Public Function UpdateCompilationUnitOrNamespaceDeclaration(service As ICodeGenerationService,
                                                                           declaration As SyntaxNode,
                                                                           newMembers As IList(Of ISymbol),
                                                                           options As CodeGenerationOptions,
                                                                           cancellationToken As CancellationToken) As SyntaxNode
            declaration = RemoveAllMembers(declaration)
            declaration = service.AddMembers(declaration, newMembers, options, cancellationToken)
            Return AddCleanupAnnotationsTo(declaration)
        End Function

        Private Function GetDeclarationSyntaxWithoutMembers([namespace] As INamespaceSymbol, innermostNamespace As INamespaceSymbol, name As String, options As CodeGenerationOptions) As SyntaxNode
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of SyntaxNode)([namespace], options)
            If reusableSyntax Is Nothing Then
                Return GenerateNamespaceDeclarationWorker(name, innermostNamespace)
            End If

            Return RemoveAllMembers(reusableSyntax)
        End Function

        Private Function RemoveAllMembers(declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.CompilationUnit
                    Return DirectCast(declaration, CompilationUnitSyntax).WithMembers(Nothing)
                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(declaration, NamespaceBlockSyntax).WithMembers(Nothing)
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function GenerateNamespaceDeclarationWorker(name As String, [namespace] As INamespaceSymbol) As SyntaxNode
            If name = String.Empty Then
                Return SyntaxFactory.CompilationUnit().WithImports(GenerateImportsStatements([namespace]))
            Else
                Return SyntaxFactory.NamespaceBlock(
                    SyntaxFactory.NamespaceStatement(SyntaxFactory.ParseName(name)))
            End If
        End Function

        Private Function GenerateImportsStatements([namespace] As INamespaceSymbol) As SyntaxList(Of ImportsStatementSyntax)
            Dim statements =
                CodeGenerationNamespaceInfo.GetImports([namespace]).
                                            Select(AddressOf GenerateImportsStatement).
                                            WhereNotNull().ToList()

            Return If(statements.Count = 0, Nothing, SyntaxFactory.List(statements))
        End Function

        Private Function GenerateImportsStatement(import As ISymbol) As ImportsStatementSyntax
            If TypeOf import Is IAliasSymbol Then
                Dim [alias] = DirectCast(import, IAliasSymbol)
                Dim name = GenerateName([alias].Target)
                If name IsNot Nothing Then
                    Return SyntaxFactory.ImportsStatement(
                        SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(
                            SyntaxFactory.SimpleImportsClause(SyntaxFactory.ImportAliasClause([alias].Name.ToIdentifierToken), name)))
                End If
            ElseIf TypeOf import Is INamespaceOrTypeSymbol Then
                Dim name = GenerateName(DirectCast(import, INamespaceOrTypeSymbol))
                If name IsNot Nothing Then
                    Return SyntaxFactory.ImportsStatement(
                        SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(
                            SyntaxFactory.SimpleImportsClause(name)))
                End If
            End If

            Return Nothing
        End Function

        Private Function GenerateName(symbol As INamespaceOrTypeSymbol) As NameSyntax
            If TypeOf symbol Is ITypeSymbol Then
                Return TryCast(DirectCast(symbol, ITypeSymbol).GenerateTypeSyntax(), NameSyntax)
            Else
                Return SyntaxFactory.ParseName(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            End If
        End Function
    End Module
End Namespace
