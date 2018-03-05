' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class CompilationUnitStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of CompilationUnitSyntax)

        Protected Overrides Sub CollectBlockSpans(compilationUnit As CompilationUnitSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(compilationUnit, spans)

            If Not compilationUnit.Imports.IsEmpty Then
                Dim startPos = compilationUnit.Imports.First().SpanStart
                Dim endPos = compilationUnit.Imports.Last().Span.End

                Dim span = TextSpan.FromBounds(startPos, endPos)
                spans.AddIfNotNull(CreateBlockSpan(
                    span, span, bannerText:="Imports" & SpaceEllipsis,
                    autoCollapse:=True, type:=BlockTypes.Imports, isCollapsible:=True,
                    isDefaultCollapsed:=False))
            End If

            CollectCommentsRegions(compilationUnit.EndOfFileToken.LeadingTrivia, spans)
        End Sub

        Protected Overrides Function SupportedInWorkspaceKind(kind As String) As Boolean
            Return True
        End Function
    End Class
End Namespace
