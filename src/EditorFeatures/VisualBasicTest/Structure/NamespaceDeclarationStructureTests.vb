' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class NamespaceDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of NamespaceStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New NamespaceDeclarationStructureProvider()
        End Function

        <Fact>
        Public Async Function TestNamespace() As Task
            Const code = "
{|span:$$Namespace N1
End Namespace|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Namespace N1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestNamespaceWithComments() As Task
            Const code = "
{|span1:'My
'Namespace|}
{|span2:$$Namespace N1
End Namespace|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Namespace N1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestNamespaceWithNestedComments() As Task
            Const code = "
{|span1:$$Namespace N1
{|span2:'My
'Namespace|}
End Namespace|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "Namespace N1 ...", autoCollapse:=False),
                Region("span2", "' My ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
