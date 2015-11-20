' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class MethodDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of MethodStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New MethodDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSub()
            Const code = "
Class C
    {|span:Sub $$Foo()
    End Sub|} ' Foo
End Class
"

            Regions(code,
                Region("span", "Sub Foo() ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithGenericTypeParameter()
            Const code = "
Class C
    {|span:Sub $$Foo(Of T)()
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub Foo(Of T)() ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithGenericTypeParameterAndSingleConstraint()
            Const code = "
Class C
    {|span:Sub $$Foo(Of T As Class)()
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub Foo(Of T As Class)() ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithGenericTypeParameterAndMultipleConstraint()
            Const code = "
Class C
    {|span:Sub $$Foo(Of T As {Class, New})()
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub Foo(Of T As {Class, New})() ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPrivateSub()
            Const code = "
Class C
    {|span:Private Sub $$Foo()
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Private Sub Foo() ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithByRefParameter()
            Const code = "
Class C
    {|span:Sub $$Foo(ByRef i As Integer)
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub Foo(ByRef i As Integer) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithByValParameter()
            Const code = "
Class C
    {|span:Sub $$Foo(ByVal i As Integer)
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub Foo(i As Integer) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithOptionalParameter()
            Const code = "
Class C
    {|span:Sub $$Foo(Optional i As Integer = 1)
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub Foo(Optional i As Integer = 1) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithHandlesClause()
            Const code = "
Class C
    {|span:Sub $$Foo() Handles Bar.Baz
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub Foo() Handles Bar.Baz ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithImplementsClause()
            Const code = "
Class C
    {|span:Sub $$Foo() Implements Bar.Baz
    End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub Foo() Implements Bar.Baz ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithComments()
            Const code = "
Class C
    {|span1:'My
    'Constructor|}
    {|span2:Sub $$Foo() Implements Bar.Baz
    End Sub|}
End Class
"

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Sub Foo() Implements Bar.Baz ...", autoCollapse:=True))
        End Sub

    End Class
End Namespace
