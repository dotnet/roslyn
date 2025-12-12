' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InheritanceMargin
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InheritanceMarginService

    <ExportLanguageService(GetType(IInheritanceMarginService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicInheritanceMarginService
        Inherits AbstractInheritanceMarginService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property GlobalImportsTitle As String = VBFeaturesResources.Project_level_Imports

        Protected Overrides Function GetMembers(nodesToSearch As IEnumerable(Of SyntaxNode)) As ImmutableArray(Of SyntaxNode)
            Dim typeBlockNodes = nodesToSearch.OfType(Of TypeBlockSyntax)

            Dim builder As ArrayBuilder(Of SyntaxNode) = Nothing
            Using ArrayBuilder(Of SyntaxNode).GetInstance(builder)
                builder.AddRange(typeBlockNodes.Select(Function(node) node.BlockStatement))

                For Each node In typeBlockNodes
                    For Each member In node.Members
                        'Sub and Function
                        Dim methodBlockNode = TryCast(member, MethodBlockSyntax)
                        If methodBlockNode IsNot Nothing Then
                            builder.Add(methodBlockNode.BlockStatement)
                        End If

                        Dim methodStatementNode = TryCast(member, MethodStatementSyntax)
                        If methodStatementNode IsNot Nothing Then
                            builder.Add(methodStatementNode)
                        End If

                        'Property
                        Dim propertyBlockNode = TryCast(member, PropertyBlockSyntax)
                        If propertyBlockNode IsNot Nothing Then
                            builder.Add(propertyBlockNode.PropertyStatement)
                        End If

                        Dim propertyStatementNode = TryCast(member, PropertyStatementSyntax)
                        If propertyStatementNode IsNot Nothing Then
                            builder.Add(propertyStatementNode)
                        End If

                        'Custom event
                        Dim eventDeclarationNode = TryCast(member, EventBlockSyntax)
                        If eventDeclarationNode IsNot Nothing Then
                            builder.Add(eventDeclarationNode.EventStatement)
                        End If

                        'One line event
                        If TypeOf member Is EventStatementSyntax Then
                            builder.Add(member)
                        End If
                    Next
                Next

                Return builder.ToImmutable()
            End Using
        End Function

        Protected Overrides Function GetDeclarationToken(declarationNode As SyntaxNode) As SyntaxToken

            Dim typeStatementNode = TryCast(declarationNode, TypeStatementSyntax)
            If typeStatementNode IsNot Nothing Then
                Return typeStatementNode.Identifier
            End If

            Dim propertyStatementNode = TryCast(declarationNode, PropertyStatementSyntax)
            If propertyStatementNode IsNot Nothing Then
                Return propertyStatementNode.Identifier
            End If

            Dim eventBlockNode = TryCast(declarationNode, EventBlockSyntax)
            If eventBlockNode IsNot Nothing Then
                Return eventBlockNode.EventStatement.Identifier
            End If

            Dim eventStatementNode = TryCast(declarationNode, EventStatementSyntax)
            If eventStatementNode IsNot Nothing Then
                Return eventStatementNode.Identifier
            End If

            Dim methodStatementNode = TryCast(declarationNode, MethodStatementSyntax)
            If methodStatementNode IsNot Nothing Then
                Return methodStatementNode.Identifier
            End If

            Throw ExceptionUtilities.UnexpectedValue(declarationNode)
        End Function
    End Class
End Namespace

