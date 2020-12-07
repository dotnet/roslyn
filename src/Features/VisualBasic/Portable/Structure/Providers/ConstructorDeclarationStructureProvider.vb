' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class ConstructorDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of SubNewStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(constructorDeclaration As SubNewStatementSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  optionProvider As BlockStructureOptionProvider,
                                                  cancellationToken As CancellationToken)
            Dim regions As New List(Of BlockSpan)

            CollectCommentsRegions(constructorDeclaration, spans, optionProvider)

            Dim block = TryCast(constructorDeclaration.Parent, ConstructorBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=constructorDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Member, isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
