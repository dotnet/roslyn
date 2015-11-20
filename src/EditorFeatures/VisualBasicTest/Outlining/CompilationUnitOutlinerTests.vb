' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining

    Public Class CompilationUnitOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of CompilationUnitSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New CompilationUnitOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestImports()
            Const code = "
{|span:$$Imports System
Imports System.Linq|}
Class C1
End Class
"
            Regions(code,
                Region("span", "Imports ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestImportsAliases()
            Const code = "
{|span:$$Imports System
Imports linq = System.Linq|}
Class C1
End Class
"
            Regions(code,
                Region("span", "Imports ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestComments()
            Const code = "
{|span1:$$'Top
'Of
'File|}
Class C
End Class
{|span2:'Bottom
'Of
'File|}
"
            Regions(code,
                Region("span1", "' Top ...", autoCollapse:=True),
                Region("span2", "' Bottom ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestImportsAndComments()
            Const code = "
{|span1:$$'Top
'Of
'File|}
{|span2:Imports System
Imports System.Linq|}
{|span3:'Bottom
'Of
'File|}
"
            Regions(code,
                Region("span1", "' Top ...", autoCollapse:=True),
                Region("span2", "Imports ...", autoCollapse:=True),
                Region("span3", "' Bottom ...", autoCollapse:=True))
        End Sub

    End Class
End Namespace
