' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class VisualBasicSyntaxHelper
        Inherits AbstractSyntaxHelper

        Public Shared ReadOnly Instance As ISyntaxHelper = New VisualBasicSyntaxHelper()

        Private Sub New()
        End Sub

        Public Overrides ReadOnly Property IsCaseSensitive As Boolean = False

        Public Overrides Function IsValidIdentifier(name As String) As Boolean
            Return SyntaxFacts.IsValidIdentifier(name)
        End Function

        Public Overrides Function IsAnyNamespaceBlock(node As SyntaxNode) As Boolean
            Return TypeOf node Is NamespaceBlockSyntax
        End Function

        Public Overrides Function IsAttribute(node As SyntaxNode) As Boolean
            Return TypeOf node Is AttributeSyntax
        End Function

        Public Overrides Function GetNameOfAttribute(node As SyntaxNode) As SyntaxNode
            Return DirectCast(node, AttributeSyntax).Name
        End Function

        Public Overrides Function IsAttributeList(node As SyntaxNode) As Boolean
            Return TypeOf node Is AttributeListSyntax
        End Function

        Public Overrides Sub AddAttributeTargets(node As SyntaxNode, targets As ArrayBuilder(Of SyntaxNode))
            Dim attributeList = DirectCast(node, AttributeListSyntax)

            Dim container = attributeList.Parent
            If TypeOf container Is AttributesStatementSyntax Then
                ' for attribute statements (like `<Assembly: ...>`) we want to get the parent compilation unit as that's
                ' what symbol will actually own the attribute.
                targets.Add(container.Parent)
            ElseIf TypeOf container Is FieldDeclarationSyntax Then
                Dim field = DirectCast(container, FieldDeclarationSyntax)
                For Each varDecl In field.Declarators
                    For Each id In varDecl.Names
                        targets.Add(id)
                    Next
                Next
            Else
                targets.Add(container)
            End If
        End Sub

        Public Overrides Function GetAttributesOfAttributeList(node As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode)
            Return DirectCast(node, AttributeListSyntax).Attributes
        End Function

        Public Overrides Function IsLambdaExpression(node As SyntaxNode) As Boolean
            Return TypeOf node Is LambdaExpressionSyntax
        End Function

        Public Overrides Function GetUnqualifiedIdentifierOfName(node As SyntaxNode) As SyntaxToken
            Dim name = DirectCast(node, NameSyntax)

            Dim qualifiedName = TryCast(name, QualifiedNameSyntax)
            If qualifiedName IsNot Nothing Then
                Return qualifiedName.Right.Identifier
            End If

            Dim simpleName = TryCast(node, SimpleNameSyntax)
            If simpleName IsNot Nothing Then
                Return simpleName.Identifier
            End If

            Throw ExceptionUtilities.UnexpectedValue(node.Kind())
        End Function

        Public Overrides Sub AddAliases(node As SyntaxNode, aliases As ArrayBuilder(Of (aliasName As String, symbolName As String)), [global] As Boolean)
            ' VB does not have global aliases at the syntax level.
            If [global] Then
                Return
            End If

            Dim compilationUnit = TryCast(node, CompilationUnitSyntax)
            If compilationUnit Is Nothing Then
                Return
            End If

            For Each importsStatement In compilationUnit.Imports
                For Each importsClause In importsStatement.ImportsClauses
                    ProcessImportsClause(aliases, importsClause)
                Next
            Next
        End Sub

        Public Overrides Sub AddAliases(options As CompilationOptions, aliases As ArrayBuilder(Of (aliasName As String, symbolName As String)))
            Dim vbOptions = DirectCast(options, VisualBasicCompilationOptions)

            For Each globalImport In vbOptions.GlobalImports
                Dim clause = globalImport.Clause
                ProcessImportsClause(aliases, clause)
            Next
        End Sub

        Private Sub ProcessImportsClause(aliases As ArrayBuilder(Of (aliasName As String, symbolName As String)), clause As ImportsClauseSyntax)
            Dim importsClause = TryCast(clause, SimpleImportsClauseSyntax)
            If importsClause?.Alias IsNot Nothing Then
                aliases.Add((importsClause.Alias.Identifier.ValueText, GetUnqualifiedIdentifierOfName(importsClause.Name).ValueText))
            End If
        End Sub
    End Class
End Namespace
