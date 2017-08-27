' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class ConstructorDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of SubNewStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(constructorDeclaration As SubNewStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            Dim regions As New List(Of BlockSpan)

            CollectCommentsRegions(constructorDeclaration, spans)

            Dim block = TryCast(constructorDeclaration.Parent, ConstructorBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=constructorDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Member, isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
