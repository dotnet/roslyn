' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class EnumDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of EnumStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New EnumDeclarationOutliner()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestEnum() As Task
            Const code = "
{|span:Enum $$E1
End Enum|} ' Foo
"

            Await VerifyRegionsAsync(code,
                Region("span", "Enum E1 ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestEnumWithLeadingComments() As Task
            Const code = "
{|span1:'Hello
'World!|}
{|span2:Enum $$E1
End Enum|} ' Foo
"

            Await VerifyRegionsAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Enum E1 ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestEnumWithNestedComments() As Task
            Const code = "
{|span1:Enum $$E1
{|span2:'Hello
'World!|}
End Enum|} ' Foo
"

            Await VerifyRegionsAsync(code,
                Region("span1", "Enum E1 ...", autoCollapse:=True),
                Region("span2", "' Hello ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
