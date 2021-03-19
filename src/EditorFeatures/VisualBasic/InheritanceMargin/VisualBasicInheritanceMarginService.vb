' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InheritanceMargin
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.InheritanceMarginService

    <ExportLanguageService(GetType(IInheritanceMarginService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicInheritanceMarginService
        Inherits AbstractInheritanceMarginService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetMembers(root As SyntaxNode, spanToSearch As TextSpan) As ImmutableArray(Of SyntaxNode)
            Dim typeBlockNodes = root.DescendantNodes(spanToSearch).OfType(Of TypeBlockSyntax).ToImmutableArray()

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

        Protected Overrides Function GetIdentifierLineNumber(sourceText As SourceText, declarationNode As SyntaxNode) As Integer
            Dim lines = sourceText.Lines

            Dim typeStatementNode = TryCast(declarationNode, TypeStatementSyntax)
            If typeStatementNode IsNot Nothing Then
                Dim position = typeStatementNode.Identifier.SpanStart
                Return lines.GetLineFromPosition(position).LineNumber
            End If

            Dim propertyStatementNode = TryCast(declarationNode, PropertyStatementSyntax)
            If propertyStatementNode IsNot Nothing Then
                Dim position = propertyStatementNode.Identifier.SpanStart
                Return lines.GetLineFromPosition(position).LineNumber
            End If

            Dim eventBlockNode = TryCast(declarationNode, EventBlockSyntax)
            If eventBlockNode IsNot Nothing Then
                Dim position = eventBlockNode.EventStatement.Identifier.SpanStart
                Return lines.GetLineFromPosition(position).LineNumber
            End If

            Dim eventStatementNode = TryCast(declarationNode, EventStatementSyntax)
            If eventStatementNode IsNot Nothing Then
                Dim position = eventStatementNode.Identifier.SpanStart
                Return lines.GetLineFromPosition(position).LineNumber
            End If

            Dim methodStatementNode = TryCast(declarationNode, MethodStatementSyntax)
            If methodStatementNode IsNot Nothing Then
                Dim position = methodStatementNode.Identifier.SpanStart
                Return lines.GetLineFromPosition(position).LineNumber
            End If

            Throw ExceptionUtilities.UnexpectedValue(declarationNode)
        End Function
    End Class
End Namespace

