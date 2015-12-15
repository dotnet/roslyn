' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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
        Public Async Function TestSub() As Task
            Const code = "
Class C
    {|span:Sub $$Foo()
    End Sub|} ' Foo
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo() ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithGenericTypeParameter() As Task
            Const code = "
Class C
    {|span:Sub $$Foo(Of T)()
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo(Of T)() ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithGenericTypeParameterAndSingleConstraint() As Task
            Const code = "
Class C
    {|span:Sub $$Foo(Of T As Class)()
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo(Of T As Class)() ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithGenericTypeParameterAndMultipleConstraint() As Task
            Const code = "
Class C
    {|span:Sub $$Foo(Of T As {Class, New})()
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo(Of T As {Class, New})() ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestPrivateSub() As Task
            Const code = "
Class C
    {|span:Private Sub $$Foo()
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Private Sub Foo() ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithByRefParameter() As Task
            Const code = "
Class C
    {|span:Sub $$Foo(ByRef i As Integer)
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo(ByRef i As Integer) ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithByValParameter() As Task
            Const code = "
Class C
    {|span:Sub $$Foo(ByVal i As Integer)
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo(ByVal i As Integer) ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithOptionalParameter() As Task
            Const code = "
Class C
    {|span:Sub $$Foo(Optional i As Integer = 1)
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo(Optional i As Integer = 1) ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithHandlesClause() As Task
            Const code = "
Class C
    {|span:Sub $$Foo() Handles Bar.Baz
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo() Handles Bar.Baz ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithImplementsClause() As Task
            Const code = "
Class C
    {|span:Sub $$Foo() Implements Bar.Baz
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Sub Foo() Implements Bar.Baz ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithComments() As Task
            Const code = "
Class C
    {|span1:'My
    'Constructor|}
    {|span2:Sub $$Foo() Implements Bar.Baz
    End Sub|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Sub Foo() Implements Bar.Baz ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestMethodDeclarationWithLineBreaks() As Task
            Const code = "
Class C
    {|span:Public Function $$myFunction(myFunc1 As Func(Of System.String,
                                                           System.String,
                                                           System.String), myFunc2 As Func(Of System.String,
                                                           System.String,
                                                           System.String))
    End Function|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Public Function myFunction(myFunc1 As Func(Of System.String, System.String, System.String), myFunc2 As Func(Of System.String, System.String, System.String)) ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
