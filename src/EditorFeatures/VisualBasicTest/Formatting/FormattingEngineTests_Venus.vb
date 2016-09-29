' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting
    Public Class FormattingEngineTests_Venus
        Inherits FormattingTestBase

        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function SimpleOneLineNugget() As Threading.Tasks.Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)[|
Dim   x    As   Integer =    5
|]#End ExternalSource
  End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
       Dim x As Integer = 5
#End ExternalSource
End Sub
End Module</Code>

            Await AssertFormatWithBaseIndentAfterReplacingLfToCrLfAsync(code.Value, expected.Value, baseIndentation:=3)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(530138, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530138")>
        Public Async Function SimpleScriptBlock() As Threading.Tasks.Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
  End Sub
#ExternalSource ("Default.aspx", 3)[|
            Sub     Foo (   )   
            End Sub 
|]#End ExternalSource
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
  End Sub
#ExternalSource ("Default.aspx", 3)
    Sub Foo()
    End Sub
#End ExternalSource
End Module</Code>

            Await AssertFormatWithBaseIndentAfterReplacingLfToCrLfAsync(code.Value, expected.Value, baseIndentation:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function SimpleMultiLineNugget() As Threading.Tasks.Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)[|
If       True    Then
Console.WriteLine(True)
      Else
  Console.WriteLine(False)
End If
|]#End ExternalSource
  End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
           If True Then
               Console.WriteLine(True)
           Else
               Console.WriteLine(False)
           End If
#End ExternalSource
End Sub
End Module</Code>

            Await AssertFormatWithBaseIndentAfterReplacingLfToCrLfAsync(code.Value, expected.Value, baseIndentation:=7)
        End Function

        <WorkItem(576526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576526")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function SimpleQueryWithinNugget() As Threading.Tasks.Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)[|
   Dim numbers = {4, 5, 1}
      Dim even =
                    From  n   In numbers
              Where n Mod 2 =   0
                 Select n
|]#End ExternalSource
  End Sub
End Module</Code>

            ' Note - since From starts a line, we leave it where it is relative to the "Dim", and format the rest of the query to line up with it.
            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
           Dim numbers = {4, 5, 1}
           Dim even =
                         From n In numbers
                         Where n Mod 2 = 0
                         Select n
#End ExternalSource
End Sub
End Module</Code>

            Await AssertFormatWithBaseIndentAfterReplacingLfToCrLfAsync(code.Value, expected.Value, baseIndentation:=7)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function SingleLineFunctionLambdaInNugget() As Threading.Tasks.Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)[|
           Dim numbers = {4, 5, 6, 9}
        For  Each  i As Integer In numbers.Where(Function( x  As Integer) x > 5)
               Console.Write(i)
    Next
|]#End ExternalSource
  End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
           Dim numbers = {4, 5, 6, 9}
           For Each i As Integer In numbers.Where(Function(x As Integer) x > 5)
               Console.Write(i)
           Next
#End ExternalSource
End Sub
End Module</Code>

            Await AssertFormatWithBaseIndentAfterReplacingLfToCrLfAsync(code.Value, expected.Value, baseIndentation:=7)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function MultiLineFunctionLambdaInNugget() As Threading.Tasks.Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)[|
        Dim numbers = {4, 3, 8, 4, 6, 1, 7, 9, 2, 4, 8}
        For Each i As Integer In numbers.Where(
            Function ( x  As Integer)
                If x&lt;=3 Then
            Return True
                ElseIf x &gt;7 Then
            Return True
                   Else
            Return False
                End If
            End Function  )
            Console.Write(i)
        Next
|]#End ExternalSource
  End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
  Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
           Dim numbers = {4, 3, 8, 4, 6, 1, 7, 9, 2, 4, 8}
           For Each i As Integer In numbers.Where(
               Function(x As Integer)
                   If x &lt;= 3 Then
                       Return True
                   ElseIf x &gt; 7 Then
                       Return True
                   Else
                       Return False
                   End If
               End Function)
               Console.Write(i)
           Next
#End ExternalSource
End Sub
End Module</Code>

            Await AssertFormatWithBaseIndentAfterReplacingLfToCrLfAsync(code.Value, expected.Value, baseIndentation:=7)
        End Function

        ''' <summary>
        ''' Sets up the Base Indentation Formatting Rule with the given Base Indent
        ''' and exactly one Selected Span that the document contains.
        ''' Then asserts that the formatting on that span results in text that we'd expect.
        ''' </summary>
        ''' <remarks>The rule has to be set up for each set of spans, currently we test just one</remarks>
        Private Async Function AssertFormatWithBaseIndentAfterReplacingLfToCrLfAsync(content As String,
                                                                     expected As String,
                                                                     baseIndentation As Integer) As Threading.Tasks.Task

            ' do this since xml value put only vbLf
            content = content.Replace(vbLf, vbCrLf)
            expected = expected.Replace(vbLf, vbCrLf)

            Dim code As String = Nothing
            Dim textSpan As TextSpan
            MarkupTestFile.GetSpan(content, code, textSpan)

            Await AssertFormatSpanAsync(content, expected, baseIndentation, textSpan)
        End Function
    End Class
End Namespace