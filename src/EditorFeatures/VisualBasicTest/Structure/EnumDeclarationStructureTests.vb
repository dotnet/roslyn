' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class EnumDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of EnumStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New EnumDeclarationStructureProvider()
        End Function

        <Fact>
        Public Async Function TestEnum() As Task
            Const code = "
{|span:Enum $$E1
End Enum|} ' Goo
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Enum E1 ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestEnumWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World!|}
{|span2:Enum $$E1
End Enum|} ' Goo
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Enum E1 ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestEnumWithNestedComments() As Task
            Const code = "
{|span1:Enum $$E1
{|span2:'Hello
'World!|}
End Enum|} ' Goo
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "Enum E1 ...", autoCollapse:=True),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
