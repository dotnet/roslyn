' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging

    Public Class DataTipInfoGetterTests

        Private Sub TestNoDataTip(input As XElement)
            Dim parsedInput As String = Nothing
            Dim expectedPosition As Integer
            MarkupTestFile.GetPosition(input.NormalizedValue, parsedInput, expectedPosition)

            TestSpanGetter(input.NormalizedValue, expectedPosition, Sub(document, position)
                                                                        Dim result = DataTipInfoGetter.GetInfoAsync(document, position, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
                                                                        Assert.True(result.IsDefault)
                                                                    End Sub)
        End Sub

        Private Sub Test(input As XElement, Optional expectedText As String = Nothing)
            Dim parsedInput As String = Nothing
            Dim expectedPosition As Integer
            Dim textSpan As TextSpan
            MarkupTestFile.GetPositionAndSpan(input.NormalizedValue, parsedInput, expectedPosition, textSpan)

            TestSpanGetter(input.NormalizedValue, expectedPosition, Sub(document, position)
                                                                        Dim result = DataTipInfoGetter.GetInfoAsync(document, position, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
                                                                        Assert.False(result.IsDefault)
                                                                        Assert.Equal(textSpan, result.Span)
                                                                        If Not String.IsNullOrEmpty(expectedText) Then
                                                                            Assert.Equal(expectedText, result.Text)
                                                                        End If
                                                                    End Sub)
        End Sub

        Private Sub TestSpanGetter(parsedInput As String, position As Integer, continuation As Action(Of Document, Integer))
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(parsedInput)
                Dim debugInfo = New VisualBasicLanguageDebugInfoService()
                continuation(workspace.CurrentSolution.Projects.First.Documents.First, position)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestVisualBasicLanguageDebugInfoGetDataTipSpanAndText()
            Test(<text>Module [|$$M|] : End Module</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test1()
            Test(<text>
class C
  sub Foo()
    [|Sys$$tem|].Console.WriteLine(args)
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test2()
            Test(<text>
class C
  sub Foo()
    [|System$$.Console|].WriteLine(args)
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test3()
            Test(<text>
class C
  sub Foo()
    [|System.$$Console|].WriteLine(args)
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test4()
            Test(<text>
class C
  sub Foo()
    [|System.Con$$sole|].WriteLine(args)
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test5()
            Test(<text>
class C
  sub Foo()
    [|System.Console.Wri$$teLine(args)|]
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test6()
            TestNoDataTip(<text>
class C
  sub Foo()
    System.Console.WriteLine$$(args)
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test7()
            Test(<text>
class C
  sub Foo()
    System.Console.WriteLine($$[|args|])
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test8()
            TestNoDataTip(<text>
class C
  sub Foo()
    System.Console.WriteLine(args$$)
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub Test9()
            Test(<text>
class C
  sub Foo()
    dim [|$$i|] = 5
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestLiterals()
            TestNoDataTip(<text>
class C
  sub Foo()
    dim i = 5$$6
  end sub
end class</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestNonExpressions()
            TestNoDataTip(<text>
class C
  sub Foo()
    dim i = 5
  end sub$$
end class</text>)
        End Sub

        <WorkItem(538152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnComma()
            TestNoDataTip(<text>
class C
  sub Foo()
    Dim ia3 As Integer() = {1, 2, 3, 4 $$, 5}

  end sub
end class</text>)
        End Sub

        <WorkItem(546280)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnParameter()
            Test(<text>
Module Module1
    Sub Main()
        Foo(1, 2, 3)
    End Sub

    Private Sub Foo([|$$v1|] As Integer, v2 As Integer, v3 As Integer)
        Throw New NotImplementedException() ' breakpoint here
    End Sub
End Module
</text>)
        End Sub

        <WorkItem(942699)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnCatchVariable()
            Test(<text>
Module Module1
    Sub Main()
        Try

        Catch [|$$e|] As Exception

        End Try
    End Sub
End Module
</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnTypeDeclaration()
            Test(<text>
Module [|$$M|]
End Module
</text>)
            Test(<text>
Class [|$$M|]
End Class
</text>)
            Test(<text>
Structure [|$$M|]
End Structure
</text>)
            Test(<text>
Interface [|$$M|]
End Interface
</text>)
            Test(<text>
Enum [|$$M|]
    A
End Enum
</text>)
            Test(<text>
Delegate Sub [|$$M|] ()
</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnEnumMember()
            Test(<text>
Enum E
    [|$$M|]
End Enum
</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnTypeParameter()
            Test(<text>
Class C(Of [|$$T|])
End Class
</text>)
            Test(<text>
Class C
    Sub M(Of [|$$T|])()
    End Sub
End Class
</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnProperty()
            Test(<text>
Class C
    Property [|$$P|] As Integer
End Class
</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnEvent()
            Test(<text>
Class C
    Event [|$$E|] As System.Action
End Class
</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestOnMethod()
            Test(<text>
Class C
    Sub [|$$M|]()
    End Sub
End Class
</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Sub TestInQuery()
            Test(<text>
Class C
    Shared Sub Main(args As String())
        Dim o = From [|$$a|] In args Select a
    End Sub
End Class
</text>)

            Test(<text>
Class C
    Shared Sub Main(args As String())
        Dim o = From a In args Let [|$$b|] = "B" Select a + b
    End Sub
End Class
</text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(1077843)>
        Public Sub TestConditionalAccessExpression()
            Const sourceTemplate = "
Class A
    Public B As New B()

    Function M() As Object
        Return {0}
    End Function
End Class

Class B
    Public C As New C()
End Class

Class C
    Public D As New D()
End Class

Class D
End Class
"

            ' One level.
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|]") %></text>)

            ' Two levels.
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|].C") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B.$$C|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me.$$B|]?.C") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me.B?.$$C|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|]?.C") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B?.$$C|]") %></text>)

            ' Three levels.
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|].C.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B.$$C|].D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B.C.$$D|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me.$$B|]?.C.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me.B?.$$C|].D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me.B?.C.$$D|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me.$$B|].C?.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me.B.$$C|]?.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me.B.C?.$$D|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|]?.C.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B?.$$C|].D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B?.C.$$D|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|].C?.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B.$$C|]?.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B.C?.$$D|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me.$$B|]?.C?.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me.B?.$$C|]?.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me.B?.C?.$$D|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|]?.C?.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B?.$$C|]?.D") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.B?.C?.$$D|]") %></text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(1077843)>
        Public Sub TestConditionalAccessExpression_Dictionary()
            Const sourceTemplate = "
Class A
    Function M() As Object
        Return {0}
    End Function

    Default ReadOnly Property Item(s As String) As A
        Get
            Return Me
        End Get
    End Property
End Class
"

            ' One level
            Test(<text><%= String.Format(sourceTemplate, "[|Me?!$$B|]") %></text>)

            ' Two levels
            Test(<text><%= String.Format(sourceTemplate, "[|Me?!$$B|]!C") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?!B!$$C|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me!$$B|]?!C") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me!B?!$$C|]") %></text>)

            Test(<text><%= String.Format(sourceTemplate, "[|Me?!$$B|]?!C") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?!B?!$$C|]") %></text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(2602)>
        Public Sub TestParameterizedProperty()
            Const sourceTemplate = "
Class Class1
    Public row = New DataRow()

    Function F()
        Return {0}
    End Function

    Public ReadOnly Property Item(ByVal i As Integer) As String
        Get
            Return str
        End Get
    End Property

    Class DataRow
        Public ReadOnly Property Item(ByVal str As String) As String
            Get
                Return str
            End Get
        End Property
    End Class
End Class
"

            Test(<text><%= String.Format(sourceTemplate, "[|$$Item(42)|].Length") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|row.It$$em(""Test Row"")|].Length") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|row?.Ite$$m(""Test Row"")|].Length") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.row.$$Item(""Test Row"")|].Length") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me.row?.It$$em(""Test Row"")|].Length") %></text>)
            Test(<text><%= String.Format(sourceTemplate, "[|Me?.row?.It$$em(""Test Row"")|].Length") %></text>)
        End Sub

    End Class

End Namespace
