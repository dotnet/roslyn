' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class TypeDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of TypeStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New TypeDeclarationStructureProvider()
        End Function

        <Fact>
        Public Async Function TestClass() As Task
            Const code = "
{|span:Class $$C1
End Class|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Class C1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestFriendClass() As Task
            Const code = "
{|span:Friend Class $$C1
End Class|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Friend Class C1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestClassWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World|}
{|span2:Class $$C1
End Class|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Class C1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestClassWithNestedComments() As Task
            Const code = "
{|span1:Class $$C1
{|span2:'Hello
'World|}
End Class|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "Class C1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestModule() As Task
            Const code = "
{|span:Module $$M1
End Module|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Module M1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestModuleWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World|}
{|span2:Module $$M1
End Module|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Module M1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestModuleWithNestedComments() As Task
            Const code = "
{|span1:Module $$M1
{|span2:'Hello
'World|}
End Module|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "Module M1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestInterface() As Task
            Const code = "
{|span:Interface $$I1
End Interface|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Interface I1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestInterfaceWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World|}
{|span2:Interface $$I1
End Interface|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Interface I1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestInterfaceWithNestedComments() As Task
            Const code = "
{|span1:Interface $$I1
{|span2:'Hello
'World|}
End Interface|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "Interface I1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestStructure() As Task
            Const code = "
{|span:Structure $$S1
End Structure|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Structure S1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestStructureWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World|}
{|span2:Structure $$S1
End Structure|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Structure S1 ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestStructureWithNestedComments() As Task
            Const code = "
{|span1:Structure $$S1
{|span2:'Hello
'World|}
End Structure|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "Structure S1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
