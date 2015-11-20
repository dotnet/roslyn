' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
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

        Private Async Function TestNoDataTipAsync(input As XElement) As Task
            Dim parsedInput As String = Nothing
            Dim expectedPosition As Integer
            MarkupTestFile.GetPosition(input.NormalizedValue, parsedInput, expectedPosition)

            Await TestSpanGetterAsync(input.NormalizedValue, expectedPosition, Sub(document, position)
                                                                                   Dim result = DataTipInfoGetter.GetInfoAsync(document, position, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
                                                                                   Assert.True(result.IsDefault)
                                                                               End Sub)
        End Function

        Private Async Function TestAsync(input As XElement, Optional expectedText As String = Nothing) As Task
            Dim parsedInput As String = Nothing
            Dim expectedPosition As Integer
            Dim textSpan As TextSpan
            MarkupTestFile.GetPositionAndSpan(input.NormalizedValue, parsedInput, expectedPosition, textSpan)

            Await TestSpanGetterAsync(input.NormalizedValue, expectedPosition,
                                      Async Function(document, position)
                                          Dim result = Await DataTipInfoGetter.GetInfoAsync(document, position, CancellationToken.None)
                                          Assert.False(result.IsDefault)
                                          Assert.Equal(textSpan, result.Span)
                                          If Not String.IsNullOrEmpty(expectedText) Then
                                              Assert.Equal(expectedText, result.Text)
                                          End If
                                      End Function)
        End Function

        Private Async Function TestSpanGetterAsync(parsedInput As String, position As Integer, continuation As Func(Of Document, Integer, Task)) As Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromLinesAsync(parsedInput)
                Dim debugInfo = New VisualBasicLanguageDebugInfoService()
                Await continuation(workspace.CurrentSolution.Projects.First.Documents.First, position)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestVisualBasicLanguageDebugInfoGetDataTipSpanAndText() As Task
            Await TestAsync(<text>Module [|$$M|] : End Module</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test1() As Task
            Await TestAsync(<text>
class C
  sub Foo()
    [|Sys$$tem|].Console.WriteLine(args)
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test2() As Task
            Await TestAsync(<text>
class C
  sub Foo()
    [|System$$.Console|].WriteLine(args)
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test3() As Task
            Await TestAsync(<text>
class C
  sub Foo()
    [|System.$$Console|].WriteLine(args)
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test4() As Task
            Await TestAsync(<text>
class C
  sub Foo()
    [|System.Con$$sole|].WriteLine(args)
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test5() As Task
            Await TestAsync(<text>
class C
  sub Foo()
    [|System.Console.Wri$$teLine(args)|]
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test6() As Task
            Await TestNoDataTipAsync(<text>
class C
  sub Foo()
    System.Console.WriteLine$$(args)
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test7() As Task
            Await TestAsync(<text>
class C
  sub Foo()
    System.Console.WriteLine($$[|args|])
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test8() As Task
            Await TestNoDataTipAsync(<text>
class C
  sub Foo()
    System.Console.WriteLine(args$$)
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function Test9() As Task
            Await TestAsync(<text>
class C
  sub Foo()
    dim [|$$i|] = 5
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestLiterals() As Task
            Await TestNoDataTipAsync(<text>
class C
  sub Foo()
    dim i = 5$$6
  end sub
end class</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestNonExpressions() As Task
            Await TestNoDataTipAsync(<text>
class C
  sub Foo()
    dim i = 5
  end sub$$
end class</text>)
        End Function

        <WorkItem(538152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnComma() As Task
            Await TestNoDataTipAsync(<text>
class C
  sub Foo()
    Dim ia3 As Integer() = {1, 2, 3, 4 $$, 5}

  end sub
end class</text>)
        End Function

        <WorkItem(546280)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnParameter() As Task
            Await TestAsync(<text>
Module Module1
    Sub Main()
        Foo(1, 2, 3)
    End Sub

    Private Sub Foo([|$$v1|] As Integer, v2 As Integer, v3 As Integer)
        Throw New NotImplementedException() ' breakpoint here
    End Sub
End Module
</text>)
        End Function

        <WorkItem(942699)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnCatchVariable() As Task
            Await TestAsync(<text>
Module Module1
    Sub Main()
        Try

        Catch [|$$e|] As Exception

        End Try
    End Sub
End Module
</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnTypeDeclaration() As Task
            Await TestAsync(<text>
Module [|$$M|]
End Module
</text>)
            Await TestAsync(<text>
Class [|$$M|]
End Class
</text>)
            Await TestAsync(<text>
Structure [|$$M|]
End Structure
</text>)
            Await TestAsync(<text>
Interface [|$$M|]
End Interface
</text>)
            Await TestAsync(<text>
Enum [|$$M|]
    A
End Enum
</text>)
            Await TestAsync(<text>
Delegate Sub [|$$M|] ()
</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnEnumMember() As Task
            Await TestAsync(<text>
Enum E
    [|$$M|]
End Enum
</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnTypeParameter() As Task
            Await TestAsync(<text>
Class C(Of [|$$T|])
End Class
</text>)
            Await TestAsync(<text>
Class C
    Sub M(Of [|$$T|])()
    End Sub
End Class
</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnProperty() As Task
            Await TestAsync(<text>
Class C
    Property [|$$P|] As Integer
End Class
</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnEvent() As Task
            Await TestAsync(<text>
Class C
    Event [|$$E|] As System.Action
End Class
</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestOnMethod() As Task
            Await TestAsync(<text>
Class C
    Sub [|$$M|]()
    End Sub
End Class
</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)>
        Public Async Function TestInQuery() As Task
            Await TestAsync(<text>
Class C
    Shared Sub Main(args As String())
        Dim o = From [|$$a|] In args Select a
    End Sub
End Class
</text>)

            Await TestAsync(<text>
Class C
    Shared Sub Main(args As String())
        Dim o = From a In args Let [|$$b|] = "B" Select a + b
    End Sub
End Class
</text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(1077843)>
        Public Async Function TestConditionalAccessExpression() As Task
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
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|]") %></text>)

            ' Two levels.
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|].C") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B.$$C|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.$$B|]?.C") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.B?.$$C|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|]?.C") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B?.$$C|]") %></text>)

            ' Three levels.
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|].C.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B.$$C|].D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B.C.$$D|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.$$B|]?.C.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.B?.$$C|].D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.B?.C.$$D|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.$$B|].C?.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.B.$$C|]?.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.B.C?.$$D|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|]?.C.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B?.$$C|].D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B?.C.$$D|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|].C?.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B.$$C|]?.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B.C?.$$D|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.$$B|]?.C?.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.B?.$$C|]?.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.B?.C?.$$D|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.$$B|]?.C?.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B?.$$C|]?.D") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.B?.C?.$$D|]") %></text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(1077843)>
        Public Async Function TestConditionalAccessExpression_Dictionary() As Task
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
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?!$$B|]") %></text>)

            ' Two levels
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?!$$B|]!C") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?!B!$$C|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me!$$B|]?!C") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me!B?!$$C|]") %></text>)

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?!$$B|]?!C") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?!B?!$$C|]") %></text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(2602)>
        Public Async Function TestParameterizedProperty() As Task
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

            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|$$Item(42)|].Length") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|row.It$$em(""Test Row"")|].Length") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|row?.Ite$$m(""Test Row"")|].Length") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.row.$$Item(""Test Row"")|].Length") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me.row?.It$$em(""Test Row"")|].Length") %></text>)
            Await TestAsync(<text><%= String.Format(sourceTemplate, "[|Me?.row?.It$$em(""Test Row"")|].Length") %></text>)
        End Function
    End Class
End Namespace