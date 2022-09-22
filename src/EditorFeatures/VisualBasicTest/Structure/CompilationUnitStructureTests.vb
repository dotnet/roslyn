' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class CompilationUnitStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of CompilationUnitSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New CompilationUnitStructureProvider()
        End Function

        <Fact>
        Public Async Function TestImports() As Task
            Const code = "
{|span:$$Imports System
Imports System.Linq|}
Class C1
End Class
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "Imports ...", autoCollapse:=True))
        End Function

        <Theory, CombinatorialData>
        Public Async Function ImportsShouldBeCollapsedByDefault(collapseUsingsByDefault As Boolean) As Task
            Const code = "
{|span:$$Imports System
Imports System.Linq|}
Class C1
End Class
"

            Dim options = New BlockStructureOptions() With {.CollapseImportsWhenFirstOpened = collapseUsingsByDefault}

            Await VerifyBlockSpansAsync(code, options,
                Region("span", "Imports ...", autoCollapse:=True, isDefaultCollapsed:=collapseUsingsByDefault))
        End Function

        <Fact>
        Public Async Function TestImportsAliases() As Task
            Const code = "
{|span:$$Imports System
Imports linq = System.Linq|}
Class C1
End Class
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "Imports ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestComments() As Task
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
            Await VerifyBlockSpansAsync(code,
                Region("span1", "' Top ...", autoCollapse:=True),
                Region("span2", "' Bottom ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestImportsAndComments() As Task
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
            Await VerifyBlockSpansAsync(code,
                Region("span1", "' Top ...", autoCollapse:=True),
                Region("span2", "Imports ...", autoCollapse:=True),
                Region("span3", "' Bottom ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
