' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class NamespaceDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of NamespaceStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New NamespaceDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestNamespace()
            Const code = "
{|span:$$Namespace N1
End Namespace|}
"

            Regions(code,
                Region("span", "Namespace N1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestNamespaceWithComments()
            Const code = "
{|span1:'My
'Namespace|}
{|span2:$$Namespace N1
End Namespace|}
"

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Namespace N1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestNamespaceWithNestedComments()
            Const code = "
{|span1:$$Namespace N1
{|span2:'My
'Namespace|}
End Namespace|}
"

            Regions(code,
                Region("span1", "Namespace N1 ...", autoCollapse:=False),
                Region("span2", "' My ...", autoCollapse:=True))
        End Sub

    End Class
End Namespace
