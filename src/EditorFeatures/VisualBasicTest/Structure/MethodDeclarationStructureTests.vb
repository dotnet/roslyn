' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class MethodDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of MethodStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New MethodDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSub() As Task
            Const code = "
Class C
    {|span:{|hintspan:Sub $$Goo()
    End Sub|}|} ' Goo
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "hintspan", "Sub Goo() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithGenericTypeParameter() As Task
            Const code = "
Class C
    {|span:Sub $$Goo(Of T)()
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub Goo(Of T)() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithGenericTypeParameterAndSingleConstraint() As Task
            Const code = "
Class C
    {|span:Sub $$Goo(Of T As Class)()
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub Goo(Of T As Class)() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithGenericTypeParameterAndMultipleConstraint() As Task
            Const code = "
Class C
    {|span:Sub $$Goo(Of T As {Class, New})()
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub Goo(Of T As {Class, New})() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestPrivateSub() As Task
            Const code = "
Class C
    {|span:Private Sub $$Goo()
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Private Sub Goo() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithByRefParameter() As Task
            Const code = "
Class C
    {|span:Sub $$Goo(ByRef i As Integer)
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub Goo(ByRef i As Integer) ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithByValParameter() As Task
            Const code = "
Class C
    {|span:Sub $$Goo(ByVal i As Integer)
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub Goo(ByVal i As Integer) ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithOptionalParameter() As Task
            Const code = "
Class C
    {|span:Sub $$Goo(Optional i As Integer = 1)
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub Goo(Optional i As Integer = 1) ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithHandlesClause() As Task
            Const code = "
Class C
    {|span:Sub $$Goo() Handles Bar.Baz
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub Goo() Handles Bar.Baz ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithImplementsClause() As Task
            Const code = "
Class C
    {|span:Sub $$Goo() Implements Bar.Baz
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub Goo() Implements Bar.Baz ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSubWithComments() As Task
            Const code = "
Class C
    {|span1:'My
    'Constructor|}
    {|span2:Sub $$Goo() Implements Bar.Baz
    End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Sub Goo() Implements Bar.Baz ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        <WorkItem(27462, "https://github.com/dotnet/roslyn/issues/27462")>
        Public Async Function TestSubWithAttribute() As Task
            Const code = "
Imports System.Runtime.CompilerServices

Public Module SomeModule
  {|span:<Extension>
  {|hintspan:Public Sub $$WillNotShowPreviewWhenFolded(ArgFoo As Object)
    'this will NOT be visible in the preview tooltip.
  End Sub|}|}
End Module
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "hintspan", "<Extension> Public Sub WillNotShowPreviewWhenFolded(ArgFoo As Object) ...", autoCollapse:=True))
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

            Await VerifyBlockSpansAsync(code,
                Region("span", "Public Function myFunction(myFunc1 As Func(Of System.String, System.String, System.String), myFunc2 As Func(Of System.String, System.String, System.String)) ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
