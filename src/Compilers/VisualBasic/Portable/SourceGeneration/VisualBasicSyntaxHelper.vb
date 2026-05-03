' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
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

        Public Overrides Function RemapAttributeTarget(target As SyntaxNode) As SyntaxNode
            If TypeOf target Is ModifiedIdentifierSyntax AndAlso
               TypeOf target.Parent Is VariableDeclaratorSyntax AndAlso
               TypeOf target.Parent.Parent Is FieldDeclarationSyntax Then
                Return target.Parent.Parent
            End If

            Return target
        End Function

        Public Overrides Function GetAttributeOwningNode(attribute As SyntaxNode) As SyntaxNode
            Debug.Assert(TypeOf attribute Is AttributeSyntax)
            Debug.Assert(TypeOf attribute.Parent Is AttributeListSyntax)
            Debug.Assert(attribute.Parent.Parent IsNot Nothing)
            Dim owningNode = attribute.Parent.Parent

            ' for attribute statements (like `<Assembly: ...>`) we want to get the parent compilation unit as that's
            ' what symbol will actually own the attribute.
            If TypeOf owningNode Is AttributesStatementSyntax Then
                Return owningNode.Parent
            End If

            Return owningNode
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

        Public Overrides Function GetUnqualifiedIdentifierOfName(node As SyntaxNode) As String
            Return GetUnqualifiedIdentifierOfName(DirectCast(node.Green, InternalSyntax.NameSyntax))
        End Function

        Private Overloads Shared Function GetUnqualifiedIdentifierOfName(name As InternalSyntax.NameSyntax) As String
            Dim qualifiedName = TryCast(name, InternalSyntax.QualifiedNameSyntax)
            If qualifiedName IsNot Nothing Then
                Return qualifiedName.Right.Identifier.ValueText
            End If

            Dim simpleName = TryCast(name, InternalSyntax.SimpleNameSyntax)
            If simpleName IsNot Nothing Then
                Return simpleName.Identifier.ValueText
            End If

            Throw ExceptionUtilities.UnexpectedValue(name.KindText)
        End Function

        Public Overrides Sub AddAliases(node As GreenNode, aliases As ArrayBuilder(Of (aliasName As String, symbolName As String)), [global] As Boolean)
            ' VB does not have global aliases at the syntax level.
            If [global] Then
                Return
            End If

            Dim compilationUnit = TryCast(node, InternalSyntax.CompilationUnitSyntax)
            If compilationUnit Is Nothing Then
                Return
            End If

            For Each importsStatement In compilationUnit.Imports
                For i = 0 To importsStatement.ImportsClauses.Count - 1
                    ProcessImportsClause(aliases, importsStatement.ImportsClauses(i))
                Next
            Next
        End Sub

        Public Overrides Sub AddAliases(options As CompilationOptions, aliases As ArrayBuilder(Of (aliasName As String, symbolName As String)))
            Dim vbOptions = DirectCast(options, VisualBasicCompilationOptions)

            For Each globalImport In vbOptions.GlobalImports
                Dim clause = globalImport.Clause
                ProcessImportsClause(aliases, DirectCast(clause.Green, InternalSyntax.ImportsClauseSyntax))
            Next
        End Sub

        Private Shared Sub ProcessImportsClause(aliases As ArrayBuilder(Of (aliasName As String, symbolName As String)), clause As InternalSyntax.ImportsClauseSyntax)
            Dim importsClause = TryCast(clause, InternalSyntax.SimpleImportsClauseSyntax)
            If importsClause?.Alias IsNot Nothing Then
                aliases.Add((importsClause.Alias.Identifier.ValueText, GetUnqualifiedIdentifierOfName(importsClause.Name)))
            End If
        End Sub

        Public Overrides Function ContainsGlobalAliases(root As SyntaxNode) As Boolean
            ' VB does not have global aliases
            Return False
        End Function
    End Class
End Namespace
