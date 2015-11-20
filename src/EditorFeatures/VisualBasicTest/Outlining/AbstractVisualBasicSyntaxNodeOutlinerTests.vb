' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public MustInherit Class AbstractVisualBasicSyntaxOutlinerTests(Of T As SyntaxNode)
        Inherits AbstractSyntaxOutlinerTests

        Protected Overrides Function ParseCompilationUnit(code As String) As SyntaxNode
            Return SyntaxFactory.ParseCompilationUnit(code)
        End Function

        Friend Overridable Function GetRegions(node As T) As IEnumerable(Of OutliningSpan)
            Return New List(Of OutliningSpan)
        End Function

        Friend Function GetRegion(node As T) As OutliningSpan
            Dim regions = GetRegions(node).ToList()
            Assert.Equal(1, regions.Count())

            Return regions(0)
        End Function
    End Class
End Namespace
