' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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
        Public Async Function TestClass() As Task
            Const code = "
{|span:Class $$C1
End Class|}
"

            Await VerifyRegionsAsync(code,
                Region("span", "Class C1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestFriendClass() As Task
            Const code = "
{|span:Friend Class $$C1
End Class|}
"

            Await VerifyRegionsAsync(code,
                Region("span", "Friend Class C1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestClassWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World|}
{|span2:Class $$C1
End Class|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Class C1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestClassWithNestedComments() As Task
            Const code = "
{|span1:Class $$C1
{|span2:'Hello
'World|}
End Class|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "Class C1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestModule() As Task
            Const code = "
{|span:Module $$M1
End Module|}
"

            Await VerifyRegionsAsync(code,
                Region("span", "Module M1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestModuleWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World|}
{|span2:Module $$M1
End Module|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Module M1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestModuleWithNestedComments() As Task
            Const code = "
{|span1:Module $$M1
{|span2:'Hello
'World|}
End Module|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "Module M1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestInterface() As Task
            Const code = "
{|span:Interface $$I1
End Interface|}
"

            Await VerifyRegionsAsync(code,
                Region("span", "Interface I1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestInterfaceWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World|}
{|span2:Interface $$I1
End Interface|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Interface I1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestInterfaceWithNestedComments() As Task
            Const code = "
{|span1:Interface $$I1
{|span2:'Hello
'World|}
End Interface|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "Interface I1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestStructure() As Task
            Const code = "
{|span:Structure $$S1
End Structure|}
"

            Await VerifyRegionsAsync(code,
                Region("span", "Structure S1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestStructureWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World|}
{|span2:Structure $$S1
End Structure|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Structure S1 ...", autoCollapse:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestStructureWithNestedComments() As Task
            Const code = "
{|span1:Structure $$S1
{|span2:'Hello
'World|}
End Structure|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "Structure S1 ...", autoCollapse:=False),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
