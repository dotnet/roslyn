' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class TypeDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of TypeStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New TypeDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestClass()
            Const code = "
{|span:Class $$C1
End Class|}
"

            Regions(code,
                Region("span", "Class C1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestFriendClass()
            Const code = "
{|span:Friend Class $$C1
End Class|}
"

            Regions(code,
                Region("span", "Friend Class C1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestClassWithLeadingComments()
            Const code = "
{|span1:'Hello
'World|}
{|span2:Class $$C1
End Class|}
"

            Regions(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Class C1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestClassWithNestedComments()
            Const code = "
{|span1:Class $$C1
{|span2:'Hello
'World|}
End Class|}
"

            Regions(code,
                Region("span1", "Class C1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestModule()
            Const code = "
{|span:Module $$M1
End Module|}
"

            Regions(code,
                Region("span", "Module M1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestModuleWithLeadingComments()
            Const code = "
{|span1:'Hello
'World|}
{|span2:Module $$M1
End Module|}
"

            Regions(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Module M1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestModuleWithNestedComments()
            Const code = "
{|span1:Module $$M1
{|span2:'Hello
'World|}
End Module|}
"

            Regions(code,
                Region("span1", "Module M1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInterface()
            Const code = "
{|span:Interface $$I1
End Interface|}
"

            Regions(code,
                Region("span", "Interface I1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInterfaceWithLeadingComments()
            Const code = "
{|span1:'Hello
'World|}
{|span2:Interface $$I1
End Interface|}
"

            Regions(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Interface I1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInterfaceWithNestedComments()
            Const code = "
{|span1:Interface $$I1
{|span2:'Hello
'World|}
End Interface|}
"

            Regions(code,
                Region("span1", "Interface I1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestStructure()
            Const code = "
{|span:Structure $$S1
End Structure|}
"

            Regions(code,
                Region("span", "Structure S1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestStructureWithLeadingComments()
            Const code = "
{|span1:'Hello
'World|}
{|span2:Structure $$S1
End Structure|}
"

            Regions(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Structure S1 ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestStructureWithNestedComments()
            Const code = "
{|span1:Structure $$S1
{|span2:'Hello
'World|}
End Structure|}
"

            Regions(code,
                Region("span1", "Structure S1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Sub

    End Class
End Namespace
