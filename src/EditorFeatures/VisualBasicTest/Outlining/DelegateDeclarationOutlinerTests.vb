' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DelegateDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of DelegateStatementSyntax)

        Friend Overrides Function GetRegions(delegateDeclaration As DelegateStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New DelegateDeclarationOutliner
            Return outliner.GetOutliningSpans(delegateDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDelegateWithComments()
            Dim tree = ParseLines("'Hello",
                                  "'World",
                                  "Delegate Sub Foo()")

            Dim delegateDecl = tree.DigToFirstNodeOfType(Of DelegateStatementSyntax)()
            Assert.NotNull(delegateDecl)

            Dim actualRegion = GetRegion(delegateDecl)
            Dim expectedRegion = New OutliningSpan(
                         TextSpan.FromBounds(0, 14),
                         "' Hello ...",
                         autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
